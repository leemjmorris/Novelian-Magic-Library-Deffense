using Layer_lab._3D_Casual_Character;
using UnityEngine;

public class PartsItem : MonoBehaviour
{
     public int Idx { get; set; }
    private bool IsEquip { get; set; }
    

    private PartsType MyPartsType { get; set; }

    public void SetItem(PartsType type, int idx)
    {
        MyPartsType = type;
        Idx = idx;
    }
    
    public void Equip()
    {
        gameObject.SetActive(true);
    }

    public void UnEquip()
    {
        gameObject.SetActive(false);
    }
}
