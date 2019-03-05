using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class FrameListener
    {
        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public delegate void PublishHandler(int eyeIdx, byte[] frameData);
        public event PublishHandler OnReceiveEyeFrame;

        public FrameListener(SubscriptionsController subsCtrl)
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

        ~FrameListener()
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
            Debug.Log("Enabling Frame Publisher");

            subsCtrl.SubscribeTo("frame.eye.", CustomReceiveData);
            requestCtrl.StartPlugin("Frame_Publisher");
        }

        public void Disable()
        {
            Debug.Log("Disabling Frame Publisher");

            subsCtrl.UnsubscribeFrom("frame.eye.", CustomReceiveData);
            requestCtrl.StopPlugin("Frame_Publisher");
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


