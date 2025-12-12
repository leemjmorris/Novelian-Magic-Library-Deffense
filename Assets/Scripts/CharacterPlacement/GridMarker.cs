using UnityEngine;

/// <summary>
/// 그리드 설정 저장용 마커 컴포넌트
/// 맵 프리팹 내부에 배치하여 CharacterPlacementManager에 그리드 설정을 전달
/// Transform.position이 그리드 중심 위치로 사용됨
///
/// 사용법:
/// - GridMarker 태그: Grid1 위치 (이 오브젝트의 Transform.position)
/// - Grid2Marker 태그: Grid2 위치 (별도 오브젝트로 배치, 양방향 방어 시)
/// </summary>
public class GridMarker : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("그리드 행 수")]
    public int gridRows = 1;

    [Tooltip("그리드 열 수")]
    public int gridColumns = 4;

    [Tooltip("X축 간격")]
    public float gridSpacingX = 2f;

    [Tooltip("Z축 간격")]
    public float gridSpacingZ = 2f;

    [Tooltip("2행 레이아웃에서 행 사이 간격 (ProtectionObj 공간)")]
    public float rowGap = 0f;

    /// <summary>
    /// 그리드 중심 위치 반환 (Transform.position 사용)
    /// </summary>
    public Vector3 GetGridCenterPosition()
    {
        return transform.position;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 그리드 시각화
    /// </summary>
    private void OnDrawGizmos()
    {
        DrawGridGizmos(transform.position, new Color(0f, 1f, 0f, 0.3f), Color.yellow);
    }

    private void DrawGridGizmos(Vector3 centerPos, Color fillColor, Color wireColor)
    {
        float totalWidth = (gridColumns - 1) * gridSpacingX;
        Vector3 cubeSize = new Vector3(gridSpacingX * 0.8f, 0.1f, gridSpacingZ * 0.8f);

        if (gridRows == 1)
        {
            Vector3 startPos = new Vector3(
                -totalWidth / 2f + centerPos.x,
                centerPos.y,
                centerPos.z
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = startPos + new Vector3(col * gridSpacingX, 0f, 0f);
                Gizmos.color = fillColor;
                Gizmos.DrawCube(position, cubeSize);
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(position, cubeSize);
            }
        }
        else if (gridRows == 2)
        {
            // 위쪽 행
            Vector3 topRowStart = new Vector3(
                -totalWidth / 2f + centerPos.x,
                centerPos.y,
                centerPos.z + rowGap / 2f + gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = topRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                Gizmos.color = fillColor;
                Gizmos.DrawCube(position, cubeSize);
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(position, cubeSize);
            }

            // 아래쪽 행
            Vector3 bottomRowStart = new Vector3(
                -totalWidth / 2f + centerPos.x,
                centerPos.y,
                centerPos.z - rowGap / 2f - gridSpacingZ / 2f
            );

            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 position = bottomRowStart + new Vector3(col * gridSpacingX, 0f, 0f);
                Gizmos.color = fillColor;
                Gizmos.DrawCube(position, cubeSize);
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(position, cubeSize);
            }
        }

        // 그리드 경계
        Gizmos.color = Color.cyan;
        float boundaryDepth = gridRows == 2 ? (gridSpacingZ + rowGap) : gridSpacingZ;
        Vector3 gridBoundarySize = new Vector3(gridColumns * gridSpacingX, 0.05f, boundaryDepth);
        Gizmos.DrawWireCube(centerPos, gridBoundarySize);
    }

    private void OnDrawGizmosSelected()
    {
        // 선택 시 라벨 표시
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"GridMarker\n{gridRows}x{gridColumns}\nSpacing: {gridSpacingX}x{gridSpacingZ}");
    }
#endif
}
