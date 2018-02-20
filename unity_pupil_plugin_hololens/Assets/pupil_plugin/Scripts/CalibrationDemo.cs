using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationDemo : MonoBehaviour 
{
	void OnEnable()
	{
		if (PupilTools.IsConnected)
		{
			PupilGazeTracker.Instance.StartVisualizingGaze ();		
			print ("We are gazing");
		}
	}
	void OnDisable()
	{
		if (PupilTools.IsConnected && PupilTools.IsGazing)
		{
			PupilGazeTracker.Instance.StopVisualizingGaze ();		
			print ("We stopped gazing");
		}
	}
}
