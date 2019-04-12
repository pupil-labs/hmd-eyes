using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener
    {

        public event Action<GazeData> OnReceive3dGaze;

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

            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
        }

        public void Disable()
        {
            Debug.Log("Disabling Gaze Listener");

            subsCtrl.UnsubscribeFrom("gaze.3d", Receive3DGaze);
        }

        void Receive3DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {

            GazeData gazeData = new GazeData(topic, dictionary, requestCtrl.UnityToPupilTimeOffset);

            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze(gazeData);
            }
        }
    }
}
