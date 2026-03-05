using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPopupUI : MonoBehaviour
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
    [SerializeField] private Button closeButton;
    [SerializeField] private Button fixedUnlockButton;
    [SerializeField] private TMP_Text fixedUnlockButtonText;
    [SerializeField] private Button selectButton;

    [Header("Preview Stage")]
    [SerializeField] private ShopPreviewStage previewStage;

    [SerializeField] private ConfettiBurstEffect confettiBurstEffect;
    private ShopManager manager;
    private ShopItemDefinition current;
    private bool isOwned;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (fixedUnlockButton != null) fixedUnlockButton.onClick.AddListener(OnFixedUnlockClicked);
        if (selectButton != null) selectButton.onClick.AddListener(OnSelectClicked);

        Close();
        if (root != null) root.SetActive(false);
    }

    public void Open(ShopManager manager, ShopItemDefinition item, bool owned)
    {
        this.manager = manager;
        this.current = item;
        this.isOwned = owned;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        if (selectedPreviewPanel != null) selectedPreviewPanel.SetActive(false);

        if (titleText != null) titleText.text = item != null ? item.displayName : "";

        if (previewStage != null)
        {
            previewStage.gameObject.SetActive(true);
            previewStage.Show(item, lockedVisual: !owned);
            if (previewRawImage != null) previewRawImage.texture = previewStage.Output;
        }

        if (lockedUiOverlay != null) lockedUiOverlay.SetActive(!owned);

        if (owned)
        {
            if (messageText != null) messageText.text = "해금된 아이템입니다. 선택하면 적용됩니다.";
            if (fixedUnlockButton != null) fixedUnlockButton.gameObject.SetActive(false);
            if (selectButton != null) selectButton.gameObject.SetActive(true);
        }
        else
        {
            int price = item != null ? item.fixedUnlockPrice : 0;
            if (messageText != null) messageText.text = "잠긴 아이템입니다. 고정 해금으로 바로 해금할 수 있어요.";
            if (fixedUnlockButton != null) fixedUnlockButton.gameObject.SetActive(true);
            if (fixedUnlockButtonText != null) fixedUnlockButtonText.text = $"고정 해금 ({price}G)";
            if (selectButton != null) selectButton.gameObject.SetActive(false);
        }
    }

    public void Close()
    {
        if (previewStage != null)
        {
            previewStage.Clear();
            previewStage.gameObject.SetActive(false);
        }
        //if (root != null) root.SetActive(false);

        if (selectedPreviewPanel != null) selectedPreviewPanel.SetActive(true);

        current = null;
        manager = null;
        isOwned = false;
    }

    private void OnFixedUnlockClicked()
    {
        if (manager == null || current == null) return;

        bool ok = manager.TryFixedUnlock(current);
        if (!ok)
        {
            //if (messageText != null) messageText.text = "골드가 부족합니다.";
            return;
        }
        if (confettiBurstEffect != null) confettiBurstEffect.PlayBurst();
        if (EffectSounds.Instance != null) EffectSounds.Instance.GetCharacterSound();
        Open(manager, current, owned: true);
    }

    private void OnSelectClicked()
    {
        if (manager == null || current == null) return;
        manager.SelectItem(current);

        panelEffects.Close();
        Close();
    }
}