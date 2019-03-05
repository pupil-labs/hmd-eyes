using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class FrameVisualizer : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;

        public int targetFPS = 20;
        public Transform cameraAsParent;

        Texture2D[] eyeTexture = new Texture2D[2];
        byte[][] eyeImageRaw = new byte[2][];
        MeshRenderer[] eyeRenderer = new MeshRenderer[2];
        bool[] eyePublishingInitialized = new bool[2];

        FrameListener publisher = null;

        void OnEnable()
        {
            if (cameraAsParent == null)
            {
                Debug.LogWarning("Frame Publisher needs the camera transform");
                enabled = false;
                return;
            }

            if (subscriptionsController == null)
            {
                Debug.LogWarning("Frame Publisher needs access to SubscriptionsController");
                enabled = false;
                return;
            }


            if (publisher == null)
            {
                publisher = new FrameListener(subscriptionsController);
            }

            Debug.Log("Enabling Frame Visualizer");

            publisher.OnReceiveEyeFrame += ReceiveEyeFrame;

            eyePublishingInitialized = new bool[] { false, false };
        }

        void ReceiveEyeFrame(int eyeIdx, byte[] frameData)
        {
            if (!eyePublishingInitialized[eyeIdx])
            {
                InitializeFramePublishing(eyeIdx);
            }
            eyeImageRaw[eyeIdx] = frameData;
        }

        public void InitializeFramePublishing(int eyeIndex)
        {
            Transform parent = cameraAsParent;
            Shader shader = Shader.Find("Unlit/Texture");

            eyeTexture[eyeIndex] = new Texture2D(100, 100);
            eyeRenderer[eyeIndex] = InitializeEyeObject(eyeIndex, parent);
            eyeRenderer[eyeIndex].material = new Material(shader);
            eyeRenderer[eyeIndex].material.mainTexture = eyeTexture[eyeIndex];
            if (eyeIndex == 1)
                eyeRenderer[eyeIndex].material.mainTextureScale = new Vector2(-1, -1);

            lastUpdate = Time.time;

            eyePublishingInitialized[eyeIndex] = true;
        }

        MeshRenderer InitializeEyeObject(int eyeIndex, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Eye " + eyeIndex.ToString();
            go.transform.parent = parent;
            go.transform.localEulerAngles = Vector3.left * 90;
            go.transform.localScale = Vector3.one * 0.05f;
            go.transform.localPosition = new Vector3((eyeIndex == 0 ? -0.3f : 0.3f), -0.5f, 1.9999f);

            Destroy(go.GetComponent<Collider>());

            return go.GetComponent<MeshRenderer>();
        }

        float lastUpdate;
        void Update()
        {
            //Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
            if ((Time.time - lastUpdate) >= (1f / targetFPS))
            {
                for (int i = 0; i < 2; i++)
                    if (eyePublishingInitialized[i])
                        eyeTexture[i].LoadImage(eyeImageRaw[i]);
                lastUpdate = Time.time;
            }
        }

        void OnDisable()
        {
            Debug.Log("Disabling Frame Visualizer");

            if (publisher != null)
            {
                publisher.OnReceiveEyeFrame -= ReceiveEyeFrame;
            }

            for (int i = eyeRenderer.Length - 1; i >= 0; i--)
            {
                if (eyeRenderer[i] != null && eyeRenderer[i].gameObject != null)
                {
                    Destroy(eyeRenderer[i].gameObject);
                }
            }
        }
    }
}


