using UnityEngine;

public abstract class MonsterBase : MonoBehaviour, IPoolable
{
    public static event System.Action<MonsterBase> OnMonsterDied;
    protected virtual void Die()
    {
        Debug.Log("Monster died.");
        OnMonsterDied?.Invoke(this);
    }
    public abstract void OnDespawn();
    public abstract void OnSpawn();
}
