using NovelianMagicLibraryDefense.Settings;
using UnityEngine;

/// <summary>
/// WaveSettings의 SpawnAreaData를 씬에서 시각화하는 컴포넌트
/// Gizmo를 통해 스폰 영역을 에디터에서 확인 가능
/// </summary>
public class SpawnAreaVisualizer : MonoBehaviour
{
    [Header("Settings Reference")]
    [Tooltip("시각화할 WaveSettings")]
    [SerializeField] private WaveSettings waveSettings;

    [Header("Visualization Type")]
    [Tooltip("시각화할 스폰 영역 타입")]
    [SerializeField] private SpawnAreaType areaType = SpawnAreaType.Normal;

    [Header("Gizmo Settings")]
    [Tooltip("Gizmo 색상")]
    [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.3f);

    [Tooltip("Gizmo 와이어 색상")]
    [SerializeField] private Color wireColor = Color.yellow;

    public enum SpawnAreaType
    {
        Normal,
        Boss
    }

    private void OnDrawGizmos()
    {
        if (waveSettings == null) return;

        SpawnAreaData areaData = areaType == SpawnAreaType.Normal
            ? waveSettings.normalSpawnArea
            : waveSettings.bossSpawnArea;

        if (areaData == null) return;

        // 영역의 중심과 크기 계산
        Vector3 center = areaData.GetCenter();
        Vector3 size = areaData.GetSize();

        // 반투명 박스 그리기
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, size);

        // 와이어 박스 그리기
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(center, size);

        // 라벨 표시 (선택 사항)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            center + Vector3.up * (size.y / 2 + 0.5f),
            $"{areaType} Spawn Area\n{size.x:F1} x {size.y:F1}"
        );
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (waveSettings == null) return;

        SpawnAreaData areaData = areaType == SpawnAreaType.Normal
            ? waveSettings.normalSpawnArea
            : waveSettings.bossSpawnArea;

        if (areaData == null) return;

        // 선택되었을 때 더 밝은 색상으로 표시
        Vector3 center = areaData.GetCenter();
        Vector3 size = areaData.GetSize();

        Gizmos.color = new Color(wireColor.r, wireColor.g, wireColor.b, 0.8f);
        Gizmos.DrawWireCube(center, size);

        // 코너 마커 표시
        float markerSize = 0.2f;
        Gizmos.color = Color.red;

        Vector3 halfSize = size / 2f;
        Vector3[] corners = new Vector3[]
        {
            center + new Vector3(-halfSize.x, -halfSize.y, 0),
            center + new Vector3(halfSize.x, -halfSize.y, 0),
            center + new Vector3(-halfSize.x, halfSize.y, 0),
            center + new Vector3(halfSize.x, halfSize.y, 0)
        };

        foreach (var corner in corners)
        {
            Gizmos.DrawWireSphere(corner, markerSize);
        }
    }
}
