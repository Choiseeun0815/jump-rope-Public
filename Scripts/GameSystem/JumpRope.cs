using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LineRenderer))]
public class JumpRope : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform handleA;
    public Transform handleB;
    public float ropeRadius = 8.0f;

    [Header("Visual Settings")]
    [Range(10, 100)] public int visualSegmentCount = 20;

    [ColorUsage(true, true)]
    public Color neonColor = new Color(0, 1, 1, 2);

    [Header("Collision Settings - Angle Based")]
    [Tooltip("플레이어 충돌 판정 시작 각도 (degree)")]
    [Range(0f, 360f)] public float collisionStartAngle = 160f;

    [Tooltip("플레이어 충돌 판정 종료 각도 (degree)")]
    [Range(0f, 360f)] public float collisionEndAngle = 200f;

    [Tooltip("플레이어 참조 (isGrounded 체크용)")]
    public PlayerController player;

    [Tooltip("게임 시작/재시작 시 줄넘기의 초기 각도")]
    [Range(0f, 360f)] public float initAngle = 210f;

    [Header("Floor Settings")]
    public bool useFloorCollision = true;
    public float floorY = 0f;

    // 내부 상태 변수
    private LineRenderer lineRenderer;
    private Material ropeMaterial;
    private Vector3[] visualPositions;
    private float currentAngle = 210f;
    private float currentSpeed = 0f;
    private bool isSpinning = false;

    // 충돌 판정 최적화
    private bool wasInCollisionZone = false;

    [HideInInspector] public UnityEvent OnRopeHitGround;
    [HideInInspector] public UnityEvent OnRopeHitPlayer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;

        ropeMaterial = lineRenderer.material;
        UpdateRopeColor(neonColor);

        visualPositions = new Vector3[visualSegmentCount + 1];
        lineRenderer.positionCount = visualSegmentCount + 1;

        if (OnRopeHitGround == null) OnRopeHitGround = new UnityEvent();
        if (OnRopeHitPlayer == null) OnRopeHitPlayer = new UnityEvent();
    }

    public void UpdateRopeColor(Color newColor)
    {
        neonColor = newColor;
        if (ropeMaterial != null)
        {
            ropeMaterial.SetColor("_EmissionColor", neonColor);
        }
    }

    public void InitRope(float startSpeed)
    {
        currentAngle = initAngle;

        currentSpeed = startSpeed;
        isSpinning = true;
        wasInCollisionZone = false;

        UpdateVisuals(GetControlPoint());
    }

    public void StopRope()
    {
        isSpinning = false;
    }

    public void SetSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
    }

    private void Update()
    {
        if (handleA == null || handleB == null) return;

        if (isSpinning)
        {
            UpdateRotation();
        }

        Vector3 controlPoint = GetControlPoint();
        UpdateVisuals(controlPoint);

        if (isSpinning)
        {
            CheckAngleBasedCollision();
        }
    }

    void UpdateRotation()
    {
        float prevAngle = currentAngle;
        currentAngle += currentSpeed * Time.deltaTime;

        if (prevAngle < 180f && currentAngle >= 180f)
        {
            if (player != null && !player.IsGrounded)
            {
                OnRopeHitGround?.Invoke();
                TriggerInkWithProbability();
            }
        }

        if (currentAngle >= 360f)
        {
            currentAngle -= 360f;
        }
    }
    private void TriggerInkWithProbability()
    {
        if (Obstacle_Ink.Instance == null) return;

        if (ScoreManager.Instance.currentScore >= Obstacle_Ink.Instance.startScore)
        {
            if (Random.Range(0f, 1f) < Obstacle_Ink.Instance.inkPercent)
            {
                Obstacle_Ink.Instance.InkObstacle();
            }
        }
    }
    Vector3 GetControlPoint()
    {
        Vector3 center = (handleA.position + handleB.position) / 2f;
        Vector3 axis = (handleB.position - handleA.position).normalized;
        Vector3 perpendicular = Vector3.Cross(axis, Vector3.up).normalized;
        if (perpendicular == Vector3.zero) perpendicular = Vector3.right;
        Vector3 initialDir = Vector3.Cross(perpendicular, axis).normalized;

        Quaternion rotation = Quaternion.AngleAxis(currentAngle, axis);
        Vector3 dir = rotation * initialDir;

        return center + (dir * ropeRadius);
    }

    void UpdateVisuals(Vector3 controlPoint)
    {
        if (visualPositions.Length != visualSegmentCount + 1)
        {
            visualPositions = new Vector3[visualSegmentCount + 1];
            lineRenderer.positionCount = visualSegmentCount + 1;
        }

        for (int i = 0; i <= visualSegmentCount; i++)
        {
            float t = i / (float)visualSegmentCount;
            Vector3 pos = GetBezierPoint(t, handleA.position, controlPoint, handleB.position);

            if (useFloorCollision) pos.y = Mathf.Max(pos.y, floorY);

            visualPositions[i] = pos;
        }
        lineRenderer.SetPositions(visualPositions);
    }

    Vector3 GetBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }


    void CheckAngleBasedCollision()
    {
        if (player == null) return;

        bool isInCollisionZone = IsAngleInRange(currentAngle, collisionStartAngle, collisionEndAngle);

        // 충돌 존에 막 진입한 순간만 체크 (중복 호출 방지)
        if (isInCollisionZone && !wasInCollisionZone)
        {
            // 플레이어가 땅에 있으면 게임 오버
            if (player.IsGrounded)
            {
                OnRopeHitPlayer?.Invoke();
            }
        }

        wasInCollisionZone = isInCollisionZone;
    }


    bool IsAngleInRange(float angle, float start, float end)
    {
        // 정규화
        angle = NormalizeAngle(angle);
        start = NormalizeAngle(start);
        end = NormalizeAngle(end);

        if (start <= end)
        {
            return angle >= start && angle <= end;
        }
        else
        {
            return angle >= start || angle <= end;
        }
    }

    float NormalizeAngle(float angle)
    {
        while (angle < 0f) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }
    public void ResetPositionToAngle(float targetAngle)
    {
        currentAngle = targetAngle;
        isSpinning = false;
        wasInCollisionZone = false;

        Vector3 controlPoint = GetControlPoint();
        UpdateVisuals(controlPoint);
    }
    public (bool isSuccess, string text, Color color) GetJumpTimingCheck()
    {
        if (currentAngle >= 110f && currentAngle < 170f)
        {
            return (true, "Perfect!!", Color.yellow);

        }
        else
        {
            return (false, "Bad...", Color.cyan);
        }
    }
}