using UnityEngine;

public class BookMark
{
    private string name; // JML: Example default name in Korean
    private Grade grade;
    private int type;
    private float optionValue;

    /// <summary>
    /// JML: Constructor for BookMark
    /// </summary>
    /// <param name="name">Item Name</param>
    /// <param name="grade">Item Grade</param>
    /// <param name="type">Item Type</param>
    /// <param name="optionValue">Item Value</param>
    public BookMark(string name = "책갈피", Grade grade = Grade.Common, int type = 0, float optionValue = 0)
    {
        this.name = name;
        this.grade = grade;
        this.type = type;
        this.optionValue = optionValue;
    }

    public BookMark GetBookMark()
    {
        return this;
    }

    public override string ToString()
    {
        return $"현재 책갈피 : 이름: {name}, 등급: {grade}, 타입: {type}, 옵션 값: {optionValue}";
    }

    //TODO JML: 2차 주간빌드 하고삭제 예정 - 인벤토리 표시용 Getter
    public string GetName() => name;
    public Grade GetGrade() => grade;
    public int GetOptionType() => type;
    public float GetOptionValue() => optionValue;
}
