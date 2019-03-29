using UnityEngine;

namespace PupilLabs.Demos
{
    public class SetupOnSceneLoad : MonoBehaviour
    {
        public GazeVisualizer gazeVisualizer;

        void Start(){
            SubscriptionsController subsController = FindObjectOfType<SubscriptionsController>();

            if (subsController == null)
            {
                Debug.LogWarning("No SubscriptionController found. Missing 'DontDestory' on Pupil Connection object?");
                return;
            }

            gazeVisualizer.subscriptionsController = subsController;
            gazeVisualizer.enabled = true;
        }
    }
}
