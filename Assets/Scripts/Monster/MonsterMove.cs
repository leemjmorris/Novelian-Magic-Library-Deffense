using UnityEngine;

//JML: Generic movement controller for all movable entities using Rigidbody
public class MonsterMove : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator monsterAnimator;

    //JML: Generic movement method using velocity-based physics
    public void Move<T>(T entity, float speed) where T : IMovable
    {
        if (!entity.IsWallHit)
        {
            rb.linearVelocity = Vector3.back * speed;
            if (monsterAnimator != null && HasParameter("1_Move"))
            {
                monsterAnimator.SetBool("1_Move", true);
            }
        }
        else
        {
            if (monsterAnimator != null && HasParameter("1_Move"))
            {
                monsterAnimator.SetBool("1_Move", false);
            }
        }
    }

    private bool HasParameter(string paramName)
    {
        if (monsterAnimator == null) return false;
        foreach (var param in monsterAnimator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    //JML: Future navigation logic placeholder
    public void Navigate(Transform target)
    {
        //TODO JML: Implement pathfinding/navigation logic here
    }
}
