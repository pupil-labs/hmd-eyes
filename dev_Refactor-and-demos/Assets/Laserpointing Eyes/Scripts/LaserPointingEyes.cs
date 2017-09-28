using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointingEyes : MonoBehaviour
{
	public LineRenderer laserpointerLeftEye;
	public LineRenderer laserpointerRightEye;

	// Use this for initialization
	void Start ()
	{
		PupilData.calculateMovingAverage = true;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (PupilSettings.Instance.connection.isConnected)
		{	
			Vector3 gazeIntoWorld = Camera.main.ViewportToWorldPoint(new Vector3(PupilData._2D.GazePosition.x, PupilData._2D.GazePosition.y, 10));
			laserpointerLeftEye.transform.LookAt(gazeIntoWorld);
			laserpointerRightEye.transform.LookAt(gazeIntoWorld);
		}
	}
}
