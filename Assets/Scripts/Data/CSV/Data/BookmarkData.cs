using System;
using System.Collections.Generic;

[Serializable]
public class BookmarkData
{
    public int Bookmark_ID { get; set; }
    public int Bookmark_Name_ID { get; set; }
    public int Grade_ID { get; set; }
    public int Option_ID { get; set; }
    public int Skill_ID { get; set; }
}
[Serializable]
public class BookmarkCraftData
{
    public int Recipe_ID { get; set; }
    public int Recipe_Name_ID { get; set; }
    public BookmarkType Recipe_Type { get; set; }
    public int Material_1_ID { get; set; }
    public int Material_1_Count { get; set; }
    public int Material_2_ID { get; set; }
    public int Material_2_Count { get; set; }
    public int Material_3_ID { get; set; }
    public int Material_3_Count { get; set; }
    public int Currency_ID { get; set; }
    public int Currency_Count { get; set; }
    public float Success_Rate { get; set; }
    public float Great_Success_Rate { get; set; }
    public int Result_ID { get; set; }
    public int Great_Result_ID { get; set; }
}

[Serializable]
public class BookmarkOptionData
{
    public int Option_ID { get; set; }
    public int Option_Name_ID { get; set; }
    public int Grade { get; set; }
    public OptionType Option_Type { get; set; }
    public int Option_Value { get; set; }
}

[Serializable]
public class BookmarkListData
{
    public int List_ID { get; set; }
    public int List_Name_ID { get; set; }
    public int Option_1_ID { get; set; }
    public int Option_2_ID { get; set; }
    public int Option_3_ID { get; set; }
    public int Option_4_ID { get; set; }
}