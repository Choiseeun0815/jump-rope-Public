using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CreatePlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ShopCatalog characterCatalog;
    [SerializeField] private Transform playerRoot;

    [Header("Effects")]
    [SerializeField] private GhostTrail ghostTrail;

    private void Awake()
    {
        if (playerRoot == null || characterCatalog == null) return;

        CreateFromEquippedIdAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid CreateFromEquippedIdAsync(CancellationToken ct)
    {
        float timeout = 3f;

        while ((DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (ct.IsCancellationRequested) return;

        string equippedId = "Char_default";
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            if (!string.IsNullOrEmpty(DatabaseManager.Instance.currentData.equippedCharID))
                equippedId = DatabaseManager.Instance.currentData.equippedCharID;
        }

        ShopItemDefinition def = characterCatalog.GetById(equippedId);

        if (def == null || def.prefab == null)
        {
            Debug.LogWarning($"equippedCharID '{equippedId}' 를 Catalog에서 못 찾음. fallback 처리");
            def = characterCatalog.GetById("Char_default");

            if (def == null || def.prefab == null)
            {
                if (characterCatalog.items != null && characterCatalog.items.Count > 0)
                    def = characterCatalog.items[0];
            }
        }

        if (def == null || def.prefab == null)
        {
            Debug.LogError("CreatePlayer 실패: 생성할 캐릭터 prefab을 찾을 수 없음");
            return;
        }

        for (int i = playerRoot.childCount - 1; i >= 0; i--)
            Destroy(playerRoot.GetChild(i).gameObject);

        GameObject go = Instantiate(def.prefab, playerRoot);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (ghostTrail != null && renderer != null)
        {
            ghostTrail.Init(renderer);
            ghostTrail.SetTrailColor(def.trailColor);
        }
    }
}