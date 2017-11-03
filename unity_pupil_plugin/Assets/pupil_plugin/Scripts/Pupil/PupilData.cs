using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PupilData
{
	public static Calibration.data CalibrationData;

	private static int SamplesCount = 4;

	class MovingAverage
	{
		List<float> samples = new List<float> ();
		int length = 5;

		public MovingAverage (int len)
		{
			length = len;
		}

		public float AddSample (float v)
		{
			samples.Add (v);
			while (samples.Count > length)
			{
				samples.RemoveAt (0);
			}
			float s = 0;
			for (int i = 0; i < samples.Count; ++i)
				s += samples [i];

			return s / (float)samples.Count;

		}
	}

	public class EyeData
	{
		MovingAverage xavg;
		MovingAverage yavg;
		MovingAverage zavg;

		public EyeData (int len)
		{
			xavg = new MovingAverage (len);
			yavg = new MovingAverage (len);
			zavg = new MovingAverage (len);
		}

		public Vector2 gaze2D = new Vector2 ();

		public Vector2 AddGaze (float x, float y, int eyeID)
		{
			//			print ("adding gaze : " + x + " , " + y + "for the eye : " + eyeID);
			gaze2D.x = xavg.AddSample (x);
			gaze2D.y = yavg.AddSample (y);
			return gaze2D;
		}
		public Vector2 AddGaze (Vector2 position)
		{
			//			print ("adding gaze : " + x + " , " + y + "for the eye : " + eyeID);
			gaze2D.x = xavg.AddSample (position.x);
			gaze2D.y = yavg.AddSample (position.y);
			return gaze2D;
		}

		public Vector3 gaze3D = new Vector3 ();

		public Vector3 AddGaze (float x, float y, float z)
		{
			gaze3D.x = xavg.AddSample (x);
			gaze3D.y = yavg.AddSample (y);
			gaze3D.z = zavg.AddSample (z);
			return gaze3D;
		}
		public Vector3 AddGaze (Vector3 position)
		{
			gaze3D.x = xavg.AddSample (position.x);
			gaze3D.y = yavg.AddSample (position.y);
			gaze3D.z = zavg.AddSample (position.z);
			return gaze3D;
		}
	}

	private static EyeData _leftEye;
	public static EyeData leftEye
	{
		get
		{
			if (_leftEye == null)
				_leftEye = new EyeData (SamplesCount);
			return _leftEye;
		}
		set
		{
			_leftEye = value;
		}
	}
	private static EyeData _rightEye;
	public static EyeData rightEye
	{
		get
		{
			if (_rightEye == null)
				_rightEye = new EyeData (SamplesCount);
			return _rightEye;
		}
		set
		{
			_rightEye = value;
		}
	}

	public enum GazeSource
	{
		LeftEye,
		RightEye,
		BothEyes,
		NoEye
	}
	public static GazeSource gazeSourceForString (string id)
	{
		switch (id)
		{
		case PupilSettings.stringForLeftEyeID:
			return GazeSource.LeftEye;
		case PupilSettings.stringForRightEyeID:
			return GazeSource.RightEye;
		default:
			return GazeSource.NoEye;
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
			{
				leftEye = new EyeData (SamplesCount);
				rightEye = new EyeData (SamplesCount);
			}
		}
	}

	private static Dictionary<string, object> _gazeDictionary;
	public static Dictionary<string, object> gazeDictionary
	{
		get
		{
			return _gazeDictionary;
		}
		set
		{
			_gazeDictionary = value;

			if (calculateMovingAverage)
			{
				Vector2 position2D = _2D.Norm_Pos ();
				switch (eyeID)
				{
				case GazeSource.LeftEye:
					leftEye.AddGaze (position2D);
					break;
				case GazeSource.RightEye:
					rightEye.AddGaze (position2D);
					break;
				default:
					break;
				}
			}
		}
	}

	public static Dictionary<string, object> pupil0Dictionary;
	public static Dictionary<string, object> pupil1Dictionary;

	private static object o;

	public static double Diameter ()
	{
		return new double ();
	}

	public static class _3D
	{
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

		public static Vector3 EyeCenters (int eyeID)
		{

			return Vector3.zero;

		}

		public static Vector3 EyeNormals (int eyeID)
		{

			return Vector3.zero;

		}

		public static Vector3 Gaze ()
		{

			object o = new object ();
			object[] gaze_point_3d_o;

			Vector3 _v3 = new Vector3 ();

			if (pupil0Dictionary.TryGetValue ("gaze_point_3d", out o))
			{

				gaze_point_3d_o = o as object[];

				_v3.Set ((float)(double)gaze_point_3d_o [0], (float)(double)gaze_point_3d_o [1], (float)(double)gaze_point_3d_o [2]);

			}

			return _v3;

		}

	}

	public class _2D
	{
		private static int eyeID;

		public _2D (int _eyeID)
		{

			eyeID = _eyeID;

		}

		public static Vector2 Norm_Pos ()
		{

			object o = new object ();
			object[] norm_pos_o;

			Vector2 _v2 = new Vector2 ();

			if (gazeDictionary.TryGetValue ("norm_pos", out o))
			{

				norm_pos_o = o as object[];

				_v2.Set ((float)(double)norm_pos_o [0], (float)(double)norm_pos_o [1]);

			}

			return _v2;

		}

		// André: Not, yet implemented..
		private static Vector2 _normalizedEyePos2D;
		public static Vector2 NormalizedEyePos2D
		{
			get{ return _normalizedEyePos2D; }
		}

		private static Vector2 LeftEyePos
		{
			get{ return leftEye.gaze2D; }
		}

		private static Vector2 RightEyePos
		{
			get{ return rightEye.gaze2D; }
		}

		private static Vector2 GazePosition
		{
			get { return 0.5f * (LeftEyePos + RightEyePos); }
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

		public static Vector2 ApplyFrustumOffset(Vector2 position, PupilData.GazeSource gazeSource)
		{
			Vector2 offsetPoint = position;

			switch (gazeSource)
			{
			case PupilData.GazeSource.LeftEye:
				offsetPoint -= (frustumOffsetsLeftEye - standardFrustumCenter);
				break;
			case PupilData.GazeSource.RightEye:
				offsetPoint -= (frustumOffsetsRightEye - standardFrustumCenter);
				break;
			default:
				break;
			}
			return offsetPoint;
		}

		public static Vector2 GetEyePosition (Camera sceneCamera, GazeSource gazeSource)
		{
			if (_sceneCamera == null || _sceneCamera != sceneCamera)
			{
				_sceneCamera = sceneCamera;
				InitializeFrustumEyeOffset ();
			}
			return ApplyFrustumOffset (GetEyeGaze(gazeSource), gazeSource);
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
				return NormalizedEyePos2D;
			}
		}
	}

	public static string stringForEyeID ()
	{
		object IDo;
		bool isID = gazeDictionary.TryGetValue ("id", out IDo);

		if (isID)
		{
			return IDo.ToString ();

		}
		else
		{
			return null;
		}
	}

	public static GazeSource eyeID
	{
		get
		{
			object IDo;
			if (gazeDictionary == null)
				return GazeSource.NoEye;
			
			bool isID = gazeDictionary.TryGetValue ("id", out IDo);

			if (isID)
			{
				return gazeSourceForString(IDo.ToString ());

			} else
			{
				return GazeSource.NoEye;
			}
		}
	}

	public static double Confidence (int eyeID)
	{
		if (eyeID == PupilSettings.rightEyeID)
		{
			return ConfidenceForDictionary(pupil0Dictionary);
		} 
		else
		{
			return ConfidenceForDictionary(pupil1Dictionary);
		}
	}
	public static double Confidence (GazeSource s)
	{
		switch (s)
		{
		case GazeSource.RightEye:
			return Confidence (PupilSettings.rightEyeID);
		default:
			return Confidence (PupilSettings.leftEyeID);
		}
	}
	public static double ConfidenceForDictionary(Dictionary<string,object> dictionary)
	{
		object conf0;
		dictionary.TryGetValue ("confidence", out conf0);
		return (double)conf0;
	}

	public static Dictionary<object,object> BaseData ()
	{
		gazeDictionary.TryGetValue ("base_data", out o);
		return o as Dictionary<object,object>;
	}

}
