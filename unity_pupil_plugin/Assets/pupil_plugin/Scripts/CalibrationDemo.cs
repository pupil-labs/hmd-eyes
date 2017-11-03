using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrationDemo : MonoBehaviour 
{
	void OnEnable()
	{
		if (PupilSettings.Instance.connection.isConnected)
		{
			PupilGazeTracker.Instance.StartVisualizingGaze ();		
			print ("We are gazing");
		}
	}
	void OnDisable()
	{
		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.ProcessingGaze)
		{
			PupilGazeTracker.Instance.StopVisualizingGaze ();		
			print ("We stopped gazing");
		}
	}
}
