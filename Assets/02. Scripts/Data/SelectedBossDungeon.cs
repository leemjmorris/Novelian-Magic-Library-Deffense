/// <summary>
/// Issue #476 - 선택된 도전던전 정보를 씬 간에 전달하기 위한 static 클래스
/// LobbyScene에서 설정 → BossDungeonScene에서 사용
/// </summary>
public static class SelectedBossDungeon
{
    /// <summary>
    /// 선택된 도전던전 데이터 (CSV에서 로드됨)
    /// </summary>
    public static BossDungeonData Data { get; set; }

    /// <summary>
    /// 선택된 던전이 있는지 확인
    /// </summary>
    public static bool HasSelection => Data != null;

    /// <summary>
    /// 선택 초기화 (BossDungeonScene 종료 시 호출)
    /// </summary>
    public static void Clear()
    {
        Data = null;
    }
}
