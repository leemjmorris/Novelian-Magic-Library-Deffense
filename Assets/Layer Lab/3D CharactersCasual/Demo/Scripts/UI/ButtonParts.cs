using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Layer_lab._3D_Casual_Character
{
    public class ButtonParts : MonoBehaviour
    {
        private int _index;
        [SerializeField] private TMP_Text textTitle;
        [SerializeField] private TMP_Text textNum;
        [SerializeField] private Image imageIcon;

        private Parts Parts { get; set; }
        
        public void SetButton(Parts parts)
        {
            Parts = parts;
            
            imageIcon.sprite = Parts.SpriteIcon;
            imageIcon.SetNativeSize();
            textTitle.text = Parts.PartsType.ToString();
            
            SetParts();
        }
    
        private void SetParts()
        {
            if (Parts.UseEmpty)
            {
                CharacterBase.Instance.SetItem(Parts.PartsType, -1);
                _index = -1;
            }
            else
            {
                CharacterBase.Instance.SetItem(Parts.PartsType, 0);
            }

            _SetTitle();
        }

  
    
        public void OnClick_Next()
        {
            _index++;
        
            if (Parts.UseEmpty)
            {
                if (_index >= Parts.parts.Count) _index = -1;
            }
            else
            {
                if (_index >= Parts.parts.Count) _index = 0;
            }
        
            _SetParts();
            _SetTitle();
        }

        public void OnClick_Previous()
        {
            _index--;

            if (Parts.UseEmpty)
            {
                if (_index < -1) _index = Parts.parts.Count - 1;
            }
            else
            {
                if (_index < 0) _index = Parts.parts.Count - 1;   
            }

            _SetParts();
            _SetTitle();
        }


        private void _SetParts()
        {
            CharacterBase.Instance.SetItem(Parts.PartsType, _index);
        }
    

        private void _SetTitle()
        {
            if (!Parts.UseEmpty && _index <= -1 || Parts.UseEmpty && _index <= -1)
            {
                // textTitle.text = "--";
                // textNum.CrossFadeAlpha(0.3f, 0f, true);
                textNum.text = $"-- / {Parts.parts.Count}";
            }
            else
            {
                textNum.text = $"{_index + 1} / {Parts.parts.Count}";
                // string result = Parts.parts[_index].name.Replace("Pack1_", "");
                // result = result.Replace("_", "");
                // textNum.text = result;
                // textNum.CrossFadeAlpha(1f, 0f, true);
            }

            
            
        }

        public void SetRandom()
        {
            for (int i = 0; i < Parts.parts.Count; i++)
            {
                var result = Random.Range(Parts.UseEmpty ? -1 : 0, Parts.parts.Count - 1);
                _index = result;
                Parts.SetParts(result);
            }
            
            _SetTitle();
        }
    }
}
