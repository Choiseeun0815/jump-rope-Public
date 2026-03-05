using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Obstacle_log : MonoBehaviour, IObstacle
{
    [Header("Speed Settings")]
    [SerializeField] float randomMinSpeed = 4f;
    [SerializeField] float randomMaxSpeed = 10f;

    [Header("Scale Settings")]
    [SerializeField] float randomMinY = 0.8f;
    [SerializeField] float randomMaxY = 2.8f;

    private Rigidbody rb;

    private void Awake() { rb = GetComponent<Rigidbody>(); }

    private void OnEnable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Setup(Vector3 defaultDir, Transform playerTransform)
    {
        float randomY = Random.Range(randomMinY, randomMaxY);
        transform.localScale = new Vector3(transform.localScale.x, randomY, transform.localScale.z);

        float moveSpeed = Random.Range(randomMinSpeed, randomMaxSpeed);

        if (!GameManager.Instance.IsGameOver)
        {
            rb.linearVelocity = defaultDir * moveSpeed;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, defaultDir).normalized;
            rb.angularVelocity = rotationAxis * (360f * Mathf.Deg2Rad);
        }
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            GameManager.Instance.GameOver();
        }
    }
}