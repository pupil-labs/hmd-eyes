using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeVisualizer : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;
        public CalibrationController calibrationController;
        public Transform gazeEstimateMarker;
        public Camera cam;

        [Header("Settings")]
        public bool filterByConfidence = true;
        [Range(0f, 1f)]
        public float confidenceThreshold = 0.6f;

        Gaze3dListener gazeListener = null;

        void OnEnable()
        {
            if (gazeListener == null)
            {
                gazeListener = new Gaze3dListener(subscriptionsController);
            }

            if (calibrationController == null)
            {
                Debug.LogWarning("CalibrationController missing");
                return;
            }

            if (cam == null)
            {
                Debug.LogWarning("Camera reference missing");
                return;
            }

            calibrationController.OnCalibrationSucceeded += StartVisualizing;
        }

        void OnDisable()
        {
            StopVisualizing();
        }

        void StartVisualizing()
        {
            Debug.Log("Start Visualizing Gaze");
            gazeListener.OnReceive3dGaze += Update3d;

            if (gazeEstimateMarker == null)
            {
                Debug.LogWarning("Gaze Marker missing");
                return;
            }

            gazeEstimateMarker.gameObject.SetActive(true);
        }

        void StopVisualizing()
        {

            if (gazeEstimateMarker != null)
            {
                gazeEstimateMarker.gameObject.SetActive(false);
            }
        }

        void Update3d(GazeData gazeData)
        {
            // Debug.Log($"GV::Update3d {pos} {confidence}");
            
            if (filterByConfidence && gazeData.confidence >= confidenceThreshold)
            {
                gazeEstimateMarker.position = cam.transform.localToWorldMatrix.MultiplyPoint(gazeData.gazePoint3d);
                //TODO visualize mono vs bino 
            }
        }
    }
}
