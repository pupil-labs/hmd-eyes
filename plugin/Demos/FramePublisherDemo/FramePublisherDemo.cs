using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{
    public class FramePublisherDemo : MonoBehaviour
    {
        public RequestController requestController;
        public Text text;

        void Update()
        {
            if (requestController == null || text == null) { return; }

            text.text = requestController.IsConnected ? "Connected" : "Not connected";
        }
    }
}

