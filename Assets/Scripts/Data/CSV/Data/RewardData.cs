public class RewardData
{
    public int Reward_ID { get; set; }
    public int Item_ID { get; set; }
    public int Min_Count { get; set; }
    public int Max_Count { get; set; }
    public float Probability { get; set; }  // 확률
    public bool Is_Fixed { get; set; }
}
