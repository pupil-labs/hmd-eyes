using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        [Range(1, 120)]
        public int maxFrameRate = 90;

        public Texture2D StreamTexture { get; private set; }

        PublisherSocket pubSocket;
        RenderTexture renderTexture;
        bool isSetup = false;
        int index = 0;
        float tLastFrame;
        int width, height;
        float[] projection_matrix = new float[9];

        const string topic = "hmd_streaming.world";

        void Awake()
        {
            width = initialWidth;
            height = initialHeight;

            renderTexture = new RenderTexture(width, height, 24);
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

            if (Time.time - tLastFrame < 1 / (float)maxFrameRate)
            {
                return;
            }

            tLastFrame = Time.time;
            
            AsyncGPUReadback.Request
            (
                renderTexture, 0, TextureFormat.RGB24,
                (AsyncGPUReadbackRequest r) => ReadbackDone(r, Time.realtimeSinceStartup)
            );   
        }

        void ReadbackDone(AsyncGPUReadbackRequest r, float timestamp)
        {
            if (StreamTexture == null)
            {
                return;
            }

            StreamTexture.LoadRawTextureData(r.GetData<byte>());
            StreamTexture.Apply();
            
            SendFrame(timestamp);
        }

        void SendFrame(float timestamp)
        {
            UpdateIntrinsics();

            Dictionary<string, object> payload = new Dictionary<string, object> {
                {"topic", topic},
                {"width", width},
                {"height", height},
                {"index", index},
                {"timestamp", timestamp},
                {"format", "rgb"},
                {"projection_matrix", projection_matrix}
            };

            NetMQMessage m = new NetMQMessage();
            m.Append(topic);
            m.Append(MessagePackSerializer.Serialize<Dictionary<string, object>>(payload));
            m.Append(StreamTexture.GetRawTextureData());

            pubSocket.SendMultipartMessage(m);

            index++;
        }

        void UpdateIntrinsics()
        {
            int idx = 0;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    projection_matrix[idx] = centeredCamera.projectionMatrix[r,c];
                    idx++;
                }
            }
        }
    }
}