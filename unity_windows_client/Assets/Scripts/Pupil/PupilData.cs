using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PupilData
{
	public enum GazeSource
	{
		LeftEye,
		RightEye,
		BothEyes,
		GazeOnly
	}

	public enum udpMode
	{
		LeftEyeOnly = 0,
		RightEyeOnly = 1,
		LeftAndRight = 2,
		Gaze2D = 3,
		Gaze3D = 4,
		DontSendData = 5
	}
	public static udpMode mode = udpMode.DontSendData;

	private static int SamplesCount = 4;

	private static Dictionary<string,EyeData> eyeData = new Dictionary<string,EyeData>();

	public static int leftEyeID = 1;
	private const string stringForLeftEyeID = "1";
	private static string leftEyeKey = "norm_pos" + "_" + stringForLeftEyeID;
	public static EyeData leftEye
	{
		get
		{
			if (!eyeData.ContainsKey(leftEyeKey))
				eyeData.Add(leftEyeKey, new EyeData(SamplesCount,2));
			return eyeData [leftEyeKey]; 
		}
	}

	public static int rightEyeID = 0;
	private const string stringForRightEyeID = "0";
	private static string rightEyeKey = "norm_pos" + "_" + stringForRightEyeID;
	public static EyeData rightEye
	{
		get
		{
			if (!eyeData.ContainsKey(rightEyeKey))
				eyeData.Add(rightEyeKey, new EyeData(SamplesCount,2));
			return eyeData [rightEyeKey]; 
		}
	}
	private static string gazePointKey = "gaze_point_3d";
	public static EyeData gazePoint
	{
		get
		{
			if (!eyeData.ContainsKey(gazePointKey))
				eyeData.Add(gazePointKey, new EyeData(SamplesCount,3));
			return eyeData [gazePointKey]; 
		}
	}
	private static string leftGazeNormalKey = "gaze_normals_3d" + "_" + stringForLeftEyeID;
	public static EyeData leftGazeNormal
	{
		get
		{
			if (!eyeData.ContainsKey(leftGazeNormalKey))
				eyeData.Add(leftGazeNormalKey, new EyeData(SamplesCount,3));
			return eyeData [leftGazeNormalKey]; 
		}
	}
	private static string rightGazeNormalKey = "gaze_normals_3d" + "_" + stringForRightEyeID;
	public static EyeData rightGazeNormal
	{
		get
		{
			if (!eyeData.ContainsKey(rightGazeNormalKey))
				eyeData.Add(rightGazeNormalKey, new EyeData(SamplesCount,3));
			return eyeData [rightGazeNormalKey]; 
		}
	}
	private static string leftEyeCenterKey = "eye_centers_3d" + "_" + stringForLeftEyeID;
	public static EyeData leftEyeCenter
	{
		get
		{
			if (!eyeData.ContainsKey(leftEyeCenterKey))
				eyeData.Add(leftEyeCenterKey, new EyeData(SamplesCount,3));
			return eyeData [leftEyeCenterKey]; 
		}
	}
	private static string rightEyeCenterKey = "eye_centers_3d" + "_" + stringForRightEyeID;
	public static EyeData rightEyeCenter
	{
		get
		{
			if (!eyeData.ContainsKey(rightEyeCenterKey))
				eyeData.Add(rightEyeCenterKey, new EyeData(SamplesCount,3));
			return eyeData [rightEyeCenterKey]; 
		}
	}

	public static void AddGazeToEyeData(string key, float[] position)
	{
		if (!eyeData.ContainsKey (key))
		{
			if (key.StartsWith ("norm_pos"))
				eyeData.Add (key, new EyeData (SamplesCount, 2));
			else
				eyeData.Add (key, new EyeData (SamplesCount, 3));
		}
		eyeData[key].AddGaze(position,calculateMovingAverage);
	}

	public static GazeSource currentEyeID;
	public static void UpdateCurrentEyeID(string id)
	{
		switch (id)
		{
		case stringForLeftEyeID:
			currentEyeID = GazeSource.LeftEye;
			break;
		case stringForRightEyeID:
			currentEyeID = GazeSource.RightEye;
			break;
		default:
			currentEyeID = GazeSource.GazeOnly;
			break;
		}
	}

	private static bool _calculateMovingAverage = false;
	public static bool calculateMovingAverage
	{
		get
		{
			return _calculateMovingAverage;
		}
		set
		{
			_calculateMovingAverage = value;
		}
	}

	public static double Diameter ()
	{
		return new double ();
	}

	public static class _3D
	{
		public static Vector3 GazePosition
		{
			get { return gazePoint.Average3D; }
		}

		public static Vector3 LeftEyeCenter
		{
			get { return leftEyeCenter.Average3D; }
		}
		public static Vector3 RightEyeCenter
		{
			get { return rightEyeCenter.Average3D; }
		}

		public static Vector3 LeftGazeNormal
		{
			get { return leftGazeNormal.Average3D; }
		}
		public static Vector3 RightGazeNormal
		{
			get { return rightGazeNormal.Average3D; }
		}

		public static class Circle
		{

			public static Vector3 Center (int eyeID)
			{
				return Vector3.zero;
			}

			public static double Radius (int eyeID)
			{

				return 0.0;

			}

			public static Vector3 Normal (int eyeID)
			{

				return Vector3.zero;

			}

		}
	}

	public class _2D
	{
		private static Vector2 LeftEyePos
		{
			get{ return leftEye.Average2D; }
		}

		private static Vector2 RightEyePos
		{
			get{ return rightEye.Average2D; }
		}

		private static Vector2 GazePosition
		{
			get { return 0.5f * (LeftEyePos + RightEyePos); }
		}

		public static Vector2 GetEyeGaze (GazeSource s)
		{
			switch (s)
			{
			case GazeSource.LeftEye:
				return LeftEyePos;
			case GazeSource.RightEye:
				return RightEyePos;
			default:
				return GazePosition;
			}
		}
		public static Vector2 GetEyeGaze (string eyeID)
		{
			switch (eyeID)
			{
			case "0":
				return GetEyeGaze(GazeSource.RightEye);
			case "1":
				return GetEyeGaze(GazeSource.LeftEye);
			default:
				return Vector2.zero;
			}
		}
	}
}
