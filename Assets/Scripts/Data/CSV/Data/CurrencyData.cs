using System;

[Serializable]
public class CurrencyData
{
   public int Currency_ID { get; set; }
   public int Currency_Name_ID { get; set; }
   public CurrencyType Currency_Type { get; set; }
   public int Currency_Max_Count { get; set; }
   public bool Currency_Purchase { get; set; }
   public bool Currency_Consume { get; set; }
}