using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

public class DatabaseManager : MonoBehaviour
{
    static public DatabaseManager Instance;
    private FirebaseFirestore db;
    private readonly SemaphoreSlim economyGate = new SemaphoreSlim(1, 1);
    public bool IsPurchaseInProgress { get; private set; }
    public event Action PurchaseStateChanged;

    private void NotifyPurchaseStateChanged()
    {
        // A closed/reopened shop can subscribe to the persistent database manager.
        // Presentation errors must not strand the transaction or its completion callback.
        if (PurchaseStateChanged == null) return;
        foreach (Action listener in PurchaseStateChanged.GetInvocationList())
        {
            try { listener(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }

    private static bool WriteSucceeded(Task task, string operation)
    {
        if (task.IsCanceled)
        {
            Debug.LogWarning($"[Database] {operation} cancelled.");
            return false;
        }
        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError($"[Database] {operation} failed: {task.Exception}");
            return false;
        }
        return true;
    }

    private void ObserveWrite(Task task, string operation)
    {
        task.ContinueWithOnMainThread(done => { WriteSucceeded(done, operation); });
    }




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
            if (task.IsFaulted || task.IsCanceled)
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
        playerUid = userId;
        currentData = new UserGameData(name, score);
        DocumentReference docRef = db.Collection("users").Document(userId);
        ObserveWrite(docRef.SetAsync(currentData), "CreateNewData");
    }

    public void UpdateGameResult(int newScore, int earnedGold, int maxCombo)
    {
        if (currentData == null) return;

        if (earnedGold > 0) AddGoldAsync(earnedGold);

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
        if (amount <= 0) return;
        AddGoldAsync(amount);
    }

    public void UpdateEquippedIcon(string iconID)
    {
        if (currentData == null) return;
        currentData.equippedIconID = iconID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedIconID", iconID).ContinueWithOnMainThread(task =>
        {
            WriteSucceeded(task, "UpdateEquippedIcon");
        });
    }

    public void UpdateEquippedTheme(string themeID)
    {
        if (currentData == null) return;
        currentData.equippedThemeID = themeID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedThemeID", themeID).ContinueWithOnMainThread(task =>
        {
            WriteSucceeded(task, "UpdateEquippedTheme");
        });
    }

    public void UpdateEquippedChar(string charID)
    {
        if (currentData == null) return;
        currentData.equippedCharID = charID;

        DocumentReference docRef = db.Collection("users").Document(playerUid);
        docRef.UpdateAsync("equippedCharID", charID).ContinueWithOnMainThread(task =>
        {
            WriteSucceeded(task, "UpdateEquippedChar");
        });
    }

    public void UpdateUnlockedIcons()
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;
        List<string> values = currentData.unlockedIconIDs;
        if (values == null || values.Count == 0) return;
        DocumentReference docRef = db.Collection("users").Document(playerUid);
        ObserveWrite(docRef.UpdateAsync("unlockedIconIDs",
            FieldValue.ArrayUnion(values.ConvertAll<object>(value => value).ToArray())), "UpdateUnlockedIcons");
    }

    public void UpdateUnlockedThemes()
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;
        List<string> values = currentData.unlockedThemeIDs;
        if (values == null || values.Count == 0) return;
        DocumentReference docRef = db.Collection("users").Document(playerUid);
        ObserveWrite(docRef.UpdateAsync("unlockedThemeIDs",
            FieldValue.ArrayUnion(values.ConvertAll<object>(value => value).ToArray())), "UpdateUnlockedThemes");
    }

    public void UpdateUnlockedChars()
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;
        List<string> values = currentData.unlockedCharIDs;
        if (values == null || values.Count == 0) return;
        DocumentReference docRef = db.Collection("users").Document(playerUid);
        ObserveWrite(docRef.UpdateAsync("unlockedCharIDs",
            FieldValue.ArrayUnion(values.ConvertAll<object>(value => value).ToArray())), "UpdateUnlockedChars");
    }

    public async void SpendGoldAndUnlock(ShopCategory category, string itemId, int cost, Action<bool> onDone = null)
    {
        UserGameData data = currentData;
        string uid = playerUid;
        if (IsPurchaseInProgress || db == null || data == null || string.IsNullOrEmpty(uid) ||
            string.IsNullOrWhiteSpace(itemId) || cost < 0 || data.gold < cost ||
            (category != ShopCategory.Character && category != ShopCategory.Map))
        {
            onDone?.Invoke(false);
            return;
        }
        List<string> owned = category == ShopCategory.Map ? data.unlockedThemeIDs : data.unlockedCharIDs;
        if (owned != null && owned.Contains(itemId))
        {
            onDone?.Invoke(false);
            return;
        }

        IsPurchaseInProgress = true;
        NotifyPurchaseStateChanged();
        bool success = false;
        await economyGate.WaitAsync();
        try
        {
            if (currentData == data && playerUid == uid)
            {
                DocumentReference docRef = db.Collection("users").Document(uid);
                // Firestore may retry this callback. Only transaction-local copies may change here.
                UserGameData committed = await db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(docRef);
                    if (!snapshot.Exists) return null;
                    UserGameData serverData = snapshot.ConvertTo<UserGameData>();
                    List<string> items = category == ShopCategory.Map
                        ? serverData.unlockedThemeIDs : serverData.unlockedCharIDs;
                    if (items == null) items = new List<string>();
                    if (serverData.gold < cost || items.Contains(itemId)) return null;

                    serverData.gold -= cost;
                    items.Add(itemId);
                    var updates = new Dictionary<string, object> { { "gold", serverData.gold } };
                    if (category == ShopCategory.Map)
                    {
                        serverData.unlockedThemeIDs = items;
                        updates["unlockedThemeIDs"] = items;
                    }
                    else
                    {
                        serverData.unlockedCharIDs = items;
                        if (serverData.unlockedIconIDs == null) serverData.unlockedIconIDs = new List<string>();
                        if (!serverData.unlockedIconIDs.Contains(itemId)) serverData.unlockedIconIDs.Add(itemId);
                        updates["unlockedCharIDs"] = items;
                        updates["unlockedIconIDs"] = serverData.unlockedIconIDs;
                    }
                    transaction.Update(docRef, updates);
                    return serverData;
                });

                if (this != null && committed != null && currentData == data && playerUid == uid)
                {
                    data.gold = committed.gold;
                    // Preserve local challenge/default unlocks whose ArrayUnion may still be pending.
                    data.unlockedCharIDs = MergeUnlocks(committed.unlockedCharIDs, data.unlockedCharIDs);
                    data.unlockedThemeIDs = MergeUnlocks(committed.unlockedThemeIDs, data.unlockedThemeIDs);
                    data.unlockedIconIDs = MergeUnlocks(committed.unlockedIconIDs, data.unlockedIconIDs);
                    success = true;
                }
                else Debug.LogWarning("[Database] Purchase rejected or user session changed.");
            }
        }
        catch (OperationCanceledException) { Debug.LogWarning("[Database] Purchase cancelled."); }
        catch (Exception exception) { Debug.LogError($"[Database] Purchase failed: {exception}"); }
        finally
        {
            economyGate.Release();
            IsPurchaseInProgress = false;
        }
        NotifyPurchaseStateChanged();
        onDone?.Invoke(success);
    }

    private async void AddGoldAsync(int amount)
    {
        UserGameData data = currentData;
        string uid = playerUid;
        if (data == null || string.IsNullOrEmpty(uid) || db == null || amount <= 0) return;
        await economyGate.WaitAsync();
        try
        {
            if (currentData != data || playerUid != uid) return;
            DocumentReference docRef = db.Collection("users").Document(uid);
            int balance = await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(docRef);
                if (!snapshot.Exists) throw new InvalidOperationException("User document is missing.");
                int gold = snapshot.GetValue<int>("gold");
                int updated = checked(gold + amount);
                transaction.Update(docRef, new Dictionary<string, object> { { "gold", updated } });
                return updated;
            });
            if (this != null && currentData == data && playerUid == uid) data.gold = balance;
        }
        catch (OperationCanceledException) { Debug.LogWarning("[Database] Gold reward cancelled."); }
        catch (Exception exception) { Debug.LogError($"[Database] Gold reward failed: {exception}"); }
        finally { economyGate.Release(); }
    }

    private static List<string> MergeUnlocks(List<string> refreshed, List<string> local)
    {
        if (refreshed == null) refreshed = new List<string>();
        if (local != null)
            foreach (string id in local)
                if (!refreshed.Contains(id)) refreshed.Add(id);
        return refreshed;
    }

    private Dictionary<string, object> BuildProfileUpdates()
    {
        // Broad profile saves must not replace authoritative economy fields or
        // remove unlocks committed by another device.
        var updates = new Dictionary<string, object>
        {
            { "nickName", currentData.nickName }, { "highScore", currentData.highScore },
            { "maxCombo", currentData.maxCombo }, { "equippedIconID", currentData.equippedIconID },
            { "equippedThemeID", currentData.equippedThemeID }, { "equippedCharID", currentData.equippedCharID },
            { "buttonCase1", currentData.buttonCase1 }, { "frame_60", currentData.frame_60 },
            { "challengeProgress", currentData.challengeProgress }, { "totalPlayCount", currentData.totalPlayCount },
            { "totalAdCount", currentData.totalAdCount }, { "lastLoginDate", currentData.lastLoginDate },
            { "currentLoginStreak", currentData.currentLoginStreak }
        };
        AddUnlockUnion(updates, "unlockedCharIDs", currentData.unlockedCharIDs);
        AddUnlockUnion(updates, "unlockedThemeIDs", currentData.unlockedThemeIDs);
        AddUnlockUnion(updates, "unlockedIconIDs", currentData.unlockedIconIDs);
        return updates;
    }

    private static void AddUnlockUnion(Dictionary<string, object> updates, string field, List<string> values)
    {
        if (values != null && values.Count > 0)
            updates[field] = FieldValue.ArrayUnion(values.ConvertAll<object>(value => value).ToArray());
    }

    public void SaveToCloud()
    {
        if (currentData == null || string.IsNullOrEmpty(playerUid)) return;
        DocumentReference docRef = db.Collection("users").Document(playerUid);
        ObserveWrite(docRef.UpdateAsync(BuildProfileUpdates()), "SaveToCloud");
    }

    public void SaveUserData(string userId, int newScore)
    {
        if (currentData == null || string.IsNullOrEmpty(userId) || userId != playerUid) return;
        if (newScore > currentData.highScore) currentData.highScore = newScore;
        SaveToCloud();
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
            WriteSucceeded(task, "SaveChallengeProgress");
        });
    }
}
