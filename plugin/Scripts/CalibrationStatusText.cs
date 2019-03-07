using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs
{
    [RequireComponent(typeof(CalibrationController))]
    public class CalibrationStatusText : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        
        public Text statusText;

        private CalibrationController calibrationController;

        void Awake()
        {
            SetStatusText("Not connected");
            calibrationController = GetComponent<CalibrationController>();
        }

        void OnEnable()
        {
            subsCtrl.requestCtrl.OnConnected += OnConnected;
            calibrationController.OnCalibrationStarted += OnCalibrationStarted;
            calibrationController.OnCalibrationSucceeded += CalibrationSucceeded;
            calibrationController.OnCalibrationFailed += CalibrationFailed;
        }

        void OnDisable()
        {
            subsCtrl.requestCtrl.OnConnected -= OnConnected;
            calibrationController.OnCalibrationStarted -= OnCalibrationStarted;
            calibrationController.OnCalibrationSucceeded -= CalibrationSucceeded;
            calibrationController.OnCalibrationFailed -= CalibrationFailed;
        }

        void Update()
        {
            if (statusText != null)
            {
                statusText.enabled = !calibrationController.IsCalibrating;
            }
        }

        
        private void OnConnected()
        {
            string text = "Connected";
            text += "\n\nPlease warm up your eyes and press 'C' to start the calibration.";
            SetStatusText(text);
        }

        private void OnCalibrationStarted()
        {
            statusText.enabled = false;
            SetStatusText("Waiting for calibration to finish.");
        }

        private void CalibrationSucceeded()
        {
            SetStatusText("Calibration succeeded.");
        }

        private void CalibrationFailed()
        {
            SetStatusText("Calibration failed.");
        }

        private void SetStatusText(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
        }
    }
}
