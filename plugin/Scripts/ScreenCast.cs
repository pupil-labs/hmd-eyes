using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetMQ;
using NetMQ.Sockets;
using MessagePack;

namespace PupilLabs
{
    public class ScreenCast : MonoBehaviour
    {
        public RequestController requestCtrl;
        public Camera centerCam;
        public int width, height;
        public float delay = 0.3f;
        
        private RenderTexture renderTexture;
        public Texture2D streamTexture;
        bool isSetup = false;
        PublisherSocket pubSocket;
        int index = 0;
        float tLastFrame;

        void Awake()
        {
            renderTexture = new RenderTexture(width,height,24);
            centerCam.targetTexture = renderTexture;
            streamTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }

        void OnEnable()
        {
            requestCtrl.OnConnected += Setup;
        }

        void Setup()
        {
            string connectionStr = requestCtrl.GetPubConnectionString();
            pubSocket = new PublisherSocket(connectionStr);
            // pubSocket.SendReady += ? 
            
            isSetup = true;
            Debug.Log("screen cast ready");
        }

        void Update()
        {
            // activeCamera.Render();
            RenderTexture.active = renderTexture;

            // Read pixels
            streamTexture.ReadPixels(new Rect(0,0,width,height),0,0);
            streamTexture.Apply();

            // Clean up
            RenderTexture.active = null; // added to avoid errors 

            if (!isSetup)
            {
                return;
            }

            // if (Time.time - tLastFrame > delay)
            // {
                Send();
            //     tLastFrame = Time.time;
            // }
        }

        void Send()
        {
            Debug.Log("Send");
            byte[] rawTextureData = streamTexture.GetRawTextureData();

            Dictionary<string, object> payload = new Dictionary<string, object> {
                {"topic", "frame.world"},
                {"width", width},
                {"height", height},
                {"index", index},
                {"timestamp", Time.realtimeSinceStartup},
                {"format", "rgb"},
            };
        
            NetMQMessage m = new NetMQMessage();
            m.Append("frame.world"); 
            m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(payload));
            m.Append(rawTextureData);

            pubSocket.SendMultipartMessage(m);

            index++;
        }
    }
}