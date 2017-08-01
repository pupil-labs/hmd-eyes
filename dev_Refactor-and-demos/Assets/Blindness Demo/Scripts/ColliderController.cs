using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderController : MonoBehaviour {

	CapsuleCollider coll;

	Camera cam;

	public float minRadius;
	public float maxRadius;
	public float targetRadius;

	public float lerpSpeed;
	public float threshold;

	void Start () {

		coll = GetComponent<CapsuleCollider> ();

		cam = Camera.main;

		InvokeRepeating ("ChangeTargetRadius", 5f, 5f);

	}

	void ChangeTargetRadius(){

		targetRadius = Random.Range (minRadius, maxRadius);

	}

	private Vector3 screenPoint;

	void Update () {

		if (Mathf.Abs (coll.radius - targetRadius) > threshold)
			coll.radius = Mathf.Lerp (coll.radius, targetRadius, lerpSpeed);

		if (PupilSettings.Instance.connection.isConnected){
			
			if (PupilData._2D.ID() == "0"){

				screenPoint = new Vector3 ((cam.pixelWidth * PupilData._2D.Norm_Pos ().x) - (cam.pixelWidth / 2), (cam.pixelHeight * PupilData._2D.Norm_Pos ().y) - (cam.pixelHeight / 2), .5f);

				transform.localPosition = cam.ScreenToViewportPoint (screenPoint);

			}

		}

	}

}
