using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 아이템 상세 팝업 UI를 담당하는 컴포넌트
// - 아이템 이름/설명 표시
// - 미리보기 출력
// - 고정 해금 / 선택(장착) 버튼 처리
public class ShopPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;                 // 팝업 전체 루트 오브젝트
    [SerializeField] private PanelEffects panelEffects;       // 팝업 열기/닫기 연출 제어

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;              // 아이템 이름 텍스트
    [SerializeField] private TMP_Text messageText;            // 상태/설명 텍스트

    [Header("Preview")]
    [SerializeField] private RawImage previewRawImage;        // RenderTexture 미리보기 출력용 이미지
    [SerializeField] private GameObject lockedUiOverlay;      // 잠금 상태일 때 덮어씌울 UI

    [SerializeField] private GameObject selectedPreviewPanel; // 기본 선택 프리뷰 패널

    [Header("Buttons")]
    [SerializeField] private Button closeButton;              // 팝업 닫기 버튼
    [SerializeField] private Button fixedUnlockButton;        // 고정 해금 버튼
    [SerializeField] private TMP_Text fixedUnlockButtonText;  // 고정 해금 버튼 가격 텍스트
    [SerializeField] private Button selectButton;             // 선택(장착) 버튼

    [Header("Preview Stage")]
    [SerializeField] private ShopPreviewStage previewStage;   // 3D 미리보기 전용 스테이지

    [SerializeField] private ConfettiBurstEffect confettiBurstEffect; // 해금 성공 연출
    private ShopManager manager;                              // 현재 상점 매니저 참조
    private ShopItemDefinition current;                       // 현재 팝업에 표시 중인 아이템
    private bool isOwned;                                     // 현재 아이템 보유 여부

    private void Awake()
    {
        // 버튼 이벤트 연결
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (fixedUnlockButton != null) fixedUnlockButton.onClick.AddListener(OnFixedUnlockClicked);
        if (selectButton != null) selectButton.onClick.AddListener(OnSelectClicked);

        // 시작 시 팝업 비활성 상태로 초기화
        Close();
        if (root != null) root.SetActive(false);
    }

    // 팝업 열기
    // - 아이템 정보 표시
    // - 미리보기 출력
    // - 보유 여부에 따라 버튼/메시지 갱신
    public void Open(ShopManager manager, ShopItemDefinition item, bool owned)
    {
        this.manager = manager;
        this.current = item;
        this.isOwned = owned;

        // 루트 활성화
        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        // 기본 선택 프리뷰는 숨김
        if (selectedPreviewPanel != null) selectedPreviewPanel.SetActive(false);

        // 아이템 이름 표시
        if (titleText != null)
            titleText.text = item != null ? item.displayName : "";

        // 3D 미리보기 출력
        if (previewStage != null)
        {
            previewStage.gameObject.SetActive(true);
            previewStage.Show(item, lockedVisual: !owned);

            if (previewRawImage != null)
                previewRawImage.texture = previewStage.Output;
        }

        // 잠금 상태 오버레이 표시
        if (lockedUiOverlay != null)
            lockedUiOverlay.SetActive(!owned);

        if (owned)
        {
            // 이미 보유 중이면 선택(장착) 가능 상태
            if (messageText != null)
                messageText.text = "해금된 아이템입니다. 선택하면 적용됩니다.";

            if (fixedUnlockButton != null)
                fixedUnlockButton.gameObject.SetActive(false);

            if (selectButton != null)
                selectButton.gameObject.SetActive(true);
        }
        else
        {
            // 미보유 상태면 고정 해금 버튼 노출
            int price = item != null ? item.fixedUnlockPrice : 0;

            if (messageText != null)
                messageText.text = "잠긴 아이템입니다. 고정 해금으로 바로 해금할 수 있어요.";

            if (fixedUnlockButton != null)
                fixedUnlockButton.gameObject.SetActive(true);

            if (fixedUnlockButtonText != null)
                fixedUnlockButtonText.text = $"고정 해금 ({price}G)";

            if (selectButton != null)
                selectButton.gameObject.SetActive(false);
        }
    }

    // 팝업 닫기 및 내부 상태 초기화
    public void Close()
    {
        // 미리보기 스테이지 정리
        if (previewStage != null)
        {
            previewStage.Clear();
            previewStage.gameObject.SetActive(false);
        }

        // 기본 선택 프리뷰 다시 표시
        if (selectedPreviewPanel != null)
            selectedPreviewPanel.SetActive(true);

        // 현재 선택 상태 초기화
        current = null;
        manager = null;
        isOwned = false;
    }

    // 고정 해금 버튼 클릭 시 처리
    private void OnFixedUnlockClicked()
    {
        if (manager == null || current == null)
            return;

        bool ok = manager.TryFixedUnlock(current);
        if (!ok)
        {
            // 골드 부족 등으로 실패한 경우 여기서 종료
            return;
        }

        // 해금 성공 연출 및 사운드 재생
        if (confettiBurstEffect != null)
            confettiBurstEffect.PlayBurst();

        if (EffectSounds.Instance != null)
            EffectSounds.Instance.GetCharacterSound();

        // 해금 직후 상태를 보유 상태로 다시 열어 UI 갱신
        Open(manager, current, owned: true);
    }

    // 선택(장착) 버튼 클릭 시 처리
    private void OnSelectClicked()
    {
        if (manager == null || current == null)
            return;

        // 현재 아이템 장착
        manager.SelectItem(current);

        // 패널 닫기 연출 후 팝업 종료
        panelEffects.Close();
        Close();
    }
}