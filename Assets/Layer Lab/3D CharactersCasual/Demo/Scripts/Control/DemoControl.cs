using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    public class DemoControl : MonoBehaviour
    {
        public static DemoControl Instance;
        
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CharacterBase.Instance.Init();
            UIControl.Instance.Init();
        }
    }
}
