using System;
using System.Collections.Generic;

[Serializable]
public class BookmarkCraftData
{
    public int Recipe_ID { get; set; }
    public int Material_1_ID { get; set; }
    public int Material_1_Count { get; set; }
    public int Material_2_ID { get; set; }
    public int Material_2_Count { get; set; }
    public float Success_Rate { get; set; }
    public float Great_Success_Rate { get; set; }
    public int Result_Grade { get; set; }
    public int Great_Result_Grade { get; set; }
}

[Serializable]
public class BookmarkResultData
{
    public int Result_ID { get; set; }
    public int Grade { get; set; }
    public int Min_Option_ID { get; set; }
    public int Max_Option_ID { get; set; }
}

[Serializable]
public class BookmarkOptionData
{
    public int Option_ID { get; set; }
    public string Option_Name { get; set; }
    public int Grade { get; set; }
    public string Min_Value { get; set; }
    public string Max_Value { get; set; }
    public int Bookmark_1_ID { get; set; }
    public int Bookmark_2_ID { get; set; }
    public int Bookmark_3_ID { get; set; }
    public int Bookmark_4_ID { get; set; }
    public int OptionMaster { get; set; }
    public string Description { get; set; } 
}

[Serializable]
public class BookmarkItemData
{
    public int Bookmark_ID { get; set; }
    public int Grade { get; set; }
    public int Option_Type { get; set; }
    public float Option_Value { get; set; }
}