using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PupilDemoManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;

	public List<float> calibrationPointRadii;
	private List<float> previewCircleRadii;
	private List<Transform> calibrationPointPreviewCircles;

	public bool displayEyeImages = true;

	public List<GameObject> gameObjectsToEnable;

	GameObject cameraObject;
	Text calibrationText;

	void Start()
	{	
		PupilTools.OnConnected += OnConnected;
		PupilTools.OnDisconnecting += OnDisconnected;
		PupilTools.OnCalibrationStarted += OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded += OnCalibrationEnded;
		PupilTools.OnCalibrationFailed += OnCalibrationFailed;
	
		PupilSettings.Instance.currentCamera = GetComponentInChildren<Camera> ();
		cameraObject = PupilSettings.Instance.currentCamera.gameObject;

		ResetCalibrationText ();
	}

	void ResetCalibrationText()
	{
		if (calibrationText == null)
			calibrationText = cameraObject.GetComponentInChildren<Text> ();

		calibrationText.text = "Trying to connect to Pupil.\nPlease start Pupil Service/Capture\n(if you have not done so, already)";
	}

	void OnDisconnected()
	{
		ResetCalibrationText ();
	}

	void OnConnected()
	{
		calibrationText.text = "Success";

		PupilTools.CalibrationMode = calibrationMode;

		InitializeCalibrationPointPreview ();

		if (displayEyeImages)
			gameObject.AddComponent<FramePublishing> ();
		
		Invoke ("ShowCalibrate", 1f);
	}

	void InitializeCalibrationPointPreview()
	{
		calibrationPointPreviewCircles = new List<Transform> ();
		calibrationPointRadii = new List<float> ();
		previewCircleRadii = new List<float> ();
		var type = PupilTools.CalibrationType;
		var camera = PupilSettings.Instance.currentCamera;
		Vector3 centerPoint = PupilTools.CalibrationType.centerPoint;
		if (PupilTools.CalibrationMode == Calibration.Mode._2D)
		{
			centerPoint.z = type.vectorDepthRadius [0].x;
			centerPoint = camera.worldToCameraMatrix.MultiplyPoint3x4 (camera.ViewportToWorldPoint (centerPoint));
		}
		foreach (var vector in type.vectorDepthRadius)
		{
			Transform previewCircle = GameObject.Instantiate<Transform> (Resources.Load<Transform> ("CalibrationPointExtendPreview"));
			previewCircle.parent = camera.transform;
			previewCircle.localPosition = new Vector3(centerPoint.x, centerPoint.y, vector.x);
			previewCircle.localEulerAngles = Vector3.zero;
			calibrationPointPreviewCircles.Add (previewCircle);
			calibrationPointRadii.Add (vector.y);
			previewCircleRadii.Add (0);
		}
	}
	void UpdateCalibrationPointPreview()
	{
		for (int i = 0; i < calibrationPointRadii.Count; i++)
		{
			if (previewCircleRadii[i] != calibrationPointRadii [i])
			{
				previewCircleRadii[i] = calibrationPointRadii [i];
				if (PupilTools.CalibrationMode == Calibration.Mode._2D)
					calibrationPointPreviewCircles[i].localScale = new Vector3 (previewCircleRadii[i], previewCircleRadii[i] / PupilSettings.Instance.currentCamera.aspect, 1);
				else
					calibrationPointPreviewCircles[i].localScale = new Vector3 (previewCircleRadii[i] * 0.2f, previewCircleRadii[i] * 0.2f, 1);
				PupilTools.CalibrationType.vectorDepthRadius [i].y = calibrationPointRadii [i];
			}
		}
	}

	void ShowCalibrate()
	{
		calibrationText.text = "Press 'c' to start calibration.";
	}

	void OnCalibtaionStarted()
	{
		cameraObject.SetActive (true);
		PupilSettings.Instance.currentCamera = cameraObject.GetComponent<Camera> ();
		calibrationText.text = "";
			
		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (false);
		}

		if (displayEyeImages)
			GetComponent<FramePublishing> ().enabled = false;
	}
		
	void OnCalibrationEnded()
	{
		calibrationText.text = "Calibration ended.";

		Invoke ("StartDemo", 1f);
	}

	void OnCalibrationFailed()
	{
		calibrationText.text = "Calibration failed\nPress 'c' to start it again.";

		if (displayEyeImages)
			GetComponent<FramePublishing> ().enabled = true;
	}

	void StartDemo()
	{
		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (true);
		}
		cameraObject.SetActive (false);
	}

	void Update()
	{
		if (Input.GetKeyUp (KeyCode.S)) 
			StartDemo ();

		UpdateCalibrationPointPreview ();
	}

	void OnApplicationQuit()
	{
		PupilTools.OnConnected -= OnConnected;
		PupilTools.OnDisconnecting -= OnDisconnected;
		PupilTools.OnCalibrationStarted -= OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded -= OnCalibrationEnded;
		PupilTools.OnCalibrationFailed -= OnCalibrationFailed;
	}
}
