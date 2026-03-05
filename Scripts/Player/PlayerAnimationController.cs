using UnityEngine;
using System.Collections.Generic;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private const int Layer = 0;

    // ===== 고정 상태(그냥 씀) =====
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int HitHash = Animator.StringToHash("Hit");

    // ===== locomotion 후보(캐시용) =====
    private static readonly string[] RunCandidates = { "Run" };
    private static readonly string[] WalkCandidates = { "Walk" };
    private static readonly string[] FlyCandidates = { "Fly Inplace" };

    private RuntimeAnimatorController _cachedController;
    private HashSet<string> _clipNames;

    private int _locoHash;          // Run/Walk/Fly 중 선택된 1개
    private int _lastPlayedHash;    // 같은 상태면 스킵
    public string locos;

    private void Awake()
    {
        EnsureAnimator();
        BuildLocomotionCacheIfNeeded(force: true);
    }

    private void OnEnable()
    {
        EnsureAnimator();
        BuildLocomotionCacheIfNeeded(force: false);
    }

    public void BindAnimator(Animator newAnimator)
    {
        animator = newAnimator;
        _cachedController = null;   // 강제 갱신
        EnsureAnimator();
        BuildLocomotionCacheIfNeeded(force: true);
    }

    private void EnsureAnimator()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// ✅ locomotion만 초기 1번 캐시 (컨트롤러 바뀌면 그때만 다시 1번)
    /// 우선순위: Run > Walk > Fly Inplace
    /// </summary>
    private void BuildLocomotionCacheIfNeeded(bool force)
    {
        if (!animator || animator.runtimeAnimatorController == null) return;

        var ctrl = animator.runtimeAnimatorController;
        if (!force && ctrl == _cachedController && _clipNames != null) return;

        _cachedController = ctrl;

        // 클립 이름 set은 여기서만 구성(= “찾기” 1번)
        _clipNames = new HashSet<string>();
        var clips = ctrl.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i]) _clipNames.Add(clips[i].name);
        }

        string loco =
            PickFirstExisting(RunCandidates) ??
            PickFirstExisting(WalkCandidates) ??
            PickFirstExisting(FlyCandidates) ??
            "Run"; // 마지막 fallback

        locos = loco;
        _locoHash = Animator.StringToHash(loco);
        _lastPlayedHash = 0; // 컨트롤러 바뀌면 리셋
    }

    private string PickFirstExisting(string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            var n = candidates[i];
            if (!string.IsNullOrEmpty(n) && _clipNames.Contains(n))
                return n;
        }
        return null;
    }

    private void PlayHash(int hash)
    {
        if (!animator)
        {
            EnsureAnimator();
            if (!animator) return;
        }

        // 컨트롤러가 바뀐 경우에만 캐시 갱신(계속 찾지 않음)
        BuildLocomotionCacheIfNeeded(force: false);

        // 같은 상태면 넘김(루프 리셋 방지)
        if (hash == _lastPlayedHash) return;

        var cur = animator.GetCurrentAnimatorStateInfo(Layer);
        if (cur.shortNameHash == hash || cur.fullPathHash == hash)
        {
            _lastPlayedHash = hash;
            return;
        }

        animator.Play(hash, Layer, 0f);
        _lastPlayedHash = hash;
    }

    // ===== Public API =====
    public void PlayIdle() => PlayHash(IdleHash);
    public void PlayHit() => PlayHash(HitHash);
    public void PlayRun() => PlayHash(_locoHash);
}