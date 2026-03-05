using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    static public SceneController Instance;

    [SerializeField] Image fadeImage;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] TextMeshProUGUI loadingText;
    [SerializeField] string LobbySceneName= "LobbyScene";
    [SerializeField] string GameSceneName = "GameScene";

    private readonly string[] loadingTextFrames = { "Loading", "Loading.", "Loading..", "Loading..." };
    private Coroutine loadingCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SceneTransition(string SceneName)
    {
        StartCoroutine(TransitionSequence(SceneName));
    }
    public void SceneTransitionToLobby()
    {
        StartCoroutine(TransitionSequence(LobbySceneName));
    }
    private IEnumerator TransitionSequence(string SceneName)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();
        StartLoadingAnimation();
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneName);

        while (!op.isDone)
        {
            yield return null;
        }

        if (BGMSounds.Instance != null)
            BGMSounds.Instance.PlayBGM(SceneName);

        if (SceneName == GameSceneName || SceneName == LobbySceneName)
        {
            yield break;
        }
        StopLoadingAnimation();
        yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);

    }
    public IEnumerator FadeIn()
    {
        StopLoadingAnimation();
        if (fadeImage != null)
        {
            yield return fadeImage.DOFade(0f, fadeDuration).WaitForCompletion();
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }
    public IEnumerator FadeOut()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true; // 터치 막기

            yield return fadeImage.DOFade(1f, fadeDuration).WaitForCompletion();

            StartLoadingAnimation();
        }
    }
    private void StartLoadingAnimation()
    {
        if (loadingText == null) return;
        loadingText.gameObject.SetActive(true);

        if(loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(AnimateLoadingTextRoutine());
    }
    private void StopLoadingAnimation()
    {
        if(loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        if (loadingText != null) loadingText.gameObject.SetActive(false);
    }
    private IEnumerator AnimateLoadingTextRoutine()
    {
        int idx = 0;
        while (true)
        {
            loadingText.text = loadingTextFrames[idx];
            idx = (idx + 1) % loadingTextFrames.Length;

            yield return new WaitForSeconds(.3f);
        }
    }
}