using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemCellUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image lockIcon;
    [SerializeField] private TMP_Text nameText;

    [SerializeField] private GameObject notificationIcon;

    private ShopItemDefinition item;
    private Action<ShopItemDefinition> onClick;

    public void Setup(ShopItemDefinition item, bool owned, Action<ShopItemDefinition> onClick)
    {
        this.item = item;
        this.onClick = onClick;

        UpdateVisuals(item, owned);

        if (notificationIcon != null) notificationIcon.SetActive(false);

        BindClickEvent();
    }

    public void SetupChallenge(ShopItemDefinition item, bool isUnlocked, bool isClaimable, Action<ShopItemDefinition> onClick)
    {
        this.item = item;
        this.onClick = onClick;

        UpdateVisuals(item, isUnlocked);

        if (notificationIcon != null)
        {
            bool showNotify = !isUnlocked && isClaimable;
            notificationIcon.SetActive(showNotify);
        }

        BindClickEvent();
    }

    private void UpdateVisuals(ShopItemDefinition item, bool isUnlocked)
    {
        if (nameText != null) nameText.text = item != null ? item.displayName : "";

        if (iconImage != null)
        {
            iconImage.sprite = item != null ? item.thumbnail : null;
            iconImage.preserveAspect = true;
        }

        SetLockedVisual(!isUnlocked);
    }

    public void SetLockedVisual(bool locked)
    {
        if (item.category != ShopCategory.Map && iconImage != null)
        {
            iconImage.color = locked ? Color.black : Color.white;
            if (!locked && (item == null || item.thumbnail == null))
                iconImage.color = new Color(1, 1, 1, 0.25f);
        }
        if (item.category == ShopCategory.Map && lockIcon != null) lockIcon.color = Color.black;
        if (lockIcon != null) lockIcon.gameObject.SetActive(locked);
    }

    private void BindClickEvent()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke(this.item));
        }
    }
}