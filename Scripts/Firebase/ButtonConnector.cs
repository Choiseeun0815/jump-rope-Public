using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonConnector : MonoBehaviour
{
    public void LogInButton() //로그인 버튼
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginButtonClicked();
        }
        else
        {
            Debug.Log("AuthManager 없음");
        }
    }

    public void LogOutButton() //로그아웃 버튼
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLogoutButtonClicked();
        }
        else
        {
            Debug.Log("AuthManager 없음");
        }
    }

    public void WithdrawButton() //탈퇴 버튼
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnWithdrawButtonClicked();
        }
        else
        {
            Debug.Log("AuthManager 없음");
        }
    }

    public void NicknameConfirmButton() //닉네임 확정 버튼
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.nicknameController.OnConfirmButtonClicked();
        }
        else
        {
            Debug.Log("AuthManager 없음");
        }
    }

    public void StartGameButton()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.SceneTransition("GameScene");
        }
    }

    public void RobbySceneButton()
    {
        if (SceneController.Instance != null)
        {
            // LobbyScene
            SceneController.Instance.SceneTransitionToLobby();
        }
    }
}