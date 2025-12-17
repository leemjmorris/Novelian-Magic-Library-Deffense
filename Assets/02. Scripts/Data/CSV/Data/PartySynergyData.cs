public class PartySynergyData
{
    public int Party_ID { get; set; }
    public int Party_Name_ID { get; set; }
    public int Party_Size { get; set; }

    // 캐릭터 ID
    public int Req_Char_1_ID { get; set; }
    public int Req_Char_2_ID { get; set; }
    public int Req_Char_3_ID { get; set; }
    public int Req_Char_4_ID { get; set; }

    // 효과 1
    public int Party_Upgrade_Lv1_ID { get; set; }
    public int Effect_1_ID { get; set; }
    public float Effect_1_Value { get; set; }

    // 효과 2
    public int Party_Upgrade_Lv2_ID { get; set; }
    public int Effect_2_ID { get; set; }
    public float Effect_2_Value { get; set; }

    // 효과 3
    public int Party_Upgrade_Lv3_ID { get; set; }
    public int Effect_3_ID { get; set; }
    public float Effect_3_Value { get; set; }

    // 효과 4
    public int Party_Upgrade_Lv4_ID { get; set; }
    public int Effect_4_ID { get; set; }
    public float Effect_4_Value { get; set; }

    // 효과 5
    public int Party_Upgrade_Lv5_ID { get; set; }
    public int Effect_5_ID { get; set; }
    public float Effect_5_Value { get; set; }
}