using UnityEngine;

namespace PupilLabs.Demos
{
    public class SetupOnSceneLoad : MonoBehaviour
    {
        public GazeController gazeCtrl;

        void Start(){
            SubscriptionsController subsController = FindObjectOfType<SubscriptionsController>();
            if (subsController == null)
            {
                Debug.LogWarning("No SubscriptionController and/or TimeSync found. Missing 'DontDestory' on Pupil Connection object?");
                return;
            }

            gazeCtrl.subscriptionsController = subsController;
            gazeCtrl.enabled = true;
        }
    }
}
