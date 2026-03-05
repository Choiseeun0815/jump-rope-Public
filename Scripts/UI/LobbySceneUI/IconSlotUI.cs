using UnityEngine;
using UnityEngine.UI;
using System; // Action 사용을 위해 필요

public class IconSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image characterImg;    
    public Image characterBG;     
    public GameObject lockIcon;    
    public Button myButton;        

    private ProfileIconData myData;
    private bool isUnlocked = false;

    private Action<string> onIconClicked;

    private void Start()
    {
        if (myButton != null)
        {
            myButton.onClick.RemoveAllListeners();
            myButton.onClick.AddListener(OnClickIcon);
        }
    }

    public void Setup(ProfileIconData data, bool unlocked, bool equipped, Action<string> onClickCallback)
    {
        myData = data;
        isUnlocked = unlocked;
        onIconClicked = onClickCallback; 

        if (characterImg != null)
            characterImg.sprite = data.iconSprite;

        if (isUnlocked)
        {
            if (characterImg != null) characterImg.color = Color.white;
            if (lockIcon != null) lockIcon.SetActive(false);
            if (characterBG != null) characterBG.color = data.backgroundColor;
        }
        else
        {
            if (characterImg != null) characterImg.color = Color.black; 
            if (characterBG != null) characterBG.color = Color.gray; 
            if (lockIcon != null) lockIcon.SetActive(true);
        }
    }

    private void OnClickIcon()
    {
        if (!isUnlocked)
        {
            Debug.Log("해금되지 않은 아이콘입니다.");
            return;
        }

        UserIconManager.Instance.EquipIcon(myData.iconID);

        onIconClicked?.Invoke(myData.iconID);
    }
}