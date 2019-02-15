using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs.Demos
{
    public class FramePublishingDemo : MonoBehaviour
    {
        public FramePublishing publisher;
        public RequestController requestCtrl;

        void OnEnable()
        {
            if (requestCtrl == null)
            {
                Debug.LogWarning("EyeCamVisDemo needs access to a RequestController");
                enabled = false;
                return;
            }

            requestCtrl.OnConnected += StartFramePublishing;
            requestCtrl.OnDisconnecting += StopFramePublishing;
        }

        void StartFramePublishing()
        {
            if (publisher != null)
            {
                publisher.enabled = true;
            }
        }

        void StopFramePublishing()
        {
            if (publisher != null)
            {
                publisher.enabled = false;
            }
        }

        void OnDisable()
        {
            if (requestCtrl == null)
            {
                return;
            }

            StopFramePublishing();

            requestCtrl.OnConnected -= StartFramePublishing;
            requestCtrl.OnDisconnecting -= StopFramePublishing;
        }
    }
}
