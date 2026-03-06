using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CreateMap : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private ShopCatalog mapCatalog; // 맵 SO들이 들어있는 카탈로그 SO

    [Header("Root")]
    [SerializeField] private Transform mapRoot; // 맵 프리팹이 생성될 부모 Transform

    [Header("Fallback")]
    [SerializeField] private string defaultThemeId = "Theme_default"; // Firebase DB에서 장착된 테마 ID 맵을 못 찾았을 때 사용할 기본 맵 ID

    private void Start()
    {
        // mapRoot가 비어 있으면 현재 오브젝트를 부모로 사용
        if (mapRoot == null)
            mapRoot = transform;

        // 오브젝트가 파괴될 때 자동으로 취소되도록 CancellationToken 전달
        CreateMapFromEquippedThemeAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // Firebase DB에서 현재 장착된 테마 ID를 기준으로 맵을 찾아 생성
    private async UniTaskVoid CreateMapFromEquippedThemeAsync(CancellationToken cancellationToken)
    {
        // DatabaseManager / currentData가 아직 준비되지 않았을 수 있으므로 잠깐 대기
        await WaitForDatabaseAsync(3f, cancellationToken);

        // 기본값 테마 ID
        string equippedThemeID = defaultThemeId;

        // Firebase DB에서 장착된 테마 ID 읽기
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            var id = DatabaseManager.Instance.currentData.equippedThemeID;
            if (!string.IsNullOrEmpty(id))
                equippedThemeID = id;
        }

        // 1차: Firebase DB에서 장착된 테마 ID로 맵 찾기
        ShopMapItemDefinition def = FindMapDefinitionById(equippedThemeID);

        // 2차: 못 찾았거나 prefab이 비어 있으면 기본 맵 찾기
        if (def == null || def.prefab == null)
        {
            Debug.LogWarning($"equippedThemeID '{equippedThemeID}' 를 못 찾아서 fallback='{defaultThemeId}' 로 시도");
            def = FindFallbackDefinition();
        }

        // 3차: 최종적으로도 못 찾으면 생성 중단
        if (def == null || def.prefab == null)
        {
            Debug.LogError("CreateMap 실패: 생성할 맵 prefab을 찾을 수 없음");
            return;
        }

        // 기존 mapRoot 하위 맵들 제거
        ClearCurrentMap();

        // 새 맵 생성
        var instance = Instantiate(def.prefab, mapRoot);

        // SO에 저장된 위치/회전/스케일 적용
        ApplyTransform(instance.transform, def);
    }

    // DatabaseManager와 currentData가 준비될 때까지 대기
    // timeout이 지나면 그냥 다음 로직 진행 -> 무한으로 대기하는 것을 방지
    private async UniTask WaitForDatabaseAsync(float timeout, CancellationToken cancellationToken)
    {
        float remain = timeout;

        while ((DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) && remain > 0f)
        {
            // unscaledDeltaTime 사용: Time.timeScale 영향 없이 대기
            remain -= Time.unscaledDeltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    // 카탈로그 SO에서 특정 ID와 일치하는 맵 SO 찾기
    private ShopMapItemDefinition FindMapDefinitionById(string themeId)
    {
        if (mapCatalog == null || mapCatalog.items == null)
            return null;

        return mapCatalog.items
            .OfType<ShopMapItemDefinition>()
            .FirstOrDefault(x => x != null && x.id == themeId);
    }

    // 특정 ID 맵 찾기 실패 시 처리 로직
    // 1순위: defaultThemeId와 일치하는 맵 SO
    // 2순위: 카탈로그 SO 안의 첫 번째 유효한 맵 SO
    private ShopMapItemDefinition FindFallbackDefinition()
    {
        if (mapCatalog == null || mapCatalog.items == null)
            return null;

        var fallback = mapCatalog.items
            .OfType<ShopMapItemDefinition>()
            .FirstOrDefault(x => x != null && x.id == defaultThemeId);

        if (fallback != null)
            return fallback;

        return mapCatalog.items
            .OfType<ShopMapItemDefinition>()
            .FirstOrDefault(x => x != null);
    }

    // mapRoot 아래에 있는 기존 맵 오브젝트 전부 제거
    private void ClearCurrentMap()
    {
        for (int i = mapRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(mapRoot.GetChild(i).gameObject);
        }
    }

    // Map SO에 저장된 GameScene용 Transform 값 적용
    private void ApplyTransform(Transform target, ShopMapItemDefinition def)
    {
        if (target == null)
            return;

        target.localPosition = def.gameLocalPosition;
        target.localEulerAngles = def.gameLocalEuler;
        target.localScale = def.gameLocalScale;
    }
}