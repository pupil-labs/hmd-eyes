using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationDemo : MonoBehaviour 
{
	void OnEnable()
	{
		if (PupilTools.Settings.connection.isConnected)
		{
			PupilGazeTracker.Instance.StartVisualizingGaze ();		
			print ("We are gazing");
		}
	}
	void OnDisable()
	{
		if (PupilTools.Settings.connection.isConnected && PupilTools.Settings.DataProcessState == PupilSettings.EStatus.ProcessingGaze)
		{
			PupilGazeTracker.Instance.StopVisualizingGaze ();		
			print ("We stopped gazing");
		}
	}
}
