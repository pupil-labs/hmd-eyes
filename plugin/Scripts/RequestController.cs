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

        public bool IsConnected
        {
            get { return request.IsConnected; }
        }

        public delegate void ConnectionDelegate();
        public event ConnectionDelegate OnConnected;
        public event ConnectionDelegate OnDisconnecting;

        // [SerializeField]
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
                    if (retry)
                    {
                        Debug.Log("Could not connect, Re-trying in 5 seconds ! ");
                        yield return new WaitForSeconds(retryDelay);

                    }
                    else
                    {
                        request.TerminateContext();
                        yield return null;
                    }
                }
            }
            Debug.Log(" Succesfully connected to Pupil! ");
            UpdatePupilVersion();

            // RepaintGUI(); //
            if (OnConnected != null)
                OnConnected();
            yield break;
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

            request.SendRequestMessage(startPluginDic);
        }

        public void StopPlugin(string name)
        {
            request.SendRequestMessage(new Dictionary<string, object> {
                { "subject","stop_plugin" },
                { "name", name }
            });
        }

        public void SetPupilTimestamp(float time)
        {
            string response;
            string command = "T " + time.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
            Debug.Log($"Sync Time Command: {command}");
            request.SendCommand(command, out response);
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
    }
}