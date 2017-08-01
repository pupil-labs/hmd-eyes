using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteractionController : MonoBehaviour {

	TiltShiftController tiltController;

	Camera cam;

	[Range(0.01f,10f)]
	public float areaEffectFactor = 0.1f;

	void Start(){
	
		cam = Camera.main;

		tiltController = cam.GetComponent<TiltShiftController> ();

	}

	void OnMouseUp(){

		Vector3 screenPosition = cam.WorldToScreenPoint (gameObject.transform.position);
		float distance = Vector3.Distance (cam.transform.position, gameObject.transform.position);

//		print (Screen.width + " | " + Screen.height);
//		print (gameObject.name + " screen position : " + screenPosition.ToString () + " height/pos : " + (Screen.height / screenPosition.y));

		float newOffset = -((Mathf.InverseLerp (0, Screen.height, screenPosition.y) * 2) - 1f);

		tiltController.targetOffset = newOffset;

		print (distance);

		float newArea = Mathf.Clamp (Mathf.Sqrt (distance) * areaEffectFactor, 1.7f, 10f);

		tiltController.targetArea = newArea;

	}

}
