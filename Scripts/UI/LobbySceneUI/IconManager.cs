using UnityEngine;
using System.Collections.Generic;

public class IconManager : MonoBehaviour
{
    public static IconManager Instance;

    [Header("Icon Catalog")]
    public IconCatalog iconCatalog; 

    private Dictionary<string, ProfileIconData> iconDictionary = new Dictionary<string, ProfileIconData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        iconDictionary.Clear();

        // 카탈로그가 비어있는지 안전 검사 추가
        if (iconCatalog == null || iconCatalog.allIcons == null)
        {
            Debug.LogWarning("[IconManager] Icon Catalog가 할당되지 않았거나 비어있습니다!");
            return;
        }

        // 카탈로그 안의 리스트를 순회합니다.
        foreach (var icon in iconCatalog.allIcons)
        {
            if (icon == null) continue;

            if (!iconDictionary.ContainsKey(icon.iconID))
            {
                iconDictionary.Add(icon.iconID, icon);
            }
            else
            {
                Debug.LogWarning($"[IconManager] 중복된 아이콘 ID가 발견되었습니다: {icon.iconID}");
            }
        }
    }

    public ProfileIconData GetIconDataByID(string id)
    {
        if (iconDictionary.ContainsKey(id))
        {
            return iconDictionary[id];
        }

        Debug.LogWarning($"[IconManager] 아이콘 데이터를 찾을 수 없습니다: {id}");
        return null;
    }

    public List<ProfileIconData> GetAllIcons()
    {
        if (iconCatalog != null && iconCatalog.allIcons != null)
            return iconCatalog.allIcons;

        return new List<ProfileIconData>();
    }
}