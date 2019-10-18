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

        Publisher publisher;
        bool isSetup = false;

        void OnEnable()
        {
            requestCtrl.OnConnected += Setup;
        }

        void Setup()
        {
            requestCtrl.StartPlugin("Annotation_Capture");

            publisher = new Publisher(requestCtrl);

            isSetup = true;
        }

        void OnApplicationQuit()
        {
            if (isSetup)
            {
                publisher.Destroy();
            }
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

            publisher.Send("annotation", annotation);
        }
    }
}
