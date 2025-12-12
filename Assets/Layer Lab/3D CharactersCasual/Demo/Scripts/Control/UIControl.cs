using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    public class UIControl : MonoBehaviour
    {
        public static UIControl Instance { get; private set; }
        
        [field: SerializeField] public PartsUI PartsUI { get; set; }
        [field: SerializeField] public AnimationUI AnimationUI { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Init()
        {
            AnimationUI.Init();
            PartsUI.Init();
        }

        public void SetPlayAnimationName(string name)
        {
            AnimationUI.textAnimationName.text = name;
        }
    }
}