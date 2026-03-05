using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [SerializeField] private ShopCatalog mapCatalog;

    private Dictionary<string, ShopMapItemDefinition> mapItemById;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        BuildCache();
    }

    private void BuildCache()
    {
        mapItemById = new Dictionary<string, ShopMapItemDefinition>();

        if (mapCatalog == null || mapCatalog.items == null) return;

        foreach (var baseItem in mapCatalog.items)
        {
            var mapItem = baseItem as ShopMapItemDefinition;
            if (mapItem == null) continue;
            if (string.IsNullOrEmpty(mapItem.id)) continue;

            if (!mapItemById.ContainsKey(mapItem.id))
                mapItemById.Add(mapItem.id, mapItem);
        }
    }

    public ShopMapItemDefinition GetMapItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (mapItemById == null || mapItemById.Count == 0) BuildCache();

        mapItemById.TryGetValue(id, out var item);
        return item;
    }

    // (옵션) 기존 호환
    public MapThemeData GetThemeDataByID(string id)
    {
        var item = GetMapItemByID(id);
        if (item == null) return null;

        // ⚠️ 여기 필드명은 네 ShopMapItemDefinition 실제 필드명으로 맞춰야 함
        return item.mapThemeData; // 예: item.mapThemeData
    }
}