using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.XR.WSA.Input;

public class PupilManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;
	public Transform canvasUI;
	public Button connectionButton;
	public Button calibrationButton;

	GameObject cameraObject;
	Camera menuCamera;
    GameObject menuPointer;
	Text calibrationText;

	void OnEnable()
    {
		PupilTools.OnConnected += OnConnected;
		PupilTools.OnDisconnecting += OnDisconnecting;

		PupilTools.OnCalibrationStarted += OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded += OnCalibrationEnded;
		PupilTools.OnCalibrationFailed += OnCalibrationFailed;
	
		PupilSettings.Instance.currentCamera = GetComponentInChildren<Camera> ();
		cameraObject = PupilSettings.Instance.currentCamera.gameObject;
		menuCamera = cameraObject.GetComponent<Camera> ();
        menuPointer = cameraObject.GetComponentInChildren<MeshRenderer>().gameObject;
		calibrationText = cameraObject.GetComponentInChildren<Text> ();
        
		PupilTools.CalibrationMode = calibrationMode;

        InitializeGestureRecognizer ();
	}

	public void OnConnectionButtonClicked()
	{
		if (PupilTools.IsConnected)
		{
			PupilGazeTracker.Instance.CloseShop ();
		}
		else
		{
			PupilGazeTracker.Instance.RunConnect ();

			connectionButton.interactable = false;
			connectionButton.GetComponentInChildren<Text> ().text = "Trying to connect";
		}
	}

	public void OnCalibrationButtonClicked()
	{
		if (PupilTools.IsCalibrating)
		{
			PupilTools.StopCalibration ();
		}
		else
		{
			PupilTools.StartCalibration ();
			ResetCanvasUI (false);
            calibrationText.text = "";
        }
	}

	private GestureRecognizer recognizer;
	void InitializeGestureRecognizer()
	{
		recognizer = new GestureRecognizer ();
		recognizer.SetRecognizableGestures (GestureSettings.Tap | GestureSettings.DoubleTap);
		recognizer.TappedEvent += (source, tapCount, ray) => 
		{
			if (tapCount == 2)
			{
				ResetCanvasUI(!canvasUI.gameObject.activeInHierarchy);
            }
			else if (tapCount == 1 && canvasUI.gameObject.activeInHierarchy)
			{
				if (EventSystem.current.currentSelectedGameObject == connectionButton.gameObject)
				{
					OnConnectionButtonClicked();
				}
				else if (EventSystem.current.currentSelectedGameObject == calibrationButton.gameObject)
				{
					OnCalibrationButtonClicked();
				}
			}
		};
		recognizer.StartCapturingGestures ();
	}

	void OnConnected()
	{
		calibrationButton.interactable = true;
		calibrationButton.GetComponentInChildren<Text> ().text = "Start\nCalibration";

		connectionButton.interactable = true;
		connectionButton.GetComponentInChildren<Text> ().text = "Disconnect\nfrom Pupil";

		InitializeCalibrationPointPreview ();
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

	void OnDisconnecting()
	{
		connectionButton.interactable = true;
		connectionButton.GetComponentInChildren<Text> ().text = "Connect\nto Pupil";

		calibrationButton.interactable = false;
		calibrationButton.GetComponentInChildren<Text> ().text = "Start\nCalibration";

		ResetDemo ();
	}

	void ResetDemo()
	{
		if (PupilTools.IsGazing)
			PupilGazeTracker.Instance.StopVisualizingGaze ();
		
		cameraObject.SetActive (true);

		PupilSettings.Instance.currentCamera = cameraObject.GetComponent<Camera> ();

		if (loadedSceneIndex != -1)
			StartCoroutine (UnloadCurrentScene());
	}

	void ResetCanvasUI(bool visible)
	{
        EventSystem.current.SetSelectedGameObject(null);

        if (!cameraObject.activeInHierarchy)
            ResetDemo();

        canvasUI.gameObject.SetActive(visible);
        menuPointer.SetActive(visible);
        if (visible)
		{
			canvasUI.position = cameraObject.transform.position + cameraObject.transform.forward;
			canvasUI.LookAt (canvasUI.position + cameraObject.transform.forward);
		}
		calibrationText.text = visible ? "Double (air-)tap\nto hide menu" : "Double (air-)tap\nto show menu";
	}

	void OnCalibtaionStarted()
	{
		calibrationButton.GetComponentInChildren<Text> ().text = "Stop\nCalibration";
		ResetDemo ();
    }
		
	void OnCalibrationEnded()
	{
		calibrationButton.GetComponentInChildren<Text> ().text = "Restart\nCalibration";

		Invoke ("StartDemo", 1f);
	}

	void OnCalibrationFailed()
	{
		calibrationButton.GetComponentInChildren<Text> ().text = "Calibration\nfailed,\nrestart";

        ResetCanvasUI(true);
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
		StartCoroutine (LoadCurrentScene ());

		cameraObject.SetActive (false);
		canvasUI.gameObject.SetActive (false);
	}

	float selectionDistance = 0.1f;
	Vector2 viewportCenter = Vector2.one * 0.5f;
	void Update()
	{
		if (Input.GetKeyUp (KeyCode.S)) 
			StartDemo ();

		// If you are using Holographic Emulation mode and have no gamepad available, you can use these keyboard commands
		if (Input.GetKeyUp (KeyCode.M))
			ResetCanvasUI (!canvasUI.gameObject.activeInHierarchy);
		if (Input.GetKeyUp (KeyCode.X))
			OnConnectionButtonClicked ();
		if (Input.GetKeyUp (KeyCode.Y))
			OnCalibrationButtonClicked ();

		if (menuCamera != null && canvasUI.gameObject.activeInHierarchy)
		{
			float connectionButtonDistanceToCenter = Vector2.Distance (viewportCenter, menuCamera.WorldToViewportPoint (connectionButton.transform.position));
			float calibrationButtonDistanceToCenter = Vector2.Distance (viewportCenter, menuCamera.WorldToViewportPoint (calibrationButton.transform.position));
			float canvasDistanceToCamera = Vector3.Distance (cameraObject.transform.position, canvasUI.position);

			if (   connectionButtonDistanceToCenter < calibrationButtonDistanceToCenter
				&& connectionButtonDistanceToCenter < (selectionDistance/canvasDistanceToCamera) )
			{
				connectionButton.Select ();
			}
			else if (	calibrationButtonDistanceToCenter < connectionButtonDistanceToCenter
					&&	calibrationButtonDistanceToCenter < (selectionDistance/canvasDistanceToCamera) )
				calibrationButton.Select ();
			else
				EventSystem.current.SetSelectedGameObject (null);
		}
	}
    
    void OnDisable()
	{
		PupilTools.OnConnected -= OnConnected;
		PupilTools.OnDisconnecting += OnDisconnecting;

		PupilTools.OnCalibrationStarted -= OnCalibtaionStarted;
		PupilTools.OnCalibrationEnded -= OnCalibrationEnded;
		PupilTools.OnCalibrationFailed -= OnCalibrationFailed;
	}
}
