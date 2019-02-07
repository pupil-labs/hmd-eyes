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
				_instance = Resources.Load<PupilSettings> ("PupilSettings");
			return _instance;
		}
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
	public class DebugVars
	{
		public bool printSampling;
		public bool printMessage;
		public bool printMessageType;
	}

	public DebugVars debug;

	public Connection connection;
	public Calibration calibration;
	public CustomGUIVariables customGUIVariables;
	public Camera currentCamera;
#if !UNITY_WSA
	public Recorder recorder;
#endif
	public List<GUIStyle> GUIStyles;

	public static int numberOfMessages = 6;

	public static float PupilUnitScalingFactor = 1000;	// Pupil is currently operating in mm
	public const string gaze2DLeftEyeKey = "norm_pos_1";
	public const string gaze2DRightEyeKey = "norm_pos_0";
	public const string gaze2DKey = "gaze_point_2d";
	public const string gaze3DKey = "gaze_point_3d";

	public static Color leftEyeColor = Color.green;
	public static Color rightEyeColor = Color.blue;
}

