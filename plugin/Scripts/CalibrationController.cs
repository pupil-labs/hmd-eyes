using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs
{
    public class CalibrationController : MonoBehaviour
    {
        [Header("Pupil Labs Connection")]
        public SubscriptionsController subsCtrl;
        public TimeSync timeSync;

        [Header("Scene References")]
        public new Camera camera;
        public Transform marker;

        [Header("Settings")]
        public CalibrationSettings settings;
        public CalibrationTargets targets;
        public bool showPreview;

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
        List<GameObject> previewMarkers = new List<GameObject>();

        bool previewMarkersActive = false;

        void OnEnable()
        {
            calibration.OnCalibrationSucceeded += CalibrationSucceeded;
            calibration.OnCalibrationFailed += CalibrationFailed;

            if (subsCtrl == null || timeSync == null || marker == null || camera == null || settings == null || targets == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }

            InitPreviewMarker();
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
            if (showPreview != previewMarkersActive)
            {
                SetPreviewMarkers(showPreview);
            }

            if (calibration.IsCalibrating)
            {
                UpdateCalibration();
            }

            if (Input.GetKeyUp(KeyCode.C))
            {
                ToggleCalibration();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                showPreview = !showPreview;
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

            showPreview = false;

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
                double sampleTimeStamp = timeSync.ConvertToPupilTime(Time.realtimeSinceStartup);
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

            refData = new float[] { currLocalTargetPos.x, currLocalTargetPos.y, currLocalTargetPos.z };
            refData[1] /= camera.aspect;

            for (int i = 0; i < refData.Length; i++)
            {
                refData[i] *= Helpers.PupilUnitScalingFactor;
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

        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                return;
            }
            Gizmos.matrix = camera.transform.localToWorldMatrix;
            for (int i = 0; i < targets.GetTargetCount(); ++i)
            {
                var target = targets.GetLocalTargetPosAt(i);
                Gizmos.DrawWireSphere(target, 0.035f);
            }
        }

        void InitPreviewMarker()
        {

            var previewMarkerParent = new GameObject("Calibration Targets Preview");
            previewMarkerParent.transform.SetParent(camera.transform);
            previewMarkerParent.transform.localPosition = Vector3.zero;
            previewMarkerParent.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < targets.GetTargetCount(); ++i)
            {
                var target = targets.GetLocalTargetPosAt(i);
                var previewMarker = Instantiate<GameObject>(marker.gameObject);
                previewMarker.transform.parent = previewMarkerParent.transform;
                previewMarker.transform.localPosition = target;
                previewMarker.transform.LookAt(camera.transform.position);
                previewMarker.SetActive(true);
                previewMarkers.Add(previewMarker);
            }

            previewMarkersActive = true;
        }

        void SetPreviewMarkers(bool value)
        {
            foreach (var marker in previewMarkers)
            {
                marker.SetActive(value);
            }

            previewMarkersActive = value;
        }


    }
}
