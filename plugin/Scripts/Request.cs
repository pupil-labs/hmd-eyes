using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{

    public partial class RequestController
    {

        [System.Serializable]
        private class Request
        {

            [Header("Connection")]
            public string IP = "127.0.0.1";
            public int PORT = 50020;
            private string IPHeader;
            private string subport;
            private string pubport;

            public RequestSocket requestSocket = null;
            private bool contextExists = false;
            private TimeSpan requestTimeout = new System.TimeSpan(0, 0, 1); //= 1sec

            public bool IsConnected { get; set; }

            public string GetSubConnectionString()
            {
                return IPHeader + subport;
            }

            public string GetPubConnectionString()
            {
                return IPHeader + pubport;
            }

            public IEnumerator InitializeRequestSocketAsync(float timeout)
            {
                float tStarted = Time.realtimeSinceStartup;

                IPHeader = ">tcp://" + IP + ":";
                Debug.Log("Attempting to connect to : " + IPHeader + PORT);

                if (!contextExists)
                {
                    CreateContext();
                }

                requestSocket = new RequestSocket(IPHeader + PORT);
                requestSocket.SendFrame("SUB_PORT");

                while (!IsConnected)
                {

                    if (Time.realtimeSinceStartup - tStarted > timeout)
                    {
                        yield break;
                    }
                    else
                    {
                        if (requestSocket.HasIn)
                        {
                            IsConnected = requestSocket.TryReceiveFrameString(requestTimeout, out subport);
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }

                if (IsConnected)
                {
                    requestSocket.SendFrame("PUB_PORT");

                    bool msgReceived = false;
                    while (!msgReceived)
                    {
                        if (Time.realtimeSinceStartup - tStarted > timeout)
                        {
                            yield break;
                        }
                        else
                        {
                            if (requestSocket.HasIn)
                            {
                                msgReceived = requestSocket.TryReceiveFrameString(requestTimeout, out pubport);
                            }
                            else
                            {
                                yield return new WaitForSeconds(0.1f);
                            }
                        }
                    }
                    
                }
            }

            public void CloseSockets()
            {
                if (requestSocket != null)
                    requestSocket.Close();

                TerminateContext();

                IsConnected = false;
            }

            ~Request()
            {
                CloseSockets();
            }

            public bool SendRequestMessage(Dictionary<string, object> data)
            {
                if (requestSocket == null || !IsConnected)
                {
                    return false;
                }

                NetMQMessage m = new NetMQMessage();

                m.Append("notify." + data["subject"]);
                m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(data));

                requestSocket.SendMultipartMessage(m);
                return ReceiveRequestResponse();
            }

            public bool SendCommand(string cmd, out string response)
            {
                if (requestSocket == null || !IsConnected)
                {
                    response = null;
                    return false;
                }

                requestSocket.SendFrame(cmd);
                return requestSocket.TryReceiveFrameString(requestTimeout, out response);
            }

            public bool ReceiveRequestResponse()
            {
                if (requestSocket == null || !IsConnected)
                {
                    return false;
                }

                NetMQMessage m = new NetMQMessage();
                return requestSocket.TryReceiveMultipartMessage(requestTimeout, ref m);
            }

            private void CreateContext()
            {
                AsyncIO.ForceDotNet.Force();
                contextExists = true;
            }

            public void TerminateContext()
            {
                if (contextExists)
                {
                    Debug.Log("Request Context Cleanup");
                    NetMQConfig.Cleanup(false);
                    contextExists = false;
                }
            }

            public void resetDefaultLocalConnection()
            {
                IP = "127.0.0.1";
                PORT = 50020;
            }

        }
    }
}
