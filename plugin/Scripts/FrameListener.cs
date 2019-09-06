using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class FrameListener : BaseListener
    {
        public event Action<int, byte[]> OnReceiveEyeFrame;

        public FrameListener(SubscriptionsController subsCtrl) : base(subsCtrl) {}

        protected override void CustomEnable()
        {
            Debug.Log("Enabling Frame Listener");

            subsCtrl.SubscribeTo("frame.eye.", CustomReceiveData);
            subsCtrl.requestCtrl.StartPlugin("Frame_Publisher");
        }

        protected override void CustomDisable()
        {
            Debug.Log("Disabling Frame Listener");

            subsCtrl.UnsubscribeFrom("frame.eye.", CustomReceiveData);
            subsCtrl.requestCtrl.StopPlugin("Frame_Publisher");
        }

        void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            if (thirdFrame == null)
            {
                Debug.LogError("Received Frame Data without any image data");
                return;
            }

            int eyeIdx;
            if (topic == "frame.eye.0")
            {
                eyeIdx = 0;
            }
            else if (topic == "frame.eye.1")
            {
                eyeIdx = 1;
            }
            else
            {
                Debug.LogError($"{topic} isn't matching");
                return;
            }

            if (OnReceiveEyeFrame != null)
            {
                OnReceiveEyeFrame(eyeIdx, thirdFrame);
            }
        }
    }
}


