using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour {

	ParticleSystem fogParticleSystem;

	ParticleSystem.Particle[] particles;

	public GameObject subQuad;

	public float minScale;
	public float maxScale;


	void Start () {

		fogParticleSystem = GetComponent <ParticleSystem> ();

		Invoke ("StopSimulation", 2);

	}

	void StopSimulation(){
	
		print ("stopping");
		ParticleSystem.MainModule main = fogParticleSystem.main;
		main.simulationSpeed = 0;

		SpawnSubEmitters ();

	}

	void SpawnSubEmitters(){
	
		particles = new ParticleSystem.Particle[fogParticleSystem.particleCount];

		fogParticleSystem.GetParticles (particles);

		Camera cam = Camera.main;

		GameObject fogGO = new GameObject ("Fog");

		fogGO.transform.SetParent (cam.transform);

		foreach (ParticleSystem.Particle p in particles ) {
			

			GameObject subPGO = Instantiate (subQuad);

			subPGO.transform.SetParent (fogGO.transform);

			subPGO.transform.position = fogParticleSystem.transform.TransformPoint (p.position);




			subPGO.transform.LookAt (cam.transform);

			subPGO.transform.Rotate (0, 0, Random.Range (-180, 180));

			float scale = Random.Range (minScale, maxScale);

			subPGO.transform.localScale = new Vector3 (scale, scale, 1f);


		}

	}
		
}
