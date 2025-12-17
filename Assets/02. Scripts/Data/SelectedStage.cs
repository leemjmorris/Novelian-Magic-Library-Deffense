/// <summary>
/// 선택된 스테이지 정보를 씬 간에 전달하기 위한 static 클래스
/// StageScene에서 설정 → GameScene에서 사용
/// </summary>
public static class SelectedStage
{
    /// <summary>
    /// 선택된 스테이지 데이터 (CSV에서 로드됨)
    /// </summary>
    public static StageData Data { get; set; }

    /// <summary>
    /// 선택된 스테이지가 있는지 확인
    /// </summary>
    public static bool HasSelection => Data != null;

    /// <summary>
    /// 선택 초기화 (GameScene 종료 시 호출)
    /// </summary>
    public static void Clear()
    {
        Data = null;
    }
}
