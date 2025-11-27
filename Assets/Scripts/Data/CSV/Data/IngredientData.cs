using System;
using Unity.VisualScripting;

[Serializable]
public class IngredientData
{
    public int Ingredient_ID { get; set; }
    public int Ingredient_Name_ID { get; set; }
    public int Grade_ID { get; set; }
    public UseType Use_Type { get; set; }
    public bool Inventory { get; set; }
    public int Max_Stack { get; set; }
    public int Max_Count { get; set; }
}