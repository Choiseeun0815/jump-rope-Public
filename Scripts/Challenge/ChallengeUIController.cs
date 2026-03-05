using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ChallengeUIController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private ChallengeManager manager;

    [Header("Slide Settings (Animation)")]
    [SerializeField] private Button switchModeBtn;          // 모드 전환 버튼
    [SerializeField] private TextMeshProUGUI switchModeText;          // 모드 전환 텍스트
    [SerializeField] private RectTransform shopPanelRect;   // 상점 패널
    [SerializeField] private RectTransform challengePanelRect; // 도전과제 패널

    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideDistance = 1080f;   // 화면 너비 (예: 1080)
    [SerializeField] private Ease slideEase = Ease.InOutQuart;

    [SerializeField] private GameObject shopExclusiveUI;
    [SerializeField] private GameObject challengeTitle;

    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private ShopItemCellUI cellPrefab;

    [Header("Popup")]
    [SerializeField] private ChallengePopupUI popup;
    [SerializeField] private GameObject switchModeBadge;
    private List<GameObject> spawnedCells = new List<GameObject>();
    private bool isChallengeMode = false;
    private bool isAnimating = false;

    private void Start()
    {
        if (manager == null) manager = ChallengeManager.Instance;

        if (switchModeBtn) switchModeBtn.onClick.AddListener(OnSwitchModeClicked);

        if (manager != null)
        {
            manager.OnChanged += RefreshGrid;
        }

        if (shopPanelRect)
        {
            shopPanelRect.anchoredPosition = new Vector2(0, shopPanelRect.anchoredPosition.y);
            shopPanelRect.gameObject.SetActive(true);
        }

        if (challengePanelRect)
        {
            challengePanelRect.anchoredPosition = new Vector2(slideDistance, challengePanelRect.anchoredPosition.y);
            challengePanelRect.gameObject.SetActive(false);
        }

        RefreshGrid();
        UpdateBadgeVisibility();
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnChanged -= RefreshGrid;
    }

    private void OnSwitchModeClicked()
    {
        if (isAnimating) return;
        ToggleMode(!isChallengeMode);
    }

    private void ToggleMode(bool toChallenge)
    {
        isAnimating = true;
        isChallengeMode = toChallenge;

        if(isChallengeMode)
            switchModeText.text = "상점으로 이동";
        else
            switchModeText.text = "도전 과제로 이동";

        RectTransform outRect = toChallenge ? shopPanelRect : challengePanelRect;
        RectTransform inRect = toChallenge ? challengePanelRect : shopPanelRect;

        if (inRect != null)
        {
            inRect.anchoredPosition = new Vector2(slideDistance, inRect.anchoredPosition.y);
            inRect.gameObject.SetActive(true);
        }

        if (shopExclusiveUI)
        {
            shopExclusiveUI.SetActive(!toChallenge);
        }
        if (challengeTitle)
        {
            challengeTitle.SetActive(toChallenge);
        } 

        Sequence seq = DOTween.Sequence();

        if (outRect != null)
            seq.Join(outRect.DOAnchorPosX(-slideDistance, slideDuration).SetEase(slideEase));

        if (inRect != null)
            seq.Join(inRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase));

        seq.OnComplete(() =>
        {
            isAnimating = false;
            if (outRect != null) outRect.gameObject.SetActive(false);
        });
        UpdateBadgeVisibility();
    }

    public void RefreshGrid()
    {
        foreach (var cell in spawnedCells) Destroy(cell);
        spawnedCells.Clear();

        if (manager == null) return;

        var items = manager.GetAllChallenges();
        foreach (var item in items)
        {
            var cellObj = Instantiate(cellPrefab, gridContent);

            bool isUnlocked = manager.IsUnlocked(item);
            bool isClaimable = manager.GetCurrentProgress(item) >= item.targetValue;

            cellObj.SetupChallenge(item, isUnlocked, isClaimable, OnCellClicked);
            spawnedCells.Add(cellObj.gameObject);
        }
        UpdateBadgeVisibility();
    }

    private void OnCellClicked(ShopItemDefinition baseItem)
    {
        var challengeItem = baseItem as ChallengeItemDefinition;
        if (challengeItem == null) return;

        if (popup != null)
        {
            bool isUnlocked = manager.IsUnlocked(challengeItem);
            int progress = manager.GetCurrentProgress(challengeItem);
            popup.Open(challengeItem, isUnlocked, progress);
        }
    }
    private void UpdateBadgeVisibility()
    {
        if (switchModeBadge == null || manager == null) return;

        bool hasReward = manager.HasAnyClaimableReward();

        switchModeBadge.SetActive(!isChallengeMode && hasReward);
    }
}