using UnityEngine;
using UnityEngine.EventSystems;

public class ShopPreviewDragRotate : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private ShopPreviewStage stage;
    [SerializeField] private float rotateSpeed = 0.2f;

    private Vector2 lastPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (stage == null) return;
        if (!stage.IsRotationEnabled) return;

        Vector2 now = eventData.position;
        Vector2 delta = now - lastPos;
        lastPos = now;

        stage.RotateYaw(-delta.x * rotateSpeed);
    }
}
