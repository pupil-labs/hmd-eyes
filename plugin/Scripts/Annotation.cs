using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{

    public class Annotation : MonoBehaviour
    {
        public RequestController requestCtrl;
        public TimeSync timeSync;

        PublisherSocket pubSocket;
        bool isSetup = false;

        void OnEnable()
        {
            requestCtrl.OnConnected += Setup;
        }

        void Setup()
        {
            requestCtrl.StartPlugin("Annotation_Capture");

            string connectionStr = requestCtrl.GetPubConnectionString();
            pubSocket = new PublisherSocket(connectionStr);

            isSetup = true;
        }

        public void SendAnnotation(string label, float duration)
        {
            if (!isSetup)
            {
                Setup();
            }

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["topic"] = "annotation";
            data["label"] = label;
            data["timestamp"] = timeSync.ConvertToPupilTime(Time.realtimeSinceStartup);
            data["duration"] = duration;

            SendPubMessage(data);
        }


        private void SendPubMessage(Dictionary<string, object> data)
        {
            if (pubSocket == null || !isSetup)
            {
                Debug.Log("No valid Pub Socket found. Nothing sent.");
                return;
            }

            NetMQMessage m = new NetMQMessage();

            m.Append(data["topic"].ToString());
            m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(data));

            pubSocket.SendMultipartMessage(m);
            return;
        }
    }
}
