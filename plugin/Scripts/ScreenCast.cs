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
        public TimeSync timeSync;
        public Camera centeredCamera;
        [Tooltip("Can't be changed at runtime")]
        public int initialWidth = 640, initialHeight = 480;
        [Range(1, 120)]
        public int maxFrameRate = 90;
        public bool pauseStreaming = false;

        public Texture2D StreamTexture { get; private set; }

        PublisherSocket pubSocket;
        RenderTexture renderTexture;
        bool isSetup = false;
        int index = 0;
        float tLastFrame;
        int width, height;
        float[] intrinsics = {0,0,0,0,0,0,0,0,1};

        const string topic = "hmd_streaming.world";

        float deltaTimeAcc = 0f;
        
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
            requestCtrl.StartPlugin("HMD_Streaming_Source");

            string connectionStr = requestCtrl.GetPubConnectionString();
            pubSocket = new PublisherSocket(connectionStr);

            isSetup = true;
        }

        void Update()
        {
            if (!isSetup || pauseStreaming)
            {
                return;
            }

            if (IgnoreFrameBasedOnFPS())
            {
                return;
            }

            UpdateIntrinsics();

            AsyncGPUReadback.Request
            (
                renderTexture, 0, TextureFormat.RGB24,
                (AsyncGPUReadbackRequest r) => ReadbackDone(r, timeSync.ConvertToPupilTime(Time.realtimeSinceStartup))
            );   
        }

        void ReadbackDone(AsyncGPUReadbackRequest r, double timestamp)
        {
            if (StreamTexture == null)
            {
                return;
            }

            StreamTexture.LoadRawTextureData(r.GetData<byte>());
            StreamTexture.Apply();
            
            SendFrame(timestamp);
        }

        void SendFrame(double timestamp)
        {
            Dictionary<string, object> payload = new Dictionary<string, object> {
                {"topic", topic},
                {"width", width},
                {"height", height},
                {"index", index},
                {"timestamp", timestamp},
                {"format", "rgb"},
                {"projection_matrix", intrinsics}
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
            float fov = centeredCamera.fieldOfView;
            float focalLength = 1f / (Mathf.Tan(fov / (2 * Mathf.Rad2Deg)) / height) / 2;
            
            // f 0   width/2
            // 0   f height/2
            // 0   0   1

            intrinsics[0] = focalLength;
            intrinsics[2] = width/2;
            intrinsics[4] = focalLength;
            intrinsics[5] = height/2;
        }

        bool IgnoreFrameBasedOnFPS()
        {
            deltaTimeAcc += Time.deltaTime;
            if ( deltaTimeAcc < 1 / (float)maxFrameRate)
            {
                return true;
            }
            deltaTimeAcc %= 1 / (float)maxFrameRate;

            return false;
        }
    }
}