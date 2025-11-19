using UnityEngine;

/// <summary>
/// Automatically configure Physics 2D Layer Collision Matrix on game start
/// </summary>
public class PhysicsSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigurePhysics2DLayers()
    {
        // Layer indices
        int wallLayer = LayerMask.NameToLayer("Wall");  // Layer 3
        int uiLayer = LayerMask.NameToLayer("UI");  // Layer 5 (used by Characters)
        int projectileLayer = LayerMask.NameToLayer("Projectile");  // Layer 6
        int monsterLayer = LayerMask.NameToLayer("Monster");  // Layer 7

        if (wallLayer == -1 || projectileLayer == -1 || monsterLayer == -1)
        {
            Debug.LogWarning("[PhysicsSetup] Some layers not found. Using default collision settings.");
            return;
        }

        // Configure collision matrix:
        // 1. Projectile should IGNORE Wall (pass through)
        Physics2D.IgnoreLayerCollision(projectileLayer, wallLayer, true);

        // 2. Projectile should COLLIDE with Monster (hit detection)
        Physics2D.IgnoreLayerCollision(projectileLayer, monsterLayer, false);

        // 3. Monster should COLLIDE with Wall (blocked by wall)
        Physics2D.IgnoreLayerCollision(monsterLayer, wallLayer, false);

        // 4. Projectile should IGNORE UI/Character (pass through friendly units)
        if (uiLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(projectileLayer, uiLayer, true);
        }

        Debug.Log("[PhysicsSetup] ✅ Physics 2D layer collision matrix configured!");
        Debug.Log($"  → Projectile (Layer {projectileLayer}) IGNORES Wall (Layer {wallLayer})");
        Debug.Log($"  → Projectile (Layer {projectileLayer}) COLLIDES with Monster (Layer {monsterLayer})");
        Debug.Log($"  → Monster (Layer {monsterLayer}) COLLIDES with Wall (Layer {wallLayer})");
        if (uiLayer != -1)
        {
            Debug.Log($"  → Projectile (Layer {projectileLayer}) IGNORES UI/Character (Layer {uiLayer})");
        }
    }
}
