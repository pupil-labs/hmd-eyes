using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener : BaseListener
    {
        public event Action<GazeData> OnReceive3dGaze;

        public GazeListener(SubscriptionsController subsCtrl) : base(subsCtrl) { }

        protected override void CustomEnable()
        {
            Debug.Log("Enabling Gaze Listener");
            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
        }

        protected override void CustomDisable()
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
