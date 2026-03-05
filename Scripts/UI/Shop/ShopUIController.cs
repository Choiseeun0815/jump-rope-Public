using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ShopUIController : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private ShopManager manager;

    [Header("Top Bar")]
    [SerializeField] private Button characterTabButton;
    [SerializeField] private Button mapTabButton;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button randomUnlockButton;
    [SerializeField] private TMP_Text randomUnlockButtonText;

    [Header("Selected Preview (B안)")]
    [SerializeField] private ShopPreviewStage selectedPreviewStage;
    [SerializeField] private RawImage selectedPreviewRawImage;
    [SerializeField] private TMP_Text selectedNameText;

    [Header("Grid")]
    [SerializeField] private Transform gridContent;
    [SerializeField] private ShopItemCellUI cellPrefab;

    [Header("Popup")]
    [SerializeField] private ShopPopupUI popup;

    [Header("Lobby Positions")]
    [SerializeField] Transform charcterPos;
    [SerializeField] Transform mapPos;

    private ShopCategory currentTab = ShopCategory.Character;
    private readonly List<ShopItemCellUI> spawnedCells = new();

    private Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();
    private GameObject currentCharacter;
    private GameObject currentMap;

    [SerializeField] private ConfettiBurstEffect confettiBurstEffect;

    private void Start()
    {
        if (manager == null) manager = ShopManager.Instance;

        if (characterTabButton != null) characterTabButton.onClick.AddListener(() => SwitchTab(ShopCategory.Character));
        if (mapTabButton != null) mapTabButton.onClick.AddListener(() => SwitchTab(ShopCategory.Map));
        if (randomUnlockButton != null) randomUnlockButton.onClick.AddListener(OnRandomUnlock);

        if (manager != null)
        {
            manager.OnChanged += Refresh;
            manager.OnGoldChanged += HandleGoldChanged;
            manager.OnSelected += HandleSelected;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.OnChanged -= Refresh;
            manager.OnGoldChanged -= HandleGoldChanged;
            manager.OnSelected -= HandleSelected;
        }

        if (selectedPreviewStage != null) selectedPreviewStage.Clear();
    }

    public async UniTaskVoid ApplyEquippedOnEnterAsync(CancellationToken ct)
    {
        if (manager == null) return;

        bool ok = await WaitUntilUserDataReadyAsync(5f, ct);
        if (!ok) return;

        var d = DatabaseManager.Instance?.currentData;
        if (d == null) return;

        string equippedCharId = d.equippedCharID;
        string equippedThemeId = d.equippedThemeID;

        if (!string.IsNullOrEmpty(equippedCharId))
        {
            var charItem = manager.GetById(equippedCharId, ShopCategory.Character);
            if (charItem != null) HandleSelected(charItem);
            else Debug.LogWarning($"[ShopUI] equippedCharID '{equippedCharId}' 를 찾지 못함");
        }

        if (!string.IsNullOrEmpty(equippedThemeId))
        {
            var mapItem = manager.GetById(equippedThemeId, ShopCategory.Map);
            if (mapItem != null) HandleSelected(mapItem);
            else Debug.LogWarning($"[ShopUI] equippedThemeID '{equippedThemeId}' 를 찾지 못함");
        }
    }

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

    private GameObject GetOrCreate(ShopItemDefinition item)
    {
        if (spawned.TryGetValue(item.id, out var go)) return go;

        if (item.category == ShopCategory.Character)
        {
            go = Instantiate(item.prefab, charcterPos);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(false);
        }
        else
        {
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

    private void HandleSelected(ShopItemDefinition item)
    {
        if (item == null || item.prefab == null) return;

        if (item.category == ShopCategory.Character)
        {
            if (charcterPos == null) return;
            if (currentCharacter != null) currentCharacter.SetActive(false);

            currentCharacter = GetOrCreate(item);
            currentCharacter.SetActive(true);
        }
        else
        {
            if (mapPos == null) return;
            if (currentMap != null) currentMap.SetActive(false);

            currentMap = GetOrCreate(item);
            currentMap.SetActive(true);
        }

        if (item.category == currentTab)
            RefreshSelectedPreview();
    }

    private void SwitchTab(ShopCategory tab)
    {
        currentTab = tab;

        UpdateRandomButtonUI();
        RefreshGrid();
        RefreshSelectedPreview();
        if (popup != null) popup.Close();
    }

    private void Refresh()
    {
        UpdateGoldUI();
        UpdateRandomButtonUI();
        RefreshGrid();
        RefreshSelectedPreview();
    }

    private void HandleGoldChanged(int _)
    {
        UpdateGoldUI();
    }

    private void UpdateGoldUI()
    {
        if (goldText == null || manager == null) return;
        goldText.text = $"{manager.GetGold()}G";
    }

    private void UpdateRandomButtonUI()
    {
        if (randomUnlockButton == null || manager == null) return;

        bool isCharTab = currentTab == ShopCategory.Character;
        randomUnlockButton.gameObject.SetActive(isCharTab);

        if (!isCharTab) return;

        randomUnlockButtonText.text = $"랜덤 해금 ({manager.RandomUnlockCost}G)";
        //randomUnlockButton.interactable = manager.GetGold() >= manager.RandomUnlockCost;
    }

    private void RefreshGrid()
    {
        if (gridContent == null || cellPrefab == null || manager == null) return;

        for (int i = 0; i < spawnedCells.Count; i++)
        {
            if (spawnedCells[i] != null) Destroy(spawnedCells[i].gameObject);
        }
        spawnedCells.Clear();

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

    private void RefreshSelectedPreview()
    {
        if (manager == null || selectedPreviewStage == null) return;

        string selectedId = manager.GetSelectedId(currentTab);
        ShopItemDefinition item = null;

        if (!string.IsNullOrEmpty(selectedId))
            item = manager.GetById(selectedId, currentTab);

        if (item == null)
        {
            var items = manager.GetItems(currentTab);
            foreach (var it in items)
            {
                if (it != null && manager.IsOwned(it.id, it.category)) { item = it; break; }
            }
            if (item == null && items.Count > 0) item = items[0];
        }

        if (item == null)
        {
            selectedPreviewStage.Clear();
            if (selectedPreviewRawImage != null) selectedPreviewRawImage.texture = null;
            if (selectedNameText != null) selectedNameText.text = "";
            return;
        }

        bool owned = manager.IsOwned(item.id, item.category);
        selectedPreviewStage.Show(item, lockedVisual: !owned);

        if (selectedPreviewRawImage != null)
            selectedPreviewRawImage.texture = selectedPreviewStage.Output;

        if (selectedNameText != null)
            selectedNameText.text = item.displayName;
    }

    private void OnCellClicked(ShopItemDefinition item)
    {
        if (manager == null || popup == null || item == null) return;

        bool owned = manager.IsOwned(item.id, item.category);
        popup.Open(manager, item, owned);
    }

    private void OnRandomUnlock()
    {
        if (manager == null) return;

        var unlocked = manager.TryRandomUnlock(currentTab);
        if (unlocked == null) return;

        if (confettiBurstEffect != null) confettiBurstEffect.PlayBurst();
        if (EffectSounds.Instance != null) EffectSounds.Instance.GetCharacterSound();

        if (popup != null) popup.Open(manager, unlocked, owned: true);
        RefreshSelectedPreview();
    }
}