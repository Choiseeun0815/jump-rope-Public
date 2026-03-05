using System.Collections;
using System.Linq;
using UnityEngine;

public class CreateMap : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private ShopCatalog mapCatalog; // ✅ 맵용 카탈로그(ShopMapItemDefinition 리스트를 들고 있어야 함)

    [Header("Root")]
    [SerializeField] private Transform mapRoot; // ✅ 생성될 부모 (CreateMap 오브젝트 본인 Transform 써도 됨)

    [Header("Fallback")]
    [SerializeField] private string defaultThemeId = "Theme_default";

    private GameObject instance;

    private void Start()
    {
        if (mapRoot == null) mapRoot = transform;
        StartCoroutine(CoCreateMapFromEquippedTheme());
    }

    private IEnumerator CoCreateMapFromEquippedTheme()
    {
        // ✅ Firebase 로드 대기(너 프로젝트 비동기 구조 때문에)
        float timeout = 3f;
        while ((DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // equippedThemeID 가져오기
        string equippedThemeID = defaultThemeId;
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            var id = DatabaseManager.Instance.currentData.equippedThemeID;
            if (!string.IsNullOrEmpty(id)) equippedThemeID = id;
        }

        // ✅ 카탈로그에서 id로 찾기 (맵은 ShopMapItemDefinition)
        // ※ mapCatalog.items 타입이 ShopMapItemDefinition 리스트라고 가정
        ShopMapItemDefinition def = null;
        if (mapCatalog != null && mapCatalog.items != null)
        {
            def = mapCatalog.items
                .OfType<ShopMapItemDefinition>()
                .FirstOrDefault(x => x != null && x.id == equippedThemeID);
        }

        // 못 찾으면 fallback
        if (def == null || def.prefab == null)
        {
            Debug.LogWarning($"equippedThemeID '{equippedThemeID}' 를 못 찾아서 fallback='{defaultThemeId}' 로 시도");
            if (mapCatalog != null && mapCatalog.items != null)
            {
                def = mapCatalog.items
                    .OfType<ShopMapItemDefinition>()
                    .FirstOrDefault(x => x != null && x.id == defaultThemeId);

                if (def == null)
                {
                    def = mapCatalog.items.OfType<ShopMapItemDefinition>().FirstOrDefault(x => x != null);
                }
            }
        }

        if (def == null || def.prefab == null)
        {
            Debug.LogError("CreateMap 실패: 생성할 맵 prefab을 찾을 수 없음");
            yield break;
        }

        // 기존 맵 제거
        if (instance != null) Destroy(instance);
        for (int i = mapRoot.childCount - 1; i >= 0; i--)
            Destroy(mapRoot.GetChild(i).gameObject);

        // 생성
        instance = Instantiate(def.prefab, mapRoot);

        if (def != null)
        {
            instance.transform.localPosition = def.gameLocalPosition;
            instance.transform.localEulerAngles = def.gameLocalEuler;
            instance.transform.localScale = def.gameLocalScale;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }
    }
}