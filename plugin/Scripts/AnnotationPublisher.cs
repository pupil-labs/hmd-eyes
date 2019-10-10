using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
    public class AnnotationPublisher : MonoBehaviour
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

        public void SendAnnotation(string label, float duration = 0.0f, Dictionary<string, object> customData = null)
        {
            double pupiltime = timeSync.ConvertToPupilTime(Time.realtimeSinceStartup);
            SendAnnotation(label, pupiltime, duration, customData);
        }

        public void SendAnnotation(string label, double pupiltimestamp, float duration = 0.0f, Dictionary<string, object> customData = null)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!isSetup)
            {
                Setup();
            }

            Dictionary<string, object> annotation = new Dictionary<string, object>();
            annotation["topic"] = "annotation";
            annotation["label"] = label;
            annotation["timestamp"] = pupiltimestamp;
            annotation["duration"] = duration;

            //add custom data
            if (customData != null)
            {
                foreach (var kv in customData)
                {
                    annotation.Add(kv.Key,kv.Value);
                }
            }

            SendPubMessage(annotation);
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
