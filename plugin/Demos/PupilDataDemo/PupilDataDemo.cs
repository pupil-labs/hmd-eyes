using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{
    public class PupilDataDemo : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public Text statusText;
        private PupilListener listener;

        void OnEnable()
        {
            listener = new PupilListener(subsCtrl);

            subsCtrl.requestCtrl.OnConnected += listener.Enable;
            listener.OnReceivePupilData += ReceivePupilData;
        }


        void Update()
        {
            if (statusText == null) { return; }

            statusText.text = subsCtrl.IsConnected ? "Connected" : "Not connected";

            if (subsCtrl.IsConnected)
            {
                statusText.text += "\n ... but nothing happening here. \nPlease check the console and have a look at the source code to get started.";
            }
        }

        void ReceivePupilData(PupilData pupilData)
        {
            Debug.Log($"Receive Pupil Data with method {pupilData.Method} and confidence {pupilData.Confidence}");
            if (pupilData.EyeIdx == 0)
            {
                Debug.Log($"theta {Mathf.Rad2Deg * pupilData.Circle.Theta} phi {Mathf.Rad2Deg * pupilData.Circle.Phi}");
            }
        }
    }
}