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
            private float timeout = 1f;
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

                yield return RequestReceiveAsync(
                    () => requestSocket.SendFrame("SUB_PORT"),
                    () => IsConnected = requestSocket.TryReceiveFrameString(out subport)
                );

                if (IsConnected)
                {
                    yield return RequestReceiveAsync(
                        () => requestSocket.SendFrame("PUB_PORT"),
                        () => requestSocket.TryReceiveFrameString(out pubport)
                    );
                }
            }

            private IEnumerator RequestReceiveAsync(Action request, Action receive)
            {
                float tStarted = Time.realtimeSinceStartup;
                
                request();

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
                            msgReceived = true;
                            receive();
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
            }

            public void CloseSockets()
            {
                if (requestSocket != null)
                    requestSocket.Close();

                IsConnected = false;
            }

            ~Request()
            {
                CloseSockets();
            }

            public void SendRequestMessage(Dictionary<string, object> data)
            {
                NetMQMessage m = new NetMQMessage();

                m.Append("notify." + data["subject"]);
                m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(data));

                requestSocket.SendMultipartMessage(m);
                ReceiveRequestResponse();
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

            private void ReceiveRequestResponse()
            {
                NetMQMessage m = new NetMQMessage();
                requestSocket.TryReceiveMultipartMessage(requestTimeout, ref m);
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
