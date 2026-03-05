using UnityEngine;
[RequireComponent(typeof(AudioSource))]

public class EffectSounds : MonoBehaviour
{
    static public EffectSounds Instance;
    private AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip jumpSound;
    public AudioClip coinClip;
    public AudioClip bonusScoreClip;
    public AudioClip bonusGoldClip;
    public AudioClip buttonClickClip;
    public AudioClip getCharacterClip;
    public AudioClip[] perfectClip;
    public AudioClip countDownClip;
    public AudioClip hitClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;

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
            return;
        }

        audioSource = GetComponent<AudioSource>();
        masterVolume = PlayerPrefs.GetFloat("SFX_Volume", 1.0f);
    }

    public void SetVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat("SFX_Volume", masterVolume);
    }

    public void JumpSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(jumpSound, 0.8f * masterVolume);
    }
    public void HitSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(hitClip, 0.8f * masterVolume);
    }
    public void CoinSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(coinClip, 0.8f * masterVolume);
    }
    public void BonusScoreSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(bonusScoreClip, 1.0f * masterVolume);
    }
    public void GetCharacterSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(getCharacterClip, 1.0f * masterVolume);
    }
    public void BonusGoldSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(bonusGoldClip, 1.0f * masterVolume);
    }
    public void PerfectSound()
    {
        if (ScoreManager.Instance != null && audioSource != null)
        {
            int idx = ScoreManager.Instance.perfectCombo - 1;
            idx = Mathf.Min(idx, 7);
            audioSource.PlayOneShot(perfectClip[idx], 1.2f * masterVolume);
        }
    }
    public void CountdownSound()
    {
        if (audioSource != null) audioSource.PlayOneShot(countDownClip, 1.0f * masterVolume);
    }
    public void PlayClickSound()
    {
        if (audioSource != null && buttonClickClip != null)
        {
            audioSource.PlayOneShot(buttonClickClip, 1.0f * masterVolume);
        }
    }
}