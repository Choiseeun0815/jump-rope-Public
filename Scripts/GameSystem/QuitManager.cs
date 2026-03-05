using UnityEngine;
using UnityEngine.InputSystem;
public class QuitManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("종료 확인 팝업 패널을 연결해주세요.")]
    public GameObject quitPopupPanel;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.IsGameStarted &&
                !GameManager.Instance.IsGameOver)
            {
                return; 
            }
            if (quitPopupPanel.activeSelf)
            {
                ClosePopup();
            }
            else
            {
                OpenPopup();
            }
        }
    }

    public void OpenPopup()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        if (quitPopupPanel != null) quitPopupPanel.SetActive(false);
    }

    public void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}