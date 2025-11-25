using UnityEngine;

// This script is used for sprites that should always be facing the camera horizontally.

namespace RetroArsenal
{

public class RetroRotateTowardsCamera : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found in the scene!");
        }
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0f;

            if (directionToCamera != Vector3.zero)
            {
                float targetAngle = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;

                transform.rotation = Quaternion.Euler(-90f, targetAngle, 0f);
            }
        }
    }
}

}