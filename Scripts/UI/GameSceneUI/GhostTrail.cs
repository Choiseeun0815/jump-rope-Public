using UnityEngine;
using System.Collections.Generic;

public class GhostTrail : MonoBehaviour
{
    [Header("References")]
    public GameObject ghostPrefab;

    [Header("Settings")]
    public float spawnInterval = 0.1f;      // 잔상 생성 간격
    public float fadeDuration = 0.5f;       // 잔상 지속 시간
    public int poolSize = 10;               // 잔상 최대 개수
    public float scaleMultiplier = 0.9f;    // 잔상 크기 배율

    [Header("Color Settings")]
    public bool useCustomColor = false;
    [ColorUsage(true, true)] public Color customTrailColor = Color.white;

    private SkinnedMeshRenderer targetRenderer;
    private bool isFeverCombo = false;
    private float spawnTimer = 0f;

    private Queue<ActiveGhost> ghostPool = new Queue<ActiveGhost>();
    private List<ActiveGhost> activeGhosts = new List<ActiveGhost>();
    private GameObject poolHolder;

    private int baseColorID;
    private int colorID;

    private class ActiveGhost
    {
        public GameObject go;
        public MeshRenderer meshRenderer;
        public MaterialPropertyBlock propBlock;
        public float timeAlive;

        public Color originalColor;
        public int activeColorPropertyID;
    }

    public void Init(SkinnedMeshRenderer newRenderer)
    {
        targetRenderer = newRenderer;

        baseColorID = Shader.PropertyToID("_BaseColor");
        colorID = Shader.PropertyToID("_Color");

        DeactivateAllGhosts();
        InitializePool();
    }

    private void InitializePool()
    {
        if (targetRenderer == null || ghostPrefab == null) return;

        if (poolHolder == null)
            poolHolder = new GameObject($"GhostTrail_pool_{gameObject.name}");

        Mesh sharedMesh = targetRenderer.sharedMesh;

        int amountToCreate = poolSize - ghostPool.Count;
        for (int i = 0; i < amountToCreate; i++)
        {
            GameObject obj = Instantiate(ghostPrefab, poolHolder.transform);
            obj.name = $"Ghost_{ghostPool.Count}";

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = sharedMesh;

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            obj.SetActive(false);

            ActiveGhost newGhostData = new ActiveGhost
            {
                go = obj,
                meshRenderer = mr,
                propBlock = new MaterialPropertyBlock(),
                timeAlive = 0f
            };

            ghostPool.Enqueue(newGhostData);
        }
    }

    private void DeactivateAllGhosts()
    {
        foreach (var ghost in activeGhosts)
        {
            ghost.go.SetActive(false);
            ghostPool.Enqueue(ghost);
        }
        activeGhosts.Clear();
    }

    public void SetTrailColor(Color newColor)
    {
        useCustomColor = true;
        customTrailColor = newColor;
    }
    void Update()
    {
        if (isFeverCombo)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                SpawnGhost();
                spawnTimer = 0f;
            }
        }

        FadeAndRecycleGhosts();
    }

    void SpawnGhost()
    {
        if (targetRenderer == null || ghostPool.Count == 0) return;

        ActiveGhost ghost = ghostPool.Dequeue();

        ghost.go.transform.position = targetRenderer.transform.position;
        ghost.go.transform.rotation = targetRenderer.transform.rotation;
        ghost.go.transform.localScale = targetRenderer.transform.lossyScale * scaleMultiplier;

        MeshFilter mf = ghost.go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != targetRenderer.sharedMesh)
        {
            mf.sharedMesh = targetRenderer.sharedMesh;
        }

        ghost.timeAlive = 0f;

        if (ghost.meshRenderer != null)
        {
            ghost.meshRenderer.GetPropertyBlock(ghost.propBlock);

            if (ghost.meshRenderer.sharedMaterial.HasProperty(baseColorID))
                ghost.activeColorPropertyID = baseColorID;
            else
                ghost.activeColorPropertyID = colorID;

            if (ghost.meshRenderer.sharedMaterial.HasProperty(ghost.activeColorPropertyID))
            {
                if (useCustomColor)
                {
                    ghost.originalColor = customTrailColor;
                }
                else
                {
                    ghost.originalColor = ghost.meshRenderer.sharedMaterial.GetColor(ghost.activeColorPropertyID);
                }
                ghost.propBlock.SetColor(ghost.activeColorPropertyID, ghost.originalColor);
                ghost.meshRenderer.SetPropertyBlock(ghost.propBlock);
            }

            ghost.meshRenderer.SetPropertyBlock(ghost.propBlock);
        }

        ghost.go.SetActive(true);
        activeGhosts.Add(ghost);
    }

    void FadeAndRecycleGhosts()
    {
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            ActiveGhost ghost = activeGhosts[i];
            ghost.timeAlive += Time.deltaTime;

            float progress = ghost.timeAlive / fadeDuration;

            if (progress >= 1f)
            {
                ghost.go.SetActive(false);
                ghostPool.Enqueue(ghost); 
                activeGhosts.RemoveAt(i);
            }
            else
            {
                if (ghost.meshRenderer != null)
                {
                    Color fadeColor = ghost.originalColor;
                    fadeColor.a = Mathf.Lerp(ghost.originalColor.a, 0f, progress);

                    ghost.propBlock.SetColor(ghost.activeColorPropertyID, fadeColor);
                    ghost.meshRenderer.SetPropertyBlock(ghost.propBlock);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (poolHolder != null)
            Destroy(poolHolder);
    }

    public void StartFeverEffect()
    {
        isFeverCombo = true;
        spawnTimer = 0f;
    }

    public void StopFeverEffect()
    {
        isFeverCombo = false;
    }
}