using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengePopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private PanelEffects panelEffects;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Preview")]
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private GameObject lockedUiOverlay;
    [SerializeField] private GameObject selectedPreviewPanel;

    [Header("Buttons")]
    [SerializeField] private Button actionButton;         // 선택 / 보상 받기 버튼
    [SerializeField] private TMP_Text actionBtnText;
    [SerializeField] private Button progressButton;       // 진행도 표시 / 광고 시청 버튼
    [SerializeField] private TMP_Text progressButtonText;

    [Header("Preview Stage")]
    [SerializeField] private ShopPreviewStage previewStage;

    [SerializeField] private ConfettiBurstEffect confettiBurstEffect;

    private ChallengeItemDefinition currentItem;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);

        if (progressButton != null)
            progressButton.onClick.AddListener(OnProgressButtonClicked);
    }

    private void OnEnable()
    {
        if (ChallengeManager.Instance != null)
            ChallengeManager.Instance.OnChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (ChallengeManager.Instance != null)
            ChallengeManager.Instance.OnChanged -= RefreshUI;
    }


    private void RefreshUI()
    {
        if (currentItem == null || !gameObject.activeInHierarchy) return;

        bool isUnlocked = ChallengeManager.Instance.IsUnlocked(currentItem);
        int progress = ChallengeManager.Instance.GetCurrentProgress(currentItem);

        Open(currentItem, isUnlocked, progress);
    }

    public void Open(ChallengeItemDefinition item, bool isUnlocked, int currentProgress)
    {
        this.currentItem = item;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        if (selectedPreviewPanel != null) selectedPreviewPanel.SetActive(false);

        if (titleText != null) titleText.text = item.displayName;
        if (messageText != null) messageText.text = item.description;

        if (previewStage != null)
        {
            previewStage.gameObject.SetActive(true);
            previewStage.Show(item, lockedVisual: !isUnlocked);
            if (previewRawImage != null) previewRawImage.texture = previewStage.Output;
        }

        bool isClaimable = !isUnlocked && (currentProgress >= item.targetValue);

        if (lockedUiOverlay != null)
        {
            lockedUiOverlay.SetActive(!isUnlocked && !isClaimable);
        }

        if (isUnlocked)
        {
            actionButton.gameObject.SetActive(true);
            actionBtnText.text = "선택";
            progressButton.gameObject.SetActive(false);
        }
        else if (isClaimable)
        {
            actionButton.gameObject.SetActive(true);
            actionBtnText.text = "보상 받기";
            progressButton.gameObject.SetActive(false);
        }
        else
        {
            actionButton.gameObject.SetActive(false);
            progressButton.gameObject.SetActive(true);

            if (item.type == ChallengeType.AccumulateAds)
            {
                progressButton.interactable = true;
                if (progressButtonText != null)
                    progressButtonText.text = $"광고 시청\n({currentProgress} / {item.targetValue})";
            }
            else
            {
                progressButton.interactable = false;
                if (progressButtonText != null)
                {
                    if (item.type == ChallengeType.UnlockMap) progressButtonText.text = "미획득";
                    else progressButtonText.text = $"{currentProgress} / {item.targetValue}";
                }
            }
        }
    }

    public void Close()
    {
        if (previewStage != null)
        {
            previewStage.Clear();
            previewStage.gameObject.SetActive(false);
        }

        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);

        if (selectedPreviewPanel != null) selectedPreviewPanel.SetActive(true);
    }


    private void OnActionButtonClicked()
    {
        if (currentItem == null) return;

        bool isUnlocked = ChallengeManager.Instance.IsUnlocked(currentItem);

        if (!isUnlocked)
        {
            ChallengeManager.Instance.ClaimReward(currentItem);

            if (confettiBurstEffect != null) confettiBurstEffect.PlayBurst();
            if (EffectSounds.Instance != null) EffectSounds.Instance.GetCharacterSound();

            RefreshUI();
        }
        else
        {
            if (UserIconManager.Instance != null)
                UserIconManager.Instance.UnlockIcon(currentItem.id);

            if (ShopManager.Instance != null)
                ShopManager.Instance.SelectItem(currentItem);

            if (panelEffects != null) panelEffects.Close();

            Close();
        }
    }

   
    private void OnProgressButtonClicked()
    {
        if (currentItem == null) return;

        if (currentItem.type == ChallengeType.AccumulateAds)
        {
            if (LevelPlayManager.Instance != null)
            {
                LevelPlayManager.Instance.ClickShowReward(currentItem.id);
            }
        }
    }
}