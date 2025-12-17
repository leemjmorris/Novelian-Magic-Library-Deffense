using UnityEngine;

/// <summary>
/// 카메라 위치/회전 저장용 마커 컴포넌트
/// 맵 프리팹 내부에 배치하여 CinemachineCamera에 카메라 설정을 전달
/// Transform.position = 카메라 위치
/// Transform.eulerAngles = 카메라 회전
/// </summary>
public class CameraTarget : MonoBehaviour
{
    /// <summary>
    /// 카메라 위치 반환
    /// </summary>
    public Vector3 GetCameraPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// 카메라 회전 반환 (Euler Angles)
    /// </summary>
    public Vector3 GetCameraRotation()
    {
        return transform.eulerAngles;
    }

    /// <summary>
    /// 카메라 Quaternion 회전 반환
    /// </summary>
    public Quaternion GetCameraQuaternion()
    {
        return transform.rotation;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 카메라 위치 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        // 카메라 아이콘 표시
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 카메라 방향 표시 (forward 방향)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);

        // 카메라 시야 범위 시각화 (간단한 피라미드 형태)
        DrawCameraFrustum();
    }

    private void DrawCameraFrustum()
    {
        float length = 5f;
        float angle = 30f; // 대략적인 FOV 절반

        Vector3 forward = transform.forward * length;
        Vector3 up = transform.up * Mathf.Tan(angle * Mathf.Deg2Rad) * length;
        Vector3 right = transform.right * Mathf.Tan(angle * Mathf.Deg2Rad) * length * 1.5f; // 가로 비율

        Vector3 topLeft = transform.position + forward + up - right;
        Vector3 topRight = transform.position + forward + up + right;
        Vector3 bottomLeft = transform.position + forward - up - right;
        Vector3 bottomRight = transform.position + forward - up + right;

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawLine(transform.position, topLeft);
        Gizmos.DrawLine(transform.position, topRight);
        Gizmos.DrawLine(transform.position, bottomLeft);
        Gizmos.DrawLine(transform.position, bottomRight);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

    private void OnDrawGizmosSelected()
    {
        // 선택 시 라벨 표시
        string rotationInfo = $"Rot: ({transform.eulerAngles.x:F0}, {transform.eulerAngles.y:F0}, {transform.eulerAngles.z:F0})";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
            $"CameraTarget\nPos: {transform.position}\n{rotationInfo}");
    }
#endif
}
