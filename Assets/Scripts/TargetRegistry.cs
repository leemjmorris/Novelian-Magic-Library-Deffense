using System.Collections.Generic;
using UnityEngine;

public class TargetRegistry
{
    private static TargetRegistry instance;
    public static TargetRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new TargetRegistry();
            }
            return instance;
        }
    }

    private List<ITargetable> targets = new List<ITargetable>();

    private TargetRegistry() { }

    /// <summary>
    /// JML: Initialize and register a targetable entity.
    /// </summary>
    /// <param name="target">The targetable entity to register.</param>
    public void RegisterTarget(ITargetable target)
    {
        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    /// <summary>
    /// JML: Unregister a targetable entity.
    /// </summary>
    /// <param name="target">The targetable entity to unregister.</param>
    public void UnregisterTarget(ITargetable target)
    {
        if (targets.Contains(target))
        {
            targets.Remove(target);
        }
    }

    /// <summary>
    /// JML: Get all registered targetable entities.
    /// </summary>
    public List<ITargetable> GetAllTargets()
    {
        // Clean up null or dead targets
        targets.RemoveAll(t => t == null || !t.IsAlive());

        return targets;
    }

    /// <summary>
    /// JML: Find the closest targetable entity within a specified range.
    /// </summary>
    /// <param name="position"> The position from which to search for targets.</param>
    /// <param name="range"> The maximum range within which to search for targets.</param>
    public ITargetable FindTarget(Vector3 position, float range)
    {
        ITargetable closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (var target in targets)
        {
            if (!target.IsAlive()) continue;

            float distance = Vector3.Distance(position, target.GetPosition());

            if (distance <= range && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }
        return closestTarget;
    }

    /// <summary>
    /// JML: Clear all registered targets.
    /// </summary>
    public void Clear()
    {
        targets.Clear();
    }
}
