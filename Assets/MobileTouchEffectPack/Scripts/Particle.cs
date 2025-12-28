using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileTouchEffectPack{
public class Particle : MonoBehaviour {
    ParticleSystem thisParticle;

	// Use this for initialization
	void Start () {

        thisParticle = this.GetComponent<ParticleSystem>();
            AutoPlayParticleLoop();
	}
	

    void AutoPlayParticleLoop()
    {
        thisParticle.Play();
        Invoke("AutoPlayParticleLoop", 1);
    }

    }


}
