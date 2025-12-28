using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MobileTouchEffectPack
{
    public class Touch : MonoBehaviour
    {
        public Transform particle_transform;
        public ParticleSystem particle_particle;
        public bool dragPlayMode;
        Vector3 pos_particle;


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {

                MoveParticlePosition();
                particle_particle.Play();

            }

            if (dragPlayMode)
            {
                if (Input.GetMouseButton(0))
                {

                    MoveParticlePosition();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                particle_particle.Stop();
            }
        }


        void MoveParticlePosition()
        {
            pos_particle = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            pos_particle.z = 0;

            particle_transform.localPosition = pos_particle;
        }


    }
}