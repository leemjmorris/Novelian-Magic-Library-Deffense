using UnityEngine;

//JML: Rigidbody-based straight-line movement controller for monsters
public class MonsterMove : MonoBehaviour
{
    [SerializeField] private Animator monsterAnimator;

    // Rigidbody reference (injected from Monster)
    private Rigidbody rb;

    // Movement state
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isActive = false;
    private bool isStopped = false;

    // 애니메이터 파라미터 해시 (성능 최적화)
    private static readonly int ANIM_IS_MOVING = Animator.StringToHash("IsMoving");

    /// <summary>
    /// Initialize movement controller with Rigidbody and target direction
    /// Called from Monster.OnSpawn()
    /// </summary>
    /// <param name="rigidbody">Monster's Rigidbody component</param>
    /// <param name="targetPosition">Wall position to move towards</param>
    public void Initialize(Rigidbody rigidbody, Vector3 targetPosition)
    {
        rb = rigidbody;

        // Calculate direction to target (horizontal only)
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;
        moveDirection = direction.normalized;

        isActive = true;
        isStopped = false;

        // Face toward target
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    /// <summary>
    /// Physics-based movement in FixedUpdate
    /// Called from Monster.FixedUpdate()
    /// </summary>
    /// <param name="entity">Monster entity implementing IMovable</param>
    /// <param name="speed">Movement speed</param>
    public void Move<T>(T entity, float speed) where T : IMovable
    {
        if (rb == null || !isActive) return;

        currentSpeed = speed;

        if (!entity.IsWallHit && !isStopped)
        {
            // 수평 이동 속도 설정 (Y축은 중력에 맡김)
            Vector3 targetVelocity = moveDirection * speed;
            targetVelocity.y = rb.linearVelocity.y; // 기존 Y 속도 유지 (중력 적용)

            rb.linearVelocity = targetVelocity;

            // Wall 방향 바라보기 (즉시 회전)
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            if (monsterAnimator != null)
            {
                monsterAnimator.SetBool(ANIM_IS_MOVING, true);
            }
        }
        else
        {
            // 정지 (Y축은 중력에 맡김)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (monsterAnimator != null)
            {
                monsterAnimator.SetBool(ANIM_IS_MOVING, false);
            }
        }
    }

    /// <summary>
    /// Set destination (updates move direction)
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        Vector3 direction = (destination - transform.position);
        direction.y = 0f;
        moveDirection = direction.normalized;

        isStopped = false;

        // Face toward destination
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    /// <summary>
    /// Update move direction toward target (called every FixedUpdate for tracking)
    /// </summary>
    public void UpdateDirection(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            moveDirection = direction.normalized;
        }
    }

    /// <summary>
    /// Enable/Disable movement (used for CC states like Dizzy, Root)
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isStopped = !enabled;

        if (isStopped && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Stop movement immediately
    /// </summary>
    public void Stop()
    {
        isStopped = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(ANIM_IS_MOVING, false);
        }
    }

    /// <summary>
    /// Reset state for pooling reuse
    /// </summary>
    public void ResetState()
    {
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        isActive = false;
        isStopped = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Set position directly (replaces NavMesh Warp)
    /// </summary>
    public void WarpToPosition(Vector3 position)
    {
        transform.position = position;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Get current move direction
    /// </summary>
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    /// <summary>
    /// Check if currently moving
    /// </summary>
    public bool IsMoving()
    {
        return isActive && !isStopped && moveDirection.sqrMagnitude > 0.001f;
    }
}
