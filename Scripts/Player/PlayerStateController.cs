using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 플레이어 상태 전용 컴포넌트
// 현재는 스턴만 관리하지만, 이후 무적/슬로우 등의 상태도 확장 가능
public sealed class PlayerStateController : MonoBehaviour
{
    private CancellationTokenSource _stunCts;

    /// <summary>현재 스턴 상태인지</summary>
    public bool IsStunned { get; private set; }

    private void OnDestroy()
    {
        _stunCts?.Cancel();
        _stunCts?.Dispose();
    }

    // 지정 시간 동안 스턴 상태 적용
    // 이미 스턴 중이면 중복 적용하지 않음
    public void ApplyStun(float seconds)
    {
        if (IsStunned)
            return;

        IsStunned = true;

        _stunCts?.Cancel();
        _stunCts?.Dispose();
        _stunCts = new CancellationTokenSource();

        ReleaseStunAfter(seconds, _stunCts.Token).Forget();
    }

    // 지정 시간이 지나면 스턴 해제
    private async UniTaskVoid ReleaseStunAfter(float seconds, CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
        }
        catch
        {
            return;
        }

        IsStunned = false;
    }

    // 상태 강제 초기화
    public void ResetState()
    {
        _stunCts?.Cancel();
        _stunCts?.Dispose();
        _stunCts = null;

        IsStunned = false;
    }
}