using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PupilDemoManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;
	public List<GameObject> gameObjectsToEnable;

	GameObject cameraObject;
	Text calibrationText;

	void Start()
	{	
		PupilTools.OnConnected += OnConnected;
		PupilTools.OnCalibrationStarted += OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded += OnCalibrationEnded;
		PupilTools.OnCalibrationFailed += OnCalibrationFailed;
	
		PupilTools.Settings.currentCamera = GetComponentInChildren<Camera> ();
		cameraObject = PupilTools.Settings.currentCamera.gameObject;
		calibrationText = cameraObject.GetComponentInChildren<Text> ();

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
		calibrationText.text = "Use remote client to start calibration";
	}

	void OnCalibtaionStarted()
	{
		cameraObject.SetActive (true);
		PupilTools.Settings.currentCamera = cameraObject.GetComponent<Camera> ();
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
		PupilTools.Settings.currentCamera = Camera.main;
		cameraObject.SetActive (false);
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
