using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Layer_lab._3D_Casual_Character
{
    public enum PartsType
    {
        Earrings,
        Eyes,
        Brows,
        HairAcc,
        RightHand,
        Mouth,
        Mask,
        Bracelet,
        LeftHand,
        Hair,
        Face,
        Headgear,
        Top,
        Bottom,
        Bag,
        Shoes,
        Glove,
        EyeWear,
        Body
    }
    
    public class CharacterBase : MonoBehaviour
    {
        public static CharacterBase Instance { get; set; }
        private List<Parts> PartsList { get; set; } = new();
        private Animator Animator { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Init()
        {
            PartsList.Clear();
            PartsList = transform.GetComponentsInChildren<Parts>().ToList();
            Animator = transform.GetComponentInChildren<Animator>();
        }
        
        public void PlayAnimation(AnimationClip clip)
        {
            Animator.CrossFadeInFixedTime(clip.name, 0.25f);
        }

        

        public void SavePrefab()
        {
            #if UNITY_EDITOR
            string localPath = "Assets/Layer lab/3D Props Casual Character Pack1/Prefabs/"+ gameObject.name +".prefab";
            bool isPrefabSuccess;

            GameObject instanceObject = Instantiate(gameObject);
            instanceObject.transform.localPosition = Vector3.zero;
            instanceObject.transform.rotation = Quaternion.identity;
            instanceObject.transform.localScale = Vector3.one;
            
            PrefabUtility.SaveAsPrefabAsset(instanceObject, localPath, out isPrefabSuccess);
            if (isPrefabSuccess)
                Debug.Log("Prefab was saved successfully");
            else
                Debug.Log("Prefab failed to save" + isPrefabSuccess);
            
            Destroy(instanceObject);
            AssetDatabase.Refresh();
            #endif
        }

        
        public void SetItem(PartsType partsType, int idx)
        {
            for (int i = 0; i < PartsList.Count; i++)
            {
                if(PartsList[i].PartsType != partsType) continue;
                PartsList[i].SetParts(idx);
            }
        }

        public void SetRandom()
        {
            // PartsList.Clear();
            // PartsList = transform.GetComponentsInChildren<Parts>().ToList();
            
            // for (int i = 0; i < PartsList.Count; i++)
            // {
            //     var parts = PartsList[i];
            //     PartsList[i].SetParts(Random.Range(PartsList[i].UseEmpty ? -1 : 0, parts.parts.Count - 1));
            // }
        }
        
        public void UnEquipPartsByType(PartsType type)
        {
            for (int i = 0; i < PartsList.Count; i++)
            {
                if (PartsList[i].PartsType == type)
                {
                    PartsList[i].AllUnEquipItem();
                }
            }
        }

    }
}
        

