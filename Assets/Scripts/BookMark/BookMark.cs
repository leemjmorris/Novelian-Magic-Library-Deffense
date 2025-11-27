using System;
using UnityEngine;

public class BookMark
{
    // JML: BookMark Unique Identifier
    public int UniqueID { get; private set; }

    // JML: BookMark Basic Info
    public int BookmarkDataID { get; private set; }
    public string Name { get; private set; }
    public Grade Grade { get; private set; }
    public BookmarkType Type { get; private set; }

    // JML: BookMark Skill ID
    public int SkillID { get; private set; }

    // JML: BookMark Stat Info
    public int OptionType { get; private set; }
    public float OptionValue { get; private set; }

    // JML: BookMark Creation Time
    public DateTime CreatedTime { get; private set; }

    // JML: BookMark Equip Info
    public bool IsEquipped { get; private set; }
    public int EquippedLibrarianID { get; private set; }
    public int EquipSlotIndex { get; private set; }

    private static int nextUniqueID = 1;

    /// <summary>
    /// Stat BookMark Constructor
    /// </summary>
    /// <param name="bookmarkDataID"></param>
    /// <param name="name"></param>
    /// <param name="grade"></param>
    /// <param name="optionType"></param>
    /// <param name="optionValue"></param>
    public BookMark(int bookmarkDataID, string name, Grade grade, int optionType, float optionValue)
    {
        UniqueID = GenerateUniqueID();
        BookmarkDataID = bookmarkDataID;
        Name = name;
        Grade = grade;
        Type = BookmarkType.Stat;
        OptionType = optionType;
        OptionValue = optionValue;
        SkillID = -1;
        CreatedTime = DateTime.Now;
        IsEquipped = false;
        EquippedLibrarianID = -1;
        EquipSlotIndex = -1;
    }

    /// <summary>
    /// SKill BookMark Constructor
    /// </summary>
    public BookMark(int bookmarkDataID, string name, Grade grade, int optionType, int optionValue, int skillID)
    {
        UniqueID = GenerateUniqueID();
        BookmarkDataID = bookmarkDataID;
        Name = name;
        Grade = grade;
        Type = BookmarkType.Skill;
        OptionType = optionType;
        OptionValue = optionValue;
        SkillID = skillID;
        CreatedTime = DateTime.Now;
        IsEquipped = false;
        EquippedLibrarianID = -1;
        EquipSlotIndex = -1;
    }

    private static int GenerateUniqueID()
    {
        return nextUniqueID++;
    }

    public void Equip(int librarianID, int slotIndex)
    {
        IsEquipped = true;
        EquippedLibrarianID = librarianID;
        EquipSlotIndex = slotIndex;
    }

    public void Unequip()
    {
        IsEquipped = false;
        EquippedLibrarianID = -1;
        EquipSlotIndex = -1;
    }

    public override string ToString()
    {
        string typeStr = Type == BookmarkType.Stat ? "스탯 책갈피" : "스킬 책갈피";
        string equipStatus = IsEquipped ? $"[장착됨 - 사서{EquippedLibrarianID}]" : "[미장착]";

        // JML: Get Grade Name from CSV
        string gradeName = GetGradeName(Grade);

        if (Type == BookmarkType.Stat)
        {
            // JML: Get Option Type Name
            string optionTypeName = GetOptionTypeName(OptionType);
            return $"[고유번호: {UniqueID}] {Name} ({typeStr}, 등급: {gradeName}, 옵션: {optionTypeName} +{OptionValue}) {equipStatus}";
        }
        else
        {
            return $"[고유번호: {UniqueID}] {Name} ({typeStr}, 등급: {gradeName}, 스킬ID: {SkillID}) {equipStatus}";
        }
    }

    /// <summary>
    /// JML: Get Grade Name from Grade Enum
    /// </summary>
    public string GetGradeName(Grade grade)
    {
        // JML: Grade enum value is already Grade_ID (151~155), use it directly
        int gradeID = (int)grade;
        var gradeData = CSVLoader.Instance.GetData<GradeData>(gradeID);
        if (gradeData == null) return grade.ToString();
        return CSVLoader.Instance.GetData<StringTable>(gradeData.Grade_Name_ID)?.Text ?? grade.ToString();
    }

    /// <summary>
    /// JML: Get Option Type Name
    /// </summary>
    private string GetOptionTypeName(int optionType)
    {
        switch (optionType)
        {
            case 1: // OptionType.AttackPower
                return "공격력";
            default:
                return ((OptionType)optionType).ToString();
        }
    }
}
