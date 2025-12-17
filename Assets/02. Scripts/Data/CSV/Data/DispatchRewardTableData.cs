using System;

[Serializable]
public class DispatchRewardTableData
{
    public int Dispatch_Reward_ID { get; set; }
    public int Dispatch_Location_ID { get; set; }
    public int Dispatch_Time_ID { get; set; }
    public int Reward_Group_ID { get; set; }
    public float Reward_Multiplier { get; set; }
}
