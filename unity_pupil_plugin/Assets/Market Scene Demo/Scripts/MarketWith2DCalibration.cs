using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketWith2DCalibration : MonoBehaviour 
{
	private Camera sceneCamera;
	private CalibrationDemo calibrationDemo;

	private LineRenderer heading;
	private Vector3 standardViewportPoint = new Vector3 (0.5f, 0.5f, 10);

	private Vector2 gazePointLeft;
	private Vector2 gazePointRight;
	private Vector2 gazePointCenter;

	public Material shaderMaterial;

	void Start () 
	{
		PupilData.calculateMovingAverage = true;

		sceneCamera = gameObject.GetComponent<Camera> ();
		calibrationDemo = gameObject.GetComponent<CalibrationDemo> ();
		heading = gameObject.GetComponent<LineRenderer> ();
	}

	void OnEnable()
	{
		if (PupilSettings.Instance.connection.isConnected)
		{
			PupilSettings.Instance.DataProcessState = PupilSettings.EStatus.ProcessingGaze;
			PupilTools.SubscribeTo ("gaze");
		}
	}

	public bool monoColorMode = true;

	void Update()
	{
		Vector3 viewportPoint = standardViewportPoint;

		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.DataProcessState == PupilSettings.EStatus.ProcessingGaze)
		{
			gazePointLeft = PupilData._2D.GetEyePosition (sceneCamera, Pupil.GazeSource.LeftEye);
			gazePointRight = PupilData._2D.GetEyePosition (sceneCamera, Pupil.GazeSource.RightEye);
			gazePointCenter = PupilData._2D.GetEyeGaze (Pupil.GazeSource.BothEyes);
			viewportPoint = new Vector3 (gazePointCenter.x, gazePointCenter.y, 1f);
		}

		if (Input.GetKeyUp (KeyCode.M))
			monoColorMode = !monoColorMode;

		if (Input.GetKeyUp (KeyCode.G))
			calibrationDemo.enabled = !calibrationDemo.enabled;

		if (Input.GetKeyUp (KeyCode.L))
			heading.enabled = !heading.enabled;
		if (heading.enabled)
		{
			heading.SetPosition (0, sceneCamera.transform.position-sceneCamera.transform.up);

			Ray ray = sceneCamera.ViewportPointToRay (viewportPoint);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit))
			{
				heading.SetPosition (1, hit.point);
			} else
			{
				heading.SetPosition (1, ray.origin + ray.direction * 50f);
			}
		}
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if (monoColorMode)
		{
			shaderMaterial.SetFloat ("_highlightThreshold", 0.1f);
			switch (sceneCamera.stereoActiveEye)
			{
			case Camera.MonoOrStereoscopicEye.Left:
				shaderMaterial.SetVector ("_viewportGazePosition", gazePointLeft);
				break;
			case Camera.MonoOrStereoscopicEye.Right:
				shaderMaterial.SetVector ("_viewportGazePosition", gazePointRight);
				break;
			default:
				shaderMaterial.SetVector ("_viewportGazePosition", gazePointCenter);
				break;
			}
			Graphics.Blit (source, destination, shaderMaterial);
		} else
			Graphics.Blit (source, destination);

	}

	void OnDisable()
	{
		if (PupilSettings.Instance.connection.isConnected)
			PupilTools.UnSubscribeFrom ("gaze");
	}
}
