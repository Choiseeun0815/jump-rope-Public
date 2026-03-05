using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class NicknameController : MonoBehaviour
{
    public GameObject nicknamePanel;
    public PanelEffects nicknamePanelEffect;
    public TMP_InputField inputField;
    public TMP_Text warningText;

    private string playerUserID;

    [SerializeField] private TextAsset badWordsFile;
    private List<string> badWordsList = new List<string>();
    private void Start()
    {
        LoadBadWords();
    }
    public void ShowPanel(string userId)
    {
        playerUserID = userId;
        nicknamePanel.SetActive(true);
        inputField.text = "";
        warningText.text = "";
    }
    void LoadBadWords()
    {
        if(badWordsFile != null)
        {
            string[] lines = badWordsFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            badWordsList.AddRange(lines);
        }
    }
    public void OnConfirmButtonClicked()
    {
        string name = inputField.text;

        if (!IsValidNickname(name)) return;

        if (DatabaseManager.Instance != null)
        {
            inputField.interactable = false; 

            DatabaseManager.Instance.CheckNicknameDuplication(name, (bool isDuplicate) =>
            {
                if (isDuplicate)
                {
                    warningText.text = "ÀÌ¹Ì Á¸ÀçÇÏ´Â ´Ğ³×ÀÓÀÔ´Ï´Ù.";
                    inputField.interactable = true; 
                }
                else
                {
                    CreateAccount(name);
                }
            });
        }
    }

    bool IsValidNickname(string name)
    {
        if(name.Length <2 || name.Length >=6)
        {
            warningText.text = "±ÛÀÚ ¼ö°¡\nÃæÁ·µÇÁö ¾Ê¾Ò½À´Ï´Ù."; return false;
        }

        string pattern = @"^[0-9a-zA-Z°¡-ÆR¤¡-¤¾¤¿-¤Ó]*$"; //Çã¿ëÇÒ ¹®ÀÚ ¹üÀ§(ÇÑ±Û, ¿µ¹®, ¼ıÀÚ)
        if(!Regex.IsMatch(name, pattern))
        {
            warningText.text = "Æ¯¼ö¹®ÀÚ³ª °ø¹éÀº\n»ç¿ëÇÒ ¼ö ¾ø½À´Ï´Ù."; return false;
        }

        foreach (string badWord in badWordsList)
        {
            if (name.Contains(badWord))
            {
                warningText.text = "ºÎÀûÀıÇÑ ´Ü¾î°¡\nÆ÷ÇÔµÇ¾î ÀÖ½À´Ï´Ù.";
                return false;
            }
        }
        return true;
    }

    void CreateAccount(string name)
    {
        DatabaseManager.Instance.CreateNewData(playerUserID, name, 0);
        nicknamePanelEffect.Close();
        //nicknamePanel.SetActive(false);

        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnNicknameRegistrationComplete();
        }
        else Debug.Log("authManager ¿¬°á ¾È µÇ¾îÀÖÀ½");
    }
}
