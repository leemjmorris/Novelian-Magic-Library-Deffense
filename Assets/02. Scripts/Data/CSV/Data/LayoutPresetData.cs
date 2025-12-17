/// <summary>
/// CSV 데이터 클래스: LayoutPresetTable.csv
/// 스테이지별 맵 레이아웃 프리셋 정보
///
/// 자가 완결형 맵 프리팹 시스템:
/// - 그리드, 카메라, 스폰 위치 등 모든 설정은 맵 프리팹 내부 마커 컴포넌트에 저장
/// - CSV는 맵 프리팹 키만 저장하여 동기화 문제 해결
/// </summary>
public class LayoutPresetData
{
    /// <summary>
    /// 레이아웃 고유 ID
    /// </summary>
    public int Layout_ID { get; set; }

    /// <summary>
    /// 레이아웃 이름 (예: Top_60, Center_90, Dual_90)
    /// </summary>
    public string Layout_Name { get; set; }

    /// <summary>
    /// Addressables 맵 프리팹 키 (예: GrassMapTop60, GrassMapDual)
    /// 맵 프리팹 내부에 GridMarker, CameraTarget, SpawnArea 등 모든 설정 포함
    /// </summary>
    public string Map_Prefab_Key { get; set; }
}
