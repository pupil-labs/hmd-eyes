using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class Gaze3dListener
    {

        public delegate void Receive3dGazeDel(GazeData gazeData);
        public event Receive3dGazeDel OnReceive3dGaze;

        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public Gaze3dListener(SubscriptionsController subsCtrl)
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

        ~Gaze3dListener()
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

            GazeData gazeData = new GazeData(topic, dictionary);

            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze(gazeData);
            }
        }
    }
}
