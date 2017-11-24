using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu (fileName = "PupilSettings")]
public class PupilSettings:ScriptableObject
{
	static PupilSettings _instance = null;

	public static PupilSettings Instance
	{
		get
		{
			if (_instance == null)
				_instance = PupilTools.Settings;
			return _instance;
		}
	}

	public enum EStatus
	{
		Idle,
		ProcessingGaze,
		Calibration
	}

	[Serializable]
	public class PupilServiceApp
	{
		public string servicePath;
	}

	[Serializable]
	public class CustomGUIVariables
	{
		[Serializable]
		public class Tabs
		{
			public int mainTab;
		}

		[Serializable]
		public class Bools
		{
			public bool isAdvanced;
		}

		public Tabs tabs;
		public Bools bools;

	}

	[Serializable]
	public class DebugView
	{
		public bool active = false;
	}

	[Serializable]
	public class DebugVars
	{
		public bool printSampling;
		public bool printMessage;
		public bool printMessageType;
	}

	public DebugVars debug;

	public EStatus DataProcessState;

	public Connection connection;
	public PupilServiceApp pupilServiceApp;
	public Calibration calibration;
	public CustomGUIVariables customGUIVariables;
	public DebugView debugView;
	public FramePublishing framePublishing;
	public bool visualizeGaze;
	public Camera currentCamera;
#if !UNITY_WSA
	public Recorder recorder;
#endif
	public List<GUIStyle> GUIStyles;

	public static int numberOfMessages = 6;

	public static float PupilUnitScalingFactor = 1000;	// Pupil is currently operating in mm
}

