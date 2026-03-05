using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TrapArea : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform fill;   // 자식 Fill(Quad) Transform

    [Header("Fill Scale")]
    [SerializeField] private float targetScaleXY = 1f; // XZ를 0 -> 1로 채움
    [SerializeField] private bool deactivateOnComplete = true;

    private void Reset()
    {
        // 같은 오브젝트(=Border 루트) 아래에서 Fill 찾기 시도
        if (fill == null)
        {
            var t = transform.Find("Fill");
            if (t != null) fill = t;
        }
    }

    /// <summary>
    /// duration초 동안 Fill을 0 -> 1로 채운 뒤(스케일),
    /// 완료되면(옵션) 이 텔레그래프 루트를 비활성화합니다.
    /// </summary>
    public async UniTask PlayFill(float duration, CancellationToken token)
    {
        if (fill == null)
        {
            Debug.LogWarning("[TrapArea] Fill reference is missing.");
            return;
        }

        // 시작 상태
        gameObject.SetActive(true);
        SetFill01(0f);

        if (duration <= 0f)
        {
            SetFill01(1f);
            if (deactivateOnComplete) gameObject.SetActive(false);
            return;
        }

        float t = 0f;
        while (t < duration)
        {
            token.ThrowIfCancellationRequested();

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            SetFill01(a);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        SetFill01(1f);

        // 끝나면 텔레그래프 전체 끄기(=Border 남는 문제 해결)
        if (deactivateOnComplete)
            gameObject.SetActive(false);
    }

    public void ResetFillAndHide()
    {
        if (fill != null) SetFill01(0f);
        gameObject.SetActive(false);
    }

    private void SetFill01(float a01)
    {
        float s = Mathf.Lerp(0f, targetScaleXY, a01);

        // Fill이 바닥에 깔린 원
        Vector3 ls = fill.localScale;
        ls.x = s;
        ls.y = s;
        fill.localScale = ls;
    }
}