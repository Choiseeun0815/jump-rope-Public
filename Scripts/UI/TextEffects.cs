using DG.Tweening;
using UnityEngine;

public class TextEffects : MonoBehaviour
{
    void Start()
    {
        
        Vector3 targetScale = transform.localScale * 1.2f;

        transform.DOScale(targetScale, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}