using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 애니메이션 전용 컨트롤러
/// - Idle / Hit 는 고정 상태명 사용
/// - locomotion은 clip 이름을 보고 Run / Walk / Fly Inplace 중 자동 선택
/// - 같은 상태 중복 재생 방지
/// - RuntimeAnimatorController가 바뀔 때만 locomotion 캐시 재구성
/// </summary>
public sealed class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private const int Layer = 0;

    // 고정 상태명
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int HitHash = Animator.StringToHash("Hit");

    // 이동 애니메이션 후보
    private static readonly string[] RunCandidates = { "Run" };
    private static readonly string[] WalkCandidates = { "Walk" };
    private static readonly string[] FlyCandidates = { "Fly Inplace" };

    // 현재 연결된 AnimatorController 캐시
    private RuntimeAnimatorController _cachedController;

    // 현재 Controller에 들어있는 clip 이름 캐시
    private HashSet<string> _clipNames;

    // 최종 선택된 locomotion hash
    private int _locoHash;

    // 마지막으로 재생한 hash
    // 같은 애니메이션 중복 재생 방지용
    private int _lastPlayedHash;

    private void Start()
    {
        EnsureAnimator();
        BuildLocomotionCacheIfNeeded(force: true);
    }

    /// <summary>
    /// Animator가 비어 있으면 자식에서 자동 탐색
    /// </summary>
    private void EnsureAnimator()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// locomotion 후보를 1회 캐시
    /// 컨트롤러가 바뀌지 않았다면 다시 찾지 않음
    /// 우선순위: Run > Walk > Fly Inplace
    /// </summary>
    private void BuildLocomotionCacheIfNeeded(bool force)
    {
        if (!animator || animator.runtimeAnimatorController == null) return;

        var ctrl = animator.runtimeAnimatorController;

        if (!force && ctrl == _cachedController && _clipNames != null)
            return;

        _cachedController = ctrl;

        _clipNames = new HashSet<string>();
        var clips = ctrl.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                _clipNames.Add(clips[i].name);
        }

        string locoName =
            PickFirstExisting(RunCandidates) ??
            PickFirstExisting(WalkCandidates) ??
            PickFirstExisting(FlyCandidates) ??
            "Run";

        _locoHash = Animator.StringToHash(locoName);

        // 컨트롤러가 바뀌면 마지막 상태 캐시도 리셋
        _lastPlayedHash = 0;
    }

    /// <summary>
    /// 후보 중 실제 clip 이름이 존재하는 첫 번째 이름 반환
    /// </summary>
    private string PickFirstExisting(string[] candidates)
    {
        if (_clipNames == null)
            return null;

        for (int i = 0; i < candidates.Length; i++)
        {
            string name = candidates[i];
            if (!string.IsNullOrEmpty(name) && _clipNames.Contains(name))
                return name;
        }

        return null;
    }

    /// <summary>
    /// hash 기준으로 상태 재생
    /// - 같은 상태 중복 재생 방지
    /// - 현재 상태와 같으면 재생 생략
    /// </summary>
    private void PlayHash(int hash)
    {
        if (!animator)
        {
            EnsureAnimator();
            if (!animator)
                return;
        }

        // 컨트롤러 변경 여부만 확인하고 필요할 때만 캐시 갱신
        BuildLocomotionCacheIfNeeded(force: false);

        // 마지막 재생 상태와 같으면 스킵
        if (hash == _lastPlayedHash)
            return;

        // 현재 Animator 상태와 같으면 재생 생략
        var cur = animator.GetCurrentAnimatorStateInfo(Layer);
        if (cur.shortNameHash == hash || cur.fullPathHash == hash)
        {
            _lastPlayedHash = hash;
            return;
        }

        animator.Play(hash, Layer, 0f);
        _lastPlayedHash = hash;
    }

    /// <summary>
    /// 마지막 재생 상태 캐시 초기화
    /// 리셋 직후 같은 애니메이션을 다시 강제로 재생하고 싶을 때 사용
    /// </summary>
    public void ResetStateCache()
    {
        _lastPlayedHash = 0;
    }

    /// <summary>
    /// Idle 재생
    /// </summary>
    public void PlayIdle()
    {
        PlayHash(IdleHash);
    }

    /// <summary>
    /// Hit 재생
    /// </summary>
    public void PlayHit()
    {
        PlayHash(HitHash);
    }

    /// <summary>
    /// Run / Walk / Fly Inplace 중 캐시된 locomotion 재생
    /// </summary>
    public void PlayRun()
    {
        if (_locoHash == 0)
            BuildLocomotionCacheIfNeeded(force: true);

        PlayHash(_locoHash);
    }
}