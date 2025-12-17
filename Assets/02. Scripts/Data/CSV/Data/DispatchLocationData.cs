using System;

[Serializable]
public class DispatchLocationData
{
    public int Dispatch_Location_ID { get; set; }
    public int Dispatch_ID { get; set; }
    public int Dispatch_Location_Name_ID { get; set; }
    public DispatchLocation Dispatch_Location { get; set; }
}
