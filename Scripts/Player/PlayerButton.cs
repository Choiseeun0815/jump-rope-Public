using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PlayerButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent onDown;
    public UnityEvent onUp;

    private bool _isDown;

    // GameScene 버튼 UI에서 버튼 눌렀을 때 동작
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isDown) return;
        _isDown = true;
        onDown?.Invoke();
    }

    // GameScene 버튼 UI에서 버튼 땠을 때 동작
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDown) return;
        _isDown = false;
        onUp?.Invoke();
    }
}