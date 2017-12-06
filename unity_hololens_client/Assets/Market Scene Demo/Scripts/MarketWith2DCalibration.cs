using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketWith2DCalibration : MonoBehaviour 
{
	private Vector2 gazePointCenter;

	public Material shaderMaterial;

	void Start () 
	{
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
		if (PupilSettings.Instance.connection.isConnected && PupilSettings.Instance.DataProcessState == PupilSettings.EStatus.ProcessingGaze)
		{
			gazePointCenter = PupilData._2D.GetEyeGaze (Pupil.GazeSource.BothEyes);
		}
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if (monoColorMode)
		{
			shaderMaterial.SetFloat ("_highlightThreshold", 0.1f);
			shaderMaterial.SetVector ("_viewportGazePosition", gazePointCenter);
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
