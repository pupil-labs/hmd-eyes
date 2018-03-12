using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
			PupilTools.IsGazing = true;
			PupilTools.SubscribeTo ("gaze");
		}
	}

	public bool monoColorMode = true;

	void Update()
	{
		if (PupilTools.IsConnected && PupilTools.IsGazing)
		{
			gazePointCenter = PupilData._2D.GazePosition;
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
}
