using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Cysharp.Threading.Tasks;
using System.Threading;       

public class RewardUIManager : MonoBehaviour
{
    [Header("UI ¿¬°á")]
    [SerializeField] GameObject bonusPanel;
    [SerializeField] TextMeshProUGUI bonusText;
    [SerializeField] Button adButton;
    [SerializeField] TextMeshProUGUI adButtonText;
    [SerializeField] CoinBurstEffect coinBurstEffect;
    [SerializeField] float effectDelay = 0.3f;
    private bool hasReceivedReward = false;

    private void Start()
    {
        hasReceivedReward = false;

        if (adButton != null)
        {
            adButton.interactable = true;
            adButtonText.color = Color.white;
        }

        if (bonusPanel != null) bonusPanel.SetActive(false);

        if (LevelPlayManager.Instance != null)
        {
            LevelPlayManager.Instance.OnRewardGiven += HandleRewardGiven;
        }
    }



    private void OnDestroy()
    {
        if (LevelPlayManager.Instance != null)
        {
            LevelPlayManager.Instance.OnRewardGiven -= HandleRewardGiven;
        }
    }

    public void OnClickShowAdButton()
    {
        if (hasReceivedReward) return;

        if (LevelPlayManager.Instance != null)
        {
            LevelPlayManager.Instance.ClickShowReward();
        }
    }

    private void HandleRewardGiven(int bonusAmount)
    {
        hasReceivedReward = true;

        if (adButton != null)
        {
            adButton.interactable = false;
            adButtonText.color = Color.darkGray;
        }

        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.GetBonusGold(bonusAmount);
        }

        ShowRewardEffectAsync(bonusAmount, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ShowRewardEffectAsync(int bonusAmount, CancellationToken ct)
    {
        if (bonusText != null) bonusText.text = $"+{bonusAmount}G";
        if (bonusPanel != null) bonusPanel.SetActive(true);

        bool isCanceled = await UniTask.Delay(System.TimeSpan.FromSeconds(effectDelay), ignoreTimeScale: true, cancellationToken: ct).SuppressCancellationThrow();

        if (isCanceled) return;

        if (bonusPanel != null)
        {
            PanelEffects panelEffects = bonusPanel.GetComponent<PanelEffects>();
            if (panelEffects != null) panelEffects.PlayOpenAnimation();
        }

        if (EffectSounds.Instance != null) EffectSounds.Instance.BonusGoldSound();
        if (coinBurstEffect != null) coinBurstEffect.PlayBurst();
    }

    public void InitAdButton()
    {
        hasReceivedReward = false;
        if (adButton != null)
        {
            adButton.interactable = true;
            adButtonText.color = Color.white;
        }
    }
}