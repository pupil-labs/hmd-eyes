using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{
    public class PupilDataDemo : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public TimeSync timeSync;

        public Text statusText;
        private PupilListener listener;

        void OnEnable()
        {
            if (listener == null)
            {
                listener = new PupilListener(subsCtrl);
            }

            listener.Enable();
            listener.OnReceivePupilData += ReceivePupilData;
        }

        void OnDisable()
        {
            listener.Disable();
            listener.OnReceivePupilData -= ReceivePupilData;
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
            double unityTime = timeSync.ConvertToUnityTime(pupilData.PupilTimestamp);
            Debug.Log($"Receive Pupil Data with method {pupilData.Method} and confidence {pupilData.Confidence} at {unityTime}");
            if (pupilData.EyeIdx == 0)
            {
                Debug.Log($"theta {Mathf.Rad2Deg * pupilData.Circle.Theta} phi {Mathf.Rad2Deg * pupilData.Circle.Phi}");
            }
        }
    }
}