using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{

    public class PupilDataDemo : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public Text statusText;

        private RequestController requestCtrl;
        private PupilListener listener;

        void Awake()
        {
            listener = new PupilListener(subsCtrl);
            requestCtrl = subsCtrl.requestCtrl;

            listener.OnReceivePupilData += ReceivePupilData;
        }


        void Update()
        {
            if (statusText == null) { return; }

            statusText.text = requestCtrl.IsConnected ? "Connected" : "Not connected";

            if (requestCtrl.IsConnected)
            {
                statusText.text += "\n ... but nothing happening here. \nPlease check the console and have a look at the source code to get started.";
            }
        }

        void ReceivePupilData(PupilData pupilData)
        {
            Debug.Log($"Receive Pupil Data with method {pupilData.Method} and confidence {pupilData.Confidence}");
        }
    }
}