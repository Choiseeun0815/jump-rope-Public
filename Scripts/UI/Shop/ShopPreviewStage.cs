using System.Collections.Generic;
using UnityEngine;

public class ShopPreviewStage : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0);

    [SerializeField] private RenderTexture targetTexture;

    [Header("Camera/Light (optional)")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Light keyLight;
    [SerializeField] private Transform pivot;

    private RenderTexture rt;
    private GameObject instance;

    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    private Material lockedMaterial;

    private bool rotationEnabled = true;
    public bool IsRotationEnabled => rotationEnabled;

    // 기본 카메라 값 캐싱(아이템마다 orthographic/fov 바꿔도 잔상 방지)
    private float baseFov;
    private bool baseOrtho;
    private float baseOrthoSize;

    public RenderTexture Output => rt;

    private void Awake()
    {
        EnsureRig();
        EnsureRenderTexture();
    }

    private void EnsureRig()
    {
        if (pivot == null)
        {
            var pivotGO = new GameObject("Pivot");
            pivotGO.transform.SetParent(transform, false);
            pivot = pivotGO.transform;
        }

        if (previewCamera == null)
        {
            var camGO = new GameObject("PreviewCamera");
            camGO.transform.SetParent(transform, false);
            previewCamera = camGO.AddComponent<Camera>();
            previewCamera.fieldOfView = 30f;
        }

        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = backgroundColor;

        baseFov = previewCamera.fieldOfView;
        baseOrtho = previewCamera.orthographic;
        baseOrthoSize = previewCamera.orthographicSize;

        if (keyLight == null)
        {
            var lightGO = new GameObject("KeyLight");
            lightGO.transform.SetParent(transform, false);
            keyLight = lightGO.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.2f;
            keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }

    private void EnsureRenderTexture()
    {
        if (targetTexture != null)
        {
            rt = targetTexture;
            if (!rt.IsCreated()) rt.Create();
        }
        else
        {
            rt = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;
            rt.Create();
        }

        previewCamera.targetTexture = rt;
    }

    public void Show(ShopItemDefinition item, bool lockedVisual)
    {
        Clear();
        EnsureRig();
        EnsureRenderTexture();

        if (item == null || item.prefab == null) return;

        rotationEnabled = item.allowRotate;

        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;

        instance = Instantiate(item.prefab, pivot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // 중심 정렬(프리팹 원점이 제각각이어도 중심이 맞게)
        Bounds b = CalculateBounds(instance);
        if (b.size == Vector3.zero) b = new Bounds(instance.transform.position, Vector3.one);

        Vector3 centerLocal = pivot.InverseTransformPoint(b.center);
        instance.transform.localPosition -= (centerLocal + item.previewPivotOffset);

        // 다시 bounds
        b = CalculateBounds(instance);
        if (b.size == Vector3.zero) b = new Bounds(pivot.position, Vector3.one);

        ApplyCamera(item, b);

        // map 카테고리 일 때는 material 검정처리 안함
        if (lockedVisual && item.category != ShopCategory.Map)
            ApplyLockedMaterial(instance);

        previewCamera.Render();
    }

    private void ApplyCamera(ShopItemDefinition item, Bounds b)
    {
        // reset
        previewCamera.fieldOfView = baseFov;
        previewCamera.orthographic = baseOrtho;
        previewCamera.orthographicSize = baseOrthoSize;

        if (item.fovOverride > 0.01f)
            previewCamera.fieldOfView = item.fovOverride;

        // ✅ 1) FixedPose: SO의 위치+Euler 그대로 적용 (LookAt 금지)
        if (item.cameraMode == PreviewCameraMode.FixedPose)
        {
            previewCamera.orthographic = item.useOrthographic;

            if (previewCamera.orthographic)
            {
                if (item.orthoSizeOverride > 0.01f)
                    previewCamera.orthographicSize = item.orthoSizeOverride;
                else
                {
                    // fallback: bounds 기반 자동 (원하면 SO에 orthoSizeOverride 넣으면 됨)
                    float aspect = Mathf.Max(0.0001f, previewCamera.aspect);
                    float halfH = b.extents.y;
                    float halfW = b.extents.x / aspect;
                    previewCamera.orthographicSize = Mathf.Max(halfH, halfW) * Mathf.Max(1.01f, item.fitPadding);
                }
            }

            previewCamera.transform.localPosition = item.cameraLocalPosition;
            previewCamera.transform.localEulerAngles = item.cameraLocalEuler;

            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 5000f;
            return;
        }

        // ✅ 2) AutoFit: 캐릭터용
        Quaternion rot = Quaternion.Euler(item.previewEuler.x, item.previewEuler.y, 0f);

        if (item.useOrthographic)
        {
            previewCamera.orthographic = true;

            float size;
            if (item.orthoSizeOverride > 0.01f) size = item.orthoSizeOverride;
            else
            {
                float aspect = Mathf.Max(0.0001f, previewCamera.aspect);
                float halfH = b.extents.y;
                float halfW = b.extents.x / aspect;
                size = Mathf.Max(halfH, halfW) * item.fitPadding;
            }

            previewCamera.orthographicSize = Mathf.Max(0.01f, size);

            Vector3 dir = rot * Vector3.back;
            float dist = Mathf.Clamp(b.extents.magnitude * 2f, item.minDistance, item.maxDistance);

            previewCamera.transform.position = pivot.position + dir * dist;
            previewCamera.transform.rotation = rot;

            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = Mathf.Max(200f, dist + b.extents.magnitude * 20f);
        }
        else
        {
            previewCamera.orthographic = false;

            float radius = b.extents.magnitude * item.fitPadding;
            float fovRad = Mathf.Max(0.0001f, previewCamera.fieldOfView * Mathf.Deg2Rad);
            float dist = radius / Mathf.Tan(fovRad * 0.5f);
            dist = Mathf.Clamp(dist, item.minDistance, item.maxDistance);

            Vector3 dir = rot * Vector3.back;
            previewCamera.transform.position = pivot.position + dir * dist;
            previewCamera.transform.rotation = rot;

            float clipPad = b.extents.magnitude * 10f;
            previewCamera.nearClipPlane = Mathf.Max(0.01f, dist - clipPad);
            previewCamera.farClipPlane = dist + clipPad;
        }
    }

    public void RotateYaw(float degrees)
    {
        if (!rotationEnabled) return;
        if (pivot == null) return;
        pivot.Rotate(Vector3.up, degrees, Space.Self);
    }

    public void Clear()
    {
        if (instance != null)
        {
            RestoreMaterials();
            Destroy(instance);
            instance = null;
        }
        originalMaterials.Clear();
    }

    private void ApplyLockedMaterial(GameObject root)
    {
        if (lockedMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            lockedMaterial = new Material(s);
            lockedMaterial.color = Color.black;
            if (lockedMaterial.HasProperty("_Cull"))
                lockedMaterial.SetFloat("_Cull", 0f);
            if (lockedMaterial.HasProperty("_CullMode"))
                lockedMaterial.SetFloat("_CullMode", 0f);
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!originalMaterials.ContainsKey(r))
                originalMaterials[r] = r.sharedMaterials;

            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = lockedMaterial;
            r.sharedMaterials = mats;
        }
    }

    private void RestoreMaterials()
    {
        foreach (var kv in originalMaterials)
        {
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
        }
    }

    private static Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}