using UnityEngine;
using System.Collections.Generic;

public class BonusItemSpawner : MonoBehaviour
{
    public GameObject[] bonusPrefabs;
    public BoxCollider spawnArea;
     
    [Header("Spawn Settings")] //n초 뒤에 소환
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 10f;

    public int minScoreToSpawn = 10;  // 10점 이후부터 스폰

    private float timer;
    private float currentInterval;

    private List<GameObject> activeItems = new List<GameObject>();

    private void Start()
    {
        SetNextInterval();
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver)
        {
            if (activeItems.Count > 0) DeactivateAllItems();
            return;
        }

        if (!GameManager.Instance.IsGameStarted) return;

        if (ScoreManager.Instance.currentScore < minScoreToSpawn) return;

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            SpawnBonusItem();
            timer = 0;
            SetNextInterval();
        }
    }

    void SetNextInterval()
    {
        currentInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void SpawnBonusItem()
    {
        if (bonusPrefabs.Length == 0) return;

        activeItems.RemoveAll(x => x == null || !x.activeSelf);

        int randomIndex = Random.Range(0, bonusPrefabs.Length);
        GameObject selectedPrefab = bonusPrefabs[randomIndex];

        GameObject item = ObjectPool.Instance.SpawnFromPool(selectedPrefab.name);

        if (item == null) return;

        Bounds bounds = spawnArea.bounds;
        float randX = Random.Range(bounds.min.x, bounds.max.x);
        float randZ = Random.Range(bounds.min.z, bounds.max.z);

        float spawnY = bounds.center.y;
        item.transform.position = new Vector3(randX, spawnY, randZ);
        item.transform.rotation = Quaternion.identity;
        item.SetActive(true);

        activeItems.Add(item);
    }

    void DeactivateAllItems()
    {
        foreach (var item in activeItems)
        {
            if (item != null && item.activeSelf)
            {
                item.SetActive(false);
            }
        }
        activeItems.Clear();
    }
}