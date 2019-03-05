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

        [Header("Confidence")]
        public bool filterByConfidence = true;
        [Range(0f, 1f)]
        public float confidenceThreshold = 0.6f;

        [Header("Fixed Depth")]
        public bool applyFixedDepth = false;
        [Range(0f,10f)]
        public float fixedDepth = 2f;

        [Header("Projected Visualization")]
        public bool showProjectedVis = true;
        public Transform projectionMarker;
        [Range(0.01f,0.1f)]
        public float sphereCastRadius = 0.05f;

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

            if (filterByConfidence && gazeData.Confidence >= confidenceThreshold)
            {
                gazeEstimateMarker.position = cam.transform.localToWorldMatrix.MultiplyPoint(gazeData.GazePoint3d);

                if (applyFixedDepth || showProjectedVis)
                {
                    gazeEstimateMarker.position = ApplyFixedDepth();
                }

                if (showProjectedVis)
                {
                    gazeEstimateMarker.gameObject.SetActive(false);
                    ShowProjectedVis();
                }
                else
                {
                    gazeEstimateMarker.gameObject.SetActive(true);
                    if (projectionMarker != null)
                    {
                        projectionMarker.gameObject.SetActive(false);
                    }
                }
            }
        }

        private Vector3 ApplyFixedDepth()
        {
            Vector3 localPosition = cam.transform.worldToLocalMatrix.MultiplyPoint(gazeEstimateMarker.position);
            localPosition.Normalize();
            
            //in case depth is < 0
            float angle = Vector3.Angle(Vector3.forward,localPosition);
            float direction = 1f;
            if(angle >= 90f){
                direction = -1f;
            }
            
            localPosition *= direction * fixedDepth;
            return cam.transform.localToWorldMatrix.MultiplyPoint(localPosition);
        }

        private void ShowProjectedVis()
        {
            if (projectionMarker == null)
            {
                Debug.LogWarning("Marker missing");
                return;
            }

            projectionMarker.gameObject.SetActive(true);

            Vector3 origin = cam.transform.position;

            Vector3 direction = gazeEstimateMarker.position - origin;
            direction.Normalize();
            if (Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hit, Mathf.Infinity))
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.yellow);

                projectionMarker.position = hit.point;
                projectionMarker.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            }
            else
            {
                Debug.DrawRay(origin, direction * 10, Color.white);
            }
        }
    }
}
