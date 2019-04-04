using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{
    [RequireComponent(typeof(RecordingController))]
    public class DataRecordingDemo : MonoBehaviour
    {
        public Text text;

        RecordingController recording;

        void Awake()
        {
            recording = GetComponent<RecordingController>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (!recording.IsRecording)
                {
                    recording.StartRecording();
                }
                else
                {
                    recording.StopRecording();
                }
            }

            bool connected = recording.requestCtrl.IsConnected;
            text.text = connected ? "Connected" : "Not connected";

            if (connected)
            {
                text.text += "\n\nPress R to Start/Stop the recording.";

                var status = recording.IsRecording ? "recording" : "not recording";
                text.text += $"\n\nStatus: {status}";
            }
        }
    }
}
