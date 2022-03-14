using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PupilLabs
{

    public partial class RequestController : MonoBehaviour
    {
        [SerializeField][HideInInspector]
        private Request request;

        [Header("Settings")]
        public float retryConnectDelay = 5f;
        public bool connectOnEnable = true;

        public event Action OnConnected = delegate { };
        public event Action OnDisconnecting = delegate { };

        public bool IsConnected
        {
            get { return request.IsConnected && connectingDone; }
        }
        private bool connectingDone;
        
        [SerializeField][HideInInspector] 
        private bool isConnecting = false;

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

        [SerializeField][HideInInspector]
        private string PupilVersion;

        public string GetSubConnectionString()
        {
            return request.GetSubConnectionString();
        }

        public string GetPubConnectionString()
        {
            return request.GetPubConnectionString();
        }

        void Awake()
        {   
            NetMQCleanup.MonitorConnection(this);
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
                RunConnect(3f);
            }
        }

        void OnDisable()
        {
            Disconnect();
        }

        void OnDestroy()
        {
            Disconnect();

            NetMQCleanup.CleanupConnection(this);
        }

        public void RunConnect(float delay = 0)
        {
            if (isConnecting)
            {
                Debug.LogWarning("Already trying to connect!");
                return;
            }

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

            StartCoroutine(Connect(retry: true, delay: delay));
        }

        private IEnumerator Connect(bool retry = false, float delay = 0)
        {
            isConnecting = true;

            yield return new WaitForSeconds(delay);

            connectingDone = false;

            while (!request.IsConnected)
            {
                yield return StartCoroutine(request.InitializeRequestSocketAsync(1f));

                if (!request.IsConnected)
                {
                    request.Close();

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

            isConnecting = false;
            Connected();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif

            yield break;
        }

        private void Connected()
        {
            Debug.Log("Succesfully connected to Pupil! ");

            UpdatePupilVersion();

            StartEyeProcesses();
            SetDetectionMode("3d");

            connectingDone = true;

            OnConnected();
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }

            OnDisconnecting();

            request.Close();
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
            return request.SendCommand(command, out response);
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