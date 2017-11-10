using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PupilDemoManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;
	public List<GameObject> gameObjectsToEnable;

	Camera calibrationCamera;
	Text calibrationText;

	void Start()
	{	
		PupilTools.OnConnected += OnConnected;
		PupilTools.OnCalibrationStarted += OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded += OnCalibrationEnded;
		PupilTools.OnCalibrationFailed += OnCalibrationFailed;
	
		calibrationCamera = GetComponentInChildren<Camera> ();
		calibrationText = calibrationCamera.gameObject.GetComponentInChildren<Text> ();

		calibrationText.text = "Connecting to pupil.";
	}

	void OnConnected()
	{
		calibrationText.text = "Success";

		PupilTools.Settings.calibration.currentMode = calibrationMode;

		Invoke ("ShowCalibrate", 1f);
	}

	void ShowCalibrate()
	{
		calibrationText.text = "Press 'c' to start calibration.";
	}

	void OnCalibtaionStarted()
	{
		calibrationCamera.gameObject.SetActive (true);
		calibrationText.text = "";
			
		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (false);
		}
	}
		
	void OnCalibrationEnded()
	{
		calibrationText.text = "Calibration ended.";

		Invoke ("StartDemo", 1f);
	}

	void OnCalibrationFailed()
	{
		calibrationText.text = "Calibration failed\nPress 'c' to start it again.";
	}

	void StartDemo()
	{
		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (true);
		}

		calibrationCamera.gameObject.SetActive (false);
	}

	void Update()
	{
		if (Input.GetKeyUp (KeyCode.S)) 
			StartDemo ();
	}

	void OnApplicationQuit()
	{
		PupilTools.OnConnected -= OnConnected;
		PupilTools.OnCalibrationStarted -= OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded -= OnCalibrationEnded;
		PupilTools.OnCalibrationFailed -= OnCalibrationFailed;
	}
}
