using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleTestHarness : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject rollingPrefab;
    public GameObject fallingPrefab;

    [Header("Spawn Areas (각 타입별로 따로)")]
    public BoxCollider rollingSpawnArea;
    public BoxCollider fallingSpawnArea;

    [Header("Optional: Player (Setup에 넘길 대상)")]
    public Transform player;

    [Header("Directions (Setup에 전달)")]
    public Vector3 rollingDirection = new Vector3(0, 0, -1);
    public Vector3 fallingDirection = new Vector3(0, -1, 0);

    [Header("Keys")]
    public Key spawnRollingKey = Key.R;
    public Key spawnFallingKey = Key.F;

    [Header("Auto Spawn")]
    public bool autoSpawn = false;
    public float autoInterval = 1.0f;
    public enum AutoType { Rolling, Falling, Alternate, Both }
    public AutoType autoType = AutoType.Alternate;

    [Header("Pooling")]
    [Tooltip("프로젝트에 ObjectPool.Instance가 있으면 자동 사용. 없으면 Instantiate로 생성.")]
    public bool usePoolIfAvailable = true;

    private float timer;
    private bool toggle;

    private void Awake()
    {
        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.transform;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[spawnRollingKey].wasPressedThisFrame)
            SpawnRolling();

        if (Keyboard.current[spawnFallingKey].wasPressedThisFrame)
            SpawnFalling();

        if (!autoSpawn) return;

        timer += Time.deltaTime;
        if (timer < autoInterval) return;
        timer = 0f;

        switch (autoType)
        {
            case AutoType.Rolling:
                SpawnRolling();
                break;
            case AutoType.Falling:
                SpawnFalling();
                break;
            case AutoType.Alternate:
                if (toggle) SpawnRolling();
                else SpawnFalling();
                toggle = !toggle;
                break;
            case AutoType.Both:
                SpawnRolling();
                SpawnFalling();
                break;
        }
    }

    public void SpawnRolling()
    {
        Spawn(rollingPrefab, rollingSpawnArea, rollingDirection, "Rolling");
    }

    public void SpawnFalling()
    {
        Spawn(fallingPrefab, fallingSpawnArea, fallingDirection, "Falling");
    }

    private void Spawn(GameObject prefab, BoxCollider area, Vector3 direction, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[ObstacleTestHarness] {label} prefab이 비었습니다.");
            return;
        }
        if (area == null)
        {
            Debug.LogWarning($"[ObstacleTestHarness] {label} SpawnArea(BoxCollider)를 연결하세요.");
            return;
        }

        Vector3 pos = GetRandomPointInBox(area);

        GameObject obj = null;

        // Pool 사용 시도
        if (usePoolIfAvailable && ObjectPool.Instance != null)
        {
            // 프로젝트 풀 구현이 다르면 여기서 컴파일 에러가 날 수 있음
            obj = ObjectPool.Instance.SpawnFromPool(prefab.name);
        }

        // Pool 실패/없으면 Instantiate
        if (obj == null)
        {
            obj = Instantiate(prefab);
        }

        obj.transform.position = pos;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);

        // Setup 호출(있으면)
        if (obj.TryGetComponent<IObstacle>(out var obstacle))
        {
            obstacle.Setup(direction, player);
        }

        Debug.Log($"[ObstacleTestHarness] Spawned {label} at {pos}");
    }

    private static Vector3 GetRandomPointInBox(BoxCollider box)
    {
        var b = box.bounds;
        float x = Random.Range(b.min.x, b.max.x);
        float y = b.center.y;
        float z = Random.Range(b.min.z, b.max.z);
        return new Vector3(x, y, z);
    }

    // UI 버튼 연결용
    public void UI_SpawnRolling() => SpawnRolling();
    public void UI_SpawnFalling() => SpawnFalling();
}