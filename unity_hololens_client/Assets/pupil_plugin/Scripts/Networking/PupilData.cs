using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pupil;

public static class PupilData
{
	public enum udpMode
	{
		LeftEyeOnly = 0,
		RightEyeOnly = 1,
		LeftAndRight = 2,
		Gaze2D = 3,
		Gaze3D = 4
	}
	private static udpMode _mode = udpMode.Gaze2D;
	public static udpMode mode
	{
		get { return _mode; }
		set
		{
			if (_mode != value)
			{
				_mode = value;
				UDPCommunicator.Instance.SendUDPMessage (new byte[] { 50, (byte) _mode });
			}
		}
	}

	public static Calibration.data CalibrationData;

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
			if (_calculateMovingAverage)
				UDPCommunicator.Instance.SendUDPMessage (new byte[]{ 59, 1 });
			else
				UDPCommunicator.Instance.SendUDPMessage (new byte[]{ 59, 0 });
		}
	}

	public static double Diameter ()
	{
		return new double ();
	}

	public static class _3D
	{
		public static Vector3 Gaze3DPosUDP = Vector3.zero;
		public static Vector3 GazePosition
		{
			get 
			{
				if (mode != udpMode.Gaze3D)
					mode = udpMode.Gaze3D;
				return Gaze3DPosUDP;
			}
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
		public static Vector2 LeftEyePosUDP = Vector2.zero;
		private static Vector2 LeftEyePos
		{
			get
			{
				switch (mode)
				{
				case udpMode.LeftAndRight:
					break;
				case udpMode.RightEyeOnly:
					mode = udpMode.LeftAndRight;
					break;
				default:
					mode = udpMode.LeftEyeOnly;
					break;
				}
				return LeftEyePosUDP;
			}
		}

		public static Vector2 RightEyePosUDP = Vector2.zero;
		private static Vector2 RightEyePos
		{
			get
			{
				switch (mode)
				{
				case udpMode.LeftAndRight:
					break;
				case udpMode.LeftEyeOnly:
					mode = udpMode.LeftAndRight;
					break;
				default:
					mode = udpMode.RightEyeOnly;
					break;
				}
				return RightEyePosUDP;
			}
		}

		public static Vector2 Gaze2DPosUDP = Vector2.zero;
		private static Vector2 GazePosition
		{
			get 
			{
				switch (mode)
				{
				case udpMode.LeftAndRight:
					return 0.5f * (LeftEyePos + RightEyePos); 
				default:
					mode = udpMode.Gaze2D;
					return Gaze2DPosUDP;
				}
			}
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
