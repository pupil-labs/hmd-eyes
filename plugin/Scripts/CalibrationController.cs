using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        public RequestController requestCtrl;
        public SubscriptionsController subsCtrl;
        new public Camera camera;
        public Transform marker;

        private Calibration calibration = new Calibration();

        private float radius;
        private double offset;

        public CalibrationSettings calibrationSettings;

        void OnEnable()
        {
            
        }

        bool updateInitialTranslation = true;
        void Update ()
        {

            if(updateInitialTranslation) 
            {
                //might be inconsistent during the first frames -> updating until calibration starts
                UpdateEyesTranslation();
            }

            if (calibration.IsCalibrating)
            {
                UpdateCalibration ();
            }

            
            if (requestCtrl.IsConnected && Input.GetKeyUp (KeyCode.C))
            {
                if (calibration.IsCalibrating)
                {
                    calibration.StopCalibration ();
                } else
                {
                    InitializeCalibration();

                    calibration.requestCtrl = requestCtrl;
                    calibration.subsCtrl = subsCtrl;
                    calibration.StartCalibration (calibrationSettings);
                    updateInitialTranslation = false;
                }
            }
        }

        void UpdateEyesTranslation()
        {
            Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.LeftEye);
            Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.RightEye);
            Vector3 centerEye = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye);
            Quaternion centerRotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.CenterEye);

            //convert local coords into center eye coordinates
            Vector3 globalCenterPos = Quaternion.Inverse(centerRotation) * centerEye;
            Vector3 globalLeftEyePos = Quaternion.Inverse(centerRotation) * leftEye;
            Vector3 globalRightEyePos = Quaternion.Inverse(centerRotation) * rightEye;
            
            //right
            var relativeRightEyePosition = globalRightEyePos - globalCenterPos;
            relativeRightEyePosition *= Helpers.PupilUnitScalingFactor;
            calibration.rightEyeTranslation = new float[] { relativeRightEyePosition.x, relativeRightEyePosition.y, relativeRightEyePosition.z };
            
            //left
            var relativeLeftEyePosition = globalLeftEyePos - globalCenterPos;
            relativeLeftEyePosition *= Helpers.PupilUnitScalingFactor;
            calibration.leftEyeTranslation = new float[] { relativeLeftEyePosition.x, relativeLeftEyePosition.y, relativeLeftEyePosition.z };
        }

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

			UpdateCalibrationPoint ();
            marker.localScale = Vector3.one * calibrationSettings.markerScale;

			//		yield return new WaitForSeconds (2f);

			Debug.Log ("Starting Calibration");
		}

        private void UpdateCalibrationPoint()
        {
            currentCalibrationPointPosition = new float[]{0};
            switch (calibrationSettings.mode)
            {
            case CalibrationSettings.Mode._3D:
                currentCalibrationPointPosition = new float[] {calibrationSettings.centerPoint.x,calibrationSettings.centerPoint.y,calibrationSettings.vectorDepthRadius [currentCalibrationDepth].x};
                offset = 0.25f * Math.PI;
                break;
            default:
                currentCalibrationPointPosition = new float[]{ calibrationSettings.centerPoint.x,calibrationSettings.centerPoint.y };
                offset = 0f;
                break;
            }
            radius = calibrationSettings.vectorDepthRadius[currentCalibrationDepth].y;
            if (currentCalibrationPoint > 0 && currentCalibrationPoint < calibrationSettings.points)
            {	
                currentCalibrationPointPosition [0] += radius * (float) Math.Cos (2f * Math.PI * (float)(currentCalibrationPoint - 1) / (calibrationSettings.points-1f) + offset);
                currentCalibrationPointPosition [1] += radius * (float) Math.Sin (2f * Math.PI * (float)(currentCalibrationPoint - 1) / (calibrationSettings.points-1f) + offset);
            }
            if (calibrationSettings.mode == CalibrationSettings.Mode._3D)
                currentCalibrationPointPosition [1] /= camera.aspect;
            
            UpdateMarkerPosition(calibrationSettings.mode, marker, currentCalibrationPointPosition);
        }

        private void UpdateMarkerPosition(CalibrationSettings.Mode mode, Transform marker, float[] newPosition)
        {
            Vector3 position;

            if (mode == CalibrationSettings.Mode._2D)
            {
                if (newPosition.Length == 2)
                {
                    position.x = newPosition[0];
                    position.y = newPosition[1];
                    position.z = calibrationSettings.vectorDepthRadius[0].x;
                    gameObject.transform.position = camera.ViewportToWorldPoint(position);
                } 
                else
                {
                    Debug.Log ("Length of new position array does not match 2D mode");
                }
            }
            else if (mode == CalibrationSettings.Mode._3D)
            {
                if (newPosition.Length == 3)
                {
                    position.x = newPosition[0];
                    position.y = newPosition[1];
                    position.z = newPosition[2];
                    gameObject.transform.localPosition = position;
                } 
                else
                {
                    Debug.Log ("Length of new position array does not match 3D mode");
                }
            }
        }

	
		static float lastTimeStamp = 0;
		static float timeBetweenCalibrationPoints = 0.02f; // was 0.1, 1000/60 ms wait in old version
		public void UpdateCalibration ()
		{
			float t = Time.time;

			if (t - lastTimeStamp > timeBetweenCalibrationPoints)
			{
				lastTimeStamp = t;

				UpdateCalibrationPoint ();// .currentCalibrationType.calibPoints [currentCalibrationPoint];
				//			print ("its okay to go on");

				//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
				if ( currentCalibrationSamples > calibrationSettings.samplesToIgnoreForEyeMovement )
					calibration.AddCalibrationPointReferencePosition (currentCalibrationPointPosition, t);
				
				// if (PupilSettings.Instance.debug.printSampling) //TODO logging wanted?
				// 	Debug.Log ("Point: " + currentCalibrationPoint + ", " + "Sampling at : " + currentCalibrationSamples + ". On the position : " + currentCalibrationPointPosition [0] + " | " + currentCalibrationPointPosition [1]);

				currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

				if (currentCalibrationSamples >= calibrationSettings.samplesPerDepth)
				{
					currentCalibrationSamples = 0;
					currentCalibrationDepth++;

					if (currentCalibrationDepth >= calibrationSettings.vectorDepthRadius.Length)
					{
						currentCalibrationDepth = 0;
						currentCalibrationPoint++;

						//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
						calibration.AddCalibrationReferenceData ();

						if (currentCalibrationPoint >= calibrationSettings.points)
						{
							calibration.StopCalibration ();
						}
					}

				}
			}
		}
    }
}
