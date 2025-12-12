using UnityEngine;
using System.Collections.Generic;

namespace Layer_lab._3D_Casual_Character
{
    public class PartsUI : MonoBehaviour
    {
        [SerializeField] private ButtonParts button;
        [SerializeField] private Transform content;
        private List<ButtonParts> _buttonParts = new();

        private Parts[] PartsArray;

        public void Init()
        {
            PartsArray = FindObjectsByType<Parts>(FindObjectsSortMode.None);
            foreach (var t in PartsArray)
            {
                var buttonParts = Instantiate(button, content, false);
                _buttonParts.Add(buttonParts);
                buttonParts.SetButton(t);
            }
            button.gameObject.SetActive(false);
        }


        public void SetAllRandom()
        {
            foreach (var t in _buttonParts) t.SetRandom();
        }

    }
}
