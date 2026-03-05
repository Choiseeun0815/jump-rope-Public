using UnityEngine;

public class ObstacleDisalbeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // ✅ 풀로 반환만 한다. (여기서 SetActive(false) 금지)
            ObjectPool.Instance.ReturnToPool(other.gameObject.name, other.gameObject);

            // ✅ ActiveCount 줄이는 것도 여기서 한다(각 Obstacle OnDisable에서 빼도 됨)
            if (ObstacleSpawner.Instance != null)
                ObstacleSpawner.Instance.DecreaseActiveCount();
        }

    }
}
