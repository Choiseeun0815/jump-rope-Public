using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class ScoreManager : MonoBehaviour
{
    static public ScoreManager Instance;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI jumpTimingText;
    [SerializeField] TextMeshProUGUI perfectComboText;

    [Header("Game Over UI")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject gameOverBG;
    [SerializeField] TextMeshProUGUI bestScore_gameOver;
    [SerializeField] TextMeshProUGUI currentScore_gameOver;
    [SerializeField] TextMeshProUGUI currentGold_gameOver;
    [SerializeField] TextMeshProUGUI bonusText;

    [SerializeField] float shakeAngle = 0f;

    [SerializeField] GhostTrail ghostTrail;
    private const int FEVER_COMBO_COUNT = 10;
    public int currentScore { get; private set; } = 0;
    public int currentGold { get; private set; } = 0;
    public int perfectCombo { get; private set; } = 0;
    private int bonusScore = 0;
    private int maxCombo = 0;

    private void Awake()
    {
        Instance = this;
        SetInit();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        //if (goldText != null) goldText.text = $"Gold: {currentGold}G";
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        if (scoreText != null) scoreText.text = $"{currentScore}점";
    }

    public void AddPerfectScore()
    {
        bonusScore = CalculatePerpectBonus(perfectCombo + 1);

        AddScore(1 + bonusScore);
    }

    private int CalculatePerpectBonus(int comboCount)
    {

        if (comboCount >= 15) return 2;
        else if (comboCount >= 5) return 1;
        return 0;
    }

    public void SetJudgeDisplay(bool isSuccess, string text, Color color)
    {
        StopAllCoroutines();

        if (isSuccess)
        {
            perfectCombo++;
            if (maxCombo < perfectCombo)
            {
                maxCombo = perfectCombo;
            }
            if (perfectCombo >= FEVER_COMBO_COUNT)
            {
                if (ghostTrail != null) ghostTrail.StartFeverEffect();
            }

            if (perfectComboText != null)
            {
                perfectComboText.text = $"Combo: {perfectCombo}";
                if (EffectSounds.Instance != null) EffectSounds.Instance.PerfectSound();
            }
        }
        else
        {
            perfectCombo = 0;
            bonusScore = 0;
            if (ghostTrail != null) ghostTrail.StopFeverEffect();
            if (perfectComboText != null) perfectComboText.text = "";
        }

        if (jumpTimingText != null)
        {
            jumpTimingText.text = text;
            jumpTimingText.color = color;
            PlayPopEffect(jumpTimingText, 0.5f, shakeAngle);
        }

        if (bonusText != null)
        {
            if (bonusScore > 0)
            {
                bonusText.text = $"+{bonusScore} Bonus!";
                PlayPopEffect(bonusText, 0.5f, 0f);
            }
            else
            {
                bonusText.text = "";
            }
        }

        StartCoroutine(HideTexts());
    }

    private void PlayPopEffect(TextMeshProUGUI target, float power, float angle)
    {
        if (target == null) return;

        target.transform.DOKill();
        target.transform.localScale = Vector3.one;

        float randomZ = Random.Range(-angle, angle);
        target.transform.localRotation = Quaternion.Euler(0, 0, randomZ);

        target.transform.DOPunchScale(Vector3.one * power, 0.2f, 10, 1).SetUpdate(true);
    }

    IEnumerator HideTexts()
    {
        yield return new WaitForSeconds(1f);

        if (jumpTimingText != null) jumpTimingText.text = "";
        if (bonusText != null) bonusText.text = "";

        if (perfectComboText != null) perfectComboText.text = "";
    }

    public void SetInit()
    {
        currentScore = 0;
        currentGold = 0;
        perfectCombo = 0;
        maxCombo = 0;
        bonusScore = 0;
        if (ghostTrail != null) ghostTrail.StopFeverEffect();
        if (scoreText != null) scoreText.text = $"{currentScore}점";
        if (jumpTimingText != null) jumpTimingText.text = "";
        if (perfectComboText != null) perfectComboText.text = "";
        if (bonusText != null) bonusText.text = "";
    }

    public void SetGameOverPanel()
    {
        if (ghostTrail != null) ghostTrail.StopFeverEffect();
        int earnedGold = currentScore / 5;
        AddGold(earnedGold);

        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.UpdateGameResult(currentScore, currentGold, maxCombo);
            bestScore_gameOver.text = DatabaseManager.Instance.currentData.highScore + "점";
            currentGold_gameOver.text = currentGold + "G";
        }
        else
        {
            Debug.Log("DB 매니저 없음");
        }

        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.ReportProgress(ChallengeType.HighScore, currentScore);
            ChallengeManager.Instance.ReportProgress(ChallengeType.MaxCombo, maxCombo);

            string currentEquippedMap = DatabaseManager.Instance.currentData.equippedThemeID;
            ChallengeManager.Instance.ReportProgress(ChallengeType.SpecificMapHighScore, currentScore, currentEquippedMap);
        }

        currentScore_gameOver.text = currentScore + "점";
        gameOverBG.SetActive(true);
        gameOverPanel.SetActive(true);

        StartCoroutine(ShowAdWithDelay());
    }

    IEnumerator ShowAdWithDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (LevelPlayManager.Instance != null)
        {
            LevelPlayManager.Instance.NotifyGameEnded();
        }
    }
}