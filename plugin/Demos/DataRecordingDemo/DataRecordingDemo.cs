using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{
    public class DataRecordingDemo : MonoBehaviour
    {
        public Text text;
        public RecordingController recorder;

        [Header("Annotations")]
        public Annotation annotation;
        public Transform head;
        public bool sendHeadAsAnnotation = false;

        void Update()
        {
            bool connected = recorder.requestCtrl.IsConnected;

            UpdateText(connected);

            if (connected && sendHeadAsAnnotation)
            {
                SendExampleAnnotations();
            }
        }

        void UpdateText(bool connected)
        {
            text.text = connected ? "Connected" : "Not connected";

            if (connected)
            {
                text.text += "\n\nPress R to Start/Stop the recording.";

                var status = recorder.IsRecording ? "recording" : "not recording";
                text.text += $"\n\nStatus: {status}";
            }
        }

        void SendExampleAnnotations()
        {
            Dictionary<string, object> headData = new Dictionary<string, object>();

            headData["head_world_x"] = head.position.x;
            headData["head_world_y"] = head.position.y;
            headData["head_world_z"] = head.position.z;

            annotation.SendAnnotation(label: "head pos", customData: headData);
        }
    }
}
