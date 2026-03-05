using UnityEngine;

public enum ShopCategory { Character, Map }
public enum PreviewCameraMode { AutoFit, FixedPose }

[CreateAssetMenu(menuName = "Shop/Item Definition")]
public class ShopItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public ShopCategory category;
    public string displayName;

    [Header("Content")]
    public GameObject prefab;
    public Sprite thumbnail;

    [Header("Unlock Prices")]
    public int fixedUnlockPrice = 300;

    [Header("--- Character Only Settings ---")]
    [Tooltip("캐릭터 피버 잔상 색상 (맵 데이터일 경우 무시)")]
    public Color trailColor = new Color(0, 1, 1, 2);

    [Header("Preview Policy")]
    [Tooltip("캐릭터: AutoFit 추천 / 맵: FixedPose 추천")]
    public PreviewCameraMode cameraMode = PreviewCameraMode.AutoFit;

    [Tooltip("캐릭터만 true, 맵은 false 권장")]
    public bool allowRotate = true;

    [Header("Common Framing")]
    [Tooltip("Bounds center에서 중심 보정(맵/캐릭터 모두 가능)")]
    public Vector3 previewPivotOffset = Vector3.zero;

    [Header("AutoFit (Character 기본)")]
    public Vector2 previewEuler = new Vector2(15f, 180f);
    public float fitPadding = 1.25f;
    public float minDistance = 0.5f;
    public float maxDistance = 50f;

    [Header("FixedPose (Map 기본)")]
    [Tooltip("FixedPose일 때 카메라 로컬 위치(ShopPreviewStage 기준)")]
    public Vector3 cameraLocalPosition = new Vector3(0f, 0.8f, 6f);

    [Tooltip("FixedPose일 때 카메라 로컬 회전(오일러)")]
    public Vector3 cameraLocalEuler = new Vector3(35f, 180f, 0f);

    [Header("Camera Settings (optional)")]
    public bool useOrthographic = false;
    public float orthoSizeOverride = 0f;
    public float fovOverride = 0f;

    private void OnValidate()
    {
        if (category == ShopCategory.Map)
        {
            allowRotate = false;
            cameraMode = PreviewCameraMode.FixedPose;
        }
        else
        {
            allowRotate = true;
            cameraMode = PreviewCameraMode.AutoFit;
        }

        fitPadding = Mathf.Max(1.01f, fitPadding);
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);

        orthoSizeOverride = Mathf.Max(0f, orthoSizeOverride);
        fovOverride = Mathf.Max(0f, fovOverride);
    }
}