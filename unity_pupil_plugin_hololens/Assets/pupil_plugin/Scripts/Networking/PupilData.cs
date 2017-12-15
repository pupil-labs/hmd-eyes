using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pupil;

public static class PupilData
{
	public static Calibration.data CalibrationData;

	public static int leftEyeID = 1;
	public static int rightEyeID = 0;

	public static GazeSource currentEyeID = GazeSource.GazeOnly;

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
				return Gaze3DPosUDP;
			}
		}

		public static Vector3 LeftEyeCenter;
		public static Vector3 RightEyeCenter;
		public static Vector3 LeftGazeNormal;
		public static Vector3 RightGazeNormal;

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
				return LeftEyePosUDP;
			}
		}

		public static Vector2 RightEyePosUDP = Vector2.zero;
		private static Vector2 RightEyePos
		{
			get
			{
				return RightEyePosUDP;
			}
		}

		public static Vector2 Gaze2DPosUDP = Vector2.zero;
		private static Vector2 GazePosition
		{
			get 
			{
				if (LeftEyePos != Vector2.zero && RightEyePos != Vector2.zero)
					return 0.5f * (LeftEyePos + RightEyePos); 
				return Gaze2DPosUDP;
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
