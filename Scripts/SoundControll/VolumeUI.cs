using UnityEngine;
using UnityEngine.UI;

public class VolumeUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Buttons")]
    [SerializeField] Button bgmBtn;
    [SerializeField] Button sfxBtn;

    [Header("Icon Images")]
    [SerializeField] Image bgmIcon;
    [SerializeField] Image sfxIcon;

    [Header("Mute Sprite")]
    [SerializeField] Sprite muteSprite; 

    private Sprite originBgmSprite;
    private Sprite originSfxSprite;

    private float savedBgmVolume = 1f;
    private float savedSfxVolume = 1f;

    private void Start()
    {
        if (bgmIcon != null) originBgmSprite = bgmIcon.sprite;
        if (sfxIcon != null) originSfxSprite = sfxIcon.sprite;

        if (BGMSounds.Instance != null)
        {
            bgmSlider.value = BGMSounds.Instance.bgmVolume;
            if (bgmSlider.value > 0) savedBgmVolume = bgmSlider.value;
            UpdateBgmIcon(bgmSlider.value);
        }

        if (EffectSounds.Instance != null)
        {
            sfxSlider.value = EffectSounds.Instance.masterVolume;
            if (sfxSlider.value > 0) savedSfxVolume = sfxSlider.value;
            UpdateSfxIcon(sfxSlider.value);
        }

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);

    }

    private void OnBGMChanged(float value)
    {
        if (BGMSounds.Instance != null)
            BGMSounds.Instance.SetVolume(value);

        if (value > 0) savedBgmVolume = value;

        UpdateBgmIcon(value);
    }

    private void OnSfxChanged(float value)
    {
        if (EffectSounds.Instance != null)
            EffectSounds.Instance.SetVolume(value);

        if (value > 0) savedSfxVolume = value;

        UpdateSfxIcon(value);
    }

    private void UpdateBgmIcon(float value)
    {
        if (bgmIcon == null) return;

        if (value <= 0.001f) 
            bgmIcon.sprite = muteSprite;
        else
            bgmIcon.sprite = originBgmSprite;
    }

    private void UpdateSfxIcon(float value)
    {
        if (sfxIcon == null) return;

        if (value <= 0.001f)
            sfxIcon.sprite = muteSprite;
        else
            sfxIcon.sprite = originSfxSprite; 
    }

    public void ToggleBGM()
    {
        if (bgmSlider.value > 0)
        {
            savedBgmVolume = bgmSlider.value;
            bgmSlider.value = 0;
        }
        else
        {
            bgmSlider.value = (savedBgmVolume > 0) ? savedBgmVolume : 1f;
        }
        UpdateBgmIcon(bgmSlider.value);
    }

    public void ToggleSFX()
    {
        if (sfxSlider.value > 0)
        {
            savedSfxVolume = sfxSlider.value;
            sfxSlider.value = 0;
        }
        else
        {
            sfxSlider.value = (savedSfxVolume > 0) ? savedSfxVolume : 1f;
        }
        UpdateSfxIcon(sfxSlider.value);

    }
}