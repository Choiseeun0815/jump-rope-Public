using UnityEngine;
using DG.Tweening;

public class PanelEffects : MonoBehaviour
{
    public enum AnimationType { Scale, Slide }

    [Header("Main Settings")]
    public AnimationType animationType = AnimationType.Scale;
    public float duration = 0.4f;
    public Ease openEase = Ease.OutBack;
    public Ease closeEase = Ease.InBack;

    public bool playOnEnable = true;

    [Header("Slide Settings (Only for Slide Mode)")]
    public float slideDistance = 1000f;

    private Transform contentObject;
    private Vector3 defaultScale;
    private Vector3 defaultPosition;

    private void Awake()
    {
        if (contentObject == null) contentObject = transform;
        defaultScale = contentObject.localScale;
        defaultPosition = contentObject.localPosition;
    }

    private void OnEnable()
    {
        if (UserIconManager.Instance != null && UserIconManager.Instance.BG != null)
            UserIconManager.Instance.BG.SetActive(true);

        contentObject.DOKill();
        contentObject.localScale = defaultScale;
        contentObject.localPosition = defaultPosition;

        if (playOnEnable)
        {
            PlayOpenAnimation();
        }
        else
        {
            if (animationType == AnimationType.Scale) contentObject.localScale = Vector3.zero;
            else if (animationType == AnimationType.Slide) contentObject.localPosition = defaultPosition + (Vector3.down * slideDistance);
        }
    }

    public void PlayOpenAnimation()
    {
        switch (animationType)
        {
            case AnimationType.Scale: PlayScaleOpen(); break;
            case AnimationType.Slide: PlaySlideOpen(); break;
        }
    }

    public void Close()
    {
        if (UserIconManager.Instance != null && UserIconManager.Instance.BG != null)
            UserIconManager.Instance.BG.SetActive(false);

        contentObject.DOKill();

        switch (animationType)
        {
            case AnimationType.Scale: PlayScaleClose(); break;
            case AnimationType.Slide: PlaySlideClose(); break;
        }
    }

    private void PlayScaleOpen()
    {
        contentObject.localScale = Vector3.zero;
        contentObject.DOScale(defaultScale, duration).SetEase(openEase).SetUpdate(true);
    }

    private void PlaySlideOpen()
    {
        contentObject.localPosition = defaultPosition + (Vector3.down * slideDistance);
        contentObject.DOLocalMove(defaultPosition, duration).SetEase(openEase).SetUpdate(true);
    }

    private void PlayScaleClose()
    {
        contentObject.DOScale(0f, duration * 0.8f).SetEase(closeEase).SetUpdate(true).OnComplete(() => gameObject.SetActive(false));
    }

    private void PlaySlideClose()
    {
        contentObject.DOLocalMove(defaultPosition + (Vector3.down * slideDistance), duration * 0.8f).SetEase(closeEase).SetUpdate(true).OnComplete(() => {
            gameObject.SetActive(false);
            contentObject.localPosition = defaultPosition;
        });
    }
}