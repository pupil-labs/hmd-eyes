using System;
using UnityEngine;

namespace PupilLabs
{
    public class GazeController : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;

        public event Action<GazeData> OnReceive3dGaze;
        
        GazeListener listener;

        void OnEnable()
        {
            if (subscriptionsController == null)
            {
                Debug.LogWarning("Required components missing.");
                enabled = false;
                return;
            }

            if (listener == null)
            {
                listener = new GazeListener(subscriptionsController);
                listener.OnReceive3dGaze += Forward3dGaze;
            }
            
            listener.Enable();            
        }

        void OnDisable()
        {
            if (listener != null)
            {
                listener.Disable();
            }
        }

        void Forward3dGaze(GazeData data)
        {
            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze(data);
            }
        }
    }
}
