using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

// 상점 UI 전체 흐름을 관리하는 컨트롤러
// - 탭 전환
// - 골드 표시
// - 아이템 그리드 생성
// - 선택된 아이템 프리뷰 갱신
// - 랜덤 해금 / 팝업 연결
// - 로비 캐릭터/맵 표시 갱신
public class ShopUIController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private ShopManager manager;                  // 상점 데이터/로직을 관리하는 매니저

    [Header("Top Bar")]
    [SerializeField] private Button characterTabButton;           // 캐릭터 탭 버튼
    [SerializeField] private Button mapTabButton;                 // 맵 탭 버튼
    [SerializeField] private TMP_Text goldText;                   // 현재 골드 표시 텍스트
    [SerializeField] private Button randomUnlockButton;           // 랜덤 해금 버튼
    [SerializeField] private TMP_Text randomUnlockButtonText;     // 랜덤 해금 버튼 가격 텍스트

    [Header("Selected Preview")]
    [SerializeField] private ShopPreviewStage selectedPreviewStage; // 현재 선택 아이템 3D 프리뷰
    [SerializeField] private RawImage selectedPreviewRawImage;      // 프리뷰 RenderTexture 출력 UI
    [SerializeField] private TMP_Text selectedNameText;             // 현재 선택 아이템 이름 표시

    [Header("Grid")]
    [SerializeField] private Transform gridContent;               // 아이템 셀들이 생성될 부모
    [SerializeField] private ShopItemCellUI cellPrefab;           // 아이템 셀 프리팹

    [Header("Popup")]
    [SerializeField] private ShopPopupUI popup;                   // 아이템 상세 팝업

    [Header("Lobby Positions")]
    [SerializeField] Transform charcterPos;                       // 로비에 캐릭터를 배치할 위치
    [SerializeField] Transform mapPos;                            // 로비에 맵을 배치할 위치

    private ShopCategory currentTab = ShopCategory.Character;     // 현재 활성화된 탭
    private readonly List<ShopItemCellUI> spawnedCells = new();   // 현재 생성된 그리드 셀 목록

    // 로비 프리뷰용으로 생성한 오브젝트 캐시
    // 한 번 생성한 뒤 재사용해서 중복 Instantiate를 줄임
    private Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();
    private GameObject currentCharacter;                          // 현재 로비에 표시 중인 캐릭터
    private GameObject currentMap;                                // 현재 로비에 표시 중인 맵

    [SerializeField] private ConfettiBurstEffect confettiBurstEffect; // 해금 성공 연출 효과

    private void Start()
    {
        // manager가 비어 있으면 싱글톤에서 자동 참조
        if (manager == null) manager = ShopManager.Instance;

        // 탭 / 랜덤 해금 버튼 이벤트 연결
        if (characterTabButton != null) characterTabButton.onClick.AddListener(() => SwitchTab(ShopCategory.Character));
        if (mapTabButton != null) mapTabButton.onClick.AddListener(() => SwitchTab(ShopCategory.Map));
        if (randomUnlockButton != null) randomUnlockButton.onClick.AddListener(OnRandomUnlock);

        // ShopManager 이벤트 구독
        if (manager != null)
        {
            manager.OnChanged += Refresh;
            manager.OnGoldChanged += HandleGoldChanged;
            manager.OnSelected += HandleSelected;
        }

        // 시작 시 UI 전체 갱신
        Refresh();
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (manager != null)
        {
            manager.OnChanged -= Refresh;
            manager.OnGoldChanged -= HandleGoldChanged;
            manager.OnSelected -= HandleSelected;
        }

        // 프리뷰 스테이지 정리
        if (selectedPreviewStage != null) selectedPreviewStage.Clear();
    }

    // 상점 진입 시 현재 장착 중인 캐릭터/맵을 로비 프리뷰에 적용
    // Firebase 유저 데이터가 준비될 때까지 잠시 대기 후 실행
    public async UniTaskVoid ApplyEquippedOnEnterAsync(CancellationToken ct)
    {
        if (manager == null) return;

        bool ok = await WaitUntilUserDataReadyAsync(5f, ct);
        if (!ok) return;

        var d = DatabaseManager.Instance?.currentData;
        if (d == null) return;

        string equippedCharId = d.equippedCharID;
        string equippedThemeId = d.equippedThemeID;

        // 장착 캐릭터 적용
        if (!string.IsNullOrEmpty(equippedCharId))
        {
            var charItem = manager.GetById(equippedCharId, ShopCategory.Character);
            if (charItem != null) HandleSelected(charItem);
            else Debug.LogWarning($"[ShopUI] equippedCharID '{equippedCharId}' 를 찾지 못함");
        }

        // 장착 맵 적용
        if (!string.IsNullOrEmpty(equippedThemeId))
        {
            var mapItem = manager.GetById(equippedThemeId, ShopCategory.Map);
            if (mapItem != null) HandleSelected(mapItem);
            else Debug.LogWarning($"[ShopUI] equippedThemeID '{equippedThemeId}' 를 찾지 못함");
        }
    }

    // DatabaseManager와 currentData가 준비될 때까지 대기
    // timeout이 지나면 false 반환
    private async UniTask<bool> WaitUntilUserDataReadyAsync(float timeoutSeconds, CancellationToken ct)
    {
        float start = Time.realtimeSinceStartup;

        while (!ct.IsCancellationRequested)
        {
            if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
                return true;

            if (Time.realtimeSinceStartup - start >= timeoutSeconds)
            {
                Debug.LogWarning("[ShopUI] userData 로딩 타임아웃");
                return false;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        return false;
    }

    // 로비에 표시할 아이템 오브젝트를 가져오거나, 없으면 새로 생성
    // 생성 후에는 spawned Dictionary에 캐싱
    private GameObject GetOrCreate(ShopItemDefinition item)
    {
        if (spawned.TryGetValue(item.id, out var go))
            return go;

        if (item.category == ShopCategory.Character)
        {
            // 캐릭터는 기본 Transform 기준으로 생성
            go = Instantiate(item.prefab, charcterPos);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(false);
        }
        else
        {
            // 맵은 ShopMapItemDefinition의 로비용 Transform 값 적용
            var mapItem = item as ShopMapItemDefinition;
            go = Instantiate(mapItem.prefab, mapPos);
            go.transform.localPosition = mapItem.lobbyLocalPosition;
            go.transform.localRotation = Quaternion.Euler(mapItem.lobbyLocalEuler);
            go.transform.localScale = mapItem.lobbyLocalScale;
            go.SetActive(false);
        }

        spawned[item.id] = go;
        return go;
    }

    // 아이템이 선택(장착)되었을 때 로비 프리뷰 오브젝트를 갱신
    private void HandleSelected(ShopItemDefinition item)
    {
        if (item == null || item.prefab == null) return;

        if (item.category == ShopCategory.Character)
        {
            if (charcterPos == null) return;

            // 기존 캐릭터는 숨기고 새 캐릭터 활성화
            if (currentCharacter != null) currentCharacter.SetActive(false);

            currentCharacter = GetOrCreate(item);
            currentCharacter.SetActive(true);
        }
        else
        {
            if (mapPos == null) return;

            // 기존 맵은 숨기고 새 맵 활성화
            if (currentMap != null) currentMap.SetActive(false);

            currentMap = GetOrCreate(item);
            currentMap.SetActive(true);
        }

        // 현재 탭과 일치하는 카테고리라면 우측 선택 프리뷰도 갱신
        if (item.category == currentTab)
            RefreshSelectedPreview();
    }

    // 탭 전환 처리
    private void SwitchTab(ShopCategory tab)
    {
        currentTab = tab;

        UpdateRandomButtonUI();
        RefreshGrid();
        RefreshSelectedPreview();

        // 탭 전환 시 팝업 닫기
        if (popup != null) popup.Close();
    }

    // 상점 전체 UI 갱신
    private void Refresh()
    {
        UpdateGoldUI();
        UpdateRandomButtonUI();
        RefreshGrid();
        RefreshSelectedPreview();
    }

    // 골드 변경 이벤트 수신 시 상단 골드 UI 갱신
    private void HandleGoldChanged(int _)
    {
        UpdateGoldUI();
    }

    // 현재 골드 표시 갱신
    private void UpdateGoldUI()
    {
        if (goldText == null || manager == null) return;
        goldText.text = $"{manager.GetGold()}G";
    }

    // 현재 탭에 따라 랜덤 해금 버튼 표시 상태 갱신
    private void UpdateRandomButtonUI()
    {
        if (randomUnlockButton == null || manager == null) return;

        bool isCharTab = currentTab == ShopCategory.Character;

        // 현재는 캐릭터 탭에서만 랜덤 해금 지원
        randomUnlockButton.gameObject.SetActive(isCharTab);

        if (!isCharTab) return;

        randomUnlockButtonText.text = $"랜덤 해금 ({manager.RandomUnlockCost}G)";
        // 필요 시 골드 부족에 따라 interactable 처리 가능
        // randomUnlockButton.interactable = manager.GetGold() >= manager.RandomUnlockCost;
    }

    // 현재 탭 기준으로 아이템 셀 그리드 다시 생성
    private void RefreshGrid()
    {
        if (gridContent == null || cellPrefab == null || manager == null) return;

        // 기존 셀 제거
        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] != null)
                Destroy(spawnedCells[i].gameObject);
        }
        spawnedCells.Clear();

        // 현재 탭의 아이템들로 셀 재생성
        var items = manager.GetItems(currentTab);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;

            var cell = Instantiate(cellPrefab, gridContent);
            bool owned = manager.IsOwned(item.id, item.category);

            cell.Setup(item, owned, OnCellClicked);
            spawnedCells.Add(cell);
        }
    }

    // 현재 탭 기준으로 선택된 아이템 프리뷰 갱신
    private void RefreshSelectedPreview()
    {
        if (manager == null || selectedPreviewStage == null) return;

        string selectedId = manager.GetSelectedId(currentTab);
        ShopItemDefinition item = null;

        // 1순위: 현재 장착 중인 아이템
        if (!string.IsNullOrEmpty(selectedId))
            item = manager.GetById(selectedId, currentTab);

        // 2순위: 현재 탭에서 보유 중인 첫 번째 아이템
        if (item == null)
        {
            var items = manager.GetItems(currentTab);
            foreach (var it in items)
            {
                if (it != null && manager.IsOwned(it.id, it.category))
                {
                    item = it;
                    break;
                }
            }

            // 3순위: 아이템 목록의 첫 번째 아이템
            if (item == null && items.Count > 0)
                item = items[0];
        }

        // 보여줄 아이템이 없으면 프리뷰 초기화
        if (item == null)
        {
            selectedPreviewStage.Clear();

            if (selectedPreviewRawImage != null)
                selectedPreviewRawImage.texture = null;

            if (selectedNameText != null)
                selectedNameText.text = "";

            return;
        }

        bool owned = manager.IsOwned(item.id, item.category);
        selectedPreviewStage.Show(item, lockedVisual: !owned);

        if (selectedPreviewRawImage != null)
            selectedPreviewRawImage.texture = selectedPreviewStage.Output;

        if (selectedNameText != null)
            selectedNameText.text = item.displayName;
    }

    // 아이템 셀 클릭 시 상세 팝업 열기
    private void OnCellClicked(ShopItemDefinition item)
    {
        if (manager == null || popup == null || item == null) return;

        bool owned = manager.IsOwned(item.id, item.category);
        popup.Open(manager, item, owned);
    }

    // 랜덤 해금 버튼 클릭 처리
    private void OnRandomUnlock()
    {
        if (manager == null) return;

        var unlocked = manager.TryRandomUnlock(currentTab);
        if (unlocked == null) return;

        // 해금 성공 연출 및 사운드 재생
        if (confettiBurstEffect != null) confettiBurstEffect.PlayBurst();
        if (EffectSounds.Instance != null) EffectSounds.Instance.GetCharacterSound();

        // 해금 결과 팝업 열기
        if (popup != null) popup.Open(manager, unlocked, owned: true);

        RefreshSelectedPreview();
    }
}