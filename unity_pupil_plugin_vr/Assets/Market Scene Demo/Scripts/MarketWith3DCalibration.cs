using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketWith3DCalibration : MonoBehaviour 
{
	public Transform marker;
	// Use this for initialization
	void Start () 
	{
		PupilData.calculateMovingAverage = true;
	}

	void OnEnable()
	{
		if (PupilTools.IsConnected)
		{
			PupilGazeTracker.Instance.StartVisualizingGaze ();		
		}	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (PupilTools.IsConnected && PupilTools.IsGazing)
		{
			marker.localPosition = PupilData._3D.GazePosition;
		}
	}
}
