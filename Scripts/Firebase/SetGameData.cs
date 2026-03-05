using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetGameData : MonoBehaviour
{
    [Header("플레이어 info")]
    [SerializeField] TextMeshProUGUI userNickname;
    [SerializeField] TextMeshProUGUI userCurrentGold;
    [SerializeField] TextMeshProUGUI userBestScore;
    [SerializeField] TextMeshProUGUI userBestCombo;

    [SerializeField] Image topProfileImage;
    [SerializeField] Image topProfileBG;

    [SerializeField] Toggle buttonCase1;
    [SerializeField] Toggle frame_60;

    public void ShowUserInfoButtonClicked()
    {
        if(DatabaseManager.Instance != null)
        {
            UserGameData user = DatabaseManager.Instance.currentData;
            userNickname.text = user.nickName;
            userCurrentGold.text = "보유 골드: "+user.gold + "G";
            userBestScore.text = "최고 기록: "+user.highScore + "점";
            userBestCombo.text = "최고 콤보: " + user.maxCombo + "콤보";
            string currentID = user.equippedIconID;
            ProfileIconData data = IconManager.Instance.GetIconDataByID(currentID);
            if (data != null)
            {
                topProfileImage.sprite = data.iconSprite;
                topProfileBG.color = data.backgroundColor;
            }

            buttonCase1.isOn = user.buttonCase1; //이곳의 on/off에 따라 Case2는 자동 결정
            frame_60.isOn = user.frame_60; //on/off에 따라 frame 30 선택 여부 자동 결정
        }
    }
}