using System;
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

            public RequestSocket requestSocket = null;
            private bool contextExists = false;
            private TimeSpan requestTimeout = new System.TimeSpan(0, 0, 1); //= 1sec

            public bool IsConnected { get; set; }

            public string GetConnectionString()
            {
                return IPHeader + subport;
            }

            public void InitializeRequestSocket()
            {
                IPHeader = ">tcp://" + IP + ":";

                Debug.Log("Attempting to connect to : " + IPHeader + PORT);

                if (!contextExists)
                {
                    CreateContext();
                }

                requestSocket = new RequestSocket(IPHeader + PORT);
                requestSocket.SendFrame("SUB_PORT");
                IsConnected = requestSocket.TryReceiveFrameString(requestTimeout, out subport);
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
