using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class RecordingController : MonoBehaviour
    {
        public RequestController requestCtrl;

        [SerializeField] private bool startRecordingButton;
        [SerializeField] private bool stopRecordingButton;

        public bool IsRecording { get; private set; }
        // ***************************************************************************
        public bool IsAwatingAnnotations { get; private set; }
        // ---------------------------------------------------------------------------
        void Update()
        {
            if (startRecordingButton)
            {
                startRecordingButton = false;
                StartRecording();
            }

            if (stopRecordingButton)
            {
                stopRecordingButton = false;
                StopRecording();
            }
        }

        public void StartRecording()
        {
            if (requestCtrl == null)
            {
                Debug.LogWarning("Request Controller missing");
                return;
            }

            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            if (IsRecording)
            {
                Debug.Log("Recording is already running.");
                return;
            }

            // ***************************************************************************
            StartAnnotationPlugin();
            IsAwatingAnnotations = true;
            // ---------------------------------------------------------------------------


            var path = GetRecordingPath().Substring(2);
            Debug.Log($"Recording path: {path}");

            requestCtrl.Send(new Dictionary<string, object>
            {
                { "subject","recording.should_start" }
                , { "session_name", path }
                , { "record_eye",true}
            });
            IsRecording = true;
        }

        public void StopRecording()
        {
            if (requestCtrl == null)
            {
                Debug.LogWarning("Request Controller missing");
                return;
            }

            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            if (!IsRecording)
            {
                Debug.Log("Recording is not running, nothing to stop.");
                return;
            }

            requestCtrl.Send(new Dictionary<string, object> 
            { 
                { "subject", "recording.should_stop" } 
            });

            IsRecording = false;
        }

        private string GetRecordingPath()
        {
            string date = System.DateTime.Now.ToString ("yyyy_MM_dd");
            string path = Application.dataPath + "/" + date;

            path = path.Replace ("Assets/", ""); //go one folder up

            if (!System.IO.Directory.Exists (path))
                System.IO.Directory.CreateDirectory (path);

            return path;
        }

        // ***************************************************************************

        public void StartAnnotationPlugin()
        {
            if (requestCtrl == null)
            {
                Debug.LogWarning("Request Controller missing");
                return;
            }

            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            requestCtrl.StartPlugin("Annotation_Capture");
            Debug.Log("Starting annotation plugin.");
        }

        public void StopAnnotationPlugin()
        {
            if (requestCtrl == null)
            {
                Debug.LogWarning("Request Controller missing");
                return;
            }

            if (!requestCtrl.IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            requestCtrl.StopPlugin("Annotation_Capture");
            Debug.Log("Stopping annotation plugin.");
        }


        public void SendSimpleTrigger(string label, float duration)
        {
            if (!IsAwatingAnnotations)
            {
                StartAnnotationPlugin();
                IsAwatingAnnotations = true;
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["topic"] = "annotation";
            data["label"] = label;
            data["timestamp"] = Time.realtimeSinceStartup;
            data["duration"] = duration;

            requestCtrl.SendTrigger(data);
        }
        // ---------------------------------------------------------------------------
    }
}
