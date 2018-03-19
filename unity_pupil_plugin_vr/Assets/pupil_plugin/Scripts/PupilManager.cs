using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;using UnityEngine.SceneManagement;

public class PupilManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;
	public bool displayEyeImages = true;

	GameObject cameraObject;
	Text calibrationText;

	void Start()
	{	
		PupilTools.OnConnected += OnConnected;
		PupilTools.OnDisconnecting += OnDisconnecting;
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

	void OnDisconnecting()
	{
		ResetCalibrationText ();

		if (displayEyeImages)
			GetComponent<FramePublishing> ().enabled = false;
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
		var type = PupilTools.CalibrationType;
		var camera = PupilSettings.Instance.currentCamera;
		Vector3 centerPoint = PupilTools.CalibrationType.centerPoint;
		foreach (var vector in type.vectorDepthRadius)
		{
			Transform previewCircle = GameObject.Instantiate<Transform> (Resources.Load<Transform> ("CalibrationPointExtendPreview"));
			previewCircle.parent = camera.transform;
			float scaleFactor = (centerPoint.x + vector.y) * 0.2f;
			if (PupilTools.CalibrationMode == Calibration.Mode._2D)
			{
				centerPoint.z = type.vectorDepthRadius [0].x;
				scaleFactor = camera.worldToCameraMatrix.MultiplyPoint3x4 (camera.ViewportToWorldPoint (centerPoint + Vector3.right * vector.y)).x * 0.2f;
				centerPoint = camera.worldToCameraMatrix.MultiplyPoint3x4 (camera.ViewportToWorldPoint (centerPoint));
			}
			previewCircle.localScale = new Vector3 (scaleFactor, scaleFactor / PupilSettings.Instance.currentCamera.aspect, 1);
			previewCircle.localPosition = new Vector3(centerPoint.x, centerPoint.y, vector.x);
			previewCircle.localEulerAngles = Vector3.zero;
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

		if (displayEyeImages)
			GetComponent<FramePublishing> ().enabled = false;

		if (loadedSceneIndex != -1)
			StartCoroutine (UnloadCurrentScene());
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

	public string[] availableScenes;
	public int currentSceneIndex;
	private int loadedSceneIndex = -1;
	IEnumerator LoadCurrentScene()
	{
		AsyncOperation asyncScene = SceneManager.LoadSceneAsync(availableScenes[currentSceneIndex],LoadSceneMode.Additive);

		while (!asyncScene.isDone)
		{
			yield return null;
		}
		loadedSceneIndex = currentSceneIndex;
	}
	IEnumerator UnloadCurrentScene()
	{
		AsyncOperation asyncLoad = SceneManager.UnloadSceneAsync(availableScenes[loadedSceneIndex]);

		while (!asyncLoad.isDone)
		{
			yield return null;
		}
		loadedSceneIndex = -1;
	}

	void StartDemo()
	{
		StartCoroutine (LoadCurrentScene());

		cameraObject.SetActive (false);
	}

	void Update()
	{
		if (Input.GetKeyUp (KeyCode.S)) 
			StartDemo ();
	}

	void OnDisable()
	{
		PupilTools.OnConnected -= OnConnected;
		PupilTools.OnDisconnecting -= OnDisconnecting;
		PupilTools.OnCalibrationStarted -= OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded -= OnCalibrationEnded;
		PupilTools.OnCalibrationFailed -= OnCalibrationFailed;
	}
}
