using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharkWith3DCalibration : MonoBehaviour 
{
	public Transform marker;
	// Use this for initialization
	void Start () 
	{
	}

	void OnEnable()
	{
		if (PupilTools.IsConnected)
		{
			PupilTools.IsGazing = true;
			PupilTools.SubscribeTo ("gaze");
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
