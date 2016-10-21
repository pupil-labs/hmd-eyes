
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class EyeGazeRenderer : MonoBehaviour
{
	public RectTransform gaze;

	public PupilGazeTracker.GazeSource Gaze;
	// Script initialization
	void Start() {	
		if (gaze == null)
			gaze = this.GetComponent<RectTransform> ();
	}

	void Update() {
		if (gaze == null)
			return;
		Canvas c = gaze.GetComponentInParent<Canvas> ();
		Vector2 g = PupilGazeTracker.Instance.GetEyeGaze (Gaze);
		gaze.localPosition = new Vector3 ((g.x - 0.5f) * c.pixelRect.width, (g.y - 0.5f) * c.pixelRect.height, 0);
	}
}