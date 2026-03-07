using UnityEngine;
using System.Collections.Generic;

public class Obstacle_animal : MonoBehaviour, IObstacle
{
    // 장애물이 이동하는 기본 속도
    public float moveSpeed = 8f;

    // 플레이어와 충돌했을 때 적용할 스턴 시간
    [SerializeField] private float stunSeconds = 0.2f;

    // 물리 이동용 Rigidbody
    private Rigidbody rb;

    // 장애물에 붙어있는 Animator
    private Animator animator;

    // 자식에 있는 파티클 시스템
    private ParticleSystem ps;

    // 현재 캐시된 AnimatorController
    // 컨트롤러가 바뀌었는지 확인할 때 사용
    private RuntimeAnimatorController _cachedController;

    // 현재 AnimatorController 안에 들어있는 클립 이름 목록 캐시
    // 매번 animationClips를 순회하지 않기 위해 1번만 저장
    private HashSet<string> _clipNames;

    // 실제로 재생할 locomotion 애니메이션 해시값
    // Run / Walk / Fly Inplace 중 하나가 들어감
    private int _locoHash;

    // Animator.Play에 사용할 레이어 인덱스
    // 기본 Base Layer는 0
    private const int Layer = 0;

    // 우선적으로 찾을 이동 애니메이션 이름 후보들
    private static readonly string[] RunCandidates = { "Run" };
    private static readonly string[] WalkCandidates = { "Walk" };
    private static readonly string[] FlyCandidates = { "Fly Inplace" };

    // 외부에서 속도를 접근/수정할 수 있도록 하는 프로퍼티
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    // 외부에서 스턴 시간을 접근/수정할 수 있도록 하는 프로퍼티
    public float StunSeconds
    {
        get => stunSeconds;
        set => stunSeconds = value;
    }

    private void Awake()
    {
        // 필요한 컴포넌트 캐싱
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        ps = GetComponentInChildren<ParticleSystem>(true);

        // 시작 시점에 locomotion 애니메이션 정보를 1회 캐시
        BuildLocomotionCacheIfNeeded(force: true);
    }

    private void OnEnable()
    {
        // 비활성화/재활성화 과정에서 파티클 참조가 비어있을 수 있으니 다시 확보
        if (!ps) ps = GetComponentInChildren<ParticleSystem>(true);
        if (!ps) return;

        // 파티클 오브젝트가 꺼져 있으면 켜줌
        if (!ps.gameObject.activeSelf)
            ps.gameObject.SetActive(true);

        // 이전 재생 흔적 제거 후 처음 상태로 리셋하고 다시 재생
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Simulate(0f, true, true, true);
        ps.Play(true);
    }

    private void OnDisable()
    {
        // 비활성화될 때 기존 속도를 제거해서
        // 풀링 재사용 시 이전 이동값이 남지 않도록 처리
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 충돌 대상이 Player 레이어가 아니면 무시
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        // 충돌 효과음 재생
        if (EffectSounds.Instance != null)
            EffectSounds.Instance.HitSound();

        // 플레이어에게 스턴 적용
        GameManager.Instance.playerController.ApplyStun(stunSeconds);
    }

    public void Setup(Vector3 defaultDir, Transform playerTransform)
    {
        // 혹시 참조가 비어있으면 다시 가져옴
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponent<Animator>();

        // y축 고정
        // 장애물이 바닥 기준으로 움직이도록 높이를 0으로 맞춤
        Vector3 pos = transform.position;
        pos.y = 0f;
        transform.position = pos;

        // 기본 이동 방향
        Vector3 moveDir = defaultDir;

        // 플레이어 Transform이 있으면 플레이어 쪽 방향으로 이동
        if (playerTransform != null)
            moveDir = playerTransform.position - transform.position;

        // 수평 이동만 하도록 y 제거
        moveDir.y = 0f;

        // 방향 벡터가 거의 0이면 현재 forward 사용
        // 아니면 정규화해서 방향 벡터로 사용
        if (moveDir.sqrMagnitude < 0.000001f) moveDir = transform.forward;
        else moveDir.Normalize();

        // 이동 방향을 바라보도록 회전
        transform.rotation = Quaternion.LookRotation(moveDir);

        // Rigidbody 속도로 실제 이동 적용
        rb.linearVelocity = moveDir * moveSpeed;

        // 캐시된 locomotion 애니메이션 재생
        PlayLocomotion();
    }

    // locomotion 애니메이션 캐시 생성
    // 시작 시 1번 수행하고, AnimatorController가 바뀐 경우에만 다시 수행
    // 우선순위: Run > Walk > Fly Inplace
    private void BuildLocomotionCacheIfNeeded(bool force)
    {
        // Animator 또는 Controller가 없으면 처리 불가
        if (!animator || animator.runtimeAnimatorController == null) return;

        var ctrl = animator.runtimeAnimatorController;

        // 강제 갱신이 아니고,
        // 현재 컨트롤러가 이전과 같고,
        // 이미 클립 이름 캐시가 있으면 다시 만들 필요 없음
        if (!force && ctrl == _cachedController && _clipNames != null) return;

        _cachedController = ctrl;

        // 클립 이름 캐시 생성
        // 이후에는 이름 비교만 하도록 해서 매번 순회하지 않게 함
        _clipNames = new HashSet<string>();
        var clips = ctrl.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i]) _clipNames.Add(clips[i].name);
        }

        // 우선순위대로 존재하는 locomotion 클립 선택
        string loco =
            PickFirstExisting(RunCandidates) ??
            PickFirstExisting(WalkCandidates) ??
            PickFirstExisting(FlyCandidates) ??
            "Run"; // 아무것도 못 찾으면 마지막 기본값

        // Animator.Play에 사용할 수 있도록 해시로 변환
        _locoHash = Animator.StringToHash(loco);
    }

    // 후보 이름들 중 실제 AnimatorController에 존재하는 첫 번째 클립 이름 반환
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

    // 캐시된 locomotion 애니메이션 재생
    private void PlayLocomotion()
    {
        if (!animator) return;

        int hash = _locoHash;
        if (hash == 0) return;

        // Base Layer(0)에서 처음 프레임부터 재생
        animator.Play(hash, Layer, 0f);
    }
}