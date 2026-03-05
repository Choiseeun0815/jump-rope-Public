using UnityEngine;
using UnityEngine.UI;

public class UISound : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlaySound);
        }
    }

    void PlaySound()
    {
        if (EffectSounds.Instance != null)
        {
            EffectSounds.Instance.PlayClickSound();
        }
    }
}