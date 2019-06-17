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
        public Camera centeredCamera;
        [Tooltip("Can't be changed at runtime")] 
        public int initialWidth = 640, initialHeight = 480;
        [Range(30,120)]
        public int maxFrameRate = 90;
        public bool inBGR = false;
        
        public Texture2D StreamTexture { get; private set; }

        PublisherSocket pubSocket;
        RenderTexture renderTexture;
        bool isSetup = false;
        int index = 0;
        float tLastFrame;
        int width, height;
        byte[] bgr24;

        const string topic = "hmd_streaming.world";

        void Awake()
        {
            width = initialWidth;
            height = initialHeight;

            bgr24 = new byte[width*height*3];

            renderTexture = new RenderTexture(width,height,24);
            centeredCamera.targetTexture = renderTexture;
            StreamTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        }

        void OnEnable()
        {
            requestCtrl.OnConnected += Setup;
        }

        void Setup()
        {
            string connectionStr = requestCtrl.GetPubConnectionString();
            pubSocket = new PublisherSocket(connectionStr);
            
            isSetup = true;
        }

        void Update()
        {
            if (!isSetup)
            {
                return;
            }

            if (Time.time - tLastFrame < 1/(float)maxFrameRate)
            {
                return;
            }

            tLastFrame = Time.time;
            
            RenderTexture.active = renderTexture;

            StreamTexture.ReadPixels(new Rect(0,0,width,height),0,0);
            StreamTexture.Apply();

            RenderTexture.active = null;

            SendFrame();            
        }

        void SendFrame()
        {
            float[] projection_matrix = new float[16];
            for (int i = 0; i<16; ++i)
            {
                projection_matrix[i] = centeredCamera.projectionMatrix[i];
            }

            Dictionary<string, object> payload = new Dictionary<string, object> {
                {"topic", topic},
                {"width", width},
                {"height", height},
                {"index", index},
                {"timestamp", Time.realtimeSinceStartup},
                {"format", inBGR ? "bgr" : "rgb"},
                {"projection_matrix", projection_matrix} //TODO everyframe? - might change I guess
            };
        
            byte[] rawTextureData = StreamTexture.GetRawTextureData();
            byte[] pixels = rawTextureData;

            if (inBGR)
            {
                rgbToBgr(rawTextureData);
                pixels = bgr24;
            }

            NetMQMessage m = new NetMQMessage();
            m.Append(topic); 
            m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(payload));
            m.Append(pixels);

            pubSocket.SendMultipartMessage(m);

            index++;
        }

        void rgbToBgr(byte[] rgb)
        {
            float tStart = Time.realtimeSinceStartup*1000f;
            for (int i=0;i<rgb.Length;i++)
            {
                // 0 -> +2; 1 -> +0; 2 -> -2;
                int offset = ((i % 3) - 1) * -2;
                bgr24[i+offset] = rgb[i];
            }
            float tAfter = Time.realtimeSinceStartup*1000f;
            Debug.Log($"rgb to bgr in {tAfter-tStart}ms");
        }
    }
}