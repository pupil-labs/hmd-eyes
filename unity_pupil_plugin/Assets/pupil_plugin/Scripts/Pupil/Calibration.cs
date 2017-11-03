using System;
using UnityEngine;

[Serializable]
public class Calibration
{
	public enum CalibMode
	{
		_2D,
		_3D
	}
	public struct CalibrationType
	{
		public string name;
		public string pluginName;
		public string positionKey;
		public double[] ref_data;
		public float depth;
		//		public List<float[]> calibPoints;
		//		public float[] center;
		public float radius;
		public float points;
	}

	[Serializable]
	public class data
	{
		public string camera_intrinsics_str;
		public Vector3[] cal_ref_points_3d;
		public Vector3[] cal_gaze_points0_3d;
		public Vector3[] cal_gaze_points1_3d;
		public Vector3[] cal_points_3d;
		public Matrix4x4 eye_camera_to_world_matrix0;
		public Matrix4x4 eye_camera_to_world_matrix1;
		public cam_intrinsics camera_intrinsics;
	}

	[Serializable]
	public class cam_intrinsics
	{
		public double[] resolution;
		public string camera_name;
		public Vector3[] camera_matrix;
		public double[][] dist_coefs;
		//figure this out if needed.
		public int intt;
	}

	private CalibrationType CalibrationType2D = new CalibrationType () 
	{ 
		name = "2d",
		pluginName = "HMD_Calibration",
		positionKey = "norm_pos",
		ref_data = new double[]{ 0.0, 0.0 },
		depth = 2f,
		//			calibPoints = new List<float[]>() {
		//				new float[]{0.5f,0.5f},
		//				new float[]{0.42f,0.555f},
		//				new float[]{0.5f,0.62f},
		//				new float[]{0.58f,0.555f},
		//				new float[]{0.65f,0.5f},
		//				new float[]{0.58f,0.445f},
		//				new float[]{0.5f,0.38f},
		//				new float[]{0.42f,0.445f},
		//				new float[]{0.35f,0.5f},
		////				new float[]{0.5f,0.5f},
		//			},
		//			center = new float[]{0.5f,0.5f},
		radius = 0.08f,
		points = 8
	};

	private CalibrationType CalibrationType3D = new CalibrationType () 
	{ 
		name = "3d",
		pluginName = "HMD_Calibration_3D",
		positionKey = "mm_pos",
		ref_data = new double[]{ 0.0, 0.0, 0.0 },
		depth = 2f,
		//			calibPoints = new List<float[]> () {
		//				new float[]{ 0f, 0f, 100f },
		//				new float[]{ -40, -40, 100f },
		//				new float[]{ -40, -0f, 100f },
		//				new float[]{ 40, -0f, 100f },
		//				new float[]{ -20, -20, 100f },
		//				new float[]{ -40, 40, 100f },
		//				new float[]{ 0f, 40, 100f },
		//				new float[]{ 0f, -40, 100f },
		//				new float[]{ -20, 20, 100f },
		//				new float[]{ 40, 40, 100f },
		//				new float[]{ 20, 20, 100f },
		//				new float[]{ 40, -40, 100f },
		//				new float[]{ 20, -20, 100f }
		////				new float[]{0f,0f, 100f}
		//			},
		//			center = new float[]{0f,0f,0f},
		radius = 1f,
		points = 10
	};

	private CalibMode _currentCalibrationMode = CalibMode._2D; // 3D should be standard mode in the future
	public CalibMode currentCalibrationMode
	{
		get { return _currentCalibrationMode; }
		set
		{
			_currentCalibrationMode = value;
			Debug.Log ("Calibration mode changed to: " + _currentCalibrationMode.ToString ());
		}
	}
	public void SwitchCalibrationMode ()
	{
		if (_currentCalibrationMode == CalibMode._2D)
			_currentCalibrationMode = CalibMode._3D;
		else
			_currentCalibrationMode = CalibMode._2D;
	}	

	public float[] GetCalibrationPoint(int index)
	{
		float[] point = new float[]{0};
		switch (currentCalibrationMode)
		{
		case CalibMode._2D:
			point = new float[]{0.5f,0.5f};
			if (index > 0 && index < CalibrationType2D.points)
			{	
				point [0] += CalibrationType2D.radius * (float) Math.Cos (2f * Math.PI * (index - 1) / (CalibrationType2D.points-1));
				point [1] += CalibrationType2D.radius * (float) Math.Sin (2f * Math.PI * (index - 1) / (CalibrationType2D.points-1));
			}
			return point;
		case CalibMode._3D:
			point = new float[]{0f,0f,CalibrationType3D.depth};
			if (index > 0 && index < CalibrationType3D.points)
			{	
				point [0] += CalibrationType3D.radius * (float) Math.Cos (2f * Math.PI * (index - 1) / (CalibrationType3D.points-1));
				point [1] += CalibrationType3D.radius * (float) Math.Sin (2f * Math.PI * (index - 1) / (CalibrationType3D.points-1));
				point [2] = CalibrationType3D.depth;
			}
			return point;
		default:
			return point;
		}
	}

	public CalibrationType currentCalibrationType
	{
		get
		{
			if (currentCalibrationMode == CalibMode._2D)
			{
				return CalibrationType2D;

			} else
			{
				return CalibrationType3D;
			}
		}
	}

	public bool initialized = false;
}