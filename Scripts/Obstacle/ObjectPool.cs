using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [Header("풀링할 프리팹들 등록")]
    public List<GameObject> prefabsToPool;
    public int defaultPoolSize = 10;

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (GameObject prefab in prefabsToPool)
        {
            RegisterPrefab(prefab, defaultPoolSize);
        }
    }

    public void RegisterPrefab(GameObject prefab, int size = 10, System.Action<GameObject> onCreate = null)
    {
        if (prefab == null) return;

        string key = prefab.name;

        if (poolDictionary.ContainsKey(key))
        {
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();

        GameObject poolHolder = new GameObject(key + "_Pool");
        poolHolder.transform.parent = transform;

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = key; // 생성된 오브젝트 이름 통일

            // 풀에 들어가기 전 초기화(스케일/레이어/컴포넌트 추가 등)
            onCreate?.Invoke(obj);

            obj.SetActive(false);
            obj.transform.parent = poolHolder.transform;
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(key, objectPool);
    }

    public GameObject SpawnFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = GetActiveObject(tag);
        return objectToSpawn;
    }

    GameObject GetActiveObject(string tag)
    {
        Queue<GameObject> pool = poolDictionary[tag];

        if (pool.Count == 0)
        {
            Debug.LogWarning($"Pool {tag} is empty. All objects are in use.");
            return null;
        }

        GameObject obj = pool.Dequeue();
        return obj;
    }
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag)) return;

        if (obj.activeSelf) obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }

    public void DeactivateAllObjects()
    {
        foreach (var queue in poolDictionary.Values)
        {
            foreach (var obj in queue)
                obj.SetActive(false);
        }
    }
}