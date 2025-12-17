//LMJ : Effect follower component for independent skill effects
//      Follows a target transform while maintaining independence from skill/projectile lifecycle
using UnityEngine;

/// <summary>
/// 이펙트가 타겟을 따라가도록 하는 컴포넌트
/// Projectile/스킬 로직과 분리된 독립 이펙트용
/// 타겟이 사라져도 이펙트는 현재 위치에서 재생 완료
/// </summary>
public class EffectFollower : MonoBehaviour
{
    [Header("따라가기 설정")]
    [Tooltip("따라갈 타겟 Transform")]
    [SerializeField] private Transform target;

    [Tooltip("타겟으로부터의 오프셋")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Tooltip("타겟 회전도 따라갈지 여부")]
    [SerializeField] private bool followRotation = false;

    [Tooltip("부드러운 이동 여부 (true: Lerp, false: 즉시)")]
    [SerializeField] private bool smoothFollow = false;

    [Tooltip("부드러운 이동 속도 (smoothFollow=true일 때만)")]
    [SerializeField] private float smoothSpeed = 15f;

    // 타겟이 사라진 후 마지막 위치 유지
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private bool targetLost = false;

    /// <summary>
    /// 초기화
    /// </summary>
    /// <param name="followTarget">따라갈 타겟</param>
    /// <param name="followOffset">오프셋</param>
    /// <param name="shouldFollowRotation">회전 따라가기 여부</param>
    public void Initialize(Transform followTarget, Vector3 followOffset = default, bool shouldFollowRotation = false)
    {
        target = followTarget;
        offset = followOffset;
        followRotation = shouldFollowRotation;

        if (target != null)
        {
            lastValidPosition = target.position + offset;
            lastValidRotation = target.rotation;
        }
        else
        {
            lastValidPosition = transform.position;
            lastValidRotation = transform.rotation;
        }
    }

    /// <summary>
    /// 오프셋 설정
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    /// <summary>
    /// 부드러운 이동 설정
    /// </summary>
    public void SetSmoothFollow(bool smooth, float speed = 15f)
    {
        smoothFollow = smooth;
        smoothSpeed = speed;
    }

    private void LateUpdate()
    {
        // 타겟이 유효한 경우
        if (target != null)
        {
            Vector3 targetPos = target.position + offset;
            lastValidPosition = targetPos;
            lastValidRotation = target.rotation;
            targetLost = false;

            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
                if (followRotation)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, smoothSpeed * Time.deltaTime);
                }
            }
            else
            {
                transform.position = targetPos;
                if (followRotation)
                {
                    transform.rotation = target.rotation;
                }
            }
        }
        else if (!targetLost)
        {
            // 타겟이 처음 사라진 순간
            targetLost = true;
            // 마지막 유효 위치에 고정 (이펙트는 그 자리에서 계속 재생)
            transform.position = lastValidPosition;
            transform.rotation = lastValidRotation;

            Debug.Log($"[EffectFollower] Target lost, effect fixed at {lastValidPosition}");
        }
        // targetLost == true 이후에는 아무것도 하지 않음 (현재 위치 유지)
    }

    /// <summary>
    /// 타겟 변경
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetLost = false;
        }
    }

    /// <summary>
    /// 현재 타겟 반환
    /// </summary>
    public Transform GetTarget()
    {
        return target;
    }

    /// <summary>
    /// 타겟이 유효한지 확인
    /// </summary>
    public bool HasValidTarget()
    {
        return target != null;
    }
}
