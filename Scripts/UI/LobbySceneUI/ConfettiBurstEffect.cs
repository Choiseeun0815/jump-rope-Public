using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class ConfettiBurstEffect : MonoBehaviour
{
    public GameObject confettiPrefab; 
    public Transform burstCenter;

    [Header("Burst Settings")]
    public int poolSize = 20;         
    public float minRadius = 150f;
    public float maxRadius = 400f;
    public float duration = 1.5f;

    [Header("Confetti Colors")]
    public Color[] colors = new Color[] { Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta }; 

    private List<GameObject> pool = new List<GameObject>();

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject piece = Instantiate(confettiPrefab, burstCenter);
            piece.SetActive(false);
            pool.Add(piece);
        }
    }

    public void PlayBurst()
    {
        foreach (GameObject piece in pool)
        {
            piece.SetActive(true);

            RectTransform rect = piece.GetComponent<RectTransform>();
            Image img = piece.GetComponent<Image>();

            rect.localScale = Vector3.zero;
            rect.anchoredPosition = Vector2.zero;

            Color randomColor = colors[Random.Range(0, colors.Length)];
            img.color = new Color(randomColor.r, randomColor.g, randomColor.b, 1f);

            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 targetPos = randomDirection * Random.Range(minRadius, maxRadius);

            targetPos.y -= Random.Range(50f, 150f);

            Sequence seq = DOTween.Sequence();

            seq.Join(rect.DOScale(Vector3.one * Random.Range(0.5f, 1.2f), duration * 0.2f).SetEase(Ease.OutBack));
            seq.Join(rect.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCirc));

            seq.Join(rect.DORotate(new Vector3(0, 0, Random.Range(-720f, 720f)), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

            seq.Insert(duration * 0.5f, img.DOFade(0f, duration * 0.5f));

            seq.OnComplete(() => piece.SetActive(false));
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}