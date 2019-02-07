using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PupilData
{
	private static int SamplesCount = 4;

	private static Dictionary<string,EyeData> eyeData = new Dictionary<string,EyeData>();

	public const string leftEyeID = "1";
	private static string leftEyeKey = "norm_pos" + "_" + leftEyeID;
	public static EyeData leftEye
	{
		get
		{
			if (!eyeData.ContainsKey(leftEyeKey))
				eyeData.Add(leftEyeKey, new EyeData(SamplesCount));
			return eyeData [leftEyeKey]; 
		}
	}

	public const string rightEyeID = "0";
	private static string rightEyeKey = "norm_pos" + "_" + rightEyeID;
	public static EyeData rightEye
	{
		get
		{
			if (!eyeData.ContainsKey(rightEyeKey))
				eyeData.Add(rightEyeKey, new EyeData(SamplesCount));
			return eyeData [rightEyeKey]; 
		}
	}
	private static string gazePointKey = "gaze_point_3d";
	public static EyeData gazePoint
	{
		get
		{
			if (!eyeData.ContainsKey(gazePointKey))
				eyeData.Add(gazePointKey, new EyeData(SamplesCount));
			return eyeData [gazePointKey]; 
		}
	}
	private static string leftGazeNormalKey = "gaze_normals_3d" + "_" + leftEyeID;
	public static EyeData leftGazeNormal
	{
		get
		{
			if (!eyeData.ContainsKey(leftGazeNormalKey))
				eyeData.Add(leftGazeNormalKey, new EyeData(SamplesCount));
			return eyeData [leftGazeNormalKey]; 
		}
	}
	private static string rightGazeNormalKey = "gaze_normals_3d" + "_" + rightEyeID;
	public static EyeData rightGazeNormal
	{
		get
		{
			if (!eyeData.ContainsKey(rightGazeNormalKey))
				eyeData.Add(rightGazeNormalKey, new EyeData(SamplesCount));
			return eyeData [rightGazeNormalKey]; 
		}
	}
	private static string leftEyeCenterKey = "eye_centers_3d" + "_" + leftEyeID;
	public static EyeData leftEyeCenter
	{
		get
		{
			if (!eyeData.ContainsKey(leftEyeCenterKey))
				eyeData.Add(leftEyeCenterKey, new EyeData(SamplesCount));
			return eyeData [leftEyeCenterKey]; 
		}
	}
	private static string rightEyeCenterKey = "eye_centers_3d" + "_" + rightEyeID;
	public static EyeData rightEyeCenter
	{
		get
		{
			if (!eyeData.ContainsKey(rightEyeCenterKey))
				eyeData.Add(rightEyeCenterKey, new EyeData(SamplesCount));
			return eyeData [rightEyeCenterKey]; 
		}
	}
		
	public static void AddGazeToEyeData(string key, Vector3 position)
	{
		if (!eyeData.ContainsKey (key))
			eyeData.Add (key, new EyeData (SamplesCount));
		
		eyeData[key].AddGaze(position,calculateMovingAverage);
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
			get 
			{
				if (calculateMovingAverage)
					return gazePoint.Average;
				else
					return gazePoint.Raw;
			}
		}

		public static Vector3 LeftEyeCenter
		{
			get 
			{
				if (calculateMovingAverage)
					return leftEyeCenter.Average;
				else
					return leftEyeCenter.Raw;
			}
		}
		public static Vector3 RightEyeCenter
		{
			get 
			{
				if (calculateMovingAverage)
					return rightEyeCenter.Average; 
				else
					return rightEyeCenter.Raw;
			}
		}

		public static Vector3 LeftGazeNormal
		{
			get 
			{
				if (calculateMovingAverage)
					return leftGazeNormal.Average; 
				else
					return leftGazeNormal.Raw;
			}
		}
		public static Vector3 RightGazeNormal
		{
			get 
			{
				if (calculateMovingAverage)
					return rightGazeNormal.Average; 
				else
					return rightGazeNormal.Raw;
			}
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
		public static Vector2 LeftEyePosition
		{
			get
			{
				if (calculateMovingAverage)
					return leftEye.Average;
				else
					return leftEye.Raw;
			}
		}

		public static Vector2 RightEyePosition
		{
			get
			{
				if (calculateMovingAverage)
					return rightEye.Average;
				else
					return rightEye.Raw;
			}
		}

		public static Vector2 GazePosition
		{
			get { return 0.5f * (LeftEyePosition + RightEyePosition); }
		}

		static Camera _sceneCamera;
		static Vector2 frustumOffsetsLeftEye = Vector2.zero;
		static Vector2 frustumOffsetsRightEye = Vector2.zero;
		static Vector2 standardFrustumCenter = Vector2.one * 0.5f;
		static void InitializeFrustumEyeOffset()
		{
			Vector3[] frustumCornersMono = new Vector3[4];
			_sceneCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _sceneCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersMono);
			Vector2 frustumWidthHeight = frustumCornersMono [2] - frustumCornersMono [0];

			Vector3[] frustumCornersLeft = new Vector3[4];
			_sceneCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _sceneCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Left, frustumCornersLeft);

			// Step by step example for x
			//		float leftEyeFrustumLeftOffset = (frustumCornersLeft [0].x - frustumCornersMono [0].x) / frustumWidth;
			//		float leftEyeFrustumRightOffset = (frustumCornersLeft [3].x - frustumCornersMono [0].x) / frustumWidth;
			//		float frustumOffsetLeftEye = leftEyeFrustumLeftOffset + 0.5f * (leftEyeFrustumRightOffset + leftEyeFrustumLeftOffset) - 0.5f;
			// Combined
			frustumOffsetsLeftEye = 1.5f * frustumCornersLeft [0] + 0.5f * frustumCornersLeft [2] - 2f * frustumCornersMono [0];
			frustumOffsetsLeftEye.x /= frustumWidthHeight.x;
			frustumOffsetsLeftEye.y /= frustumWidthHeight.y;

			Vector3[] frustumCornersRight = new Vector3[4];
			_sceneCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _sceneCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Right, frustumCornersRight);
			frustumOffsetsRightEye = 1.5f * frustumCornersRight [0] + 0.5f * frustumCornersRight [2] - 2f * frustumCornersMono [0];
			frustumOffsetsRightEye.x /= frustumWidthHeight.x;
			frustumOffsetsRightEye.y /= frustumWidthHeight.y;
		}

		public static Vector2 ApplyFrustumOffset(Vector2 position,string eyeID)
		{
			Vector2 offsetPoint = position;

			switch (eyeID)
			{
			case rightEyeID:
				offsetPoint -= (frustumOffsetsRightEye - standardFrustumCenter);
				break;
			case leftEyeID:
				offsetPoint -= (frustumOffsetsLeftEye - standardFrustumCenter);
				break;
			default:
				break;
			}
			return offsetPoint;
		}

		public static Vector2 GetEyePosition (Camera sceneCamera, string eyeID)
		{
			if (_sceneCamera == null || _sceneCamera != sceneCamera)
			{
				_sceneCamera = sceneCamera;
				InitializeFrustumEyeOffset ();
			}
			return ApplyFrustumOffset (GetEyeGaze(eyeID), eyeID);
		}

		public static Vector2 GetEyeGaze (string eyeID)
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
