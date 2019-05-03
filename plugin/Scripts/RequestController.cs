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

        public event Action OnConnected;
        public event Action OnDisconnecting;

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

        public double UnityToPupilTimeOffset { get { return timeSync.UnityToPupilTimeOffset; } }
        private TimeSync timeSync = null;

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
            StartCoroutine(Connect(retry: true));
        }

        private IEnumerator Connect(bool retry = false)
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
                        yield return new WaitForSeconds(retryConnectDelay);
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

            timeSync = new TimeSync(request);
            timeSync.UpdateTimeSync();
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

        [ContextMenu("Update TimeSync")]
        public void UpdateTimeSync()
        {
            if (!IsConnected)
            {
                return;
            }

            timeSync.UpdateTimeSync();
        }

        public double GetPupilTimeStamp()
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            return timeSync.GetPupilTimestamp();
        }

        public double ConvertToUnityTime(double pupilTimestamp)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            return timeSync.ConvertToUnityTime(pupilTimestamp);
        }

        public double ConvertToPupilTime(double unityTime)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Not connected");
                return 0;
            }

            return timeSync.ConvertToPupilTime(unityTime);
        }

        [System.Obsolete("Setting the pupil timestamp might be in conflict with other plugins.")]
        public void SetPupilTimestamp(double time)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Not connected");
                return;
            }

            timeSync.SetPupilTimestamp(time);
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
                timeSync.CheckTimeSync();
            }
            else
            {
                Debug.LogWarning("CheckTimeSync: not connected");
            }
        }

        [ContextMenu("Sync Pupil Time To Time.now")]
        void SyncPupilTimeToUnityTime()
        {
            if (IsConnected)
            {
                timeSync.SetPupilTimestamp(Time.realtimeSinceStartup);
            }
        }
    }
}