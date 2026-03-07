using System.Collections;
using UnityEngine;
using Google;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Firebase;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    public string GoogleAPI = "-";

    private FirebaseAuth auth;
    private GoogleSignInConfiguration configuration;

    public NicknameController nicknameController;
    public GameObject touchToStartPanel;
    public GameObject LoginButtonObject;
    public GameObject uiBlocker;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            int targetHeight = 1920;
            if (Screen.height > targetHeight)
            {
                float ratio = (float)Screen.width / Screen.height;
                int targetWidth = (int)(targetHeight * ratio);
                Screen.SetResolution(targetWidth, targetHeight, true);
            }
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Instance.nicknameController = this.nicknameController;
            Instance.touchToStartPanel = this.touchToStartPanel;
            Instance.LoginButtonObject = this.LoginButtonObject;
            Instance.uiBlocker = this.uiBlocker; 
            Destroy(gameObject);
            return;
        }

        if (nicknameController != null) nicknameController.nicknamePanel.SetActive(false);
        if (touchToStartPanel != null) touchToStartPanel.SetActive(false);
        if (LoginButtonObject != null) LoginButtonObject.SetActive(false);
        if (uiBlocker != null) uiBlocker.SetActive(false);
        InitializeGoogleSignInConfiguration();
    }

    private void InitializeGoogleSignInConfiguration()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = GoogleAPI,
            RequestIdToken = true,
            RequestEmail = true
        };

        GoogleSignIn.Configuration = configuration;
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                if (auth.CurrentUser != null)
                {
                    //로그인 정보가 있는 경우 → 데이터 로드 시도
                    string userId = auth.CurrentUser.UserId;

                    if (DatabaseManager.Instance != null)
                    {
                        DatabaseManager.Instance.LoadUserData(userId, (bool hasData) =>
                        {
                            if (hasData)
                            {
                                // 데이터가 있으면 TouchToStart 패널 표시
                                ShowTouchToStartPanel();
                            }
                            else
                            {
                                //인증은 되어있으나 DB 데이터가 없는 경우 (예외 상황) -> 로그아웃 처리 후 로그인 버튼 표시
                                auth.SignOut();

                                if (GoogleSignIn.DefaultInstance != null && configuration != null)
                                {
                                    GoogleSignIn.Configuration = configuration;
                                    GoogleSignIn.DefaultInstance.SignOut();
                                }

                                //로그인 버튼을 보여줌
                                ShowLoginButton();
                            }
                        });
                    }
                }
                else
                {
                    //로그인 정보가 없는 경우 -> 로그인 버튼 표시
                    ShowLoginButton();
                }
            }
            else
            {
                Debug.LogError("Firebase 의존성 문제 발생: " + task.Result);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void ShowLoginButton()
    {
        if (touchToStartPanel != null) touchToStartPanel.SetActive(false);
        if (LoginButtonObject != null) LoginButtonObject.SetActive(true);
        if (uiBlocker != null) uiBlocker.SetActive(false);
    }

    public void OnLoginButtonClicked()
    {
        if (configuration == null)
        {
            InitializeGoogleSignInConfiguration();
        }

        GoogleSignIn.Configuration = configuration;

        if (GoogleSignIn.DefaultInstance != null)
        {
            try
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SignOut 중 에러 (무시됨): " + e.Message);
            }
        }

        StartCoroutine(DelayedSignIn());
    }

    private IEnumerator DelayedSignIn()
    {
        yield return new WaitForSeconds(0.5f);

        GoogleSignIn.DefaultInstance.SignIn()
            .ContinueWith(OnGoogleLoginFinished, TaskScheduler.FromCurrentSynchronizationContext());
    }

    void OnGoogleLoginFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            if (task.Exception != null)
            {
                Debug.LogError("Google 로그인 실패: " + task.Exception);
            }
            return; 
        }

        if (task.Result == null || string.IsNullOrEmpty(task.Result.IdToken))
        {
            Debug.LogError("IdToken이 없습니다");
            return;
        }

        string idToken = task.Result.IdToken;
        Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

        auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
        {
            if (authTask.IsCanceled || authTask.IsFaulted)
            {
                if (authTask.Exception != null) Debug.LogError(authTask.Exception);
            }
            else if (authTask.Result != null)
            {
                LoginProcess(authTask.Result.UserId);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    void LoginProcess(string userId)
    {
        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.LoadUserData(userId, (bool hasData) =>
            {
                if (hasData)
                {
                    ShowTouchToStartPanel();
                }
                else
                {
                    if (nicknameController != null)
                    {
                        if (uiBlocker != null) uiBlocker.SetActive(true);
                        nicknameController.ShowPanel(userId);
                    }
                    else
                    {
                        Debug.Log("nickNameController 없음");
                    }
                }
            });
        }
    }

    public void OnWithdrawButtonClicked()
    {
        FirebaseUser user = auth.CurrentUser;
        if (user == null) return;

        StartCoroutine(WithdrawSequence(user));
    }

    private IEnumerator WithdrawSequence(FirebaseUser user)
    {
        if (SceneController.Instance != null)
            yield return StartCoroutine(SceneController.Instance.FadeOut());

        bool reAuthDone = false;
        bool reAuthFailed = false;

        GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith(googleTask =>
        {
            if (googleTask.IsFaulted || googleTask.IsCanceled)
            {
                Debug.LogError("재인증 실패: " + googleTask.Exception);
                reAuthFailed = true;
                reAuthDone = true;
                return;
            }

            string idToken = googleTask.Result.IdToken;
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

            user.ReauthenticateAsync(credential).ContinueWith(reAuthTask =>
            {
                if (reAuthTask.IsFaulted || reAuthTask.IsCanceled)
                {
                    Debug.LogError("Firebase 재인증 실패: " + reAuthTask.Exception);
                    reAuthFailed = true;
                }
                else
                {
                    Debug.Log("재인증 성공");
                }
                reAuthDone = true;

            }, TaskScheduler.FromCurrentSynchronizationContext());

        }, TaskScheduler.FromCurrentSynchronizationContext());

        yield return new WaitUntil(() => reAuthDone);

        if (reAuthFailed)
        {
            Debug.LogError("재인증 실패로 탈퇴 중단");
            if (SceneController.Instance != null)
                yield return StartCoroutine(SceneController.Instance.FadeIn());
            yield break;
        }

        bool dbDone = false;
        if (DatabaseManager.Instance != null)
        {
            DatabaseManager.Instance.DeleteUserData(user.UserId, (bool success) =>
            {
                dbDone = true;
            });
        }
        else
        {
            dbDone = true;
        }

        yield return new WaitUntil(() => dbDone);

        bool authDone = false;
        bool authFailed = false;

        user.DeleteAsync().ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("계정 삭제 실패: " + task.Exception);
                authFailed = true;
            }
            else
            {
                Debug.Log("Auth 삭제 성공");
            }
            authDone = true;

        }, TaskScheduler.FromCurrentSynchronizationContext());

        yield return new WaitUntil(() => authDone);

        if (authFailed)
        {
            if (SceneController.Instance != null)
                yield return StartCoroutine(SceneController.Instance.FadeIn());
            yield break;
        }

        if (auth != null) auth.SignOut();
        if (GoogleSignIn.DefaultInstance != null)
        {
            try { GoogleSignIn.DefaultInstance.SignOut(); } catch { }
        }

        if (SceneController.Instance != null)
            SceneController.Instance.SceneTransition("LoginScene");
    }

    public void OnLogoutButtonClicked()
    {
        if (auth != null) auth.SignOut();
        if (GoogleSignIn.DefaultInstance != null) GoogleSignIn.DefaultInstance.SignOut();

        if (DatabaseManager.Instance != null) DatabaseManager.Instance.currentData = null;
        if (nicknameController != null) nicknameController.nicknamePanel.SetActive(false);
        if (uiBlocker != null) uiBlocker.SetActive(false);
        if (SceneController.Instance != null)
            SceneController.Instance.SceneTransition("LoginScene");
        else
        {
            ShowLoginButton();
        }
    }

    public void ShowTouchToStartPanel()
    {
        if (LoginButtonObject != null) LoginButtonObject.SetActive(false);
        if (uiBlocker != null) uiBlocker.SetActive(false);
        if (touchToStartPanel != null)
        {
            touchToStartPanel.SetActive(true);
        }
        else
        {
            if (SceneController.Instance != null)
                SceneController.Instance.SceneTransitionToLobby();
            else
                Debug.Log("SceneController 없음");
        }
    }

    public void OnNicknameRegistrationComplete()
    {
        if (uiBlocker != null) uiBlocker.SetActive(false);
        ShowTouchToStartPanel();
    }
}
