using UnityEngine;

//JML: Generic movement controller for all movable entities using Rigidbody2D
public class MonsterMove : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator monsterAnimator;

    //JML: Generic movement method using velocity-based physics
    public void Move<T>(T entity, float speed) where T : IMovable
    {
        if (!entity.IsWallHit)
        {
            rb.linearVelocity = Vector2.down * speed;
            monsterAnimator.SetBool("1_Move", true);
        }
        else
        {
            //rb.linearVelocity = Vector2.zero;
            monsterAnimator.SetBool("1_Move", false);
        }
    }

    //JML: Future navigation logic placeholder
    public void Navigate(Transform target)
    {
        //TODO JML: Implement pathfinding/navigation logic here
    }
}
