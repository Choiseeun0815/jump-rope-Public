using UnityEngine;
using System.Collections.Generic;

public enum ObstacleType
{
    None,
    Rolling,
    Falling
}

public class ObstacleSpawner : MonoBehaviour
{
    public static ObstacleSpawner Instance;
    [SerializeField] private GameObject runnerParticle;

    [Header("랜덤 캐릭터(Animal_Up/Down) 풀 설정")]
    [SerializeField] private ShopCatalog characterCatalog;
    [SerializeField] private int randomCharacterPickCount = 3;
    [SerializeField] private int randomCharacterPoolSize = 10;
    [SerializeField] private Vector3 randomCharacterScale = new Vector3(0.7f, 0.7f, 0.7f);
    [SerializeField] private Vector3 runnerParticleLocalPos = new Vector3(0f, 0.1f, -1f);

    [Tooltip("생성되는 장애물 레이어(기본: Obstacle)")]
    [SerializeField] private string obstacleLayerName = "Obstacle";

    private readonly List<string> _randomCharacterPoolTags = new List<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [System.Serializable]
    public class SpawnData
    {
        [Header("기본 정보")]
        public string name;
        public GameObject prefab;
        public BoxCollider spawnArea;

        [Header("테마 설정")]
        public ObstacleType type;

        [Header("개별 설정")]
        public Vector3 direction;
        public int startScore;
        public float spawnInterval;

        [Header("(Animal 전용) 랜덤 캐릭터 풀 사용")]
        public bool useRandomCharacterPool = false;

        [Header("(Animal 전용) 이동/스턴")]
        public float moveSpeed = 10f;
        public float stunSeconds = 0.2f;

        [Tooltip("점수 1점당 줄어들 시간")]
        public float decayPerScore;
        [Tooltip("최소 스폰 간격")]
        public float minSpawnInterval;

        [HideInInspector] public float currentTimer = 0f;
        [HideInInspector] public float nextSpawnInterval = 0f;
        [HideInInspector] public bool isInitialized = false;
    }

    [Header("스폰 목록 설정")]
    public SpawnData[] spawnInfos;
    public PlayerController playerController;

    public MapThemeData currentTheme;

    private int currentActiveCount = 0;

    public void InitializeSpawner()
    {
        ApplyTheme();
        PrepareRandomCharacterPools();
        RegisterPools();
        ResetTimers();
    }

    void ApplyTheme()
    {
        if (currentTheme == null && DatabaseManager.Instance != null && DatabaseManager.Instance.currentData != null && MapManager.Instance != null)
        {
            string equippedID = DatabaseManager.Instance.currentData.equippedThemeID;
            if (!string.IsNullOrEmpty(equippedID))
            {
                currentTheme = MapManager.Instance.GetThemeDataByID(equippedID);
            }
        }

        if (currentTheme == null)
        {
            Debug.LogWarning("[ObstacleSpawner] 적용된 테마가 없습니다. 기본 프리팹을 사용합니다.");
        }
        else
        {
            Debug.Log($"테마 이름: {currentTheme.name}");
        }

        foreach (var data in spawnInfos)
        {
            if (data.useRandomCharacterPool)
                continue;

            if (currentTheme != null && data.type != ObstacleType.None)
            {
                GameObject themePrefab = currentTheme.GetPrefabByType(data.type);

                if (themePrefab != null)
                {
                    data.prefab = themePrefab;
                    data.name = themePrefab.name;
                    continue;
                }
            }

            if (data.prefab != null)
            {
                data.name = data.prefab.name;
            }
        }
    }

    void RegisterPools()
    {
        if (ObjectPool.Instance == null) return;

        foreach (var data in spawnInfos)
        {
            if (data.useRandomCharacterPool)
                continue;

            ObjectPool.Instance.RegisterPrefab(data.prefab, 10);
        }
    }

    void PrepareRandomCharacterPools()
    {
        _randomCharacterPoolTags.Clear();

        // 랜덤 캐릭터 풀을 쓰는 스폰이 하나도 없으면 스킵
        bool needRandomPool = false;
        foreach (var d in spawnInfos)
        {
            if (d.useRandomCharacterPool)
            {
                needRandomPool = true;
                break;
            }
        }
        if (!needRandomPool) return;

        if (characterCatalog == null)
        {
            Debug.LogError("[ObstacleSpawner] characterCatalog가 할당되지 않았습니다.");
            return;
        }

        var characters = characterCatalog.GetByCategory(ShopCategory.Character);
        if (characters == null || characters.Count == 0)
        {
            Debug.LogError("[ObstacleSpawner] characterCatalog에 Character 항목이 없습니다.");
            return;
        }

        // 중복 없이 랜덤 픽
        int pickCount = Mathf.Clamp(randomCharacterPickCount, 1, characters.Count);
        var picked = new List<ShopItemDefinition>(pickCount);
        var used = new HashSet<int>();
        while (picked.Count < pickCount)
        {
            int idx = Random.Range(0, characters.Count);
            if (!used.Add(idx)) continue;
            if (characters[idx] == null || characters[idx].prefab == null) continue;
            picked.Add(characters[idx]);
        }

        // 풀 등록
        foreach (var item in picked)
        {
            var prefab = item.prefab;
            if (prefab == null) continue;

            string tag = prefab.name;
            _randomCharacterPoolTags.Add(tag);

            ObjectPool.Instance.RegisterPrefab(prefab, randomCharacterPoolSize, (go) =>
            {
                // 스케일
                go.transform.localScale = randomCharacterScale;

                // 레이어
                SetLayerRecursively(go, LayerMask.NameToLayer(obstacleLayerName));

                // Particle (뒤에 붙이기)
                AttachRunnerParticle(go);

                // Collider/Rigidbody/Obstacle_animal 보장
                EnsureRunnerComponents(go);
            });
        }

        if (_randomCharacterPoolTags.Count == 0)
        {
            Debug.LogError("[ObstacleSpawner] 랜덤 캐릭터 풀 태그를 만들지 못했습니다. (prefab 누락 가능)");
        }
    }

    void ResetTimers()
    {
        currentActiveCount = 0;
        foreach (var data in spawnInfos)
        {
            data.nextSpawnInterval = data.spawnInterval;
            data.currentTimer = 0f;

            data.isInitialized = false;
        }
    }

    void Update()
    {
        if (!GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver) return;

        int currentScore = ScoreManager.Instance.currentScore;
        int maxAllowed = GetMaxActiveCount(currentScore);

        foreach (var data in spawnInfos)
        {
            if (currentScore >= data.startScore && !data.isInitialized)
            {
                data.isInitialized = true;

                bool isFirstSpawnSuccess = SpawnObstacle(data);

                if (isFirstSpawnSuccess)
                {
                    currentActiveCount++;
                }

                data.currentTimer = 0f;
                float initDecayedInterval = CalculateDecayedInterval(data, currentScore);
                data.nextSpawnInterval = Random.Range(initDecayedInterval, data.spawnInterval);
            }

            if (currentScore < data.startScore) continue;

            data.currentTimer += Time.deltaTime;

            if (data.currentTimer >= data.nextSpawnInterval)
            {
                if (currentActiveCount >= maxAllowed)
                {
                    continue;
                }

                bool isSpawnSuccess = SpawnObstacle(data);

                if (isSpawnSuccess)
                {
                    currentActiveCount++;
                    data.currentTimer = 0f;

                    float decayedInterval = CalculateDecayedInterval(data, currentScore);
                    float baseInterval = data.spawnInterval;
                    data.nextSpawnInterval = Random.Range(decayedInterval, baseInterval);
                }
                else
                {
                    data.currentTimer = 0f;
                    data.nextSpawnInterval = 0.5f;
                }
            }
        }
    }

    int GetMaxActiveCount(int score)
    {
        if (score < 50) return 1;
        else if (score < 100) return 2;
        else if (score < 150) return 3;
        return 4;
    }

    public void DecreaseActiveCount()
    {
        currentActiveCount--;
        if (currentActiveCount < 0) currentActiveCount = 0;

        // 디버그 로그 추가 (필요시)
        // Debug.Log($"[ObstacleSpawner] Active count decreased: {currentActiveCount}");
    }

    float CalculateDecayedInterval(SpawnData data, int currentScore)
    {
        int scoreGained = currentScore - data.startScore;
        float timeReduction = scoreGained * data.decayPerScore;
        float reducedTime = data.spawnInterval - timeReduction;
        return Mathf.Max(reducedTime, data.minSpawnInterval);
    }

    bool SpawnObstacle(SpawnData data)
    {
        string poolTag;
        if (data.useRandomCharacterPool)
        {
            if (_randomCharacterPoolTags.Count == 0)
            {
                Debug.LogError("[ObstacleSpawner] 랜덤 캐릭터 풀이 비어있습니다. InitializeSpawner 순서를 확인하세요.");
                return false;
            }
            poolTag = _randomCharacterPoolTags[Random.Range(0, _randomCharacterPoolTags.Count)];
        }
        else
        {
            if (data.prefab == null)
            {
                Debug.LogError($"[ObstacleSpawner] {data.name}의 프리팹이 null입니다!");
                return false;
            }
            poolTag = data.name;
        }

        GameObject obj = ObjectPool.Instance.SpawnFromPool(poolTag);

        if (obj == null)
        {
            Debug.LogWarning($"[ObstacleSpawner] 풀에서 {poolTag}을 가져올 수 없습니다.");
            return false;
        }

        Bounds bounds = data.spawnArea.bounds;
        float randX = Random.Range(bounds.min.x, bounds.max.x);
        float randZ = Random.Range(bounds.min.z, bounds.max.z);

        obj.transform.position = new Vector3(randX, bounds.center.y, randZ);

        // 혹시 런타임 중 레이어가 바뀌는 경우 대비(안전)
        SetLayerRecursively(obj, LayerMask.NameToLayer(obstacleLayerName));

        // Animal이면 스턴/속도 값 주입
        if (data.useRandomCharacterPool)
        {
            // 컴포넌트가 없으면 붙이고 값 적용
            EnsureRunnerComponents(obj);
            var oa = obj.GetComponent<Obstacle_animal>();
            if (oa != null)
            {
                oa.MoveSpeed = data.moveSpeed;
                oa.StunSeconds = data.stunSeconds;
            }
        }

        obj.SetActive(true);

        if (obj.TryGetComponent<IObstacle>(out var obstacle))
        {
            Transform pTransform = playerController != null ? playerController.transform : null;
            obstacle.Setup(data.direction, pTransform);
        }

        return true;
    }

    void AttachRunnerParticle(GameObject root)
    {
        if (runnerParticle == null || root == null) return;

        // 이미 붙어있으면 중복 방지
        Transform existing = root.transform.Find(runnerParticle.name);
        if (existing != null) return;

        var p = Instantiate(runnerParticle, root.transform);
        p.name = runnerParticle.name;
        p.transform.localPosition = runnerParticleLocalPos;
        p.transform.localRotation = Quaternion.identity;
        p.transform.localScale = Vector3.one;
    }

    void EnsureRunnerComponents(GameObject go)
    {
        if (go == null) return;

        // BoxCollider
        var col = go.GetComponent<BoxCollider>();
        if (col == null) col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.5f, 0);

        // Rigidbody
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.mass = 50f;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        // Obstacle_animal
        if (go.GetComponent<Obstacle_animal>() == null)
            go.AddComponent<Obstacle_animal>();
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0) return;
        root.layer = layer;
        foreach (Transform t in root.transform)
        {
            if (t == null) continue;
            SetLayerRecursively(t.gameObject, layer);
        }
    }
}