using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.WSA.Input;

public class PupilDemoManager : MonoBehaviour 
{
	public Calibration.Mode calibrationMode = Calibration.Mode._2D;
	public List<GameObject> gameObjectsToEnable;
	public Transform canvasUI;
	public Button connectionButton;
	public Button calibrationButton;

	GameObject cameraObject;
	Camera menuCamera;
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
		if (PupilTools.DataProcessState == Pupil.EStatus.Calibration)
		{
			PupilTools.StopCalibration ();
		}
		else
		{
			PupilTools.StartCalibration ();
			canvasUI.gameObject.SetActive (false);
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
			else if (tapCount == 1)
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
		cameraObject.SetActive (true);
		PupilSettings.Instance.currentCamera = cameraObject.GetComponent<Camera> ();

		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (false);
		}
	}

	void ResetCanvasUI(bool visible)
	{
		canvasUI.gameObject.SetActive(visible);
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
	}

	void StartDemo()
	{
		foreach (GameObject go in gameObjectsToEnable) 
		{
			go.SetActive (true);
		}
		cameraObject.SetActive (false);
		canvasUI.gameObject.SetActive (false);
	}

	float selectionDistance = 0.1f;
	Vector2 viewportCenter = Vector2.one * 0.5f;
	void Update()
	{
		if (Input.GetKeyUp (KeyCode.S)) 
			StartDemo ();

		if (Input.GetKeyUp (KeyCode.M))
			ResetCanvasUI (!canvasUI.gameObject.activeInHierarchy);

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

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            OnDisable();
        else
            OnEnable();
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
