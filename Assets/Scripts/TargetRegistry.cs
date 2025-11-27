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
    /// Returns a copy of the list to prevent concurrent modification issues.
    /// </summary>
    public List<ITargetable> GetAllTargets()
    {
        // Clean up null or dead targets
        targets.RemoveAll(t => t == null || !t.IsAlive());

        // Return copy to prevent concurrent modification
        return new List<ITargetable>(targets);
    }

    /// <summary>
    /// JML: Find target with Mark priority, then use specified strategy for non-marked targets.
    /// Priority: Focus Mark (shortest duration) > useWeight ? Weight : Distance
    /// </summary>
    /// <param name="position"> The position from which to search for targets.</param>
    /// <param name="range"> The maximum range within which to search for targets.</param>
    /// <param name="useWeightForNonMarked"> Use weight-based targeting for non-marked targets (default: distance-based)</param>
    public ITargetable FindTarget(Vector3 position, float range, bool useWeightForNonMarked = false)
    {
        ITargetable closestTarget = null;
        float closestDistance = float.MaxValue;
        float bestWeight = float.MinValue;

        ITargetable closestFocusTarget = null;
        float closestFocusRemainingTime = float.MaxValue;
        float closestFocusDistance = float.MaxValue;

        // Create snapshot to prevent concurrent modification during iteration
        var snapshot = new List<ITargetable>(targets);
        foreach (var target in snapshot)
        {
            // JML: Check for null or destroyed objects before calling methods
            if (target == null || !target.IsAlive()) continue;

            float distance = Vector3.Distance(position, target.GetPosition());

            if (distance <= range)
            {
                // Priority 1: Focus Mark targets (prioritize by shortest remaining duration, then distance)
                if (target.HasFocusMark())
                {
                    float remainingTime = target.GetMarkRemainingTime();

                    // Select target with shortest remaining mark duration
                    // If same duration, use distance as tiebreaker
                    if (remainingTime < closestFocusRemainingTime ||
                        (remainingTime == closestFocusRemainingTime && distance < closestFocusDistance))
                    {
                        closestFocusRemainingTime = remainingTime;
                        closestFocusDistance = distance;
                        closestFocusTarget = target;
                    }
                }
                // Priority 2: Non-marked targets (use weight or distance based on flag)
                else
                {
                    if (useWeightForNonMarked)
                    {
                        // Weight-based targeting (higher weight = higher priority)
                        float weight = target.Weight;
                        if (weight > bestWeight || (weight == bestWeight && distance < closestDistance))
                        {
                            bestWeight = weight;
                            closestDistance = distance;
                            closestTarget = target;
                        }
                    }
                    else
                    {
                        // Distance-based targeting (closer = higher priority)
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestTarget = target;
                        }
                    }
                }
            }
        }

        // Return focus target if exists, otherwise return best non-marked target
        return closestFocusTarget ?? closestTarget;
    }

    /// <summary>
    /// JML: Find the highest weight targetable entity within a specified range.
    /// </summary>
    public ITargetable FindSkillTarget(Vector3 position, float range)
    {
        List<ITargetable> validTargets = GetAllTargets();

        ITargetable bestTarget = null;
        float bestWeight = float.MinValue;  // JML: Start with the lowest possible weight
        float closestDistance = float.MaxValue; // JML: To break weight ties by distance

        foreach (var target in validTargets)
        {
            // JML: Additional null check for safety
            if (target == null) continue;

            float distance = Vector3.Distance(position, target.GetPosition());

            if (distance <= range)
            {
                float weight = target.Weight;

                // JML: 1st priority: highest weight, 2nd priority: closest distance
                if (weight > bestWeight ||
                    (weight == bestWeight && distance < closestDistance))
                {
                    bestTarget = target;
                    bestWeight = weight;
                    closestDistance = distance;
                }
            }
        }
        return bestTarget;
    }

    /// <summary>
    /// JML: Clear all registered targets.
    /// </summary>
    public void Clear()
    {
        targets.Clear();
    }
}
