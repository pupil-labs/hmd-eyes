using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeVisualizer : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;
        public Transform cameraTransform;

        [Header("Settings")]
        [Range(0f, 1f)]
        public float confidenceThreshold = 0.6f;

        [Header("Projected Visualization")]
        public Transform projectionMarker;
        [Range(0.01f, 0.1f)]
        public float sphereCastRadius = 0.05f;

        public GazeListener Listener { get; private set; } = null;
        
        Vector3 localGazeDirection;
        float gazeDistance;
        bool isGazing = false;

        bool errorAngleBasedMarkerRadius = true;
        float angleErrorEstimate = 2f;

        Vector3 origMarkerScale;

        void OnEnable() //automagic by conditional enable/disable component
        {
            StartVisualizing();
        }

        void OnDisable() //automagic by conditional enable/disable component
        {
            StopVisualizing();
        }

        void Update()
        {
            if (!isGazing)
            {
                return;
            }

            ShowProjected();
        }

        public void StartVisualizing()
        {
            Debug.Log("Start Visualizing Gaze");

            if (subscriptionsController == null)
            {
                Debug.LogError("SubscriptionController missing");
                return;
            }

            if (projectionMarker == null)
            {
                Debug.LogError("Marker reference missing");
                return;
            }

            origMarkerScale = projectionMarker.localScale;

            if (cameraTransform == null)
            {
                Debug.LogError("Camera reference missing");
                return;
            }

            if (Listener == null)
            {
                Listener = new GazeListener(subscriptionsController);
            }

            Listener.Enable();
            Listener.OnReceive3dGaze += ReceiveGaze;

            projectionMarker.gameObject.SetActive(true);
            isGazing = true;
        }

        public void StopVisualizing()
        {
            isGazing = false;

            if (Listener != null)
            {
                Listener.OnReceive3dGaze -= ReceiveGaze;
                Listener.Disable();
            }

            if (projectionMarker != null)
            {
                projectionMarker.gameObject.SetActive(false);
            }
        }

        void ReceiveGaze(GazeData gazeData)
        {
            if (gazeData.Confidence >= confidenceThreshold)
            {
                localGazeDirection = gazeData.GazeDirection;
                gazeDistance = gazeData.GazeDistance;
            }
        }

        void ShowProjected()
        {
            if (projectionMarker == null)
            {
                Debug.LogWarning("Marker missing");
                return;
            }

            projectionMarker.gameObject.SetActive(true);

            Vector3 origin = cameraTransform.position;

            Vector3 direction = cameraTransform.TransformDirection(localGazeDirection);

            projectionMarker.localScale = origMarkerScale;
            if (Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hit, Mathf.Infinity))
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.yellow);

                projectionMarker.position = hit.point;
                projectionMarker.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);

                if (errorAngleBasedMarkerRadius)
                {
                    projectionMarker.localScale = GetErrorAngleBasedScale(origMarkerScale, hit.distance, angleErrorEstimate);
                }
            }
            else
            {
                Debug.DrawRay(origin, direction * 10, Color.white);
            }
        }

        Vector3 GetErrorAngleBasedScale(Vector3 origScale, float distance, float errorAngle)
        {
            Vector3 scale = origScale;
            float scaleXY = distance * Mathf.Tan(Mathf.Deg2Rad * angleErrorEstimate) * 2;
            scale.x = scaleXY;
            scale.y = scaleXY;
            return scale;
        }
    }
}
