using System;

[Serializable]
public class ItemData
{
    public int Item_ID { get; set; }
    public string Item_Name { get; set; }
    public ItemType Item_Type { get; set; }
    public ItemGrade Item_Grade { get; set; }
    public UseType Use_Type { get; set; }
    public bool Inventory { get; set; }
    public int Max_Count { get; set; }
}
