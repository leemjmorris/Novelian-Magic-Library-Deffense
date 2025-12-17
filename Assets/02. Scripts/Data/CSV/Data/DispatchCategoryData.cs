using System;

[Serializable]
public class DispatchCategoryData
{
    public int Dispatch_ID { get; set; }
    public int Dispatch_Name_ID { get; set; }
    public DispatchType Dispatch_Category { get; set; }  // 1: Combat(전투형), 2: Collection(채집형)
}
