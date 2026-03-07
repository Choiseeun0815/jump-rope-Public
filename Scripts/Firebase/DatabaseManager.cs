using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System.Collections.Generic;
using System;

public class DatabaseManager : MonoBehaviour
{
    static public DatabaseManager Instance;
    private FirebaseFirestore db;

    public UserGameData currentData; 
    public string playerUid { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void LoadUserData(string userId, Action<bool> onResult)
    {
        playerUid = userId;

        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("데이터 로드 실패: " + task.Exception);
                onResult(false);
                return;
            }

            DocumentSnapshot snapShot = task.Result;

            if (snapShot.Exists)
            {
                currentData = snapShot.ConvertTo<UserGameData>();
                onResult(true);
            }
            else
            {
                onResult(false);
            }
        });
    }

    public void CreateNewData(string userId, string name, int score)
    {
        currentData = new UserGameData(name, score);
        SaveUserData(userId, score);
        SaveToCloud();
    }

    public void UpdateGameResult(int newScore, int earnedGold, int maxCombo)
    {
        if (currentData == null) return;

        currentData.gold += earnedGold;

        if (newScore > currentData.highScore)
        {
            currentData.highScore = newScore;
        }

        if (maxCombo > currentData.maxCombo)
        {
            currentData.maxCombo = maxCombo;
        }

        SaveToCloud();
    }

    public void GetBonusGold(int amount)
    {
        currentData.gold += amount;
        SaveToCloud();
    }

    public void UpdateEquippedIcon(string iconID)
    {
        if (currentData == null) return;
        currentData.equippedIconID = iconID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedIconID", iconID).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("아이콘 업데이트 성공, 변경된 아이콘 ID: " + iconID);
            else Debug.LogError("아이콘 업데이트 실패: " + task.Exception);
        });
    }

    public void UpdateEquippedTheme(string themeID)
    {
        if (currentData == null) return;
        currentData.equippedThemeID = themeID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedThemeID", themeID).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("테마 설정 완료: " + themeID);
            else Debug.LogError("테마 설정 실패: " + task.Exception);
        });
    }

    public void UpdateEquippedChar(string charID)
    {
        if (currentData == null) return;
        currentData.equippedCharID = charID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedCharID", charID).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("캐릭터 장착 정보 업데이트 완료: " + charID);
            else Debug.LogError("캐릭터 장착 정보 업데이트 실패: " + task.Exception);
        });
    }

    public void UpdateUnlockedIcons()
    {
        if (currentData == null) return;
        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("unlockedIconIDs", currentData.unlockedIconIDs).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("업데이트 완료: 잠금 해제 아이콘 목록");
        });
    }

    public void UpdateUnlockedThemes()
    {
        if (currentData == null) return;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("unlockedThemeIDs", currentData.unlockedThemeIDs).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("테마 잠금 해제 목록 업데이트 완료");
            else Debug.LogError("테마 잠금 해제 목록 업데이트 실패: " + task.Exception);
        });
    }

    public void UpdateUnlockedChars()
    {
        if (currentData == null) return;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("unlockedCharIDs", currentData.unlockedCharIDs).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("캐릭터 잠금 해제 목록 업데이트 완료");
            else Debug.LogError("캐릭터 잠금 해제 목록 업데이트 실패: " + task.Exception);
        });
    }

    public void SpendGoldAndUnlock(ShopCategory category, string itemId, int cost, Action<bool> onDone = null)
    {
        if (currentData == null)
        {
            onDone?.Invoke(false);
            return;
        }

        if (cost > 0 && currentData.gold < cost)
        {
            onDone?.Invoke(false);
            return;
        }

        if (cost > 0) currentData.gold -= cost;

        if (category == ShopCategory.Map)
        {
            if (currentData.unlockedThemeIDs == null) currentData.unlockedThemeIDs = new List<string>();
            if (!currentData.unlockedThemeIDs.Contains(itemId))
                currentData.unlockedThemeIDs.Add(itemId);
        }
        else
        {
            if (currentData.unlockedCharIDs == null) currentData.unlockedCharIDs = new List<string>();
            if (!currentData.unlockedCharIDs.Contains(itemId))
                currentData.unlockedCharIDs.Add(itemId);
        }

        DocumentReference docRef = db.Collection("users").Document(playerUid);

        var updates = new Dictionary<string, object>
        {
            { "gold", currentData.gold },
        };

        if (category == ShopCategory.Map) updates["unlockedThemeIDs"] = currentData.unlockedThemeIDs;
        else updates["unlockedCharIDs"] = currentData.unlockedCharIDs;

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"구매 반영 완료: {itemId} / cost={cost}");
                onDone?.Invoke(true);
            }
            else
            {
                Debug.LogError("구매 반영 실패: " + task.Exception);
                onDone?.Invoke(false);
            }
        });
    }

    public void SaveToCloud()
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.SetAsync(currentData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("클라우드 저장 성공");
            }
            else
            {
                Debug.LogError("클라우드 저장 실패 - " + task.Exception);
            }
        });
    }

    public void SaveUserData(string userId, int newScore)
    {
        if (currentData == null) currentData = new UserGameData();

        if (newScore > currentData.highScore) currentData.highScore = newScore;

        DocumentReference docRef = db.Collection("users").Document(userId);
        docRef.SetAsync(currentData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("사용자 데이터 저장 완료");
            }
        });
    }

    public void DeleteUserData(string userId, Action<bool> onComplete) // 유저 데이터 삭제
    {
        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.DeleteAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("데이터 삭제 실패: " + task.Exception);
                onComplete(false);
            }
            else
            {
                Debug.Log("데이터 삭제 완료!");
                currentData = null;
                onComplete(true);
            }
        });
    }

    public void CheckNicknameDuplication(string nickName, Action<bool> onResult)
    {
        Query query = db.Collection("users").WhereEqualTo("nickName", nickName);
        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("중복 확인 중 오류 발생: " + task.Exception);
                onResult(true);
            }
            else
            {
                if (task.Result.Count > 0)
                {
                    onResult(true);
                }
                else
                {
                    onResult(false);
                }
            }
        });
    }

    public void SaveChallengeProgress(string challengeKey, int newValue)
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;

        if (currentData.challengeProgress == null)
            currentData.challengeProgress = new Dictionary<string, int>();

        currentData.challengeProgress[challengeKey] = newValue;

        DocumentReference docRef = db.Collection("users").Document(playerUid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "challengeProgress." + challengeKey, newValue }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError("도전 과제 저장 실패: " + task.Exception);
            }
        });
    }
}
