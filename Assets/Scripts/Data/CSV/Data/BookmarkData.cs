using System;
using System.Collections.Generic;

[Serializable]
public class BookmarkData
{
    public int Bookmark_ID { get; set; }
    public string Bookmark_Name { get; set; }
    public int Grade_ID { get; set; }
    public int Option_ID { get; set; }
}
[Serializable]
public class BookmarkCraftData
{
    public int Recipe_ID { get; set; }
    public string Recipe_Name { get; set; }

    // JML: Material fields
    public int Material_1_ID { get; set; }
    public int Material_1_Count { get; set; }
    public int Material_2_ID { get; set; }
    public int Material_2_Count { get; set; }
    public int Material_3_ID { get; set; }
    public int Material_3_Count { get; set; }

    // JML: Currency fields
    public int Currency_ID { get; set; }
    public int Currency_Count { get; set; }

    // JML: Success rates
    public float Success_Rate { get; set; }
    public float Great_Success_Rate { get; set; }

    // JML: Resulting bookmark IDs
    public int Result_ID { get; set; }
    public int Great_Result_ID { get; set; }
}

[Serializable]
public class BookmarkResultData
{
    public int Result_ID { get; set; }
    public int Grade { get; set; }
    public int Option_ID { get; set; }
}

[Serializable]
public class BookmarkOptionData
{
    public int Option_ID { get; set; }
    public string Option_Name { get; set; }
    public int Grade { get; set; }
    public int Option_Type { get; set; }
    public float Option_Value { get; set; }
}

[Serializable]
public class BookmarkListData
{
    public int List_ID { get; set; }
    public string List_Name { get; set; }
    public int Option_1_ID { get; set; }
    public int Option_2_ID { get; set; }
    public int Option_3_ID { get; set; }
    public int Option_4_ID { get; set; }
}

[Serializable]
public class BookmarkSkillData
{
    public int Bookmark_Skill_ID { get; set; }
    public string Bookmark_Skill_Name { get; set; }
    public int Grade { get; set; }
    public int Option_Type { get; set; }
    public int Option_Value { get; set; }
    public int Effect_ID { get; set; }
}