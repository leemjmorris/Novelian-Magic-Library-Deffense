using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RetroArsenalLib
{
    public class RetroLibCanvas : MonoBehaviour
    {
        public static RetroLibCanvas GlobalAccess;

        void Awake()
        {
            GlobalAccess = this;
        }

        public bool MouseOverButton = false;
        public Text PENameText;
        public Text ToolTipText;

        // Use this for initialization
        void Start()
        {
            if (PENameText != null)
                PENameText.text = RetroVFXLibrary.GlobalAccess.GetCurrentPENameString();
        }

        // Update is called once per frame
        void Update()
        {

            // Mouse Click - Check if mouse over button to prevent spawning particle effects while hovering or using UI buttons.
            if (!MouseOverButton)
            {
                // Left Button Click
                if (Input.GetMouseButtonUp(0))
                {
                    // Spawn Currently Selected Particle System
                    SpawnCurrentParticleEffect();
                }
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                SelectPreviousPE();
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                SelectNextPE();
            }
        }

        public void UpdateToolTip(string toolTipText)
        {
            if (ToolTipText != null)
            {
                ToolTipText.text = toolTipText;
            }
        }

        public void ClearToolTip()
        {
            if (ToolTipText != null)
            {
                ToolTipText.text = "";
            }
        }

        private void SelectPreviousPE()
        {
            // Previous
            RetroVFXLibrary.GlobalAccess.PreviousParticleEffect();
            if (PENameText != null)
                PENameText.text = RetroVFXLibrary.GlobalAccess.GetCurrentPENameString();
        }

        private void SelectNextPE()
        {
            // Next
            RetroVFXLibrary.GlobalAccess.NextParticleEffect();
            if (PENameText != null)
                PENameText.text = RetroVFXLibrary.GlobalAccess.GetCurrentPENameString();
        }

        private RaycastHit rayHit;
        private void SpawnCurrentParticleEffect()
        {
            // Spawn Particle Effect
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out rayHit))
            {
                RetroVFXLibrary.GlobalAccess.SpawnParticleEffect(rayHit.point);
            }
        }

        public void UIButtonClick(string buttonTypeClicked)
        {
            switch (buttonTypeClicked)
            {
                case "Previous":
                    // Select Previous Prefab
                    SelectPreviousPE();
                    break;
                case "Next":
                    // Select Next Prefab
                    SelectNextPE();
                    break;
                default:
                    // Nothing
                    break;
            }
        }
    }
}
