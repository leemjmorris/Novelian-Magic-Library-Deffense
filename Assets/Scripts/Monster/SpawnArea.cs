using UnityEngine;

public class SpawnArea : MonoBehaviour
{
    private float fixedZ = -7.5f; // JML: Lock Z-axis

     public Vector3 GetRandomPosition()
    {
        Vector3 center = transform.position;
        
        Vector3 scale = transform.localScale;
        
        float randomX = Random.Range(
            center.x - scale.x / 2, 
            center.x + scale.x / 2
        );
        float randomY = Random.Range(
            center.y - scale.y / 2, 
            center.y + scale.y / 2
        );
        
        return new Vector3(randomX, randomY, fixedZ);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 center = transform.position;
        center.z = fixedZ;
        
        Vector3 size = new Vector3(transform.localScale.x, transform.localScale.y, 0.1f);
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
