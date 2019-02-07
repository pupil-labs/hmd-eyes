using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PupilData
{
	public const int leftEyeID = 1;
	public const int rightEyeID = 0;

	public static double Diameter ()
	{
		return new double ();
	}

	public static class _3D
	{
		public static Vector3 GazePosition = Vector3.zero;

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
		public static Vector2 LeftEyePosition = Vector2.zero;
		public static Vector2 RightEyePosition = Vector2.zero;

		public static Vector2 Gaze2DPosUDP = Vector2.zero;
		public static Vector2 GazePosition
		{
			get 
			{
				if (LeftEyePosition != Vector2.zero && RightEyePosition != Vector2.zero)
					return 0.5f * (LeftEyePosition + RightEyePosition); 
				return Gaze2DPosUDP;
			}
		}

		public static Vector2 GetEyeGaze (int eyeID)
		{
			switch (eyeID)
			{
			case rightEyeID:
				return RightEyePosition;
			case leftEyeID:
				return LeftEyePosition;
			default:
				return Vector2.zero;
			}
		}
	}
}
