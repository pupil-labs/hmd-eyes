using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTheShark : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.Rotate (0, 10 * Time.deltaTime, 0);
	}
}
