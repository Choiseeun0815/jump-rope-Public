using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public JumpRope jumpRope;
    public PlayerController playerController;
    public ObstacleSpawner obstacleSpawner;
    public ThemeLoader themeLoader;

    [Header("CountDown")]
    public GameObject countdownUI;
    [SerializeField] TextMeshProUGUI countdownText;

    [Header("Game Difficulty")]
    [SerializeField] float initRotationSpeed = 250f;
    [SerializeField] float maxRotationSpeed = 500f;
    [SerializeField] int speedUpTerm = 20;
    [SerializeField] float speedUpValue = 25f;

    [Header("Button Setting")]
    [SerializeField] GameObject case1Button;
    [SerializeField] GameObject case2Button;

    private RewardUIManager rewardUIManager;

    public bool IsGameStarted { get; private set; } = false;
    public bool IsGameOver { get; private set; } = false;

    private float currentRotationSpeed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        rewardUIManager = GetComponent<RewardUIManager>();
        InitializeGameSequence(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid InitializeGameSequence(CancellationToken ct)
    {
        await PrepareGameDataAsync(ct);
        await InitializeComponentsAsync(ct);

        if (BGMSounds.Instance != null) BGMSounds.Instance.PlayBGM("GameScene");

        System.GC.Collect();

        await UniTask.Delay(300, cancellationToken: ct);

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.FadeIn().ToUniTask(cancellationToken: ct);
        }

        await StartCountdownAsync(ct);

        RealGameStart();
    }

    private async UniTask PrepareGameDataAsync(CancellationToken ct)
    {
        IsGameStarted = false;
        IsGameOver = false;

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            bool isCase1 = DatabaseManager.Instance.currentData.buttonCase1;
            SetButtonsLocation(isCase1);
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.SetInit();
        if (rewardUIManager != null) rewardUIManager.InitAdButton();

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.DeactivateAllObjects();
        }

        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }

    private async UniTask InitializeComponentsAsync(CancellationToken ct)
    {
        MapThemeData mapData = null;
        if (themeLoader != null)
        {
            mapData = await themeLoader.SetupCurrentThemeAsync(ct);
        }

        //await UniTask.Yield(PlayerLoopTiming.Update, ct);
        await UniTask.Delay(800, cancellationToken: ct);
        if (playerController != null)
        {
            playerController.gameObject.SetActive(true);
            playerController.ResetPosition();
        }

        if (jumpRope != null)
        {
            if (mapData != null)
            {
                jumpRope.UpdateRopeColor(mapData.themeRopeColor);
            }

            jumpRope.OnRopeHitGround.RemoveListener(OnRopeScore);
            jumpRope.OnRopeHitPlayer.RemoveListener(GameOver);

            jumpRope.OnRopeHitGround.AddListener(OnRopeScore);
            jumpRope.OnRopeHitPlayer.AddListener(GameOver);

            jumpRope.ResetPositionToAngle(180f);
        }

        if (obstacleSpawner != null)
        {
            obstacleSpawner.enabled = false;
            obstacleSpawner.currentTheme = mapData;
            obstacleSpawner.InitializeSpawner();
        }

        await UniTask.Delay(100, cancellationToken: ct);
    }

    private async UniTask StartCountdownAsync(CancellationToken ct)
    {

        if (EffectSounds.Instance != null) EffectSounds.Instance.CountdownSound();

        if (countdownUI != null) countdownUI.SetActive(true);
        if (countdownText != null)
        {
            countdownText.text = "";
            countdownText.transform.localScale = Vector3.one;
            countdownText.alpha = 1f;
        }

        int count = 3;
        while (count > 0)
        {
            if (ct.IsCancellationRequested) return;

            countdownText.text = count.ToString();

            countdownText.transform.DOKill();
            countdownText.transform.localScale = Vector3.one;
            countdownText.transform.DOScale(0f, 0.9f).SetEase(Ease.InCubic);

            await UniTask.Delay(1000, cancellationToken: ct);
            count--;
        }

        if (ct.IsCancellationRequested) return;

        countdownText.text = "Start!";
        countdownText.transform.DOKill();
        countdownText.transform.localScale = Vector3.one * 1.5f;
        countdownText.alpha = 1f;
        countdownText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        await UniTask.Delay(700, cancellationToken: ct);

        if (countdownUI != null) countdownUI.SetActive(false);
    }

    public void RealGameStart()
    {
        IsGameStarted = true;
        currentRotationSpeed = initRotationSpeed;

        if (jumpRope != null) jumpRope.InitRope(currentRotationSpeed);
        if (obstacleSpawner != null) obstacleSpawner.enabled = true;
    }


    void SetButtonsLocation(bool isCase1)
    {
        if (case1Button) case1Button.SetActive(isCase1);
        if (case2Button) case2Button.SetActive(!isCase1);
    }

    void OnRopeScore()
    {
        if (IsGameOver) return;

        ScoreManager.Instance.AddPerfectScore();

        if (ScoreManager.Instance.currentScore > 0 &&
            ScoreManager.Instance.currentScore % speedUpTerm == 0)
        {
            currentRotationSpeed += speedUpValue;
            currentRotationSpeed = Mathf.Min(currentRotationSpeed, maxRotationSpeed);

            jumpRope.SetSpeed(currentRotationSpeed);
        }
    }

    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsGameStarted = false;

        if (jumpRope != null) jumpRope.StopRope();
        if (obstacleSpawner != null) obstacleSpawner.enabled = false;
        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.ReportProgress(ChallengeType.PlayCount, 1);
        }
        ScoreManager.Instance.SetGameOverPanel();
    }


    public void OnRetryButtonClicked()
    {
        RetrySequenceAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RetrySequenceAsync(CancellationToken ct)
    {
        if (countdownUI != null) countdownUI.SetActive(false);

        if (SceneController.Instance != null)
        {
            await SceneController.Instance.FadeOut().ToUniTask(cancellationToken: ct);
        }

        InitializeGameSequence(ct).Forget();
    }

    public void OnRobbyButtonClicked()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.SceneTransitionToLobby();
    }
}