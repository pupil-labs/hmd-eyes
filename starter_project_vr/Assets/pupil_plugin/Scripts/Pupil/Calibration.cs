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

	[Serializable]
	public struct Type
	{
		public string name;
		public string pluginName;
		public string positionKey;
		public double[] ref_data;
		public float points;
		public float markerScale;
		public Vector2 centerPoint;
		public Vector2[] vectorDepthRadius;
		public int samplesPerDepth;
	}

	public Type CalibrationType2D = new Type () 
	{ 
		name = "2d",
		pluginName = "HMD_Calibration",
		positionKey = "norm_pos",
		ref_data = new double[]{ 0.0, 0.0 },
		points = 8,
		markerScale = 0.05f,
		centerPoint = new Vector2(0.5f,0.5f),
		vectorDepthRadius = new Vector2[] { new Vector2( 2f, 0.07f ) },
		samplesPerDepth = 120
	};

	public Type CalibrationType3D = new Type () 
	{ 
		name = "3d",
		pluginName = "HMD_Calibration_3D",
		positionKey = "mm_pos",
		ref_data = new double[]{ 0.0, 0.0, 0.0 },
		points = 10,
		markerScale = 0.04f,
		centerPoint = new Vector2(0,-0.05f),
		vectorDepthRadius = new Vector2[] { new Vector2( 1f, 0.24f ) },
		samplesPerDepth = 40
	};

	public int samplesToIgnoreForEyeMovement = 10;

	public Type currentCalibrationType
	{
		get
		{
			if (PupilTools.CalibrationMode == Mode._2D)
				return CalibrationType2D;
 			else
				return CalibrationType3D;
		}
	}

	public float[] rightEyeTranslation;
	public float[] leftEyeTranslation;

	private float radius;
	private double offset;
	public void UpdateCalibrationPoint()
	{
		var type = currentCalibrationType;
		currentCalibrationPointPosition = new float[]{0};
		switch (PupilTools.CalibrationMode)
		{
		case Mode._3D:
			currentCalibrationPointPosition = new float[] {type.centerPoint.x,type.centerPoint.y,type.vectorDepthRadius [currentCalibrationDepth].x};
			offset = 0.25f * Math.PI;
			break;
		default:
			currentCalibrationPointPosition = new float[]{ type.centerPoint.x,type.centerPoint.y };
			offset = 0f;
			break;
		}
		radius = type.vectorDepthRadius[currentCalibrationDepth].y;
		if (currentCalibrationPoint > 0 && currentCalibrationPoint < type.points)
		{	
			currentCalibrationPointPosition [0] += radius * (float) Math.Cos (2f * Math.PI * (float)(currentCalibrationPoint - 1) / (type.points-1f) + offset);
			currentCalibrationPointPosition [1] += radius * (float) Math.Sin (2f * Math.PI * (float)(currentCalibrationPoint - 1) / (type.points-1f) + offset);
		}
		if (PupilTools.CalibrationMode == Mode._3D)
			currentCalibrationPointPosition [1] /= PupilSettings.Instance.currentCamera.aspect;
		Marker.UpdatePosition (currentCalibrationPointPosition);
		Marker.SetScale (type.markerScale);
	}

	public PupilMarker Marker;
	int currentCalibrationPoint;
	int previousCalibrationPoint;
	int currentCalibrationSamples;
	int currentCalibrationDepth;
	int previousCalibrationDepth;
	float[] currentCalibrationPointPosition;
	public void InitializeCalibration ()
	{
		Debug.Log ("Initializing Calibration");

		currentCalibrationPoint = 0;
		currentCalibrationSamples = 0;
		currentCalibrationDepth = 0;
		previousCalibrationDepth = -1;
		previousCalibrationPoint = -1;

		if (!PupilMarker.TryToReset (Marker))
			Marker = new PupilMarker ("Calibraton Marker", Color.white);
		UpdateCalibrationPoint ();

		//		yield return new WaitForSeconds (2f);

		Debug.Log ("Starting Calibration");
	}

	static float lastTimeStamp = 0;
	static float timeBetweenCalibrationPoints = 0.02f; // was 0.1, 1000/60 ms wait in old version
	public void UpdateCalibration ()
	{
		float t = Time.time;// PupilSettings.Instance.connection.currentPupilTimestamp;

		if (t - lastTimeStamp > timeBetweenCalibrationPoints)
		{
			lastTimeStamp = t;

			UpdateCalibrationPoint ();// .currentCalibrationType.calibPoints [currentCalibrationPoint];
			//			print ("its okay to go on");

			//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			if ( currentCalibrationSamples > samplesToIgnoreForEyeMovement )
				PupilTools.AddCalibrationPointReferencePosition (currentCalibrationPointPosition, t);
			
			if (PupilSettings.Instance.debug.printSampling)
				Debug.Log ("Point: " + currentCalibrationPoint + ", " + "Sampling at : " + currentCalibrationSamples + ". On the position : " + currentCalibrationPointPosition [0] + " | " + currentCalibrationPointPosition [1]);

			currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

			if (currentCalibrationSamples >= currentCalibrationType.samplesPerDepth)
			{
				currentCalibrationSamples = 0;
				currentCalibrationDepth++;

				if (currentCalibrationDepth >= currentCalibrationType.vectorDepthRadius.Length)
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
}