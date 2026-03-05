using UnityEngine;
using System;
using Unity.Services.LevelPlay;

public class LevelPlayManager : MonoBehaviour
{
    public static LevelPlayManager Instance { get; private set; }

    private const string _androidAppKey = "252e9212d";
    private const string _iosAppKey = "";

    [Header("Ad Unit IDs")]
    [SerializeField] string _rewardedAdUnitId = "9fo29tz39ec6wws1"; //기존 보상형 ID
    [SerializeField] string _interstitialAdUnitId = "4adnhkngqd2g1df2"; //전면 광고 ID 

    private string _pendingChallengeId = "";

    [Header("Settings")]
    [SerializeField] string _PlacementName = "Main_Menu";
    [SerializeField] int baseGold = 5; // 광고 시청시 기본 지급되는 골드
    private const float _RewardCooldownSeconds = 3;

    // 전면 광고 빈도 설정 (4판마다)
    private const int _interstitialFrequency = 4;
    private int _gamePlayCount = 0; // 게임 횟수 카운트

    private bool isInitialized;

    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd; //전면 광고 객체

    private DateTime _LastAdCompletionTime;

    public event Action<int> OnRewardGiven;
    public event Action<bool> AdAvailable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        RegisterSDKEvents();

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            InitializeAds();
        }
      
    }

    private void RegisterSDKEvents()
    {
        LevelPlay.OnInitSuccess += SdkInitializationCompleted;
        LevelPlay.OnInitFailed += SdkInitializationFailed;
    }

    private void InitializeAds()
    {
        string appKey = GetPlatformAppKey();
        string userId = DatabaseManager.Instance != null ? DatabaseManager.Instance.playerUid : "unknown_user";

        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.Init(appKey, userId);
        LevelPlay.SetPauseGame(true);
    }

    private void SdkInitializationCompleted(LevelPlayConfiguration configuration)
    {
        if (isInitialized) return;
        isInitialized = true;

        //보상형 광고 생성 및 로드
        CreateRewardedAd();
        RegisterRewardedEvents();
        LoadRewardedAd();

        //전면 광고 생성 및 로드
        CreateInterstitialAd();
        RegisterInterstitialEvents();
        LoadInterstitialAd();
    }

    private void SdkInitializationFailed(LevelPlayInitError error)
    {
        Debug.LogError($"[AdManager] SDK 초기화 실패: {error.ErrorMessage}");
    }

    public void OpenTestSuite()
    {
        LevelPlay.LaunchTestSuite();
    }

    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(_rewardedAdUnitId);
    }

    private void RegisterRewardedEvents()
    {
        if (rewardedAd == null) return;
        rewardedAd.OnAdLoaded += HandleRewardedLoaded;
        rewardedAd.OnAdLoadFailed += HandleRewardedLoadFailed;
        rewardedAd.OnAdDisplayed += HandleRewardedDisplayed;
        rewardedAd.OnAdDisplayFailed += HandleRewardedFailedToDisplay;
        rewardedAd.OnAdRewarded += ProcessAdReward;
        rewardedAd.OnAdClosed += HandleRewardedClosed;
    }

    private void LoadRewardedAd()
    {
        if (rewardedAd != null) rewardedAd.LoadAd();
    }

    public void ClickShowReward(string challengeId = "")
    {
        if (CanShowRewardAd())
        {
            _pendingChallengeId = challengeId; // 어떤 챌린지인지 기록

            if (string.IsNullOrEmpty(_PlacementName))
            {
                rewardedAd.ShowAd();
            }
            else rewardedAd.ShowAd(_PlacementName);
        }
    }
    public bool CanShowRewardAd()
    {
        if (!isInitialized || rewardedAd == null) return false;
        bool isReady = rewardedAd.IsAdReady();
        bool isCooldownExpired = HasCooldownExpired();
        return isReady && isCooldownExpired;
    }

    private void HandleRewardedLoaded(LevelPlayAdInfo adInfo) { AdAvailable?.Invoke(true); }
    private void HandleRewardedLoadFailed(LevelPlayAdError error) { Invoke(nameof(LoadRewardedAd), 3f); } // 실패 시 3초 후 재시도
    private void HandleRewardedDisplayed(LevelPlayAdInfo adInfo) { Debug.Log("보상형 광고 표시됨"); }
    private void HandleRewardedFailedToDisplay(LevelPlayAdInfo info, LevelPlayAdError err) { LoadRewardedAd(); }
    private void HandleRewardedClosed(LevelPlayAdInfo adInfo)
    {
        _LastAdCompletionTime = DateTime.UtcNow;
        LoadRewardedAd(); 
    }
    private void ProcessAdReward(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        if (string.IsNullOrEmpty(_pendingChallengeId))
        {
            if (ScoreManager.Instance != null)
            {
                int totalBonus = baseGold + ScoreManager.Instance.currentGold;
                OnRewardGiven?.Invoke(totalBonus);
            }
        }
        else
        {
            if (ChallengeManager.Instance != null)
            {
                ChallengeManager.Instance.ReportProgress(ChallengeType.AccumulateAds, 1, _pendingChallengeId);
            }
            _pendingChallengeId = ""; 
        }

        // 전역 광고 카운트도 올리고 싶다면 별도로 호출 가능
        // ChallengeManager.Instance.ReportProgress(ChallengeType.AccumulateAds, 1); 
    }

    private void CreateInterstitialAd()
    {
        if (string.IsNullOrEmpty(_interstitialAdUnitId)) return;
        interstitialAd = new LevelPlayInterstitialAd(_interstitialAdUnitId);
    }

    private void RegisterInterstitialEvents()
    {
        if (interstitialAd == null) return;

        interstitialAd.OnAdLoaded += HandleInterstitialLoaded;
        interstitialAd.OnAdLoadFailed += HandleInterstitialLoadFailed;
        interstitialAd.OnAdDisplayed += HandleInterstitialDisplayed;
        interstitialAd.OnAdDisplayFailed += HandleInterstitialFailedToDisplay;
        interstitialAd.OnAdClosed += HandleInterstitialClosed;
    }

    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
    }

    public void NotifyGameEnded()
    {
        _gamePlayCount++;

        // 4판마다 (4, 8, 12...) 광고 시도
        if (_gamePlayCount % _interstitialFrequency == 0)
        {
            ShowInterstitialAd();
        }
    }

    private void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
            if (ChallengeManager.Instance != null)
                ChallengeManager.Instance.ReportProgress(ChallengeType.AccumulateAds, 1);
        }
        else
        {
            LoadInterstitialAd();
        }
    }

    private void HandleInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] 전면 광고 로드 성공");
    }

    private void HandleInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"[AdManager] 전면 광고 로드 실패: {error.ErrorMessage}");
        Invoke(nameof(LoadInterstitialAd), 5f); // 5초 뒤 재시도
    }

    private void HandleInterstitialDisplayed(LevelPlayAdInfo adInfo) { }

    private void HandleInterstitialFailedToDisplay(LevelPlayAdInfo info, LevelPlayAdError err)
    {
        LoadInterstitialAd();
    }

    private void HandleInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[AdManager] 전면 광고 닫힘 -> 다음 광고 미리 로드");
        LoadInterstitialAd(); //닫히면 바로 다음 광고를 로드해둬야 다음 4판 뒤에 볼 수 있음
    }


    private string GetPlatformAppKey()
    {
#if UNITY_ANDROID
        return _androidAppKey;
#elif UNITY_IOS
        return _iosAppKey;
#else
        return "unexpected_platform";
#endif
    }

    private bool HasCooldownExpired()
    {
        if (_LastAdCompletionTime == default) return true;
        TimeSpan timeSince = DateTime.UtcNow - _LastAdCompletionTime;
        return timeSince.TotalSeconds >= _RewardCooldownSeconds;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            LevelPlay.OnInitSuccess -= SdkInitializationCompleted;
            LevelPlay.OnInitFailed -= SdkInitializationFailed;

            if (rewardedAd != null)
            {
                rewardedAd.OnAdLoaded -= HandleRewardedLoaded;
                rewardedAd.OnAdLoadFailed -= HandleRewardedLoadFailed;
                rewardedAd.OnAdDisplayed -= HandleRewardedDisplayed;
                rewardedAd.OnAdDisplayFailed -= HandleRewardedFailedToDisplay;
                rewardedAd.OnAdRewarded -= ProcessAdReward;
                rewardedAd.OnAdClosed -= HandleRewardedClosed;
            }

            if (interstitialAd != null)
            {
                interstitialAd.OnAdLoaded -= HandleInterstitialLoaded;
                interstitialAd.OnAdLoadFailed -= HandleInterstitialLoadFailed;
                interstitialAd.OnAdDisplayed -= HandleInterstitialDisplayed;
                interstitialAd.OnAdDisplayFailed -= HandleInterstitialFailedToDisplay;
                interstitialAd.OnAdClosed -= HandleInterstitialClosed;
            }
        }
    }
}