using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Layer_lab._3D_Casual_Character
{
    public class ButtonAnimation : MonoBehaviour
    {
        [SerializeField] private TMP_Text textAnimationName;
        [SerializeField] private Button button; 
        [SerializeField] private Image imageIcon; 

        public void SetButton(AnimationClip clip, Sprite sprite)
        {
            textAnimationName.text = clip.name;
            button.onClick.AddListener(()=>
            {
                UIControl.Instance.SetPlayAnimationName(clip.name);
                CharacterBase.Instance.PlayAnimation(clip);
            });
            imageIcon.sprite = sprite;
            imageIcon.SetNativeSize();
        }

    }
}
