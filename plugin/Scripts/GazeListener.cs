using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener
    {



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


        }

        void Receive2DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            Debug.Log($"2D Gaze msg topic: {topic}");


        }
    }
}
