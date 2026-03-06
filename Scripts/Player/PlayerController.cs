using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 플레이어 관련 컴포넌트들을 연결하는 메인 컨트롤러
// - 입력, 이동, 상태, 애니메이션 연결
// - 점프 사운드
// - 줄넘기 판정 표시
// - 리셋 처리
public sealed class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerMoveController moveController;
    [SerializeField] private PlayerStateController stateController;
    [SerializeField] private PlayerAnimationController animCtrl;
    [SerializeField] private JumpRope jumpRope;

    private Vector3 _startPosition;
    private Quaternion _startRotation;

    // Player가 현재 지면 상태인지 확인용
    public bool IsGrounded => moveController != null && moveController.IsGrounded;

    private void Awake()
    {
        if (!inputController)
            inputController = GetComponent<PlayerInputController>();

        if (!moveController)
            moveController = GetComponent<PlayerMoveController>();

        if (!stateController)
            stateController = GetComponent<PlayerStateController>();

        if (!animCtrl)
            animCtrl = GetComponent<PlayerAnimationController>();
    }

    private void Start()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;

        // 시작 시 기본 상태는 Idle
        animCtrl?.ResetStateCache();
        animCtrl?.PlayIdle();
    }

    /*
    // =========================================================
    // Unity Editor에서 키보드로 테스트할 때만 잠깐 켜서 쓰는 입력 코드
    // =========================================================
    
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || inputController == null)
            return;

        if (kb.leftArrowKey.wasPressedThisFrame) inputController.LeftDown();
        if (kb.leftArrowKey.wasReleasedThisFrame) inputController.LeftUp();

        if (kb.rightArrowKey.wasPressedThisFrame) inputController.RightDown();
        if (kb.rightArrowKey.wasReleasedThisFrame) inputController.RightUp();

        if (kb.spaceKey.wasPressedThisFrame) inputController.JumpDown();
        if (kb.spaceKey.wasReleasedThisFrame) inputController.JumpUp();
    } */

    private void FixedUpdate()
    {
        if (inputController == null || moveController == null || stateController == null)
            return;

        // 이번 물리 프레임에서 사용할 순간 입력값 복사
        bool jumpPressedThisFrame = inputController.JumpPressedThisFrame;

        moveController.Tick(
            inputController.LeftHeld,
            inputController.RightHeld,
            inputController.JumpHeld,
            jumpPressedThisFrame,
            stateController.IsStunned
        );

        // 이번 물리 프레임에서 사용한 순간 입력 플래그 소비
        inputController.ConsumeFrameFlags();

        // 현재 상태에 맞게 애니메이션 갱신
        UpdateAnimation();

        // 실제 점프가 발생한 프레임에만 사운드/판정 체크
        if (moveController.DidJumpThisFrame)
        {
            if (EffectSounds.Instance != null)
                EffectSounds.Instance.JumpSound();

            if (jumpRope != null)
                // 줄넘기 판정 결과 확인
                CheckJumpTimingAsync().Forget();
        }
    }

    // 현재 상태에 맞게 애니메이션 갱신
    // - 스턴 중이면 Hit 유지
    // - 이동 입력이 있으면 Run
    // - 이동 입력이 없으면 Idle
    private void UpdateAnimation()
    {
        if (stateController.IsStunned)
            return;

        if (moveController.IsMovingInput)
            animCtrl?.PlayRun();
        else
            animCtrl?.PlayIdle();
    }

    // 외부에서 Player 스턴 적용
    public void ApplyStun(float seconds)
    {
        if (stateController == null)
            return;

        if (stateController.IsStunned)
            return;

        stateController.ApplyStun(seconds);

        // 스턴 시 입력 강제 해제
        inputController?.ClearAllInput();

        // 피격 애니메이션 재생
        animCtrl?.PlayHit();
    }

    // 줄넘기 판정 결과 확인
    private async UniTaskVoid CheckJumpTimingAsync()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        var res = jumpRope.GetJumpTimingCheck();
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

        if (!GameManager.Instance.IsGameOver)
            ScoreManager.Instance.SetJudgeDisplay(res.isSuccess, res.text, res.color);
    }

    // 시작 위치로 되돌리고 상태/입력/이동/애니메이션 초기화
    public void ResetPosition()
    {
        inputController?.ClearAllInput();
        stateController?.ResetState();
        moveController?.ResetMotor(_startPosition, _startRotation);

        animCtrl?.ResetStateCache();
        animCtrl?.PlayIdle();
    }
}