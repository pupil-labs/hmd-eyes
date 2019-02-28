using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener
    {

        public delegate void Receive2dDel(string id, Vector3 pos, float confidence);
        public delegate void Receive3dDel();
        public event Receive2dDel OnReceive2dGaze;
        public event Receive3dDel OnReceive3dGaze;

        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public GazeListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
            this.requestCtrl = subsCtrl.requestCtrl;

            requestCtrl.OnConnected += Enable;
            requestCtrl.OnDisconnecting += Disable;

            if (requestCtrl.IsConnected)
            {
                Enable();
            }
        }

        ~GazeListener()
        {
            requestCtrl.OnConnected -= Enable;
            requestCtrl.OnDisconnecting -= Disable;

            if (requestCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            Debug.Log("Enabling Gaze Listener");

            subsCtrl.SubscribeTo("gaze.2d", Receive2DGaze);
            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
        }

        public void Disable()
        {
            Debug.Log("Disabling Gaze Listener");

            subsCtrl.UnsubscribeFrom("gaze.2d", Receive2DGaze);
            subsCtrl.UnsubscribeFrom("gaze.3d", Receive3DGaze);
        }

        void Receive3DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            Debug.Log($"3D Gaze msg topic: {topic}");

            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze();
            }
        }

        void Receive2DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            string key = "norm_pos";
            if (dictionary.ContainsKey(key))
            {
                string eyeId = Helpers.StringFromDictionary(dictionary, "id");
                var position2D = Helpers.Position(dictionary[key], false);

                float confidence = 0f;
                string confidenceKey = "confidence";
                if (dictionary.ContainsKey(confidenceKey))
                {
                    confidence = Helpers.FloatFromDictionary(dictionary, confidenceKey);
                }

                if (OnReceive2dGaze != null)
                {
                    OnReceive2dGaze(eyeId, position2D, confidence);
                }
            }
            else
            {
                Debug.LogWarning("2d gaze received without norm_pos data");
            }
        }
    }
}
