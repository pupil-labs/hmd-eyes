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

	LineRenderer leftEyeConfidenceLevel;
	LineRenderer rightEyeConfidenceLevel;
	float confidenceLevelScaling = 0.05f;
	float yOffset = -0.24f;

	void Awake()
	{
		pupilTracker = PupilGazeTracker.Instance;
		pupilSettings = pupilTracker.Settings;
	}

	public void Start()
	{
		graphTime = Time.time;

		gazeInfo = GetComponentInChildren<TextMesh> ();

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

		InitializeEyeVisualization (forEye: "Left");
		InitializeEyeVisualization (forEye: "Right");

		PupilTools.SubscribeTo ("pupil.");
		PupilTools.StartFramePublishing ();
	}

	void Update()
	{
		//Construct the Text box string for data display on the Operator Monitor view
		string str = 
			string.Format("Gaze Point : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.GazePosition.x,PupilData._3D.GazePosition.y,PupilData._3D.GazePosition.z)
			+ string.Format("\nEyeball 0 Center : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.RightEyeCenter.x,PupilData._3D.RightEyeCenter.y,PupilData._3D.RightEyeCenter.z)
			+ string.Format("\nEyeball 1 Center : ( X: {0} Y: {1} Z: {2} )",PupilData._3D.LeftEyeCenter.x,PupilData._3D.LeftEyeCenter.y,PupilData._3D.LeftEyeCenter.z)
			+ string.Format("\nPupil Diameter : {0}",PupilData.Diameter ())
			;
		gazeInfo.text = str;

		pupilSettings.framePublishing.UpdateEyeTextures ();
		if ((Time.time - graphTime) > updateConfidenceEveryXSeconds)
		{
			UpdateEyeVisualization ();
			graphTime = Time.time;
		}
	}

	void InitializeEyeVisualization(string forEye)
	{
		GameObject go = GameObject.CreatePrimitive (PrimitiveType.Plane);
		go.name = forEye + " Eye Rendering Plane";
		GameObject.Destroy (go.GetComponent<Collider> ());
		go.transform.eulerAngles = Vector3.left * 90f;
		go.transform.localScale *= 0.05f;
		go.transform.parent = gameObject.transform;
		go.transform.localPosition = new Vector3 (0.3f,0.1f,2f);
		if (forEye == "Right")
			go.transform.localPosition = new Vector3 (-0.3f,0.1f,2f);
		MeshRenderer mr = go.GetComponent<MeshRenderer> ();
		mr.material = (forEye == "Right") ? pupilSettings.framePublishing.eye0ImageMaterial : pupilSettings.framePublishing.eye1ImageMaterial;

		TextMesh tm = GameObject.Instantiate (gazeInfo);
		tm.name = forEye + " Eye Text";
		tm.transform.parent = go.transform;
		tm.transform.localPosition = Vector3.forward * 7;
		tm.transform.localScale = Vector3.one * 0.2f;
		tm.text = forEye + " Eye";

		var lrGO = new GameObject (forEye + " Eye Confidence Level");
		lrGO.transform.parent = go.transform;
		lrGO.transform.localPosition = Vector3.left * 4;
		LineRenderer lr = lrGO.AddComponent<LineRenderer> ();
		lr.material = new Material (Shader.Find ("Unlit/Color"));
		lr.material.color = (forEye == "Right") ? PupilSettings.rightEyeColor : PupilSettings.leftEyeColor;
		lr.positionCount = graphLength;
		lr.useWorldSpace = false;
		lr.startWidth = 0.005f;
		lr.endWidth = 0.005f;
		lr.receiveShadows = false;
		if (forEye == "Left")
			leftEyeConfidenceLevel = lr;
		else
			rightEyeConfidenceLevel = lr;
	}

	public void UpdateEyeVisualization()
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
