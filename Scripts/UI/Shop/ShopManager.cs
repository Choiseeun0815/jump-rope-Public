using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 상점 전체 흐름을 관리하는 매니저
// - 아이템 목록 조회
// - 보유 여부 / 선택 여부 확인
// - 고정 해금 / 랜덤 해금
// - 장착 처리
// - 골드 및 UI 갱신 이벤트 전달
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("Catalogs")]
    [SerializeField] private ShopCatalog characterCatalog;      // 전체 캐릭터 카탈로그
    [SerializeField] private ShopCatalog shopCharacterCatalog;  // 상점에 실제 노출할 캐릭터 카탈로그
    [SerializeField] private ShopCatalog mapCatalog;            // 전체 맵 카탈로그

    [Header("Economy")]
    [SerializeField] private int randomUnlockCost = 150;        // 랜덤 해금 비용
    [SerializeField] private ShopUIController shopUIController; // 상점 UI 컨트롤러

    [SerializeField] private GameObject lessGoldPanel;          // 골드 부족 시 표시할 패널

    // 상점 상태 변경 이벤트
    public event Action OnChanged;

    // 골드 값 변경 이벤트
    public event Action<int> OnGoldChanged;

    // 아이템 해금 성공 이벤트
    public event Action<ShopItemDefinition> OnUnlocked;

    // 아이템 선택(장착) 이벤트
    public event Action<ShopItemDefinition> OnSelected;

    public int RandomUnlockCost => randomUnlockCost;

    // 빠른 조회를 위한 ID -> 아이템 Dictionary
    private Dictionary<string, ShopItemDefinition> charDict;
    private Dictionary<string, ShopItemDefinition> mapDict;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 상점 진입 시 현재 장착 아이템을 UI에 반영
        if (shopUIController != null)
        {
            shopUIController.ApplyEquippedOnEnterAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        // 카탈로그 기반 Dictionary 생성
        BuildDictionaries();
    }

    private void Start()
    {
        // 시작 시 현재 골드/상태를 UI에 알림
        NotifyChanged();
    }

    // 카탈로그의 아이템들을 ID 기준 Dictionary로 구성
    // GetById 호출 시 빠르게 찾기 위함
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

    // 현재 사용자 게임 데이터
    private UserGameData Data => DatabaseManager.Instance != null ? DatabaseManager.Instance.currentData : null;

    // 카테고리별 상점 표시 아이템 목록 반환
    public IReadOnlyList<ShopItemDefinition> GetItems(ShopCategory category)
    {
        if (category == ShopCategory.Character)
            return shopCharacterCatalog.items; // 상점 전용 캐릭터 목록만 반환
        else
            return mapCatalog.items;
    }

    // ID로 아이템 조회
    // categoryHint를 우선 기준으로 검색하고, 없으면 반대 카테고리까지 fallback 검색
    public ShopItemDefinition GetById(string id, ShopCategory categoryHint = ShopCategory.Character)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 1차: Dictionary 우선 검색
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

        // 2차: Catalog 직접 검색 fallback
        var first = GetCatalog(categoryHint);
        var second = GetCatalog(categoryHint == ShopCategory.Character ? ShopCategory.Map : ShopCategory.Character);

        var found = first != null ? first.GetById(id) : null;
        if (found != null) return found;
        return second != null ? second.GetById(id) : null;
    }

    // 특정 카테고리 아이템을 현재 사용자가 보유 중인지 확인
    public bool IsOwned(string id, ShopCategory category)
    {
        var d = Data;
        if (d == null || string.IsNullOrEmpty(id)) return false;

        if (category == ShopCategory.Map)
            return d.unlockedThemeIDs != null && d.unlockedThemeIDs.Contains(id);

        return d.unlockedCharIDs != null && d.unlockedCharIDs.Contains(id);
    }

    // 현재 보유 골드 반환
    public int GetGold()
    {
        var d = Data;
        return d != null ? d.gold : 0;
    }

    // 특정 비용을 지불할 수 있는지 확인
    public bool CanAfford(int cost) => GetGold() >= cost;

    // 고정 가격으로 아이템 해금 시도
    public bool TryFixedUnlock(ShopItemDefinition item)
    {
        var d = Data;
        if (d == null || item == null) return false;

        // 이미 보유 중이면 성공 처리
        if (IsOwned(item.id, item.category)) return true;

        // 골드 부족 시 실패 + 안내 패널 표시
        if (!CanAfford(item.fixedUnlockPrice))
        {
            lessGoldPanel.SetActive(true);
            return false;
        }

        // DB에 골드 차감 + 해금 반영
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

                // 성공 시 골드/해금/UI 갱신 이벤트 발생
                OnGoldChanged?.Invoke(d.gold);
                OnUnlocked?.Invoke(item);
                OnChanged?.Invoke();
            });

        // 캐릭터 해금 시 연결된 아이콘도 함께 해금
        if (item.category == ShopCategory.Character)
            UserIconManager.Instance.UnlockIcon(item.id);

        return true;
    }

    // 랜덤 해금 시도
    // 아직 보유하지 않은 아이템 중 하나를 랜덤으로 선택해 해금
    public ShopItemDefinition TryRandomUnlock(ShopCategory category)
    {
        var d = Data;
        if (d == null) return null;

        // 골드 부족 시 실패 + 안내 패널 표시
        if (!CanAfford(randomUnlockCost))
        {
            lessGoldPanel.SetActive(true);
            return null;
        }

        // 아직 보유하지 않은 아이템만 후보군으로 구성
        var pool = GetItems(category)
            .Where(i => i != null && !IsOwned(i.id, i.category))
            .ToList();

        // 더 이상 해금할 아이템이 없으면 종료
        if (pool.Count == 0) return null;

        // 후보군 중 랜덤 선택
        var picked = pool[UnityEngine.Random.Range(0, pool.Count)];

        // DB에 골드 차감 + 해금 반영
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

                // 성공 시 골드/해금/UI 갱신 이벤트 발생
                OnGoldChanged?.Invoke(d.gold);
                OnUnlocked?.Invoke(picked);
                OnChanged?.Invoke();
            });

        // 캐릭터 해금 시 연결된 아이콘도 함께 해금
        if (picked.category == ShopCategory.Character)
            UserIconManager.Instance.UnlockIcon(picked.id);

        return picked;
    }

    // 보유 중인 아이템을 선택(장착)
    public void SelectItem(ShopItemDefinition item)
    {
        var d = Data;
        if (d == null || item == null) return;

        // 보유하지 않은 아이템은 선택 불가
        if (!IsOwned(item.id, item.category)) return;

        Debug.Log($"[Shop] SelectItem called id={item.id} owned={IsOwned(item.id, item.category)}");

        // 카테고리에 따라 현재 장착 ID 갱신 + DB 반영
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

        // 선택 상태/UI 갱신 이벤트 발생
        OnSelected?.Invoke(item);
        OnChanged?.Invoke();
    }

    // 현재 장착된 아이템 ID 반환
    public string GetSelectedId(ShopCategory category)
    {
        var d = Data;
        if (d == null) return "";

        return category == ShopCategory.Character ? d.equippedCharID : d.equippedThemeID;
    }

    // 카테고리에 맞는 카탈로그 반환
    private ShopCatalog GetCatalog(ShopCategory category)
    {
        return category == ShopCategory.Character ? characterCatalog : mapCatalog;
    }

    // 현재 상태를 UI에 알림
    private void NotifyChanged()
    {
        var d = Data;
        if (d == null) return;

        OnGoldChanged?.Invoke(d.gold);
        OnChanged?.Invoke();
    }
}