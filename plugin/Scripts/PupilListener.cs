using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilListener
    {

        public event Action<PupilData> OnReceivePupilData;

        public bool IsListening { get; private set; }
        
        private SubscriptionsController subsCtrl;

        public PupilListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
        }

        ~PupilListener()
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

            subsCtrl.SubscribeTo("pupil.", ReceivePupilData);
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

            subsCtrl.UnsubscribeFrom("pupil.", ReceivePupilData);
            IsListening = false;
        }

        void ReceivePupilData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            PupilData pupilData = new PupilData(dictionary, subsCtrl.requestCtrl.UnityToPupilTimeOffset);

            if (OnReceivePupilData != null)
            {
                OnReceivePupilData(pupilData);
            }
        }
    }
}