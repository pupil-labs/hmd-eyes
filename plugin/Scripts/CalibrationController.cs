using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public new Camera camera;
        public Transform marker;

        public CalibrationSettings settings;
        public CalibrationTargets targets;

        //events
        public delegate void CalibrationEndedDel();
        public event CalibrationEndedDel OnCalibrationFailed;
        public event CalibrationEndedDel OnCalibrationSucceeded;

        //members
        Calibration calibration = new Calibration();

        int currentCalibrationPoint;
        int currentCalibrationSamples;
        float[] currentCalibrationPointPosition;

        float tLastSample = 0;
        float tLastTarget = 0;

        void OnEnable()
        {
            calibration.OnCalibrationSucceeded += CalibrationSucceeded;
            calibration.OnCalibrationFailed += CalibrationFailed;
        }

        void OnDisable()
        {
            calibration.OnCalibrationSucceeded -= CalibrationSucceeded;
            calibration.OnCalibrationFailed -= CalibrationFailed;
        }

        void Update()
        {
            if (calibration.IsCalibrating)
            {
                UpdateCalibration();
            }

            if (subsCtrl.IsConnected && Input.GetKeyUp(KeyCode.C)) //TODO needs some public API instead of keypress only
            {
                if (calibration.IsCalibrating)
                {
                    calibration.StopCalibration();
                }
                else
                {
                    InitializeCalibration();
                }
            }
        }

        private void InitializeCalibration()
        {
            Debug.Log("Starting Calibration");

            currentCalibrationPoint = 0;
            currentCalibrationSamples = 0;

            UpdatePosition();

            marker.gameObject.SetActive(true);

            calibration.StartCalibration(settings, subsCtrl);
            Debug.Log($"Sample Rate: {settings.SampleRate}");
        }

        private void UpdateCalibration()
        {
            float tNow = Time.time;

            if (tNow - tLastSample >= 1f / settings.SampleRate - Time.deltaTime / 2f)
            {

                if (tNow - tLastTarget < settings.ignoreInitialSeconds - Time.deltaTime / 2f)
                {
                    return;
                }

                tLastSample = tNow;

                //Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
                calibration.AddCalibrationPointReferencePosition(currentCalibrationPointPosition, tNow);

                currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

                if (currentCalibrationSamples >= settings.samplesPerTarget || tNow - tLastTarget >= settings.secondsPerTarget)
                {
                    // Debug.Log($"update target. last duration = {tNow - tLastTarget} samples = {currentCalibrationSamples}");

                    calibration.SendCalibrationReferenceData(); //including clear!
                    
                    //NEXT TARGET
                    if (currentCalibrationPoint < targets.GetTargetCount())
                    {
                        
                        currentCalibrationSamples = 0;

                        UpdatePosition();

                    }
                    else
                    {
                        calibration.StopCalibration();
                    }
                }
            }
        }

        private void CalibrationSucceeded()
        {
            CalibrationEnded();

            if (OnCalibrationSucceeded != null)
            {
                OnCalibrationSucceeded();
            }
        }

        private void CalibrationFailed()
        {
            CalibrationEnded();

            if (OnCalibrationFailed != null)
            {
                OnCalibrationFailed();
            }
        }

        private void CalibrationEnded()
        {
            marker.gameObject.SetActive(false);
        }

        private void UpdatePosition()
        {
            Vector3 currPos = targets.GetTargetAt(currentCalibrationPoint);
            marker.localPosition = currPos;

            //TODO TBD move logic to Calibration?
            if (settings.mode == CalibrationSettings.Mode._3D)
            {
                
                currentCalibrationPointPosition = new float[]{currPos[0],currPos[1],currPos[2]};
                currentCalibrationPointPosition[1] /= camera.aspect;
                
                for (int i = 0; i < currentCalibrationPointPosition.Length; i++)
                {
                    currentCalibrationPointPosition[i] *= Helpers.PupilUnitScalingFactor;
                }
            
            }
            else
            {
                currPos = camera.WorldToViewportPoint(currPos);
                currentCalibrationPointPosition = new float[]{currPos[0],currPos[1]};
            }
            
            currentCalibrationPoint++;
            tLastTarget = Time.time;
        }
    }
}
