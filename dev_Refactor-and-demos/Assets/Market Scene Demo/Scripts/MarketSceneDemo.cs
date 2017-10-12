using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketSceneDemo : MonoBehaviour 
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

	void Update()
	{
		Vector3 viewportPoint = standardViewportPoint;

		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.ProcessingGaze)
		{
			gazePointLeft = PupilData._2D.GetEyePosition (sceneCamera, PupilData.GazeSource.LeftEye);
			gazePointRight = PupilData._2D.GetEyePosition (sceneCamera, PupilData.GazeSource.RightEye);
			gazePointCenter = PupilData._2D.GetEyePosition (sceneCamera, PupilData.GazeSource.BothEyes);
			viewportPoint = new Vector3 (gazePointCenter.x, gazePointCenter.y, 0.8f);
		}

		if (Input.GetKeyUp (KeyCode.G))
			calibrationDemo.enabled = !calibrationDemo.enabled;

		if (Input.GetKeyUp (KeyCode.L))
			heading.enabled = !heading.enabled;
		if (heading.enabled)
		{
			Ray ray = sceneCamera.ViewportPointToRay (viewportPoint);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit))
			{
				heading.SetPosition (0, sceneCamera.transform.position-sceneCamera.transform.up);
				heading.SetPosition (1, hit.point);
			}
		}
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
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
	}
}
