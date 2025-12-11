/// <summary>
/// CSV 데이터 클래스: LayoutPresetTable.csv
/// 스테이지별 맵 레이아웃 프리셋 정보
/// </summary>
public class LayoutPresetData
{
    // 기본 정보
    public int Layout_ID { get; set; }
    public string Layout_Name { get; set; }

    // 그리드 설정
    public int Grid_Rows { get; set; }
    public int Grid_Columns { get; set; }
    public float Grid_Center_X { get; set; }
    public float Grid_Center_Y { get; set; }
    public float Grid_Center_Z { get; set; }
    public float Grid_Spacing_X { get; set; }
    public float Grid_Spacing_Z { get; set; }
    public float Row_Gap { get; set; }  // 2행 레이아웃에서 행 사이 간격 (ProtectionObj 공간)

    // ProtectionObj 위치
    public float Protection_Pos_X { get; set; }
    public float Protection_Pos_Y { get; set; }
    public float Protection_Pos_Z { get; set; }

    // 카메라 위치/회전
    public float Camera_Pos_X { get; set; }
    public float Camera_Pos_Y { get; set; }
    public float Camera_Pos_Z { get; set; }
    public float Camera_Rot_X { get; set; }
    public float Camera_Rot_Y { get; set; }
    public float Camera_Rot_Z { get; set; }

    // 스폰 영역
    public int Spawn_Area_Count { get; set; }
    public float Spawn_1_X { get; set; }
    public float Spawn_1_Y { get; set; }
    public float Spawn_1_Z { get; set; }
    public float Spawn_2_X { get; set; }
    public float Spawn_2_Y { get; set; }
    public float Spawn_2_Z { get; set; }

    // 맵 프리팹
    public string Map_Prefab_Key { get; set; }

    // 다중 Protection 지원 (양방향 방어 레이아웃)
    public int Protection_Count { get; set; }  // 1 또는 2 (기본값: 1)
    public float Protection_2_Pos_X { get; set; }
    public float Protection_2_Pos_Y { get; set; }
    public float Protection_2_Pos_Z { get; set; }

    // 그리드 분리 모드 (양방향 방어 레이아웃)
    public int Grid_Split_Mode { get; set; }  // 0=단일, 1=분리 (기본값: 0)
    public float Grid_2_Center_X { get; set; }
    public float Grid_2_Center_Y { get; set; }
    public float Grid_2_Center_Z { get; set; }
}
