using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RankItem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image topProfileImage;
    [SerializeField] private Image topProfileBG;

    [Header("Colors")]
    [SerializeField] private Color myRankColor = new Color(0.4f, 0.8f, 1f); // 연한 파랑
    [SerializeField] private Color normalColor = Color.white;


    public void SetData(int rank, string nickname, int score, bool isMe, string iconID)
    {
        // 순위 표시 + 등수에 따른 색
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            
            if (rank <= 3)
            {
                Color[] medals = { Color.gold, Color.silver, Color.brown };
                rankText.color = medals[rank - 1];
            }
            else if (isMe)
            {
                rankText.color = myRankColor;
            }
            else
            {
                rankText.color = normalColor;
            }
        }

        if (nicknameText != null)
            nicknameText.text = nickname;

        if (scoreText != null)
            scoreText.text = score.ToString("N0"); // 천 단위 쉼표

        UpdateIcon(iconID);
    }

    private void UpdateIcon(string iconID)
    {
        if (string.IsNullOrEmpty(iconID))
        {
            iconID = "Icon_Default";
        }

        ProfileIconData iconData = IconManager.Instance.GetIconDataByID(iconID);

        if (iconData != null)
        {
            topProfileImage.sprite = iconData.iconSprite;
            topProfileBG.color = iconData.backgroundColor;
        }
        else
        {
            ProfileIconData defaultData = IconManager.Instance.GetIconDataByID("Icon_Default");
            topProfileImage.sprite = defaultData.iconSprite;
            topProfileBG.color = defaultData.backgroundColor;
        }
    }
}