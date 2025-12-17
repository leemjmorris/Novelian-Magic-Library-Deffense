using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    public class Parts : MonoBehaviour
    {
        [SerializeField] public List<PartsItem> parts = new();
        
        [field: SerializeField] public PartsType PartsType { get; set; }
        [field: SerializeField] public Sprite SpriteIcon { get; set; }
        [field: SerializeField] public bool UseEmpty { get; set; }
        
        [field: SerializeField] public Parts[] ExceptionPartsType { get; set; }

        public void SetParts(int index)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (i == index)
                {
                    parts[i].Equip();
                }
                else
                {
                    parts[i].UnEquip();
                }
            }
        }

        public void AllUnEquipItem()
        {
            SetParts(-1);
        }
        

        public void AddPartsItem()
        {
            foreach (Transform t in transform)
            {
                if(t.gameObject == gameObject) continue;
                if (t.GetComponent<PartsItem>() == null)
                {
                    t.gameObject.AddComponent<PartsItem>();
                }
            }
            
            parts.Clear();
            parts = transform.GetComponentsInChildren<PartsItem>(true).ToList();
            for (var i = 0; i < parts.Count; i++)
            {
                parts[i].SetItem(PartsType, i);
            }
        }
    }
}