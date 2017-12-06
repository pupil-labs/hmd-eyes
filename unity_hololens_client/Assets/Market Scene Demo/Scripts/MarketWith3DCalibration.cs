using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketWith3DCalibration : MonoBehaviour 
{
	public Transform marker;
	// Use this for initialization
	void Start () 
	{
	}

	void OnEnable()
	{
		if (PupilSettings.Instance.connection.isConnected)
		{
			PupilSettings.Instance.DataProcessState = PupilSettings.EStatus.ProcessingGaze;
			PupilTools.SubscribeTo ("gaze");
		}	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.DataProcessState == PupilSettings.EStatus.ProcessingGaze)
		{
			marker.localPosition = PupilData._3D.GazePosition;
		}
	}

	void OnDisable()
	{
		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.DataProcessState == PupilSettings.EStatus.ProcessingGaze)
		{
			PupilTools.UnSubscribeFrom("gaze");	
			print ("We stopped gazing");
		}
	}
}
