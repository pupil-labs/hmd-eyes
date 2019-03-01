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

        [Header("Settings")]
        public bool filterByConfidence = true;
        [Range(0f, 0.99f)]
        public float confidenceThreshold = 0.6f;

        [Header("2D only")]
        public Camera cam;
        [Range(0,10)]
        public float projectionDepth = 2f;

        GazeListener gazeListener = null;


        void OnEnable()
        {
            if (gazeListener == null)
            {
                gazeListener = new GazeListener(subscriptionsController);
            }

            if (calibrationController == null)
            {
                Debug.LogWarning("CalibrationController missing");
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
            gazeListener.OnReceive2dGazeTarget += Update2d;
            gazeListener.OnReceive3dGazeTarget += Update3d;

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

        void Update2d(string id, Vector3 pos, float confidence)
        {
            // Debug.Log($"GV::Update2d {id} {pos} {confidence}");

            if (cam == null)
            {
                Debug.LogWarning("Camera reference missing to project 2d gaze estimate");
                return;
            }

            if (filterByConfidence && confidence > confidenceThreshold)
            {
                pos.z = projectionDepth;
                gazeEstimateMarker.position = cam.ViewportToWorldPoint(pos);
            }
        }

        void Update3d(Vector3 pos, float confidence)
        {
            // Debug.Log($"GV::Update3d {pos} {confidence}");
            
            if (filterByConfidence && confidence > confidenceThreshold)
            {
                gazeEstimateMarker.localPosition = pos; //assuming marker is child of cam TODO better use (and show) world coord case 
            }
        }
    }
}
