using System.Collections.Generic;
using UnityEngine;

// 상점 프리뷰 전용 스테이지
// - 선택한 아이템 프리팹을 별도 공간에 생성
// - RenderTexture로 3D 미리보기 출력
// - 카메라/조명 자동 구성
// - 잠금 상태일 때 검정 머티리얼 처리
// - 필요 시 좌우 회전 지원
public class ShopPreviewStage : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private int textureSize = 512;                          // 내부 생성 RenderTexture 해상도
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0); // 프리뷰 배경색(기본 투명)

    [SerializeField] private RenderTexture targetTexture;                    // 외부에서 지정한 RenderTexture (없으면 내부 생성)

    [Header("Camera/Light (optional)")]
    [SerializeField] private Camera previewCamera;                           // 프리뷰 전용 카메라
    [SerializeField] private Light keyLight;                                 // 프리뷰용 메인 조명
    [SerializeField] private Transform pivot;                                // 회전/배치 기준 축

    private RenderTexture rt;                                                // 실제 출력에 사용하는 RenderTexture
    private GameObject instance;                                             // 현재 프리뷰에 생성된 아이템 인스턴스

    // 잠금 상태일 때 원래 머티리얼 복구를 위해 캐싱
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
    private Material lockedMaterial;                                         // 잠금 상태용 검정 머티리얼

    private bool rotationEnabled = true;                                     // 현재 아이템이 회전을 허용하는지
    public bool IsRotationEnabled => rotationEnabled;

    // 기본 카메라 값 캐싱
    // 아이템마다 FOV / Orthographic 값을 바꾼 뒤 다음 아이템에 영향이 남지 않도록 복원용으로 사용
    private float baseFov;
    private bool baseOrtho;
    private float baseOrthoSize;

    // 외부 UI(RawImage 등)에서 사용할 출력 텍스처
    public RenderTexture Output => rt;

    private void Awake()
    {
        EnsureRig();
        EnsureRenderTexture();
    }

    // 프리뷰용 Pivot / Camera / Light가 없으면 자동 생성
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

        // 현재 카메라 기본값 저장
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

    // 출력용 RenderTexture 준비
    // targetTexture가 있으면 그것을 사용하고, 없으면 내부에서 새로 생성
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

    // 아이템 프리뷰 표시
    // - 기존 프리뷰 제거
    // - 새 프리팹 생성
    // - Bounds 기준 중심 정렬
    // - 카메라 자동 배치
    // - 잠금 상태면 검정 머티리얼 적용
    public void Show(ShopItemDefinition item, bool lockedVisual)
    {
        Clear();
        EnsureRig();
        EnsureRenderTexture();

        if (item == null || item.prefab == null) return;

        rotationEnabled = item.allowRotate;

        // Pivot 초기화
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;

        // 프리팹 생성
        instance = Instantiate(item.prefab, pivot);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // 프리팹 원점이 제각각이어도 중심이 화면 중앙에 오도록 보정
        Bounds b = CalculateBounds(instance);
        if (b.size == Vector3.zero) b = new Bounds(instance.transform.position, Vector3.one);

        Vector3 centerLocal = pivot.InverseTransformPoint(b.center);
        instance.transform.localPosition -= (centerLocal + item.previewPivotOffset);

        // 보정 후 bounds 다시 계산
        b = CalculateBounds(instance);
        if (b.size == Vector3.zero) b = new Bounds(pivot.position, Vector3.one);

        // 아이템 설정에 맞게 카메라 배치
        ApplyCamera(item, b);

        // 맵 카테고리는 잠금 상태여도 머티리얼 검정 처리하지 않음
        if (lockedVisual && item.category != ShopCategory.Map)
            ApplyLockedMaterial(instance);

        // 수동 렌더링 1회 수행
        previewCamera.Render();
    }

    // 아이템 설정값과 Bounds를 기준으로 카메라 위치/회전/클리핑 결정
    private void ApplyCamera(ShopItemDefinition item, Bounds b)
    {
        // 이전 아이템 설정이 남지 않도록 카메라 기본값 초기화
        previewCamera.fieldOfView = baseFov;
        previewCamera.orthographic = baseOrtho;
        previewCamera.orthographicSize = baseOrthoSize;

        if (item.fovOverride > 0.01f)
            previewCamera.fieldOfView = item.fovOverride;

        // 1) FixedPose
        // SO에 저장된 카메라 위치/회전을 그대로 적용
        // 주로 맵처럼 구도를 고정해서 보여줄 때 사용
        if (item.cameraMode == PreviewCameraMode.FixedPose)
        {
            previewCamera.orthographic = item.useOrthographic;

            if (previewCamera.orthographic)
            {
                if (item.orthoSizeOverride > 0.01f)
                    previewCamera.orthographicSize = item.orthoSizeOverride;
                else
                {
                    // override가 없으면 bounds 기반으로 자동 계산
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

        // 2) AutoFit
        // 주로 캐릭터처럼 대상 크기에 따라 카메라를 자동으로 맞출 때 사용
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

    // Y축 기준으로 프리뷰 대상 회전
    public void RotateYaw(float degrees)
    {
        if (!rotationEnabled) return;
        if (pivot == null) return;

        pivot.Rotate(Vector3.up, degrees, Space.Self);
    }

    // 현재 프리뷰 대상 제거 및 머티리얼 복구
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

    // 잠금 상태일 때 모든 Renderer의 머티리얼을 검정 머티리얼로 교체
    private void ApplyLockedMaterial(GameObject root)
    {
        if (lockedMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");

            lockedMaterial = new Material(s);
            lockedMaterial.color = Color.black;

            // 양면 렌더링 지원
            if (lockedMaterial.HasProperty("_Cull"))
                lockedMaterial.SetFloat("_Cull", 0f);

            if (lockedMaterial.HasProperty("_CullMode"))
                lockedMaterial.SetFloat("_CullMode", 0f);
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;

            // 원래 머티리얼 캐싱
            if (!originalMaterials.ContainsKey(r))
                originalMaterials[r] = r.sharedMaterials;

            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = lockedMaterial;

            r.sharedMaterials = mats;
        }
    }

    // 잠금 처리 전 원래 머티리얼로 복구
    private void RestoreMaterials()
    {
        foreach (var kv in originalMaterials)
        {
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
        }
    }

    // 프리팹 전체 Renderer를 기준으로 Bounds 계산
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