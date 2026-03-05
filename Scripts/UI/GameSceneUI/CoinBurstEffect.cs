using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class CoinBurstEffect : MonoBehaviour
{
    public GameObject coinUIPrefab;

    [Tooltip("폭죽이 터질 중심 위치")]
    public Transform burstCenter;

    [Header("Burst Settings")]
    public int poolSize = 20;           // 한 번에 터뜨릴 최대 동전 개수 (풀 사이즈)
    public float minRadius = 150f;      // 퍼져나갈 최소 거리
    public float maxRadius = 350f;      // 퍼져나갈 최대 거리
    public float duration = 1.2f;       // 애니메이션 지속 시간

    private List<GameObject> coinPool = new List<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject coin = Instantiate(coinUIPrefab, burstCenter);
            coin.SetActive(false);
            coinPool.Add(coin);
        }
    }

    public void PlayBurst()
    {
        foreach (GameObject coin in coinPool)
        {
            coin.SetActive(true);

            RectTransform rect = coin.GetComponent<RectTransform>();
            Image img = coin.GetComponent<Image>();

            rect.localScale = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f); // 투명도 100%로 복구

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 targetPos = randomDirection * Random.Range(minRadius, maxRadius);

            Sequence seq = DOTween.Sequence();

            seq.Join(rect.DOScale(Vector3.one * Random.Range(0.8f, 1.2f), duration * 0.3f).SetEase(Ease.OutBack));
            seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCirc));
            seq.Join(rect.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            seq.Insert(duration * 0.5f, img.DOFade(0f, duration * 0.5f));

            seq.OnComplete(() => coin.SetActive(false));
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}