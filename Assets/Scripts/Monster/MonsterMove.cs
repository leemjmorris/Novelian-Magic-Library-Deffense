using UnityEngine;
using UnityEngine.AI;

//JML: NavMesh-based movement controller for monsters
public class MonsterMove : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private Animator monsterAnimator;

    private const float DESTINATION_THRESHOLD = 0.5f;

    private Vector3 targetPosition;
    private bool hasTarget = false;

    // 애니메이터 파라미터 해시 (성능 최적화)
    private static readonly int ANIM_IS_MOVING = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        // Auto-setup NavMeshAgent if not assigned
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        // Configure NavMeshAgent for monster behavior
        if (navAgent != null)
        {
            navAgent.updateRotation = true;  // NavMesh handles rotation
            navAgent.updateUpAxis = false;   // Don't update Y axis (use gravity)
        }
    }

    /// <summary>
    /// 목적지 설정 (NavMesh 경로 계산)
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        targetPosition = destination;
        hasTarget = true;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(destination);
        }
    }

    /// <summary>
    /// 리스폰 시 상태 초기화
    /// </summary>
    public void ResetState()
    {
        hasTarget = false;
        targetPosition = Vector3.zero;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
    }

    //JML: NavMesh-based movement method
    public void Move<T>(T entity, float speed) where T : IMovable
    {
        if (navAgent == null)
        {
            Debug.LogError("[MonsterMove] NavMeshAgent is null!");
            return;
        }

        // Set NavMeshAgent speed
        navAgent.speed = speed;

        if (!entity.IsWallHit)
        {
            // Enable NavMeshAgent when not hitting wall
            if (!navAgent.enabled)
            {
                navAgent.enabled = true;
            }

            // NavMeshAgent is moving if it has a path and remaining distance > threshold
            bool isMoving = hasTarget &&
                           navAgent.hasPath &&
                           navAgent.remainingDistance > navAgent.stoppingDistance;

            if (monsterAnimator != null)
            {
                monsterAnimator.SetBool(ANIM_IS_MOVING, isMoving);
            }
        }
        else
        {
            // Stop NavMeshAgent when hitting wall
            if (navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero;
            }

            if (monsterAnimator != null)
            {
                monsterAnimator.SetBool(ANIM_IS_MOVING, false);
            }
        }
    }

    /// <summary>
    /// Navigate to a target transform (updates destination every frame)
    /// </summary>
    public void Navigate(Transform target)
    {
        if (target != null && navAgent != null && navAgent.isOnNavMesh)
        {
            SetDestination(target.position);
        }
    }

    /// <summary>
    /// Enable/Disable NavMeshAgent (used when Dizzy or stunned)
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (navAgent != null)
        {
            navAgent.enabled = enabled;
        }
    }

    /// <summary>
    /// Check if agent has reached destination
    /// </summary>
    public bool HasReachedDestination()
    {
        if (navAgent == null || !navAgent.hasPath)
            return false;

        return navAgent.remainingDistance <= navAgent.stoppingDistance;
    }
}
