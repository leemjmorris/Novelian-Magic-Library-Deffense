using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileTouchEffectPack
{ 

public class CloseButton : MonoBehaviour
    {
        public GameObject tutorialObg;

       
        public void CloseObj()
        {
            tutorialObg.SetActive(false);
        }
    }
}