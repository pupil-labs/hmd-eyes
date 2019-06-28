using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener
    {

        public event Action<GazeData> OnReceive3dGaze;

        public bool IsListening { get; private set; }

        private SubscriptionsController subsCtrl;

        public GazeListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
        }

        ~GazeListener()
        {
            if (subsCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            if (!subsCtrl.IsConnected)
            {
                Debug.LogWarning("No connected!");
                return;
            }

            if (IsListening)
            {
                Debug.Log("Already running.");
                return;
            }

            Debug.Log("Enabling Gaze Listener");

            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
            IsListening = true;
        }

        public void Disable()
        {
            if (!subsCtrl.IsConnected)
            {
                Debug.Log("Not connected.");
                return;
            }

            if (!IsListening)
            {
                Debug.Log("Not running.");
                return;
            }

            Debug.Log("Disabling Gaze Listener");

            subsCtrl.UnsubscribeFrom("gaze.3d", Receive3DGaze);
            IsListening = false;
        }

        void Receive3DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {

            GazeData gazeData = new GazeData(topic, dictionary, subsCtrl.requestCtrl.UnityToPupilTimeOffset);

            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze(gazeData);
            }
        }
    }
}
