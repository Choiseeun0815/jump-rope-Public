using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonEffects : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private float buttonDownScale = 0.85f;
    private float duration = 0.2f;

    private Vector3 initialScale;
    private bool isInitialized = false; 

    private void Awake()
    {
        //최초 크기
        if (!isInitialized)
        {
            initialScale = transform.localScale;
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        if (isInitialized)
        {
            transform.localScale = initialScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale( buttonDownScale, 0.1f).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RestoreScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreScale();
    }

    private void RestoreScale()
    {
        transform.DOKill();
        transform.DOScale(initialScale, duration).SetEase(Ease.OutElastic).SetUpdate(true);
    }
}
