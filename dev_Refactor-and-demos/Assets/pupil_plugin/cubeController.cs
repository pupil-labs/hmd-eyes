using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cubeController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Input.GetKey (KeyCode.A))
			transform.Translate (0, 0, 1);

		if (Input.GetKey (KeyCode.D))
			transform.Translate (0, 0, -1);
		
	}
}
