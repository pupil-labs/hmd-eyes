using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;

        public Transform marker;

        public CalibrationSettings calibrationSettings;
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
            currentCalibrationDepth = 0;

            currentCalibrationPointPosition = targets.GetNextTarget(currentCalibrationPoint);
            UpdateMarkerPosition(calibrationSettings.mode, marker, currentCalibrationPointPosition);

            tLastTarget = Time.time;
            marker.gameObject.SetActive(true);

            calibration.StartCalibration(calibrationSettings, subsCtrl);
            Debug.Log($"Sample Rate: {calibrationSettings.SampleRate}");
        }

        private void UpdateCalibration()
        {
            float tNow = Time.time;

            if (tNow - tLastSample >= 1f / calibrationSettings.SampleRate - Time.deltaTime / 2f)
            {

                if (tNow - tLastTarget < calibrationSettings.ignoreInitialSeconds - Time.deltaTime / 2f)
                {
                    return;
                }

                tLastSample = tNow;

                // currentCalibrationPointPosition = targets.UpdateCalibrationPoint();
                // UpdateMarkerPosition(calibrationSettings.mode, marker, currentCalibrationPointPosition);
                // //Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
                calibration.AddCalibrationPointReferencePosition(currentCalibrationPointPosition, tNow);

                currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

                if (currentCalibrationSamples >= calibrationSettings.samplesPerTarget || tNow - tLastTarget >= calibrationSettings.secondsPerTarget)
                {
                    // Debug.Log($"update target. last duration = {tNow - tLastTarget} samples = {currentCalibrationSamples}");

                    //NEXT TARGET
                    if (currentCalibrationPoint < targets.GetTargetCount())
                    {
                        currentCalibrationPointPosition = targets.GetNextTarget(currentCalibrationPoint);
                        UpdateMarkerPosition(calibrationSettings.mode, marker, currentCalibrationPointPosition);
                        
                        calibration.SendCalibrationReferenceData(); //including clear!
                        
                        currentCalibrationSamples = 0;
                        currentCalibrationPoint++;

                        tLastTarget = tNow;
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



        //TODO TBD part of calibration target something?
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
                    Debug.Log("Length of new position array does not match 2D mode");
                }
            }
            else if (mode == CalibrationSettings.Mode._3D)
            {
                if (newPosition.Length == 3)
                {
                    position.x = newPosition[0];
                    position.y = newPosition[1];
                    position.z = newPosition[2];
                    gameObject.transform.localPosition = position; //TODO which parent
                }
                else
                {
                    Debug.Log("Length of new position array does not match 3D mode");
                }
            }
        }
    }
}
