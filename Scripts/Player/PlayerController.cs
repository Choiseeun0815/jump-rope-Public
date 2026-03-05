using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public sealed class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;

    [Header("Animation")]
    [SerializeField] private PlayerAnimationController animCtrl;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private float turnSlerp = 20f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundRayLength = 0.35f;
    [SerializeField] private float groundOriginUpOffset = 0.2f;

    [Header("Jump Targets (Design)")]
    [Min(0.1f)][SerializeField] private float desiredJumpHeight = 1.5f;
    [Min(0.05f)][SerializeField] private float desiredAirTime = 0.6f;
    [Range(0.1f, 0.9f)][SerializeField] private float ascentRatio = 0.4f;

    [Header("Jump Forgiveness")]
    [Min(0f)][SerializeField] private float coyoteTime = 0.12f;
    [Min(0f)][SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Jump Feel")]
    [SerializeField] private bool enableJumpCut = true;
    [Min(1f)][SerializeField] private float jumpCutMultiplier = 2.5f;

    [Header("Gameplay")]
    [SerializeField] private JumpRope jumpRope;

    [Header("Jump Computed (Auto)")]
    [SerializeField] private float riseGravityMultiplier = 1f;
    [SerializeField] private float fallGravityMultiplier = 1f;
    [SerializeField] private float initialUpVelocity = 0f;

    [Header("Stun")]
    private bool _isStunned;
    private System.Threading.CancellationTokenSource _stunCts;

    private bool _leftHeld;
    private bool _rightHeld;
    private bool _grounded;

    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;
    private bool _jumpHeld;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private static readonly Vector3 Up = Vector3.up;
    private readonly RaycastHit[] _groundHits = new RaycastHit[1];

    public bool IsGrounded => _grounded;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!animCtrl) animCtrl = GetComponent<PlayerAnimationController>();

        RecalculateJumpFromTargets();
    }

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 시작은 Idle (버튼 입력 전)
        if (_grounded && !_leftHeld && !_rightHeld)
            animCtrl?.PlayIdle();
    }

    private void OnValidate()
    {
        RecalculateJumpFromTargets();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame) LeftDown();
        if (kb.rightArrowKey.wasPressedThisFrame) RightDown();

        if (kb.leftArrowKey.wasReleasedThisFrame) LeftUp();
        if (kb.rightArrowKey.wasReleasedThisFrame) RightUp();

        if (kb.spaceKey.wasPressedThisFrame) JumpDown();
        if (kb.spaceKey.wasReleasedThisFrame) JumpUp();
    }

    private void FixedUpdate()
    {
        bool wasGrounded = _grounded;

        _grounded = CheckGroundedNonAlloc();
        if (_grounded) _lastGroundedTime = Time.time;

        TryConsumeBufferedJump();

        if (!_grounded)
            ApplyCustomGravity();

        ApplyGroundMove();
        ApplyJumpCutIfNeeded();

        // ✅ 착지했는데 버튼이 안 눌려있으면 Idle (Up을 못 받는 상황 대비)
        if (!wasGrounded && _grounded && !_leftHeld && !_rightHeld)
            animCtrl?.PlayIdle();
    }

    private void OnDestroy()
    {
        _stunCts?.Cancel();
        _stunCts?.Dispose();
    }

    public void ApplyStun(float seconds)
    {
        // ✅ 스턴 중이면 다시 안 걸리게(중복 충돌 방지)
        if (_isStunned) return;

        _isStunned = true;

        // 이동 입력 끊기
        _leftHeld = false;
        _rightHeld = false;
        _jumpHeld = false;

        // Hit 애니
        animCtrl?.PlayHit();

        // 스턴 해제 예약
        _stunCts?.Cancel();
        _stunCts?.Dispose();
        _stunCts = new System.Threading.CancellationTokenSource();

        ReleaseStunAfter(seconds, _stunCts.Token).Forget();
    }

    private async UniTaskVoid ReleaseStunAfter(float seconds, System.Threading.CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
        }
        catch
        {
            return;
        }

        _isStunned = false;

        // 스턴 풀리자마자 버튼이 안 눌려있고 지상이라면 Idle로
        // if (_grounded && !_leftHeld && !_rightHeld)
        //     animCtrl?.PlayIdle();
    }

    private void ApplyGroundMove()
    {
        if (_isStunned) return;

        // ✅ 어떤 버튼이든 "누르고 있으면" Run 유지가 기본
        bool isHolding = _leftHeld || _rightHeld;

        Transform target = null;
        if (_leftHeld) target = leftTarget;
        else if (_rightHeld) target = rightTarget;

        // ✅ 버튼 안 누르면 애니메이션은 여기서 건드리지 않음 (Idle은 Up에서만)
        if (!isHolding || !target)
            return;

        // ✅ 지상 + 버튼 누르는 중이면, dist/이동 여부와 무관하게 Run이 맞다
        if (_grounded)
            animCtrl?.PlayRun();

        Vector3 delta = target.position - rb.position;
        Vector3 planar = Vector3.ProjectOnPlane(delta, Up);

        float dist = planar.magnitude;

        // ✅ stopDistance 이하면 이동만 안 하고 Run 유지
        if (dist <= stopDistance)
            return;

        Vector3 dir = planar / dist;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSlerp * Time.fixedDeltaTime));
        }
    }

    private bool CheckGroundedNonAlloc()
    {
        Vector3 origin = rb.position + Up * groundOriginUpOffset;
        Ray ray = new Ray(origin, Vector3.down);

        int hitCount = Physics.RaycastNonAlloc(
            ray,
            _groundHits,
            groundRayLength,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        return hitCount > 0;
    }

    private void RecalculateJumpFromTargets()
    {
        float H = Mathf.Max(0.001f, desiredJumpHeight);
        float T = Mathf.Max(0.001f, desiredAirTime);

        float r = Mathf.Clamp(ascentRatio, 0.1f, 0.9f);
        float tUp = Mathf.Max(0.001f, T * r);
        float tDown = Mathf.Max(0.001f, T * (1f - r));

        float g = Mathf.Abs(Physics.gravity.y);
        if (g < 0.0001f) g = 9.81f;

        float gUp = (2f * H) / (tUp * tUp);
        float gDown = (2f * H) / (tDown * tDown);

        riseGravityMultiplier = Mathf.Max(0.01f, gUp / g);
        fallGravityMultiplier = Mathf.Max(0.01f, gDown / g);

        initialUpVelocity = gUp * tUp;
    }

    private void ApplyCustomGravity()
    {
        float mult = (rb.linearVelocity.y > 0f) ? riseGravityMultiplier : fallGravityMultiplier;
        rb.AddForce(Physics.gravity * (mult - 1f), ForceMode.Acceleration);
    }

    private void TryConsumeBufferedJump()
    {
        if (_isStunned) return;

        bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;
        bool coyote = (Time.time - _lastGroundedTime) <= coyoteTime;

        if (!_jumpHeld) return;
        if (!buffered) return;
        if (!coyote) return;

        _lastJumpPressedTime = -999f;
        _lastGroundedTime = -999f;

        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        if (EffectSounds.Instance != null)
            EffectSounds.Instance.JumpSound();

        float impulse = rb.mass * initialUpVelocity;
        rb.AddForce(Up * impulse, ForceMode.Impulse);

        if (jumpRope) CheckJumpTimingAsync().Forget();
    }

    private void ApplyJumpCutIfNeeded()
    {
        if (!enableJumpCut) return;
        if (_grounded) return;

        if (!_jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.AddForce(Vector3.down * (rb.linearVelocity.y * (jumpCutMultiplier - 1f)),
                ForceMode.Acceleration);
        }
    }

    private async UniTaskVoid CheckJumpTimingAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        var res = jumpRope.GetJumpTimingCheck();
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

        if (!GameManager.Instance.IsGameOver)
            ScoreManager.Instance.SetJudgeDisplay(res.isSuccess, res.text, res.color);
    }

    public void LeftDown()
    {
        _leftHeld = true;
        _rightHeld = false;

        if (_grounded) animCtrl?.PlayRun();
    }

    public void LeftUp()
    {
        _leftHeld = false;

        // ✅ 둘 다 뗐을 때만 Idle
        if (!_rightHeld)
            animCtrl?.PlayIdle();
    }

    public void RightDown()
    {
        _rightHeld = true;
        _leftHeld = false;

        if (_grounded) animCtrl?.PlayRun();
    }

    public void RightUp()
    {
        _rightHeld = false;

        // ✅ 둘 다 뗐을 때만 Idle
        if (!_leftHeld)
            animCtrl?.PlayIdle();
    }

    public void JumpDown()
    {
        _jumpHeld = true;
        _lastJumpPressedTime = Time.time;
    }

    public void JumpUp()
    {
        _jumpHeld = false;
    }

    public void PlayHitAnim()
    {
        animCtrl?.PlayHit();
    }

    public void ResetPosition()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (rb != null)
        {
            rb.position = startPosition;
            rb.rotation = startRotation;
        }

        _leftHeld = false;
        _rightHeld = false;
        _jumpHeld = false;
        _lastJumpPressedTime = -999f;
        _lastGroundedTime = -999f;

        // ✅ 리셋은 Idle
        if (_grounded) animCtrl?.PlayIdle();
    }
}