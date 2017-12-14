using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketWith3DCalibration : MonoBehaviour 
{
	public Transform marker;
	// Use this for initialization
	void Start () 
	{
		PupilData.calculateMovingAverage = false;
	}

	void OnEnable()
	{
		if (PupilTools.IsConnected)
		{
			PupilTools.DataProcessState = Pupil.EStatus.ProcessingGaze;
			PupilTools.SubscribeTo ("gaze");
		}	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (PupilTools.IsConnected && PupilTools.DataProcessState == Pupil.EStatus.ProcessingGaze)
		{
			marker.localPosition = PupilData._3D.GazePosition;
		}
	}

	void OnDisable()
	{
		if (PupilTools.IsConnected && PupilTools.DataProcessState == Pupil.EStatus.ProcessingGaze)
		{
			PupilTools.UnSubscribeFrom("gaze");	
			print ("We stopped gazing");
		}
	}
}
