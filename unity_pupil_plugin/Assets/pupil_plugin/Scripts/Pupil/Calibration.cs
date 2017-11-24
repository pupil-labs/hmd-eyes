using System;
using UnityEngine;

[Serializable]
public class Calibration
{
	public enum Mode
	{
		_2D,
		_3D
	}

	private Mode _currentMode = Mode._2D; // 3D should be standard mode in the future
	public Mode currentMode
	{
		get { return _currentMode; }
		set
		{		
			if (PupilTools.Settings.connection.isConnected && !PupilTools.Settings.connection.Is3DCalibrationSupported ())
				value = Mode._2D;

			if (_currentMode != value)
			{
				_currentMode = value;

				if (PupilTools.Settings.connection.isConnected)
					PupilTools.SetDetectionMode ();
			}
		}
	}
		
	[Serializable]
	public struct Type
	{
		public string name;
		public string pluginName;
		public string positionKey;
		public double[] ref_data;
		public float points;
		public Vector3[] vectorDepthRadiusScale;
		public int samplesPerDepth;
	}

	public Type CalibrationType2D = new Type () 
	{ 
		name = "2d",
		pluginName = "HMD_Calibration",
		positionKey = "norm_pos",
		ref_data = new double[]{ 0.0, 0.0 },
		points = 8,
		vectorDepthRadiusScale = new Vector3[] { new Vector3( 2f, 0.08f, 0.05f ) },
		samplesPerDepth = 120
	};

	public Type CalibrationType3D = new Type () 
	{ 
		name = "3d",
		pluginName = "HMD_Calibration_3D",
		positionKey = "mm_pos",
		ref_data = new double[]{ 0.0, 0.0, 0.0 },
		points = 10,
		vectorDepthRadiusScale = new Vector3[] { new Vector3( 1f, 0.5f, 50 ) },
		samplesPerDepth = 40
	};

	public int samplesToIgnoreForEyeMovement = 10;

	public Type currentCalibrationType
	{
		get
		{
			if (currentMode == Mode._2D)
			{
				return CalibrationType2D;

			} else
			{
				return CalibrationType3D;
			}
		}
	}

	public enum Status
	{
		NotSet,
		Started,
		Stopped,
		Succeeded
	}
	private Status _currentStatus;
	public Status currentStatus
	{
		get { return _currentStatus; }
		set
		{
			_currentStatus = value;
			calibrationMarker.SetActive (_currentStatus == Status.Started);
		}
	}

	public float[] rightEyeTranslation;
	public float[] leftEyeTranslation;

	private float radius;
	public void UpdateCalibrationPoint()
	{
		currentCalibrationPointPosition = new float[]{0};
		switch (currentMode)
		{
		case Mode._3D:
			currentCalibrationPointPosition = new float[]{ 0f, 0f, currentCalibrationType.vectorDepthRadiusScale [currentCalibrationDepth].x };
			break;
		default:
			currentCalibrationPointPosition = new float[]{ 0.5f, 0.5f };
			break;
		}
		radius = currentCalibrationType.vectorDepthRadiusScale[currentCalibrationDepth].y;
		if (currentCalibrationPoint > 0 && currentCalibrationPoint < currentCalibrationType.points)
		{	
			currentCalibrationPointPosition [0] += radius * (float) Math.Cos (2f * Math.PI * (currentCalibrationPoint - 1) / (currentCalibrationType.points-1));
			currentCalibrationPointPosition [1] += radius * (float) Math.Sin (2f * Math.PI * (currentCalibrationPoint - 1) / (currentCalibrationType.points-1));
		}
		calibrationMarker.UpdatePosition (currentCalibrationPointPosition);
		calibrationMarker.SetScale (currentCalibrationType.vectorDepthRadiusScale [currentCalibrationDepth].z);
	}

	PupilMarker calibrationMarker;
	int currentCalibrationPoint;
	int currentCalibrationSamples;
	int currentCalibrationDepth;
	float[] currentCalibrationPointPosition;
	public void InitializeCalibration ()
	{
		Debug.Log ("Initializing Calibration");

		currentCalibrationPoint = 0;
		currentCalibrationSamples = 0;
		currentCalibrationDepth = 0;

		if (!PupilMarker.TryToReset (calibrationMarker))
			calibrationMarker = new PupilMarker ("Calibraton Marker", Color.white);
		UpdateCalibrationPoint ();

		//		yield return new WaitForSeconds (2f);

		Debug.Log ("Starting Calibration");

		currentStatus = Status.Started;
	}

	static float lastTimeStamp = 0;
	static float timeBetweenCalibrationPoints = 0.1f; // was 0.1, 1000/60 ms wait in old version
	public void UpdateCalibration ()
	{
		float t = Time.time;// PupilTools.Settings.connection.currentPupilTimestamp;

		if (t - lastTimeStamp > timeBetweenCalibrationPoints)
		{
			lastTimeStamp = t;

			UpdateCalibrationPoint ();// .currentCalibrationType.calibPoints [currentCalibrationPoint];
			//			print ("its okay to go on");

			//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			if ( currentCalibrationSamples > samplesToIgnoreForEyeMovement )
				PupilTools.AddCalibrationPointReferencePosition (currentCalibrationPointPosition, t);
			
			if (PupilTools.Settings.debug.printSampling)
				Debug.Log ("Point: " + currentCalibrationPoint + ", " + "Sampling at : " + currentCalibrationSamples + ". On the position : " + currentCalibrationPointPosition [0] + " | " + currentCalibrationPointPosition [1]);

			currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

			if (currentCalibrationSamples >= currentCalibrationType.samplesPerDepth)
			{
				currentCalibrationSamples = 0;
				currentCalibrationDepth++;

				if (currentCalibrationDepth >= currentCalibrationType.vectorDepthRadiusScale.Length)
				{
					currentCalibrationDepth = 0;
					currentCalibrationPoint++;

					//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
					PupilTools.AddCalibrationReferenceData ();

					if (currentCalibrationPoint >= currentCalibrationType.points)
					{
						PupilTools.StopCalibration ();
					}
				}

			}
		}
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
}