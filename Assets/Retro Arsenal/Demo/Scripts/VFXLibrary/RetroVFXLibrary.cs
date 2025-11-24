using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RetroArsenalLib
{
    public class RetroVFXLibrary : MonoBehaviour
    {
        public static RetroVFXLibrary GlobalAccess;

        public int TotalEffects = 0;
        public int CurrentParticleEffectIndex = 0;
        public int CurrentParticleEffectNum = 0;
        public Vector3[] ParticleEffectSpawnOffsets;
        public float[] ParticleEffectLifetimes;
        public GameObject[] ParticleEffectPrefabs;

        private List<Transform> currentActivePEList = new List<Transform>();

        private StringBuilder effectNameBuilder = new StringBuilder();

        private void Awake()
		{
			GlobalAccess = this;

			// Calculate the total number of effects from the length of the ParticleEffectPrefabs array
			TotalEffects = ParticleEffectPrefabs.Length;

			if (ParticleEffectSpawnOffsets.Length != TotalEffects || ParticleEffectPrefabs.Length != TotalEffects)
			{
				Debug.LogError("RetroVFXLibrary: Array lengths do not match.");
			}

			UpdateEffectNameString();
		}

        public string GetCurrentPENameString()
        {
            return effectNameBuilder.ToString();
        }

        public void PreviousParticleEffect()
        {
            DestroyLoopingParticleEffects();
            CurrentParticleEffectIndex = (CurrentParticleEffectIndex - 1 + TotalEffects) % TotalEffects;
            UpdateEffectNameString();
        }

        public void NextParticleEffect()
        {
            DestroyLoopingParticleEffects();
            CurrentParticleEffectIndex = (CurrentParticleEffectIndex + 1) % TotalEffects;
            UpdateEffectNameString();
        }

        private void DestroyLoopingParticleEffects()
        {
            if (ParticleEffectLifetimes[CurrentParticleEffectIndex] == 0)
            {
                foreach (var effect in currentActivePEList)
                {
                    if (effect != null)
                        Destroy(effect.gameObject);
                }
                currentActivePEList.Clear();
            }
        }

        private void UpdateEffectNameString()
        {
            effectNameBuilder.Clear();
            effectNameBuilder.Append(ParticleEffectPrefabs[CurrentParticleEffectIndex].name);
            effectNameBuilder.Append(" (");
            effectNameBuilder.Append(CurrentParticleEffectIndex + 1);
            effectNameBuilder.Append("/");
            effectNameBuilder.Append(TotalEffects);
            effectNameBuilder.Append(")");
        }

        public void SpawnParticleEffect(Vector3 positionInWorldToSpawn)
        {
            Vector3 spawnPosition = positionInWorldToSpawn + ParticleEffectSpawnOffsets[CurrentParticleEffectIndex];
            GameObject newParticleEffect = Instantiate(ParticleEffectPrefabs[CurrentParticleEffectIndex], spawnPosition, Quaternion.identity);
            newParticleEffect.name = "PE_" + ParticleEffectPrefabs[CurrentParticleEffectIndex].name;

            if (ParticleEffectLifetimes[CurrentParticleEffectIndex] == 0)
                currentActivePEList.Add(newParticleEffect.transform);

            if (ParticleEffectLifetimes[CurrentParticleEffectIndex] != 0)
                Destroy(newParticleEffect, ParticleEffectLifetimes[CurrentParticleEffectIndex]);
        }
    }
}