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

    // JML: BookMark Skill Effect ID
    public int EffectID { get; private set; }

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
        EffectID = -1;
        CreatedTime = DateTime.Now;
        IsEquipped = false;
        EquippedLibrarianID = -1;
        EquipSlotIndex = -1;
    }

    /// <summary>
    /// SKill BookMark Constructor
    /// </summary>
    public BookMark(int bookmarkDataID, string name, Grade grade, int optionType, int optionValue, int effectID)
    {
        UniqueID = GenerateUniqueID();
        BookmarkDataID = bookmarkDataID;
        Name = name;
        Grade = grade;
        Type = BookmarkType.Skill;
        OptionType = optionType;
        OptionValue = optionValue;
        EffectID = effectID;
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

    public float GetStatBonus()
    {
        return Type == BookmarkType.Stat ? OptionValue : 0f;
    }

    public int GetEffectID()
    {
        return Type == BookmarkType.Skill ? EffectID : -1;
    }

    public override string ToString()
    {
        string typeStr = Type == BookmarkType.Stat ? "스탯" : "스킬";
        string equipStatus = IsEquipped ? $"[장착됨 - 사서{EquippedLibrarianID}]" : "[미장착]";
        
        if (Type == BookmarkType.Stat)
        {
            return $"[{UniqueID}] {Name} ({typeStr}, 등급: {Grade}, 옵션: {OptionValue}) {equipStatus}";
        }
        else
        {
            return $"[{UniqueID}] {Name} ({typeStr}, 등급: {Grade}, 이펙트ID: {EffectID}) {equipStatus}";
        }
    }
}
