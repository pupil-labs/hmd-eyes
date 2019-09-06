using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class Calibration
    {
        //events
        public event Action OnCalibrationStarted;
        public event Action OnCalibrationSucceeded;
        public event Action OnCalibrationFailed;

        //members
        SubscriptionsController subsCtrl;
        RequestController requestCtrl;
        CalibrationSettings settings;

        List<Dictionary<string, object>> calibrationData = new List<Dictionary<string, object>>();
        float[] rightEyeTranslation;
        float[] leftEyeTranslation;

        public bool IsCalibrating { get; set; }

        public void StartCalibration(CalibrationSettings settings, SubscriptionsController subsCtrl)
        {
            this.settings = settings;
            this.subsCtrl = subsCtrl;
            this.requestCtrl = subsCtrl.requestCtrl;

            if (OnCalibrationStarted != null)
            {
                OnCalibrationStarted();
            }

            IsCalibrating = true;

            subsCtrl.SubscribeTo("notify.calibration.successful", ReceiveSuccess);
            subsCtrl.SubscribeTo("notify.calibration.failed", ReceiveFailure);

            requestCtrl.StartPlugin(settings.PluginName);

            UpdateEyesTranslation();

            requestCtrl.Send(new Dictionary<string, object> {
                { "subject","calibration.should_start" },
                {
                    "hmd_video_frame_size",
                    new float[] {
                        1000,
                        1000
                    }
                },
                {
                    "outlier_threshold",
                    35
                },
                {
                    "translation_eye0",
                    rightEyeTranslation
                },
                {
                    "translation_eye1",
                    leftEyeTranslation
                }
            });

            Debug.Log("Calibration Started");

            calibrationData.Clear();
        }

        public void AddCalibrationPointReferencePosition(float[] position, double timestamp)
        {
            calibrationData.Add(new Dictionary<string, object>() {
                { settings.PositionKey, position },
                { "timestamp", timestamp },
                { "id", int.Parse(Helpers.rightEyeID) }
            });
            calibrationData.Add(new Dictionary<string, object>() {
                { settings.PositionKey, position },
                { "timestamp", timestamp },
                { "id", int.Parse(Helpers.leftEyeID) }
            });
        }

        public void SendCalibrationReferenceData()
        {
            Debug.Log("Send CalibrationReferenceData");

            requestCtrl.Send(new Dictionary<string, object> {
                { "subject","calibration.add_ref_data" },
                {
                    "ref_data",
                    calibrationData.ToArray ()
                }
            });

            //Clear the current calibration data, so we can proceed to the next point if there is any.
            calibrationData.Clear();
        }

        public void StopCalibration()
        {
            Debug.Log("Calibration should stop");

            IsCalibrating = false;

            requestCtrl.Send(new Dictionary<string, object> { { "subject", "calibration.should_stop" } });
        }

        private void UpdateEyesTranslation()
        {
            Vector3 leftEye = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftEye);
            Vector3 rightEye = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightEye);
            Vector3 centerEye = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.CenterEye);
            Quaternion centerRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.CenterEye);

            //convert local coords into center eye coordinates
            Vector3 globalCenterPos = Quaternion.Inverse(centerRotation) * centerEye;
            Vector3 globalLeftEyePos = Quaternion.Inverse(centerRotation) * leftEye;
            Vector3 globalRightEyePos = Quaternion.Inverse(centerRotation) * rightEye;

            //right
            var relativeRightEyePosition = globalRightEyePos - globalCenterPos;
            relativeRightEyePosition *= Helpers.PupilUnitScalingFactor;
            rightEyeTranslation = new float[] { relativeRightEyePosition.x, relativeRightEyePosition.y, relativeRightEyePosition.z };

            //left
            var relativeLeftEyePosition = globalLeftEyePos - globalCenterPos;
            relativeLeftEyePosition *= Helpers.PupilUnitScalingFactor;
            leftEyeTranslation = new float[] { relativeLeftEyePosition.x, relativeLeftEyePosition.y, relativeLeftEyePosition.z };
        }

        private void ReceiveSuccess(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame)
        {
            if (OnCalibrationSucceeded != null)
            {
                OnCalibrationSucceeded();
            }

            CalibrationEnded(topic);
        }

        private void ReceiveFailure(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame)
        {
            if (OnCalibrationFailed != null)
            {
                OnCalibrationFailed();
            }

            CalibrationEnded(topic);
        }

        private void CalibrationEnded(string topic)
        {
            Debug.Log($"Calibration response: {topic}");
            subsCtrl.UnsubscribeFrom("notify.calibration.successful", ReceiveSuccess);
            subsCtrl.UnsubscribeFrom("notify.calibration.failed", ReceiveFailure);
        }
    }
}