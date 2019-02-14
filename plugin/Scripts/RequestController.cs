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
            get { return request.IsConnected;}   
        }

        public delegate void ConnectionDelegate();
        public event ConnectionDelegate OnConnected;
        public event ConnectionDelegate OnDisconnecting;

        [SerializeField]
        private string PupilVersion;
        
        public string GetConnectionString()
        {
            return request.GetConnectionString();
        }

        void OnEnable()
        {
            if (!IsConnected)
            {
                RunConnect();
            }
        }

        void OnDisable()
        {
            if(IsConnected)
            {
                Disconnect();
            }
        }

        public void RunConnect()
        {
            StartCoroutine(Connect(retry: true, retryDelay: 5f));
        }

        private IEnumerator Connect(bool retry = false, float retryDelay = 5f)
        //TODO crash on cancel while trying to connect
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
            if (OnDisconnecting != null){
                OnDisconnecting();
            }

            request.CloseSockets();
        }

        public bool Send(Dictionary<string, object> dictionary)
        {
            if (!IsConnected)
            {
                return false;
            }
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
            if (IsConnected)
            {
                string response;
                request.SendCommand("T " + time.ToString("0.00000000"),out response);
            }
        }

        public string GetPupilVersion()
        {
            string pupilVersion;
            request.SendCommand("v",out pupilVersion);
            return pupilVersion;
        }

        private void UpdatePupilVersion()
        {
            PupilVersion = GetPupilVersion();
            Debug.Log($"Pupil Version: {PupilVersion}");
        }
    }
}