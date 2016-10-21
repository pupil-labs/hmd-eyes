
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class EyeGazeObject : MonoBehaviour
{
	public PupilGazeTracker.GazeSource Gaze;
	// Script initialization
	void Start() {	
	}

	void Update() {
		Vector2 g = PupilGazeTracker.Instance.GetEyeGaze (Gaze);
		transform.localPosition = new Vector3 ((g.x - 0.5f) * PupilGazeTracker.Instance.CanvasWidth, (g.y - 0.5f) * PupilGazeTracker.Instance.CanvasHeight, 0);
	}
}