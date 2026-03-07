using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TrapArea : MonoBehaviour
{
    // 바닥 경고 영역의 Fill 역할을 하는 자식 Transform
    // 보통 Border 오브젝트 아래의 "Fill"을 연결해서 사용
    [SerializeField] private Transform fill;

    [Header("Fill Scale")]
    // Fill이 최대로 채워졌을 때의 X/Y 스케일 값
    [SerializeField] private float targetScaleXY = 1f;

    // Fill 연출이 끝난 뒤 이 텔레그래프 오브젝트를 비활성화할지 여부
    [SerializeField] private bool deactivateOnComplete = true;

    private void Reset()
    {
        // Inspector에서 fill을 직접 연결하지 않았을 경우
        // 같은 오브젝트 하위에서 "Fill" 이름의 자식을 자동으로 찾아 연결 시도
        if (fill == null)
        {
            var t = transform.Find("Fill");
            if (t != null) fill = t;
        }
    }

    // duration 동안 Fill을 0 -> 1 비율로 채우는 연출
    // token이 취소되면 즉시 중단되며,
    // 옵션에 따라 완료 후 전체 오브젝트를 비활성화함
    public async UniTask PlayFill(float duration, CancellationToken token)
    {
        // Fill 참조가 없으면 연출 불가
        if (fill == null)
        {
            Debug.LogWarning("[TrapArea] Fill reference is missing.");
            return;
        }

        // 연출 시작 전 오브젝트를 켜고 Fill을 0 상태로 초기화
        gameObject.SetActive(true);
        SetFill01(0f);

        // duration이 0 이하이면 즉시 100% 채운 상태로 처리
        if (duration <= 0f)
        {
            SetFill01(1f);

            if (deactivateOnComplete)
                gameObject.SetActive(false);

            return;
        }

        float t = 0f;

        // 지정 시간 동안 매 프레임 Fill 비율을 증가
        while (t < duration)
        {
            // 외부에서 취소 요청이 들어오면 즉시 예외 발생 후 종료
            token.ThrowIfCancellationRequested();

            // 시간 누적
            t += Time.deltaTime;

            // 현재 진행도를 0~1 범위로 변환
            float a = Mathf.Clamp01(t / duration);

            // 진행도에 맞게 Fill 크기 적용
            SetFill01(a);

            // 다음 프레임까지 대기
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        // 마지막 프레임 보정
        SetFill01(1f);

        // 연출 완료 후 텔레그래프 전체 비활성화
        // Border만 남아 보이는 문제를 방지
        if (deactivateOnComplete)
            gameObject.SetActive(false);
    }

    // Fill 상태를 초기화하고 오브젝트를 숨김
    // 풀링 재사용 시 이전 연출 상태가 남지 않도록 사용
    public void ResetFillAndHide()
    {
        if (fill != null)
            SetFill01(0f);

        gameObject.SetActive(false);
    }

    // 0~1 진행도를 실제 Fill 스케일 값으로 반영
    private void SetFill01(float a01)
    {
        // 0이면 안 보이는 상태, 1이면 targetScaleXY 크기까지 확장
        float s = Mathf.Lerp(0f, targetScaleXY, a01);

        // Fill이 바닥에 깔린 원형/평면 오브젝트라고 가정하고
        // X, Y 스케일을 함께 조절해 채워지는 것처럼 보이게 처리
        Vector3 ls = fill.localScale;
        ls.x = s;
        ls.y = s;
        fill.localScale = ls;
    }
}