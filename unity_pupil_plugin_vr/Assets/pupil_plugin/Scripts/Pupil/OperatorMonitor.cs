using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OperatorMonitor : MonoBehaviour 
{
	float graphLineOffset = 0.5f;
	float graphWidth = 0.42f;
	int graphLength = 50;
	float graphTime;
	float updateConfidenceEveryXSeconds = 0.1f;
	float confidenceRightEye = 0.2f;
	float confidenceLeftEye = 0.2f;
	List<float> confidenceLeftEyeList = new List<float> ();
	Vector3[] confidenceLeftEyePosition;
	List<float> confidenceRightEyeList = new List<float> ();
	Vector3[] confidenceRightEyePosition;

	private static PupilGazeTracker pupilTracker;
	private static PupilSettings pupilSettings;

	TextMesh gazeInfo;

	void Awake()
	{
		pupilTracker = PupilGazeTracker.Instance;
		pupilSettings = pupilTracker.Settings;
	}

	public void Start()
	{
		pupilTracker = PupilGazeTracker.Instance;
		graphTime = Time.time;
			
		confidenceLeftEyeList.Capacity = graphLength + 1;
		confidenceLeftEyePosition = new Vector3[graphLength];
		confidenceRightEyeList.Capacity = graphLength + 1;
		confidenceRightEyePosition = new Vector3[graphLength];
		for (int i = 0; i < graphLength; i++)
		{
			var relative = (float)i / (float)(graphLength - 1);
			var position = Vector3.right * relative * graphWidth;
			confidenceLeftEyePosition [i] =  position;
			confidenceRightEyePosition [i] = position;
		}

		var leftEyeRenderingObject = GameObject.CreatePrimitive (PrimitiveType.Plane);
		GameObject.Destroy (leftEyeRenderingObject.GetComponent<Collider> ());
		leftEyeRenderingObject.transform.eulerAngles = Vector3.left * 90f;
		leftEyeRenderingObject.transform.localScale *= 0.05f;
		var rightEyeRenderingObject = GameObject.Instantiate (leftEyeRenderingObject);

		leftEyeRenderingObject.transform.parent = gameObject.transform;
		leftEyeRenderingObject.transform.localPosition = new Vector3 (0.3f,0f,2f);
		MeshRenderer leftEyeRenderer = leftEyeRenderingObject.GetComponent<MeshRenderer> ();
		leftEyeRenderer.material = pupilSettings.framePublishing.eye1ImageMaterial;

		rightEyeRenderingObject.transform.parent = gameObject.transform;
		rightEyeRenderingObject.transform.localPosition = new Vector3 (-0.3f,0f,2f);
		MeshRenderer rightEyeRenderer = rightEyeRenderingObject.GetComponent<MeshRenderer> ();
		rightEyeRenderer.material = pupilSettings.framePublishing.eye0ImageMaterial;

		gazeInfo = GetComponentInChildren<TextMesh> ();

		InitializeLineRenderer (forEye: "Left", parent: leftEyeRenderingObject.transform);
		InitializeLineRenderer (forEye: "Right", parent: rightEyeRenderingObject.transform);

		PupilTools.SubscribeTo ("pupil.");
		PupilTools.StartFramePublishing ();
	}

	void Update()
	{
		pupilSettings.framePublishing.UpdateEyeTextures ();

		//Construct the Text box string for data display on the Operator Monitor view
		string str = 
			string.Format("Gaze Point : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.GazePosition.x,PupilData._3D.GazePosition.y,PupilData._3D.GazePosition.z)
			+ string.Format("\nEyeball 0 Center : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.RightEyeCenter.x,PupilData._3D.RightEyeCenter.y,PupilData._3D.RightEyeCenter.z)
			+ string.Format("\nEyeball 1 Center : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.LeftEyeCenter.x,PupilData._3D.LeftEyeCenter.y,PupilData._3D.LeftEyeCenter.z)
			+ string.Format("\nPupil Diameter : {0}",PupilData.Diameter ())
			;
		gazeInfo.text = str;

		if ((Time.time - graphTime) > updateConfidenceEveryXSeconds)
		{
			UpdateLineRenderers ();
			graphTime = Time.time;
		}
	}

	LineRenderer leftEyeConfidenceLevel;
	LineRenderer rightEyeConfidenceLevel;
	float confidenceLevelScaling = 0.05f;
	void InitializeLineRenderer(string forEye, Transform parent)
	{
		var go = new GameObject (forEye + " Eye Confidence Level");
		go.transform.parent = parent;
		go.transform.localPosition = Vector3.left * 4;

		LineRenderer lr = go.AddComponent<LineRenderer> ();
		lr.material = new Material (Shader.Find ("Unlit/Color"));
		lr.material.color = (forEye == "Right") ? PupilSettings.rightEyeColor : PupilSettings.leftEyeColor;
		lr.positionCount = graphLength;
		lr.useWorldSpace = false;
		lr.startWidth = 0.005f;
		lr.endWidth = 0.005f;

		if (forEye == "Left")
			leftEyeConfidenceLevel = lr;
		else
			rightEyeConfidenceLevel = lr;
	}

	float yOffset = -0.24f;
	public void UpdateLineRenderers()
	{
		confidenceLeftEyeList.Add (PupilTools.Confidence(PupilData.leftEyeID));
		if (confidenceLeftEyeList.Count > graphLength)
			confidenceLeftEyeList.RemoveAt (0);

		confidenceRightEyeList.Add (PupilTools.Confidence (PupilData.rightEyeID));
		if (confidenceRightEyeList.Count > graphLength)
			confidenceRightEyeList.RemoveAt (0);
		
		for (int i = 0; i < confidenceLeftEyeList.Count; i++)
		{
			confidenceLeftEyePosition [i].y = confidenceLeftEyeList [i] * confidenceLevelScaling + yOffset;
			confidenceRightEyePosition [i].y = confidenceRightEyeList [i] * confidenceLevelScaling + yOffset;

		}
		leftEyeConfidenceLevel.SetPositions (confidenceLeftEyePosition);
		rightEyeConfidenceLevel.SetPositions (confidenceRightEyePosition);
	}

	ParticleSystem confidenceVisualization;
	ParticleSystem.Particle[] visualizationParticles;
	void InitializeParticleVisualization()
	{
		confidenceVisualization = GetComponent<ParticleSystem> ();
		if (confidenceVisualization == null)
			return;
		
		visualizationParticles = new ParticleSystem.Particle[confidenceVisualization.main.maxParticles];
		ParticleSystem.EmitParams particleSystemParameters = new ParticleSystem.EmitParams ();
		particleSystemParameters.startLifetime = float.MaxValue;
		particleSystemParameters.startColor = Color.black;
		for (int j = 0; j < graphLength; j++)
		{
			float radius = 1f - (float)j / (float) (graphLength-1);
			float size = (1f - radius) * 0.05f;
			radius *= 2f;
			float angle = Mathf.Deg2Rad * 360 * radius;
			if (j < graphLength-1)
				radius = (radius + 1f) * 0.03f;
			particleSystemParameters.position = new Vector3 (radius * Mathf.Sin (angle), radius * Mathf.Cos (angle), 2);
			if (j == graphLength-1)
				size *= 1.5f;
			particleSystemParameters.startSize = size;
			// Vector3.left * j * 0.1f + Vector3.forward * 2f;
			confidenceVisualization.Emit (particleSystemParameters, 1);
		}
	}
	void UpdateParticleVisualization()
	{
		int numberOfParticlesAlive = confidenceVisualization.GetParticles (visualizationParticles);
		for (int j = 0; j < numberOfParticlesAlive; j++)
			if (j < confidenceLeftEyeList.Count)
				visualizationParticles [j].startColor = Color.Lerp (Color.white, PupilSettings.leftEyeColor, confidenceLeftEyeList[j]);
		confidenceVisualization.SetParticles (visualizationParticles, numberOfParticlesAlive);
	}

	public void ExitOperatorMonitor()
	{
		PupilTools.UnSubscribeFrom ("pupil.");

		if (!PupilSettings.Instance.debugView.active && !pupilTracker.isOperatorMonitor)
		{	
			PupilTools.StopFramePublishing ();
		}
		pupilTracker.isOperatorMonitor = false;
		Destroy (gameObject);
	}
}
