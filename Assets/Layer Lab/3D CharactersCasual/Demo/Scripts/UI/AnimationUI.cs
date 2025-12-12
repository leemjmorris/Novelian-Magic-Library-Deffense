using System;
using TMPro;
using UnityEngine;

namespace Layer_lab._3D_Casual_Character
{
    public class AnimationUI : MonoBehaviour
    {
        [SerializeField] public TMP_Text textAnimationName;
        [SerializeField] private AnimationClip[] animDance;
        [SerializeField] private AnimationClip[] animIdle;
        [SerializeField] private AnimationClip[] animReaction;
        [SerializeField] private AnimationClip[] animInteraction;
        [SerializeField] private AnimationClip[] animEmoji;
        [SerializeField] private AnimationClip[] animAction;

        [SerializeField] private ButtonAnimation button;
        [SerializeField] private Transform content;

        [SerializeField] private Sprite[] spriteIcons;


        public void Init()
        {
            SpawnAnimationButton(animIdle, "idle");
            SpawnAnimationButton(animAction, "action");
            SpawnAnimationButton(animReaction, "reaction");
            SpawnAnimationButton(animInteraction, "interaction");
            SpawnAnimationButton(animEmoji, "emotion");
            SpawnAnimationButton(animDance, "dance");
            
            button.gameObject.SetActive(false);
            
            textAnimationName.text = "Stand_Idle1";
        }


        private Sprite GetSprite(string name)
        {
            foreach (var sprite in spriteIcons)
            {
                if (sprite.name.Contains(name))
                {
                    return sprite;
                }
            }

            return null;
        }
    
        public void SpawnAnimationButton(AnimationClip[] animationClips, string name)
        {
            for (var i = 0; i < animationClips.Length; i++)
            {
                var buttonAnimation = Instantiate(button, content, false);
                buttonAnimation.SetButton(animationClips[i], GetSprite(name));
            }
        }
    
    
    }
}
