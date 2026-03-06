using UnityEngine;

// 플레이어의 실제 물리 이동/점프/지면체크/중력 처리를 담당하는 컴포넌트
// Rigidbody 기반 제어만 맡음
public sealed class PlayerMoveController : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;

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

    [Header("Jump Computed (Auto)")]
    [SerializeField] private float riseGravityMultiplier = 1f;
    [SerializeField] private float fallGravityMultiplier = 1f;
    [SerializeField] private float initialUpVelocity = 0f;

    private bool _grounded;
    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;

    private static readonly Vector3 Up = Vector3.up;

    // GC 방지를 위해 Raycast 결과 배열 재사용
    private readonly RaycastHit[] _groundHits = new RaycastHit[1];

    // 현재 지면 위에 있는지 확인
    public bool IsGrounded => _grounded;

    // 이번 FixedUpdate에서 실제 점프가 발생했는지 확인
    public bool DidJumpThisFrame { get; private set; }

    // 현재 좌/우 이동 입력이 들어오고 있는지 확인
    public bool IsMovingInput { get; private set; }

    private void Awake()
    {
        if (!rb)
            rb = GetComponent<Rigidbody>();

        // 인스펙터 값 기준으로 점프 계산값 초기화
        RecalculateJumpFromTargets();
    }

    private void OnValidate()
    {
        // 인스펙터 값 변경 시 자동 재계산
        RecalculateJumpFromTargets();
    }

    // 이동/점프/중력을 한 번 처리
    // PlayerController.FixedUpdate()에서 호출됨
    public void Tick(bool leftHeld, bool rightHeld, bool jumpHeld, bool jumpPressedThisFrame, bool blockMovement)
    {
        DidJumpThisFrame = false;
        IsMovingInput = leftHeld || rightHeld;

        // 지면 체크
        _grounded = CheckGroundedNonAlloc();
        if (_grounded)
            _lastGroundedTime = Time.time;

        // 점프 버튼이 눌린 순간 기록
        if (jumpPressedThisFrame)
            _lastJumpPressedTime = Time.time;

        // 스턴 등으로 이동이 막혀 있지 않을 때만 이동/점프 처리
        if (!blockMovement)
        {
            TryConsumeBufferedJump(jumpHeld);
            ApplyGroundMove(leftHeld, rightHeld);
        }

        // 공중에서는 커스텀 중력 적용
        if (!_grounded)
            ApplyCustomGravity();
    }

    // 좌/우 목표 지점을 향해 지상 이동
    private void ApplyGroundMove(bool leftHeld, bool rightHeld)
    {
        Transform target = null;

        if (leftHeld)
            target = leftTarget;
        else if (rightHeld)
            target = rightTarget;

        // 이동 입력이 없으면 종료
        if (!leftHeld && !rightHeld)
            return;

        // 목표 지점이 없으면 종료
        if (target == null)
            return;

        Vector3 delta = target.position - rb.position;
        Vector3 planar = Vector3.ProjectOnPlane(delta, Up);
        float dist = planar.magnitude;

        // 거의 도착했으면 이동하지 않음
        if (dist <= stopDistance)
            return;

        Vector3 dir = planar / dist;

        // Rigidbody 기반 이동
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        // 진행 방향을 향하도록 회전
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(dir, Up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSlerp * Time.fixedDeltaTime));
        }
    }

    // RaycastNonAlloc 기반 지면 체크
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

    // 원하는 점프 높이/체공 시간 기준으로
    // 상승 중력, 하강 중력, 초기 점프 속도 계산
    private void RecalculateJumpFromTargets()
    {
        float h = Mathf.Max(0.001f, desiredJumpHeight);
        float t = Mathf.Max(0.001f, desiredAirTime);

        float r = Mathf.Clamp(ascentRatio, 0.1f, 0.9f);
        float tUp = Mathf.Max(0.001f, t * r);
        float tDown = Mathf.Max(0.001f, t * (1f - r));

        float g = Mathf.Abs(Physics.gravity.y);
        if (g < 0.0001f)
            g = 9.81f;

        float gUp = (2f * h) / (tUp * tUp);
        float gDown = (2f * h) / (tDown * tDown);

        riseGravityMultiplier = Mathf.Max(0.01f, gUp / g);
        fallGravityMultiplier = Mathf.Max(0.01f, gDown / g);

        // 위로 올라가는 시작 속도
        initialUpVelocity = gUp * tUp;
    }

    // 상승/하강 구간에 따라 서로 다른 중력 배수 적용
    private void ApplyCustomGravity()
    {
        float mult = (rb.linearVelocity.y > 0f) ? riseGravityMultiplier : fallGravityMultiplier;
        rb.AddForce(Physics.gravity * (mult - 1f), ForceMode.Acceleration);
    }

    // 점프 버퍼 + 코요테 타임을 적용해 실제 점프 실행
    // 점프 컷 기능은 제거된 상태
    private void TryConsumeBufferedJump(bool jumpHeld)
    {
        bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;
        bool coyote = (Time.time - _lastGroundedTime) <= coyoteTime;

        // 점프 버튼이 유지 중이어야 하고
        // 최근에 누른 기록이 있어야 하며
        // 최근까지 지면에 닿아 있어야 함
        if (!jumpHeld) return;
        if (!buffered) return;
        if (!coyote) return;

        _lastJumpPressedTime = -999f;
        _lastGroundedTime = -999f;

        // 아래로 떨어지는 중이면 하강 속도 제거 후 점프
        Vector3 v = rb.linearVelocity;
        if (v.y < 0f)
            v.y = 0f;

        rb.linearVelocity = v;

        float impulse = rb.mass * initialUpVelocity;
        rb.AddForce(Up * impulse, ForceMode.Impulse);

        DidJumpThisFrame = true;
    }

    // 시작 위치로 되돌리며 물리 상태 초기화
    public void ResetMotor(Vector3 position, Quaternion rotation)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();

        transform.position = position;
        transform.rotation = rotation;

        rb.position = position;
        rb.rotation = rotation;

        _grounded = false;
        _lastGroundedTime = -999f;
        _lastJumpPressedTime = -999f;
        DidJumpThisFrame = false;
        IsMovingInput = false;
    }
}