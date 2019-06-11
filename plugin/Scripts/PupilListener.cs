using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilListener
    {

        public event Action<PupilData> OnReceivePupilData;

        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public PupilListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
            this.requestCtrl = subsCtrl.requestCtrl;

            requestCtrl.OnConnected += Enable;
            requestCtrl.OnDisconnecting += Disable;

            if (subsCtrl.IsConnected)
            {
                Enable();
            }
        }

        ~PupilListener()
        {
            requestCtrl.OnConnected -= Enable;
            requestCtrl.OnDisconnecting -= Disable;

            if (subsCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            subsCtrl.SubscribeTo("pupil.", ReceivePupilData);
        }

        public void Disable()
        {
            subsCtrl.UnsubscribeFrom("pupil.", ReceivePupilData);
        }

        void ReceivePupilData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            PupilData pupilData = new PupilData(dictionary, requestCtrl.UnityToPupilTimeOffset);

            if (OnReceivePupilData != null)
            {
                OnReceivePupilData(pupilData);
            }
        }
    }
}