using UnityEngine;
using System.Collections.Generic;

public class Obstacle_animal : MonoBehaviour, IObstacle
{
    public float moveSpeed = 8f;
    [SerializeField] private float stunSeconds = 0.2f;

    private Rigidbody rb;
    private Animator animator;
    private ParticleSystem ps;

    // ===== locomotion clip cache =====
    private RuntimeAnimatorController _cachedController;
    private HashSet<string> _clipNames;

    private int _locoHash;        // Run / Walk / Fly Inplace 중 선택된 1개

    private const int Layer = 0;

    private static readonly string[] RunCandidates = { "Run" };
    private static readonly string[] WalkCandidates = { "Walk" };
    private static readonly string[] FlyCandidates = { "Fly Inplace" };

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float StunSeconds
    {
        get => stunSeconds;
        set => stunSeconds = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        ps = GetComponentInChildren<ParticleSystem>(true);
        BuildLocomotionCacheIfNeeded(force: true);
    }

    private void OnEnable()
    {
        if (!ps) ps = GetComponentInChildren<ParticleSystem>(true);
        if (!ps) return;


        if (!ps.gameObject.activeSelf)
            ps.gameObject.SetActive(true);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Simulate(0f, true, true, true);
        ps.Play(true);
    }

    private void OnDisable()
    {
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        if (EffectSounds.Instance != null)
            EffectSounds.Instance.HitSound();
        
        GameManager.Instance.playerController.ApplyStun(stunSeconds);
    }

    public void Setup(Vector3 defaultDir, Transform playerTransform)
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponent<Animator>();

        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;

        Vector3 moveDir = defaultDir;

        if (playerTransform != null)
            moveDir = playerTransform.position - transform.position;

        moveDir.y = 0f;
        if (moveDir.sqrMagnitude < 0.000001f) moveDir = transform.forward;
        else moveDir.Normalize();

        transform.rotation = Quaternion.LookRotation(moveDir);
        rb.linearVelocity = moveDir * moveSpeed;

        // ✅ 클립 기반 locomotion 재생 (캐시는 1번만)
        PlayLocomotion();
    }

    /// <summary>
    /// ✅ 초기 1번 캐시, 컨트롤러 바뀔 때만 다시 1번 캐시
    /// 우선순위: Run > Walk > Fly Inplace
    /// </summary>
    private void BuildLocomotionCacheIfNeeded(bool force)
    {
        if (!animator || animator.runtimeAnimatorController == null) return;

        var ctrl = animator.runtimeAnimatorController;
        if (!force && ctrl == _cachedController && _clipNames != null) return;

        _cachedController = ctrl;

        // 여기서만 클립 스캔(= 계속 찾지 않음)
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

        _locoHash = Animator.StringToHash(loco);
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

    private void PlayLocomotion()
    {
        if (!animator) return;

        int hash = _locoHash;
        if (hash == 0) return;

        animator.Play(hash, Layer, 0f);
    }
}