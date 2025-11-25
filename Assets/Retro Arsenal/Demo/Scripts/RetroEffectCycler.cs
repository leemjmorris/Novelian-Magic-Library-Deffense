using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace RetroArsenal
{
    public class RetroEffectCycler : MonoBehaviour
    {
        public List<GameObject> listOfEffects;
        private int effectIndex = 0;

        [Header("Spawn Settings")]
        [SerializeField]
        [Space(10)]
        public float loopLength = 1.0f;
        public float startDelay = 1.0f;
        public bool disableLights = true;
        public bool disableSound = true;
        public bool autoMode = true; // Renamed toggle

        [Header("UI Elements")]
        public Text effectNameText;

        private GameObject currentEffect;

        void Start()
        {
            if (effectNameText == null)
            {
                Debug.LogError("EffectNameText component not assigned in the Inspector.");
                return;
            }

            // Display the initial effect immediately
            PlayEffect();
        }

        public void PlayEffect()
        {
            StartCoroutine(EffectLoop());
            UpdateEffectUI();
        }

        public void NextEffect()
        {
            if (effectIndex < listOfEffects.Count - 1)
            {
                effectIndex++;
            }
            else
            {
                effectIndex = 0;
            }

            RestartEffect();
        }

        public void PreviousEffect()
        {
            if (effectIndex > 0)
            {
                effectIndex--;
            }
            else
            {
                effectIndex = listOfEffects.Count - 1;
            }

            RestartEffect();
        }

        public void ToggleAutoMode()
        {
            autoMode = !autoMode;
            UpdateEffectUI(); // Update UI to reflect the change in auto mode
        }

        private void RestartEffect()
        {
            StopAllCoroutines();

            if (currentEffect != null)
            {
                Destroy(currentEffect);
            }

            PlayEffect();
        }

        private IEnumerator EffectLoop()
        {
            currentEffect = Instantiate(listOfEffects[effectIndex], transform.position, transform.rotation);

            if (disableLights && currentEffect.GetComponent<Light>())
            {
                currentEffect.GetComponent<Light>().enabled = false;
            }

            if (disableSound && currentEffect.GetComponent<AudioSource>())
            {
                currentEffect.GetComponent<AudioSource>().enabled = false;
            }

            // Ensure particle systems start playing automatically
            var particleSystems = currentEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Play();
            }

            yield return new WaitForSeconds(loopLength);

            Destroy(currentEffect);

            if (autoMode)
            {
                NextEffect();
            }
            else
            {
                PlayEffect();
            }
        }

        private void UpdateEffectUI()
        {
            if (effectNameText != null)
            {
                string autoModeText = autoMode ? "Auto Mode: ON" : "Auto Mode: OFF";
                effectNameText.text = $"{listOfEffects[effectIndex].name} ({effectIndex + 1} of {listOfEffects.Count})";
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextEffect();
            }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                PreviousEffect();
            }

            // Assuming you have a key to toggle auto mode, for example 'T'
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleAutoMode();
            }
        }
    }
}
