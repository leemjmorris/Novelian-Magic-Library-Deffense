using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MobileTouchEffectPack
{
    public class ParticleManager : MonoBehaviour
    {

        public GameObject redParticle;
        public GameObject blueParticle;
        public GameObject yellowParticle;
        public GameObject greenParticle;
        public Text colorName;

        // Use this for initialization
        void Start()
        {
            OffParticlesAll();
            ChangeColor("Red");
        }

        public void ChangeColor(string color)
        {
            //0ff　particle
            OffParticlesAll();
            //On Particle
            switch (color)
            {
                case "Red":
                    redParticle.SetActive(true);
                    colorName.text = "Red";
                    break;
                case "Blue":
                    blueParticle.SetActive(true);
                    colorName.text = "Blue";
                    break;
                case "Yellow":
                    yellowParticle.SetActive(true);
                    colorName.text = "Yellow";
                    break;

                case "Green":
                    greenParticle.SetActive(true);
                    colorName.text = "Green";
                    break;
            }
        }




        void OffParticlesAll()
        {
            redParticle.SetActive(false);
            blueParticle.SetActive(false);
            yellowParticle.SetActive(false);
            greenParticle.SetActive(false);

        }

    }
}