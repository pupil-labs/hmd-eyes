using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class ValidationController : MonoBehaviour
    {
        public CalibrationController calibrationCtrl;
        public SubscriptionsController subscriptionsCtrl;
        public Transform cameraTransform;
        public Transform marker;
        public GazeVisualizer gazeVis;
        public Transform hideDuringValidation;
        [Header("Settings")]
        public CalibrationTargets targets;
        public float durationPerTarget = 1f;
        public float switchTargetDelay = 0.2f;
        public float minimalConfidence = 0.6f;
        public float validationTestDelay = 5f;
        [Header("Simple Results")]
        public float avgError;

        [HideInInspector] 
        public List<Sample> samples = new List<Sample>();
        
        //members
        bool validationRunning = false;
        int targetIdx = 0;
        float tTargetStart;
        Vector3 currLocalTarget;

        GazeListener gazeListener;

        bool gazeVisWasEnable = false;

        public class Sample
        {
            public int targetIndex;
            public float angularError;
            public float confidence;
            public GazeData.GazeMappingContext mappingContext;
            public Vector3 target;
            public Vector3 gazeDir;
        }

        void OnEnable()
        {
            calibrationCtrl.OnCalibrationSucceeded += OnCalibrationSucceeded;
            marker.parent = cameraTransform;
            marker.gameObject.SetActive(false);

            gazeListener = new GazeListener(subscriptionsCtrl);
            gazeListener.Disable();

            gazeListener.OnReceive3dGaze += HandleGaze;
        }

        void Update()
        {
            if (validationRunning)
            {
                float tNow = Time.time;

                if (tNow - tTargetStart > durationPerTarget)
                {
                    if (targetIdx < targets.GetTargetCount())
                    {
                        UpdateTarget();
                    }
                    else
                    {
                        StopValidation();
                    }
                }
            }
        }

        void OnCalibrationSucceeded()
        {
            StartCoroutine(DelayedValidationStart());
        }

        IEnumerator DelayedValidationStart()
        {
            yield return new WaitForSeconds(validationTestDelay);

            StartValidation();
        }

        public void StartValidation()
        {
            samples.Clear();

            if (gazeVis != null)
            {
                gazeVisWasEnable = gazeVis.enabled;
                gazeVis.enabled = false;
            }
            if (hideDuringValidation != null)
            {
                hideDuringValidation.gameObject.SetActive(false);
            }

            Debug.Log("Start Validation");
            gazeListener.Enable();

            targetIdx = 0;
            validationRunning = true;
            marker.gameObject.SetActive(true);

            UpdateTarget();
        }

        public void StopValidation()
        {
            Debug.Log("Stop Validation");
            marker.gameObject.SetActive(false);
            validationRunning = false;

            gazeListener.Disable();

            avgError = CalcAvgError();
            Debug.Log($"AVG Angular Error {avgError}");

            Debug.Log($"AVG Angular Error 0.6-0.8 {CalcAvgError(0.6f, 0.8f)}");
            Debug.Log($"AVG Angular Error 0.8-1 {CalcAvgError(0.8f, 1f)}");

            if (gazeVisWasEnable)
            {
                gazeVis.enabled = true;
            }
            if (hideDuringValidation != null)
            {
                hideDuringValidation.gameObject.SetActive(true);
            }

#if UNITY_EDITOR  
            Export();
#endif
        }

        void UpdateTarget()
        {
            currLocalTarget = targets.GetLocalTargetPosAt(targetIdx);
            targetIdx++;
            tTargetStart = Time.time;

            UpdateMarker();
        }

        void UpdateMarker()
        {
            marker.localPosition = currLocalTarget; //as marker.parent = gaze origin
            marker.LookAt(cameraTransform.position);
        }

        void HandleGaze(GazeData gaze)
        {
            if (!validationRunning)
            {
                return;
            }

            if (gaze.UnityTimestamp - tTargetStart < switchTargetDelay)
            {
                return;
            }

            if (gaze.Confidence < minimalConfidence)
            {
                return;
            }

            Vector3 targetDir = currLocalTarget.normalized;
            float angle = Vector3.Angle(targetDir, gaze.GazeDirection);

            Sample sample = new Sample();
            sample.targetIndex = targetIdx;
            sample.angularError = angle;
            sample.confidence = gaze.Confidence;
            sample.mappingContext = gaze.MappingContext;
            sample.target = currLocalTarget;
            sample.gazeDir = gaze.GazeDirection;

            samples.Add(sample);
        }

        float CalcAvgError(float minConfidence = 0.6f, float maxConfidence = 1f)
        {
            float error = 0;
            int count = 0;
            foreach (var sample in samples)
            {
                if (sample.confidence > maxConfidence || sample.confidence < minConfidence)
                {
                    continue;
                }
                error += sample.angularError;
                count++;
            }
            return error / (float)count;
        }

#if UNITY_EDITOR  
        void Export()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("index;angularError;confidence;mode;target.x;target.y;target.z;gazeDir.x;gazeDir.y;gazeDir.z");

            foreach (var sample in samples)
            {
                string sampleString = $"{sample.targetIndex};{sample.angularError};{sample.confidence};{sample.mappingContext};{sample.target.x};{sample.target.y};{sample.target.z};{sample.gazeDir.x};{sample.gazeDir.y};{sample.gazeDir.z}";
                sb.AppendLine(sampleString);
                //TODO vec3 to csv extension method
            }

            string path = UnityEditor.EditorUtility.SaveFilePanel("Save CSV", Application.dataPath, "validation", "csv");

            System.IO.StreamWriter outStream = System.IO.File.CreateText(path);
            outStream.WriteLine(sb);
            outStream.Close();
        }
#endif
    }
}
