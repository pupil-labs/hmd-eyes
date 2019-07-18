using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilListener : BaseListener
    {
        public event Action<PupilData> OnReceivePupilData;

        public PupilListener(SubscriptionsController subsCtrl) : base(subsCtrl) { }

        protected override void CustomEnable()
        {
            Debug.Log("Enabling Pupil Listener");
            subsCtrl.SubscribeTo("pupil.", ReceivePupilData);
        }

        protected override void CustomDisable()
        {
            Debug.Log("Disabling Pupil Listener");
            subsCtrl.UnsubscribeFrom("pupil.", ReceivePupilData);
        }

        void ReceivePupilData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            PupilData pupilData = new PupilData(dictionary);

            if (OnReceivePupilData != null)
            {
                OnReceivePupilData(pupilData);
            }
        }
    }
}