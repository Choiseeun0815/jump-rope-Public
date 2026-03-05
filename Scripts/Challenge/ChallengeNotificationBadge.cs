using UnityEngine;

public class ChallengeNotificationBadge : MonoBehaviour
{
    [Tooltip("켜고 끌 알림 아이콘")]
    [SerializeField] private GameObject badgeObject;

    private void Start()
    {
        if (badgeObject == null) badgeObject = this.gameObject;

        UpdateBadge();

        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.OnChanged += UpdateBadge;
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnChanged += UpdateBadge;
        }
    }

    private void OnDestroy()
    {
        if (ChallengeManager.Instance != null)
        {
            ChallengeManager.Instance.OnChanged -= UpdateBadge;
        }

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnChanged -= UpdateBadge;
        }
    }

    private void UpdateBadge()
    {
        if (badgeObject == null) badgeObject = this.gameObject;

        if (ChallengeManager.Instance == null)
        {
            badgeObject.SetActive(false);
            return;
        }

        bool hasReward = ChallengeManager.Instance.HasAnyClaimableReward();
        badgeObject.SetActive(hasReward);
    }
}