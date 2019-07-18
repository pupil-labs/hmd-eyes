using UnityEngine;

namespace PupilLabs.Demos
{
    [System.Serializable]
    public class GazeRay
    {
        public LineRenderer gazeLine;
        public Transform eye;

        public void Update(Transform origin, Vector3 eyePos, Vector3 dir, float rayLength, bool flipX)
        {
            dir.Normalize();
            if (flipX)
            {
                dir.x *= -1f;
            }
            dir *= rayLength;
            dir = origin.TransformDirection(dir);
            eyePos = origin.TransformPoint(eyePos);

            if (gazeLine != null && eye != null)
            {
                eye.position = eyePos;

                gazeLine.SetPosition(0, eye.position);
                gazeLine.SetPosition(1, eye.position + dir);

                eye.LookAt(eye.position + dir);
            }
        }
    }

    public class GazeRaysVis : MonoBehaviour
    {
        [Header("Pupil Communication")]
        public GazeController gazeController;
        public CalibrationController calibrationController;
        public float confidenceThreshold = 0.6f;
        [Header("Origin")]
        public Transform firstPersonOrigin;
        public Transform thirdPersonOrigin;
        public bool useFirstPersonOrigin = true;
        [Header("Gaze Rays")]
        public GazeRay leftGaze;
        public GazeRay rightGaze;
        [Range(0.1f, 20)]
        public float rayLength = 5; //TODO ray!

        bool isGazing = false;
        Vector3 gazeNormalLeft, gazeNormalRight;
        Vector3 eyeCenterLeft, eyeCenterRight;
        Transform origin;

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
            if (!isGazing)
            {
                return;
            }

            //switch origin
            if (Input.GetKeyDown(KeyCode.T))
            {
                useFirstPersonOrigin = !useFirstPersonOrigin;
            }

            if (useFirstPersonOrigin && origin != firstPersonOrigin)
            {
                origin = firstPersonOrigin;
            }
            else if (!useFirstPersonOrigin && origin != thirdPersonOrigin)
            {
                origin = thirdPersonOrigin;
            }

            //update rays
            if (leftGaze != null)
            {   
                leftGaze.Update(origin, eyeCenterLeft, gazeNormalLeft, rayLength, false);
            }

            if (rightGaze != null)
            {
                rightGaze.Update(origin, eyeCenterRight, gazeNormalRight, rayLength, false);
            }
        }

        void ReceiveEyeData(GazeData data)
        {
            if (data.Confidence < confidenceThreshold)
            {
                return;
            }

            if (data.IsEyeDataAvailable(0))
            {
                gazeNormalLeft = data.GazeNormal0;
                eyeCenterLeft = data.EyeCenter0;
            }

            if (data.IsEyeDataAvailable(1))
            {
                gazeNormalRight = data.GazeNormal1;
                eyeCenterRight = data.EyeCenter1;
            }
        }
    }
}