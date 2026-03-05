using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProfilePopupUI : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    public GameObject iconSlotPrefab;
    public Transform contentParent;

    [Header("Top Display")]
    public Image topProfileImage;
    public Image topProfileBG;

    private List<IconSlotUI> createdSlots = new List<IconSlotUI>();

    private void OnEnable()
    {
        if (createdSlots.Count == 0) CreateIcons();
        else RefreshAllSlotsState();

        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            string currentID = DatabaseManager.Instance.currentData.equippedIconID;
            UpdateTopImage(currentID);
        }
    }

    void CreateIcons()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        createdSlots.Clear();

        if (IconManager.Instance == null) return;

        List<ProfileIconData> allIcons = IconManager.Instance.GetAllIcons();

        foreach (var data in allIcons)
        {
            GameObject newSlot = Instantiate(iconSlotPrefab, contentParent);
            IconSlotUI slotScript = newSlot.GetComponent<IconSlotUI>();
            createdSlots.Add(slotScript);
        }

        RefreshAllSlotsState();
    }

    public void RefreshAllSlotsState()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) return;

        string currentEquippedID = DatabaseManager.Instance.currentData.equippedIconID;
        List<ProfileIconData> allIcons = IconManager.Instance.GetAllIcons();


        for (int i = 0; i < createdSlots.Count; i++)
        {
            ProfileIconData data = allIcons[i];

            bool isUnlocked = UserIconManager.Instance.IsIconUnlocked(data.iconID);
            bool isEquipped = (data.iconID == currentEquippedID);

            createdSlots[i].Setup(data, isUnlocked, isEquipped, (clickedID) => UpdateTopImage(clickedID));
        }
    }

    public void UpdateTopImage(string iconID)
    {
        if (topProfileImage == null) return;

        ProfileIconData data = IconManager.Instance.GetIconDataByID(iconID);

        if (data != null)
        {
            topProfileImage.sprite = data.iconSprite;
            if (topProfileBG != null) topProfileBG.color = data.backgroundColor;
        }
    }
}