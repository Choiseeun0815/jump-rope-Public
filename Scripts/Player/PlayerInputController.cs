using UnityEngine;

// 플레이어 입력 상태만 보관하는 컴포넌트
public sealed class PlayerInputController : MonoBehaviour
{
    // 왼쪽 이동 입력을 계속 누르고 있는지
    public bool LeftHeld { get; private set; }

    // 오른쪽 이동 입력을 계속 누르고 있는지
    public bool RightHeld { get; private set; }

    // 점프 입력을 계속 누르고 있는지
    public bool JumpHeld { get; private set; }

    // 이번 물리 처리 전에 점프 버튼이 눌렸는지
    // 점프 버퍼 처리용 1회성 플래그
    public bool JumpPressedThisFrame { get; private set; }

    // 왼쪽 이동 시작
    // 오른쪽 입력은 해제해서 한 방향만 유지되도록 처리
    public void LeftDown()
    {
        LeftHeld = true;
        RightHeld = false;
    }

    // 왼쪽 이동 종료
    public void LeftUp()
    {
        LeftHeld = false;
    }

    // 오른쪽 이동 시작
    // 왼쪽 입력은 해제해서 한 방향만 유지되도록 처리
    public void RightDown()
    {
        RightHeld = true;
        LeftHeld = false;
    }

    // 오른쪽 이동 종료
    public void RightUp()
    {
        RightHeld = false;
    }

    // 점프 시작
    // - JumpHeld 는 누르고 있는 동안 유지
    // - JumpPressedThisFrame 는 이번 물리 처리에서 한 번만 소비할 플래그
    public void JumpDown()
    {
        // 이미 누르고 있는 상태가 아니라면 "이번 프레임에 눌림" 처리
        if (!JumpHeld)
            JumpPressedThisFrame = true;

        JumpHeld = true;
    }

    // 점프 종료
    public void JumpUp()
    {
        JumpHeld = false;
    }

    // 이번 물리 프레임에서 사용한 1회성 입력 플래그 소비
    // Held 상태는 유지하고, 순간 입력 플래그만 초기화
    public void ConsumeFrameFlags()
    {
        JumpPressedThisFrame = false;
    }

    // 모든 입력 강제 해제
    // 스턴, 리셋, 게임오버 등에 사용
    public void ClearAllInput()
    {
        LeftHeld = false;
        RightHeld = false;
        JumpHeld = false;
        JumpPressedThisFrame = false;
    }
}