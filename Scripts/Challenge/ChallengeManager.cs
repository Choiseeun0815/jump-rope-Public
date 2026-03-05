using System;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeManager : MonoBehaviour
{
    public static ChallengeManager Instance;

    [Header("Catalog")]
    [Tooltip("도전 과제 카탈로그 데이터")]
    [SerializeField] private ChallengeCatalog challengeCatalog;

    public event Action OnChanged;

    private Dictionary<string, ChallengeItemDefinition> challengeDict;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        BuildDictionary();
    }

    private void Start()
    {
        CheckLoginStreak();
    }

    private void BuildDictionary()
    {
        challengeDict = new Dictionary<string, ChallengeItemDefinition>();

        if (challengeCatalog != null && challengeCatalog.items != null)
        {
            foreach (var it in challengeCatalog.items)
            {
                if (it != null && !string.IsNullOrEmpty(it.id))
                {
                    challengeDict[it.id] = it;
                }
            }
        }
    }

    public void ReportProgress(ChallengeType type, int value, string targetID="")
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;

        var data = DatabaseManager.Instance.currentData;
        bool isUpdated = false;

        switch (type)
        {
            case ChallengeType.PlayCount:
                data.totalPlayCount += value;
                isUpdated = true;
                break;
            case ChallengeType.AccumulateAds:
                if (string.IsNullOrEmpty(targetID))
                {
                    data.totalAdCount += value;
                }
                else
                {
                    string adKey = "AdCount_" + targetID;
                    if (!data.challengeProgress.ContainsKey(adKey)) data.challengeProgress[adKey] = 0;
                    data.challengeProgress[adKey] += value;
                }
                isUpdated = true;
                break;
            case ChallengeType.HighScore:
                if (value > data.highScore) { data.highScore = value; isUpdated = true; }
                break;
            case ChallengeType.MaxCombo:
                if (value > data.maxCombo) { data.maxCombo = value; isUpdated = true; }
                break;
            case ChallengeType.SpecificMapHighScore:
                if (string.IsNullOrEmpty(targetID)) return;
                string key = "MapHighScore_" + targetID;
                int currentMapScore = 0;
                if(data.challengeProgress != null && data.challengeProgress.ContainsKey(key))
                    currentMapScore = data.challengeProgress[key];
                if(value > currentMapScore)
                {
                    DatabaseManager.Instance.SaveChallengeProgress(key, value);
                    isUpdated = true;
                }
                break;
        }

        if (isUpdated)
        {
            DatabaseManager.Instance.SaveToCloud();
            OnChanged?.Invoke();
        }
    }

    private void CheckLoginStreak()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;
        var data = DatabaseManager.Instance.currentData;
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (data.lastLoginDate == today) return;

        int newStreak = 1;
        if (DateTime.TryParse(data.lastLoginDate, out DateTime lastDate))
        {
            if ((DateTime.Now.Date - lastDate.Date).Days == 1)
                newStreak = data.currentLoginStreak + 1;
        }

        data.lastLoginDate = today;
        data.currentLoginStreak = newStreak; // 전용 변수만 갱신

        DatabaseManager.Instance.SaveToCloud();
        OnChanged?.Invoke();
    }

    public bool IsUnlocked(ChallengeItemDefinition item)
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return false;
        var data = DatabaseManager.Instance.currentData;

        return data.unlockedCharIDs != null && data.unlockedCharIDs.Contains(item.id);
    }

    public int GetCurrentProgress(ChallengeItemDefinition item)
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return 0;
        var data = DatabaseManager.Instance.currentData;

        switch (item.type)
        {
            case ChallengeType.HighScore: return data.highScore;
            case ChallengeType.MaxCombo: return data.maxCombo;
            case ChallengeType.ConsecutiveLogin: return data.currentLoginStreak;
            case ChallengeType.PlayCount: return data.totalPlayCount;
            case ChallengeType.AccumulateAds:
                string adKey = "AdCount_" + item.id;
                if (data.challengeProgress != null && data.challengeProgress.ContainsKey(adKey))
                    return data.challengeProgress[adKey];
                return 0;
            case ChallengeType.UnlockMap:
                return data.unlockedThemeIDs != null && data.unlockedThemeIDs.Contains(item.targetStringID) ? 1 : 0;
            case ChallengeType.SpecificMapHighScore:
                string highKey = "MapHighScore_" + item.targetStringID;
                if(data.challengeProgress != null && data.challengeProgress.ContainsKey(highKey))
                    return data.challengeProgress[highKey];
                return 0;
            default: return 0;
        }
    }

    public List<ChallengeItemDefinition> GetAllChallenges()
    {
        if (challengeCatalog == null || challengeCatalog.items == null) return new List<ChallengeItemDefinition>();
        return challengeCatalog.items;
    }

    public ChallengeItemDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (challengeDict != null && challengeDict.TryGetValue(id, out var c)) return c;
        return challengeCatalog != null ? challengeCatalog.GetById(id) : null;
    }

    public void ClaimReward(ChallengeItemDefinition challenge)
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;
        var data = DatabaseManager.Instance.currentData;

        if (!data.unlockedCharIDs.Contains(challenge.id))
        {
            data.unlockedCharIDs.Add(challenge.id);
        }

        DatabaseManager.Instance.SaveToCloud();

        if (UserIconManager.Instance != null)
        {
            UserIconManager.Instance.UnlockIcon(challenge.id);
        }

        Debug.Log($"[Challenge] 보상 수령 완료! '{challenge.displayName}' 캐릭터 및 아이콘 해금 성공.");

        OnChanged?.Invoke();
    }
    public bool HasAnyClaimableReward()
    {
        if (challengeCatalog == null || challengeCatalog.items == null) return false;

        foreach (var item in challengeCatalog.items)
        {
            if (item == null) continue;

            bool isUnlocked = IsUnlocked(item);
            int currentProgress = GetCurrentProgress(item);

            if (!isUnlocked && currentProgress >= item.targetValue)
            {
                return true; // 받을 보상이 있음
            }
        }

        return false; // 받을 보상이 없음
    }
}