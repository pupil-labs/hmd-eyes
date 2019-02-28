using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeVisualizer : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;
        public bool showGazeBeforeCalibration = false; //TODO
        
        [Range(0f,0.99f)]
        public float confidenceThreshold = 0.6f;

        GazeListener gazeListener = null;
        

        void OnEnable()
        {
            if (gazeListener == null)
            {
                gazeListener = new GazeListener(subscriptionsController);
            }
        }

        void OnDisable()
        {

        }
    }
}
