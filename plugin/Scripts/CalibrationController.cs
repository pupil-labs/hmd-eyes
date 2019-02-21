using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;

        new public Camera camera;
        public Transform marker;

        public CalibrationSettings calibrationSettings;

        //events
        public delegate void CalibrationEndedDel();
        public event CalibrationEndedDel OnCalibrationFailed;
        public event CalibrationEndedDel OnCalibrationSucceeded;

        //members
        Calibration calibration = new Calibration();
        
        float radius;
        double offset;

        int currentCalibrationPoint;
        int currentCalibrationSamples;
        int currentCalibrationDepth;
        float[] currentCalibrationPointPosition;

        float lastTimeStamp = 0;

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

            UpdateCalibrationPoint();
            marker.gameObject.SetActive(true);
            marker.localScale = Vector3.one * calibrationSettings.markerScale;

            calibration.StartCalibration(calibrationSettings, subsCtrl);
        }

        private void UpdateCalibration()
        {
            float t = Time.time;

            if (t - lastTimeStamp > calibrationSettings.timeBetweenCalibrationPoints)
            {
                lastTimeStamp = t;

                UpdateCalibrationPoint();

                //Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
                if (currentCalibrationSamples > calibrationSettings.samplesToIgnoreForEyeMovement)
                    calibration.AddCalibrationPointReferencePosition(currentCalibrationPointPosition, t);

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
                        calibration.SendCalibrationReferenceData();

                        if (currentCalibrationPoint >= calibrationSettings.points)
                        {
                            calibration.StopCalibration();
                        }
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

        private void UpdateCalibrationPoint()
        {
            currentCalibrationPointPosition = new float[] { 0 };
            switch (calibrationSettings.mode)
            {
                case CalibrationSettings.Mode._3D:
                    currentCalibrationPointPosition = new float[] { calibrationSettings.centerPoint.x, calibrationSettings.centerPoint.y, calibrationSettings.vectorDepthRadius[currentCalibrationDepth].x };
                    offset = 0.25f * Math.PI;
                    break;
                default:
                    currentCalibrationPointPosition = new float[] { calibrationSettings.centerPoint.x, calibrationSettings.centerPoint.y };
                    offset = 0f;
                    break;
            }
            radius = calibrationSettings.vectorDepthRadius[currentCalibrationDepth].y;
            if (currentCalibrationPoint > 0 && currentCalibrationPoint < calibrationSettings.points)
            {
                currentCalibrationPointPosition[0] += radius * (float)Math.Cos(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (calibrationSettings.points - 1f) + offset);
                currentCalibrationPointPosition[1] += radius * (float)Math.Sin(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (calibrationSettings.points - 1f) + offset);
            }
            if (calibrationSettings.mode == CalibrationSettings.Mode._3D)
                currentCalibrationPointPosition[1] /= camera.aspect;

            UpdateMarkerPosition(calibrationSettings.mode, marker, currentCalibrationPointPosition);
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
