using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CreatePlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ShopCatalog characterCatalog; // 캐릭터 프리팹을 찾기 위한 카탈로그
    [SerializeField] private Transform playerRoot;         // 캐릭터가 생성될 부모 Transform

    [Header("Effects")]
    [SerializeField] private GhostTrail ghostTrail;        // 캐릭터 잔상 효과 제어용

    private const string DefaultCharacterId = "Char_default";

    private void Awake()
    {
        // 필수 참조가 없으면 실행하지 않음
        if (playerRoot == null || characterCatalog == null)
            return;

        // 파괴 시 자동 취소되도록 OnDestroy 토큰 전달
        CreateFromEquippedIdAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // Firebase DB에 현재 장착된 캐릭터 ID를 기준으로 플레이어 캐릭터를 생성
    private async UniTaskVoid CreateFromEquippedIdAsync(CancellationToken ct)
    {
        // Firebase DB 데이터가 아직 준비되지 않았을 수 있으므로 잠깐 대기
        await WaitForDatabaseAsync(3f, ct);

        // 대기 중 오브젝트가 파괴되었으면 중단
        if (ct.IsCancellationRequested)
            return;

        // Firebase DB에 장착된 캐릭터 ID 가져오기 (없으면 기본 캐릭터 사용)
        string equippedId = GetEquippedCharacterId();

        // 카탈로그 SO에서 해당 캐릭터 찾기
        ShopItemDefinition def = FindCharacterDefinition(equippedId);

        // 최종적으로도 못 찾으면 생성 중단
        if (def == null || def.prefab == null)
        {
            Debug.LogError("CreatePlayer 실패: 생성할 캐릭터 prefab을 찾을 수 없음");
            return;
        }

        // 기존 playerRoot 하위 캐릭터 제거
        ClearCurrentPlayer();

        // 새 캐릭터 생성
        GameObject go = Instantiate(def.prefab, playerRoot);
        ResetLocalTransform(go.transform);

        // 잔상 효과 초기화
        SetupGhostTrail(go, def);
    }

    // DatabaseManager와 currentData가 준비될 때까지 대기
    // timeout이 지나면 다음 로직으로 진행 -> 무한으로 대기하는 것을 방지
    private async UniTask WaitForDatabaseAsync(float timeout, CancellationToken ct)
    {
        float remain = timeout;

        while ((DatabaseManager.Instance == null || DatabaseManager.Instance.currentData == null) && remain > 0f)
        {
            remain -= Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    // 현재 DB에 장착된 캐릭터 ID를 반환
    // 없으면 기본 캐릭터 ID 반환
    private string GetEquippedCharacterId()
    {
        if (DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null)
        {
            string equippedId = DatabaseManager.Instance.currentData.equippedCharID;

            if (!string.IsNullOrEmpty(equippedId))
                return equippedId;
        }

        return DefaultCharacterId;
    }

    // 1차: Firebase DB에 장착된 캐릭터 ID로 캐릭터를 찾음
    // 2차: 없으면 기본 캐릭터 -> 첫 번째 아이템 순으로 처리
    private ShopItemDefinition FindCharacterDefinition(string equippedId)
    {
        ShopItemDefinition def = characterCatalog.GetById(equippedId);

        if (def != null && def.prefab != null)
            return def;

        Debug.LogWarning($"equippedCharID '{equippedId}' 를 Catalog에서 못 찾음. fallback 처리");

        // 1순위: 기본 캐릭터
        def = characterCatalog.GetById(DefaultCharacterId);
        if (def != null && def.prefab != null)
            return def;

        // 2순위: 카탈로그 첫 번째 아이템
        if (characterCatalog.items != null && characterCatalog.items.Count > 0)
            return characterCatalog.items[0];

        return null;
    }

    // playerRoot 아래 기존 캐릭터 오브젝트 전부 제거
    private void ClearCurrentPlayer()
    {
        for (int i = playerRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(playerRoot.GetChild(i).gameObject);
        }
    }

    // 생성된 캐릭터의 GameScene용 Transform 초기화
    private void ResetLocalTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    // 캐릭터의 SkinnedMeshRenderer를 찾아 GhostTrail 초기화 (캐릭터별 GhostTrail 색상 적용)
    private void SetupGhostTrail(GameObject go, ShopItemDefinition def)
    {
        if (ghostTrail == null || go == null || def == null)
            return;

        var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null)
            return;

        ghostTrail.Init(renderer);
        ghostTrail.SetTrailColor(def.trailColor);
    }
}