using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{

    public partial class RequestController : MonoBehaviour
    {

        [SerializeField]
        private Request request = new Request();


        public delegate void ConnectionDelegate();
        public event ConnectionDelegate OnConnected;
        public event ConnectionDelegate OnDisconnecting;

        public bool IsConnected
        {
            get { return request.IsConnected; }
        }

        public string IP
        {
            get { return request.IP; }
            set { request.IP = value; }
        }

        public int PORT 
        {
            get { return request.PORT; }
            set { request.PORT = value; }
        }

        private string PupilVersion;

        public string GetConnectionString()
        {
            return request.GetConnectionString();
        }

        void OnEnable()
        {
            PupilVersion = "not connected";
            if (!IsConnected)
            {
                RunConnect();
            }
        }

        void OnDisable()
        {
            if (IsConnected)
            {
                Disconnect();
            }
        }

        public void RunConnect()
        {
            StartCoroutine(Connect(retry: true, retryDelay: 5f));
        }

        private IEnumerator Connect(bool retry = false, float retryDelay = 5f)
        {
            yield return new WaitForSeconds(3f);

            while (!IsConnected)
            {
                request.InitializeRequestSocket();

                if (!IsConnected)
                {
                    request.TerminateContext();

                    if (retry)
                    {
                        Debug.LogWarning("Could not connect, Re-trying in 5 seconds! ");
                        yield return new WaitForSeconds(retryDelay);
                    }
                    else
                    {
                        Debug.LogWarning("Could not connect! ");
                        yield return null;
                    }
                }
            }

            Connected();
            
            yield break;
        }

        private void Connected()
        {
            Debug.Log(" Succesfully connected to Pupil! ");

            SetPupilTimestamp(Time.realtimeSinceStartup);
            UpdatePupilVersion();

            StartEyeProcesses();
            SetDetectionMode("3d");

            // RepaintGUI(); //
            if (OnConnected != null)
                OnConnected();
        }

        public void Disconnect()
        {
            if (OnDisconnecting != null)
            {
                OnDisconnecting();
            }

            request.CloseSockets();
        }

        public bool Send(Dictionary<string, object> dictionary)
        {
            return request.SendRequestMessage(dictionary);
        }

        public bool StartEyeProcesses()
        {
            var startLeftEye = new Dictionary<string, object> {
                {"subject", "eye_process.should_start.1"},
                {"eye_id", 1},
                {"delay", 0.1f}
            };
            var startRightEye = new Dictionary<string, object> {
                {"subject", "eye_process.should_start.0"},
                { "eye_id", 0},
                { "delay", 0.2f}
            };

            bool leftEyeRunning = Send(startLeftEye);
            bool rightEyeRunning = Send(startRightEye);

            return leftEyeRunning && rightEyeRunning;
        }

        public void StartPlugin(string name, Dictionary<string, object> args = null)
        {
            Dictionary<string, object> startPluginDic = new Dictionary<string, object> {
                { "subject", "start_plugin" },
                { "name", name }
            };

            if (args != null)
            {
                startPluginDic["args"] = args;
            }

            Send(startPluginDic);
        }

        public void StopPlugin(string name)
        {
            Send(new Dictionary<string, object> {
                { "subject","stop_plugin" },
                { "name", name }
            });
        }

        public bool SetDetectionMode(string mode)
        {
            return Send(new Dictionary<string, object> { { "subject", "set_detection_mapping_mode" }, { "mode", mode } });
        }

        public void SetPupilTimestamp(float time)
        {
            string response;
            string command = "T " + time.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
            request.SendCommand(command, out response);
        }

        public string GetPupilTimestamp()
        {
            string response;
            bool success = request.SendCommand("t", out response);

            if (!success)
            {
                Debug.LogWarning("GetPupilTimestamp: not connected!");
            }

            return response;
        }

        public string GetPupilVersion()
        {
            string pupilVersion = null;
            request.SendCommand("v", out pupilVersion);
            return pupilVersion;
        }

        private void UpdatePupilVersion()
        {
            PupilVersion = GetPupilVersion();
            Debug.Log($"Pupil Version: {PupilVersion}");
        }

        [ContextMenu("Reset To Default Connection")]
        public void ResetDefaultLocalConnection()
        {
            request.resetDefaultLocalConnection();
        }

        [ContextMenu("Check Time Sync")]
        public void CheckTimeSync()
        {
            if (IsConnected)
            {
                Debug.Log($"Unity time: {Time.realtimeSinceStartup}");
                Debug.Log($"Pupil Time: {GetPupilTimestamp()}");
            }
            else
            {
                Debug.LogWarning("CheckTimeSync: not connected");
            }
        }
    }
}