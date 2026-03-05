using UnityEngine;
using UnityEngine.SceneManagement;
[RequireComponent(typeof(AudioSource))]
public class BGMSounds : MonoBehaviour
{
    public static BGMSounds Instance;
    private AudioSource audioSource;

    public AudioClip lobbyBGM;
    public AudioClip gameBGM;

    [Range(0f, 1f)] public float bgmVolume = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            float savedVolume = PlayerPrefs.GetFloat("BGM_Volume", 0.5f);
            bgmVolume = (savedVolume <= 0.01f) ? 0.5f : savedVolume;
        }
        else { Destroy(gameObject); return; }
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        audioSource.volume = bgmVolume;
        string activeScene = SceneManager.GetActiveScene().name;
        Debug.Log($"ÇöÀç ¾À: {activeScene}, ¼³Á¤µÈ º¼·ý: {bgmVolume}");
        PlayBGM(activeScene);
    }
    public void PlayBGM(string sceneName)
    {
        AudioClip targetClip = null;

        if(sceneName == "GameScene")
        {
            targetClip = gameBGM;
        }
        else
        {
            targetClip = lobbyBGM;
        }

        if (audioSource.clip == targetClip && audioSource.isPlaying) return;

        audioSource.clip = targetClip;
        audioSource.Play();
    }
    public void SetGameBGM(AudioClip clip)
    {
        if (clip != null) gameBGM = clip;
    }
    public void StopBgm()
    {
        if (audioSource != null) audioSource.Stop();
    }
    public void SetVolume(float value)
    {
        bgmVolume = value;
        if (audioSource != null)
        {
            audioSource.volume = bgmVolume;
        }

        PlayerPrefs.SetFloat("BGM_Volume", bgmVolume);
    }
}
