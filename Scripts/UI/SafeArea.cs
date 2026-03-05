using UnityEngine;

public class SafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect safeArea;
    private Vector2 minAnchor;
    private Vector2 maxAnchor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    public void ApplySafeArea()
    {
        safeArea = Screen.safeArea;

        minAnchor.x = safeArea.x / Screen.width;
        minAnchor.y = safeArea.y / Screen.height;
        maxAnchor.x = (safeArea.x + safeArea.width) / Screen.width;
        maxAnchor.y = (safeArea.y + safeArea.height) / Screen.height;

        rectTransform.anchorMin = minAnchor;
        rectTransform.anchorMax = maxAnchor;
    }
}