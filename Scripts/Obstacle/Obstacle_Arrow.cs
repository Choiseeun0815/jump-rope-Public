using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle_Arrow : MonoBehaviour, IObstacle
{
    [Header("Speed Settings")]
    [SerializeField] float randomMinSpeed = 5f;
    [SerializeField] float randomMaxSpeed = 10f;

    [Header("Movement Settings")]
    float moveSpeed = 8f;
    [SerializeField] float rotationSpeed = 360f; // 초당 회전 각도

    [Header("Telegraph")]
    [SerializeField] private float minTimeShowArea = 1f;
    [SerializeField] private float maxTimeShowArea = 2f;

    [SerializeField] private TrapArea trapArea;     // Border 루트에 붙은 TrapArea

    private Rigidbody rb;
    private CancellationTokenSource cts;

    private GameManager gameManager;
    private ObstacleSpawner spawner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        spawner = ObstacleSpawner.Instance;
    }

    private void OnEnable()
    {
        StopMotion();
    }

    public void Setup(Vector3 defaultDir, Transform playerTransform)
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (gameManager.IsGameOver) return;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        StopMotion();

        ShowArrowArea(cts.Token).Forget();
    }

    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        StopMotion();

        if (trapArea != null)
            trapArea.ResetFillAndHide();
    }

    private void StopMotion()
    {
        if (rb == null) return;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = true;
    }

    private void StartFall()
    {
        if (rb == null) return;
        rb.isKinematic = false;

        rb.linearVelocity = Vector3.down * moveSpeed;
        rb.angularVelocity = Vector3.up * (rotationSpeed * Mathf.Deg2Rad);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            StopMotion();
            gameManager.GameOver();
        }
    }

    private async UniTaskVoid ShowArrowArea(CancellationToken token)
    {
        moveSpeed = Random.Range(randomMinSpeed, randomMaxSpeed);
        float speedRatio = Mathf.InverseLerp(randomMinSpeed, randomMaxSpeed, moveSpeed);

        float t = Mathf.Lerp(maxTimeShowArea, minTimeShowArea, speedRatio);

        if (trapArea != null)
        {
            trapArea.gameObject.SetActive(true);
            await trapArea.PlayFill(t, token);
        }
        else
        {
            await UniTask.Delay((int)(t * 1000f), cancellationToken: token);
        }

        if (token.IsCancellationRequested) return;
        if (gameManager != null && gameManager.IsGameOver) return;

        StartFall();
    }
}