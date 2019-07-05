﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public new Camera camera;
        public Transform marker;

        public CalibrationSettings settings;
        public CalibrationTargets targets;

        public bool IsCalibrating { get { return calibration.IsCalibrating; } }

        //events
        public event Action OnCalibrationStarted;
        public event Action OnCalibrationRoutineDone;
        public event Action OnCalibrationFailed;
        public event Action OnCalibrationSucceeded;

        //members
        Calibration calibration = new Calibration();

        int targetIdx;
        int targetSampleCount;
        Vector3 currLocalTargetPos;

        float tLastSample = 0;
        float tLastTarget = 0;

        void OnEnable()
        {
            calibration.OnCalibrationSucceeded += CalibrationSucceeded;
            calibration.OnCalibrationFailed += CalibrationFailed;

            if (subsCtrl == null || marker == null || camera == null || settings == null || targets == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }
        }

        void OnDisable()
        {
            calibration.OnCalibrationSucceeded -= CalibrationSucceeded;
            calibration.OnCalibrationFailed -= CalibrationFailed;

            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
        }

        void Update()
        {
            if (calibration.IsCalibrating)
            {
                UpdateCalibration();
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
                ToggleCalibration();
            }
        }

        public void ToggleCalibration()
        {
            if (calibration.IsCalibrating)
            {
                StopCalibration();
            }
            else
            {
                StartCalibration();
            }
        }

        public void StartCalibration()
        {
            if (!enabled)
            {
                Debug.LogWarning("Component not enabled!");
                return;
            }

            if (!subsCtrl.IsConnected)
            {
                Debug.LogWarning("Calibration not possible: not connected!");
                return;
            }

            Debug.Log("Starting Calibration");

            targetIdx = 0;
            targetSampleCount = 0;

            UpdatePosition();

            marker.gameObject.SetActive(true);

            calibration.StartCalibration(settings, subsCtrl);
            Debug.Log($"Sample Rate: {settings.SampleRate}");

            if (OnCalibrationStarted != null)
            {
                OnCalibrationStarted();
            }

            //abort process on disconnecting
            subsCtrl.requestCtrl.OnDisconnecting += StopCalibration;
        }

        public void StopCalibration()
        {
            if (!calibration.IsCalibrating)
            {
                Debug.Log("Nothing to stop.");
                return;
            }

            calibration.StopCalibration();

            marker.gameObject.SetActive(false);

            if (OnCalibrationRoutineDone != null)
            {
                OnCalibrationRoutineDone();
            }

            subsCtrl.requestCtrl.OnDisconnecting -= StopCalibration;
        }

        private void UpdateCalibration()
        {
            UpdateMarker();

            float tNow = Time.time;
            if (tNow - tLastSample >= 1f / settings.SampleRate - Time.deltaTime / 2f)
            {

                if (tNow - tLastTarget < settings.ignoreInitialSeconds - Time.deltaTime / 2f)
                {
                    return;
                }

                tLastSample = tNow;

                //Adding the calibration reference data to the list that will be passed on, once the required sample amount is met.
                double sampleTimeStamp = subsCtrl.requestCtrl.ConvertToPupilTime(Time.realtimeSinceStartup);
                AddSample(sampleTimeStamp);

                targetSampleCount++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

                if (targetSampleCount >= settings.samplesPerTarget || tNow - tLastTarget >= settings.secondsPerTarget)
                {
                    calibration.SendCalibrationReferenceData();

                    if (targetIdx < targets.GetTargetCount())
                    {
                        targetSampleCount = 0;

                        UpdatePosition();
                    }
                    else
                    {
                        StopCalibration();
                    }
                }
            }
        }

        private void CalibrationSucceeded()
        {
            if (OnCalibrationSucceeded != null)
            {
                OnCalibrationSucceeded();
            }
        }

        private void CalibrationFailed()
        {
            if (OnCalibrationFailed != null)
            {
                OnCalibrationFailed();
            }
        }

        private void AddSample(double time)
        {
            float[] refData;

            if (settings.mode == CalibrationSettings.Mode._3D)
            {
                refData = new float[] { currLocalTargetPos.x, currLocalTargetPos.y, currLocalTargetPos.z };
                refData[1] /= camera.aspect;

                for (int i = 0; i < refData.Length; i++)
                {
                    refData[i] *= Helpers.PupilUnitScalingFactor;
                }
            }
            else
            {
                Vector3 worldPos = camera.transform.localToWorldMatrix.MultiplyPoint(currLocalTargetPos);
                Vector3 viewportPos = camera.WorldToViewportPoint(worldPos);
                refData = new float[] { viewportPos.x, viewportPos.y };
            }

            calibration.AddCalibrationPointReferencePosition(refData, time);
        }

        private void UpdatePosition()
        {
            currLocalTargetPos = targets.GetLocalTargetPosAt(targetIdx);

            targetIdx++;
            tLastTarget = Time.time;
        }

        private void UpdateMarker()
        {
            marker.position = camera.transform.localToWorldMatrix.MultiplyPoint(currLocalTargetPos);
            marker.LookAt(camera.transform.position);
        }
    }
}
