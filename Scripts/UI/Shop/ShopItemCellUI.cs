using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점/도전과제 목록에서 개별 아이템 셀 UI를 담당하는 컴포넌트
// - 아이콘, 이름, 잠금 상태 표시
// - 필요 시 알림 아이콘 표시
// - 클릭 시 선택 콜백 전달
public class ShopItemCellUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button button;              // 셀 전체 클릭 버튼
    [SerializeField] private Image iconImage;            // 아이템 썸네일 이미지
    [SerializeField] private Image lockIcon;             // 잠금 상태 표시용 아이콘
    [SerializeField] private TMP_Text nameText;          // 아이템 이름 텍스트

    [SerializeField] private GameObject notificationIcon; // 도전과제 보상 수령 가능 알림 아이콘

    private ShopItemDefinition item;                     // 현재 셀에 바인딩된 아이템 데이터
    private Action<ShopItemDefinition> onClick;          // 클릭 시 외부로 전달할 콜백

    // 일반 상점용 셀 초기화
    // - 보유 여부(owned)에 따라 잠금 상태 표시
    public void Setup(ShopItemDefinition item, bool owned, Action<ShopItemDefinition> onClick)
    {
        this.item = item;
        this.onClick = onClick;

        UpdateVisuals(item, owned);

        // 일반 상점에서는 알림 아이콘 사용 안 함
        if (notificationIcon != null) notificationIcon.SetActive(false);

        BindClickEvent();
    }

    // 도전과제용 셀 초기화
    // - 해금 여부(isUnlocked), 수령 가능 여부(isClaimable)에 따라 알림 아이콘 표시
    public void SetupChallenge(ShopItemDefinition item, bool isUnlocked, bool isClaimable, Action<ShopItemDefinition> onClick)
    {
        this.item = item;
        this.onClick = onClick;

        UpdateVisuals(item, isUnlocked);

        if (notificationIcon != null)
        {
            // 아직 해금되지 않았지만 수령 가능한 상태라면 알림 표시
            bool showNotify = !isUnlocked && isClaimable;
            notificationIcon.SetActive(showNotify);
        }

        BindClickEvent();
    }

    // 이름, 썸네일, 잠금 상태 등 기본 비주얼 갱신
    private void UpdateVisuals(ShopItemDefinition item, bool isUnlocked)
    {
        if (nameText != null)
            nameText.text = item != null ? item.displayName : "";

        if (iconImage != null)
        {
            iconImage.sprite = item != null ? item.thumbnail : null;
            iconImage.preserveAspect = true;
        }

        SetLockedVisual(!isUnlocked);
    }

    // 잠금 여부에 따라 아이콘/잠금 표시 갱신
    public void SetLockedVisual(bool locked)
    {
        // 캐릭터류 아이템은 잠금 상태일 때 아이콘을 어둡게 표시
        if (item.category != ShopCategory.Map && iconImage != null)
        {
            iconImage.color = locked ? Color.black : Color.white;

            // 해금 상태인데 썸네일이 없는 경우 반투명 처리
            if (!locked && (item == null || item.thumbnail == null))
                iconImage.color = new Color(1, 1, 1, 0.25f);
        }

        // 맵 아이템은 lockIcon 자체를 검정으로 유지
        if (item.category == ShopCategory.Map && lockIcon != null)
            lockIcon.color = Color.black;

        // 잠금 상태일 때만 lockIcon 표시
        if (lockIcon != null)
            lockIcon.gameObject.SetActive(locked);
    }

    // 버튼 클릭 시 현재 아이템을 넘기도록 이벤트 바인딩
    private void BindClickEvent()
    {
        if (button != null)
        {
            // 중복 등록 방지
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke(this.item));
        }
    }
}