using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pupil;

public class SharkWith2DCalibration : MonoBehaviour 
{
	private Vector2 gazePointCenter;

	public Material shaderMaterial;

	void Start () 
	{
	}

	void OnEnable()
	{
		if (PupilTools.IsConnected)
		{
			PupilTools.DataProcessState = EStatus.ProcessingGaze;
			PupilTools.SubscribeTo ("gaze");
		}
	}

	public bool monoColorMode = true;

	void Update()
	{
		if (PupilTools.IsConnected && PupilTools.DataProcessState == EStatus.ProcessingGaze)
		{
			gazePointCenter = PupilData._2D.GetEyeGaze (GazeSource.BothEyes);
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
		if (PupilTools.IsConnected)
			PupilTools.UnSubscribeFrom ("gaze");
	}
}
