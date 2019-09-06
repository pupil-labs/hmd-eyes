using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{

    public partial class RequestController : MonoBehaviour
    {
        [Header("IP & Port")]
        [SerializeField]
        private Request request;
        [Header("Settings")]
        public float retryConnectDelay = 5f;
        public bool connectOnEnable = true;

        public event Action OnConnected;
        public event Action OnDisconnecting;

        public bool IsConnected
        {
            get { return request.IsConnected && connectingDone; }
        }
        private bool connectingDone;

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

        public string GetSubConnectionString()
        {
            return request.GetSubConnectionString();
        }

        public string GetPubConnectionString()
        {
            return request.GetPubConnectionString();
        }

        void OnEnable()
        {
            if (request == null)
            {
                request = new Request();
            }

            PupilVersion = "not connected";
            if (!request.IsConnected && connectOnEnable)
            {
                RunConnect();
            }
        }

        void OnDisable()
        {
            if (request.IsConnected)
            {
                Disconnect();
            }
        }

        public void RunConnect()
        {
            if (!enabled)
            {
                Debug.LogWarning("Component not enabled!");
                return;
            }

            if (request.IsConnected)
            {
                Debug.LogWarning("Already connected!");
                return;
            }

            StartCoroutine(Connect(retry: true));
        }

        private IEnumerator Connect(bool retry = false)
        {
            yield return new WaitForSeconds(3f);

            connectingDone = false;

            while (!request.IsConnected)
            {
                yield return StartCoroutine(request.InitializeRequestSocketAsync(1f));

                if (!request.IsConnected)
                {
                    request.TerminateContext();

                    if (retry)
                    {
                        Debug.LogWarning("Could not connect, Re-trying in 5 seconds! ");
                        yield return new WaitForSeconds(retryConnectDelay);
                    }
                    else
                    {
                        Debug.LogWarning("Could not connect! ");
                        yield break;
                    }
                }
            }

            Connected();

            yield break;
        }

        private void Connected()
        {
            Debug.Log(" Succesfully connected to Pupil! ");

            UpdatePupilVersion();

            StartEyeProcesses();
            SetDetectionMode("3d");

            connectingDone = true;

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

        public void OnDestroy()
        {
            request.TerminateContext();
        }

        public void Send(Dictionary<string, object> dictionary)
        {
            if (!request.IsConnected)
            {
                Debug.LogWarning("Not connected!");
                return;
            }

            request.SendRequestMessage(dictionary);
        }

        public bool SendCommand(string command, out string response)
        {
            return request.SendCommand(command,out response);
        }

        public void StartEyeProcesses()
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

            Send(startLeftEye);
            Send(startRightEye);
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

        public void SetDetectionMode(string mode)
        {
            Send(new Dictionary<string, object> { { "subject", "set_detection_mapping_mode" }, { "mode", mode } });
        }

        public string GetPupilVersion()
        {
            string pupilVersion = null;
            SendCommand("v", out pupilVersion);
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
    }
}