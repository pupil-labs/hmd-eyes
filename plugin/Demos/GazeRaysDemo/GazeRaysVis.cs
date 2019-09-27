using UnityEngine;

namespace PupilLabs.Demos
{
    [System.Serializable]
    public class GazeRay
    {
        public LineRenderer gazeLine;
        public Transform fixedEyePos;

        public void Update(Transform origin, Vector3 eyePos, Vector3 dir, float rayLength, bool useFixedEyePos)
        {
            dir.Normalize();
            dir.x *= -1f;
            dir *= rayLength;
            dir = origin.TransformDirection(dir);
            
            if (fixedEyePos != null && useFixedEyePos)
            {
                eyePos = fixedEyePos.position;
            }
            else
            {
                eyePos = origin.TransformPoint(eyePos);
            }
            

            if (gazeLine != null)
            {
                gazeLine.SetPosition(0, eyePos);
                gazeLine.SetPosition(1, eyePos + dir);
            }
        }

        public void VisualizeConfidence(float confidence)
        {
            
            Color c = gazeLine.startColor;
            c.a = confidence;
            gazeLine.startColor = gazeLine.endColor = c;
        }
    }

    public class GazeRaysVis : MonoBehaviour
    {
        [Header("Pupil Communication")]
        public GazeController gazeController;
        public CalibrationController calibrationController;
        public float confidenceThreshold = 0.6f;
        [Header("Origin")]
        public Transform origin;
        public bool useFixedEyePos = true;
        [Header("Gaze Rays")]
        public GazeRay leftGaze;
        public GazeRay rightGaze;
        [Range(0.1f, 20)]
        public float rayLength = 5;

        bool isGazing = false;
        Vector3 gazeNormalLeft, gazeNormalRight;
        Vector3 eyeCenterLeft, eyeCenterRight;
        float confidenceLeft, confidenceRight;

        void OnEnable()
        {
            calibrationController.OnCalibrationSucceeded += StartVis;
        }

        public void StartVis()
        {
            isGazing = true;
            gazeController.OnReceive3dGaze += ReceiveEyeData;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                useFixedEyePos = !useFixedEyePos;
            }

            if (!isGazing)
            {
                return;
            }

            //update rays
            if (leftGaze != null)
            {   
                leftGaze.VisualizeConfidence(confidenceLeft);
                leftGaze.Update(origin, eyeCenterLeft, gazeNormalLeft, rayLength, useFixedEyePos);
            }

            if (rightGaze != null)
            {
                rightGaze.VisualizeConfidence(confidenceRight);
                rightGaze.Update(origin, eyeCenterRight, gazeNormalRight, rayLength, useFixedEyePos);
            }
        }

        void ReceiveEyeData(GazeData data)
        {

            bool valid = data.Confidence > confidenceThreshold;

            if (data.IsEyeDataAvailable(1))
            {
                if (valid)
                {
                    gazeNormalLeft = data.GetGazeNormal(1);
                    eyeCenterLeft = data.GetEyeCenter(1);
                }
                confidenceLeft = data.Confidence;
            }

            if (data.IsEyeDataAvailable(0))
            {
                if (valid)
                {
                    gazeNormalRight = data.GetGazeNormal(0);
                    eyeCenterRight = data.GetEyeCenter(0);
                }
                confidenceRight = data.Confidence;
            }
        }
    }
}