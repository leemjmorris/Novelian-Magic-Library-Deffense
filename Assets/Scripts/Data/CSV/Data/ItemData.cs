using System;

[Serializable]
public class ItemData
{
    public int Item_ID { get; set; }
    public string Item_Name { get; set; }
    public ItemType Item_Type { get; set; }
    public Grade Item_Grade { get; set; }
    public UseType Use_Type { get; set; }
    public bool Inventory { get; set; }
    public int Max_Stack { get; set; } // 한 슬롯당 최대 스택 개수
    public int Max_Count { get; set; } //9999

    
}
