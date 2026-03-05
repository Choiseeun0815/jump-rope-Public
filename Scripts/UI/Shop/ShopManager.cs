using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Catalogs")]
    [SerializeField] private ShopCatalog characterCatalog;
    [SerializeField] private ShopCatalog shopCharacterCatalog;
    [SerializeField] private ShopCatalog mapCatalog;

    [Header("Economy")]
    [SerializeField] private int randomUnlockCost = 150;
    [SerializeField] private ShopUIController shopUIController;

    [SerializeField] private GameObject lessGoldPanel;

    public event Action OnChanged;
    public event Action<int> OnGoldChanged;
    public event Action<ShopItemDefinition> OnUnlocked;
    public event Action<ShopItemDefinition> OnSelected;

    public int RandomUnlockCost => randomUnlockCost;

    private Dictionary<string, ShopItemDefinition> charDict;
    private Dictionary<string, ShopItemDefinition> mapDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (shopUIController != null)
        {
            shopUIController.ApplyEquippedOnEnterAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        BuildDictionaries();
    }

    private void Start()
    {
        // DatabaseManager에서 유저 데이터 로드가 끝난 뒤 호출되는 타이밍이 따로 있으면
        // 거기서 ShopManager.NotifyChanged()를 불러주는 게 가장 깔끔함.
        NotifyChanged();
    }

    private void BuildDictionaries()
    {
        charDict = new Dictionary<string, ShopItemDefinition>();
        mapDict = new Dictionary<string, ShopItemDefinition>();

        if (characterCatalog != null && characterCatalog.items != null)
        {
            foreach (var it in characterCatalog.items)
                if (it != null && !string.IsNullOrEmpty(it.id))
                    charDict[it.id] = it;
        }

        if (mapCatalog != null && mapCatalog.items != null)
        {
            foreach (var it in mapCatalog.items)
                if (it != null && !string.IsNullOrEmpty(it.id))
                    mapDict[it.id] = it;
        }
    }

    private UserGameData Data => DatabaseManager.Instance != null ? DatabaseManager.Instance.currentData : null;

    public IReadOnlyList<ShopItemDefinition> GetItems(ShopCategory category)
    {
        if (category == ShopCategory.Character)
            return shopCharacterCatalog.items; // ✅ 상점 전용만 반환
        else
            return mapCatalog.items;
    }

    public ShopItemDefinition GetById(string id, ShopCategory categoryHint = ShopCategory.Character)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // dictionary 우선
        if (categoryHint == ShopCategory.Character)
        {
            if (charDict != null && charDict.TryGetValue(id, out var c)) return c;
            if (mapDict != null && mapDict.TryGetValue(id, out var m)) return m;
        }
        else
        {
            if (mapDict != null && mapDict.TryGetValue(id, out var m)) return m;
            if (charDict != null && charDict.TryGetValue(id, out var c)) return c;
        }

        // fallback: catalog 검색
        var first = GetCatalog(categoryHint);
        var second = GetCatalog(categoryHint == ShopCategory.Character ? ShopCategory.Map : ShopCategory.Character);

        var found = first != null ? first.GetById(id) : null;
        if (found != null) return found;
        return second != null ? second.GetById(id) : null;
    }

    // 기존 코드 호환용: id만으로 owned 확인(둘 다 탐색)
    public bool IsOwned(string id)
    {
        var d = Data;
        if (d == null || string.IsNullOrEmpty(id)) return false;

        bool inTheme = d.unlockedThemeIDs != null && d.unlockedThemeIDs.Contains(id);
        bool inChar = d.unlockedCharIDs != null && d.unlockedCharIDs.Contains(id);
        return inTheme || inChar;
    }

    // 권장: category로 정확히 확인
    public bool IsOwned(string id, ShopCategory category)
    {
        var d = Data;
        if (d == null || string.IsNullOrEmpty(id)) return false;

        if (category == ShopCategory.Map)
            return d.unlockedThemeIDs != null && d.unlockedThemeIDs.Contains(id);

        return d.unlockedCharIDs != null && d.unlockedCharIDs.Contains(id);
    }

    public int GetGold()
    {
        var d = Data;
        return d != null ? d.gold : 0;
    }

    public bool CanAfford(int cost) => GetGold() >= cost;

    public bool TryFixedUnlock(ShopItemDefinition item)
    {
        var d = Data;
        if (d == null || item == null) return false;

        if (IsOwned(item.id, item.category)) return true;
        if (!CanAfford(item.fixedUnlockPrice))
        {
            lessGoldPanel.SetActive(true);
            return false;
        }

        DatabaseManager.Instance.SpendGoldAndUnlock(
            item.category,
            item.id,
            item.fixedUnlockPrice,
            onDone: ok =>
            {
                if (!ok)
                {
                    return;
                }

                OnGoldChanged?.Invoke(d.gold);
                OnUnlocked?.Invoke(item);
                OnChanged?.Invoke();
            });

        if (item.category == ShopCategory.Character)
            UserIconManager.Instance.UnlockIcon(item.id);

        return true;
    }

    public ShopItemDefinition TryRandomUnlock(ShopCategory category)
    {
        var d = Data;
        if (d == null) return null;

        if (!CanAfford(randomUnlockCost))
        {
            lessGoldPanel.SetActive(true);
            return null;
        }

        var pool = GetItems(category)
            .Where(i => i != null && !IsOwned(i.id, i.category))
            .ToList();

        if (pool.Count == 0) return null;

        var picked = pool[UnityEngine.Random.Range(0, pool.Count)];

        DatabaseManager.Instance.SpendGoldAndUnlock(
            category,
            picked.id,
            randomUnlockCost,
            onDone: ok =>
            {
                if (!ok)
                {
                    return;
                }

                OnGoldChanged?.Invoke(d.gold);
                OnUnlocked?.Invoke(picked);
                OnChanged?.Invoke();
            });

        if (picked.category == ShopCategory.Character)
            UserIconManager.Instance.UnlockIcon(picked.id);

        return picked;
    }

    public void SelectItem(ShopItemDefinition item)
    {
        var d = Data;
        if (d == null || item == null) return;
        if (!IsOwned(item.id, item.category)) return;

        Debug.Log($"[Shop] SelectItem called id={item.id} owned={IsOwned(item.id, item.category)}");

        if (item.category == ShopCategory.Character)
        {
            d.equippedCharID = item.id;
            DatabaseManager.Instance.UpdateEquippedChar(item.id);
        }
        else
        {
            d.equippedThemeID = item.id;
            DatabaseManager.Instance.UpdateEquippedTheme(item.id);
        }

        OnSelected?.Invoke(item);
        OnChanged?.Invoke();
    }

    public string GetSelectedId(ShopCategory category)
    {
        var d = Data;
        if (d == null) return "";

        return category == ShopCategory.Character ? d.equippedCharID : d.equippedThemeID;
    }

    private ShopCatalog GetCatalog(ShopCategory category)
    {
        return category == ShopCategory.Character ? characterCatalog : mapCatalog;
    }

    private void NotifyChanged()
    {
        var d = Data;
        if (d == null) return;

        OnGoldChanged?.Invoke(d.gold);
        OnChanged?.Invoke();
    }
}
