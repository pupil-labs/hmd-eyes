using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeVisualizer : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;
        public Transform gazeOrigin;

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
            if (projectionMarker == null)
            {
                Debug.LogWarning("Marker reference missing.");
                enabled = false;
                return;
            }
            origMarkerScale = projectionMarker.localScale;

            if (subscriptionsController == null || gazeOrigin == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }

            StartVisualizing();
        }

        void OnDisable() //automagic by conditional enable/disable component
        {
            if (projectionMarker != null)
            {
                projectionMarker.localScale = origMarkerScale;
            }

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
            if (!enabled)
            {
                Debug.LogWarning("Component not enabled.");
                return;
            }

            if (isGazing)
            {
                Debug.Log("Already gazing!");
                return;
            }

            Debug.Log("Start Visualizing Gaze");

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
            if (!isGazing || !enabled)
            {
                Debug.Log("Nothing to stop.");
                return;
            }

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

            Vector3 origin = gazeOrigin.position;

            Vector3 direction = gazeOrigin.TransformDirection(localGazeDirection);

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
