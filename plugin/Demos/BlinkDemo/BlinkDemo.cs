using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{

    public class BlinkDemo : MonoBehaviour
    {
        public SubscriptionsController subscriptionsController;
        public Text text;
        public Transform leftEye;
        public Transform rightEye;
        [Range(0.1f, 10)]
        public float blinkDuration = 0.5f;
        
        private RequestController requestCtrl;
        private bool blinking = false;

        void Awake()
        {
            requestCtrl = subscriptionsController.requestCtrl;
        }

        void OnEnable()
        {
            requestCtrl.OnConnected += StartBlinkSubscription;

            if (requestCtrl.IsConnected)
            {
                StartBlinkSubscription();
            }
        }

        void OnDisable()
        {
            requestCtrl.OnConnected -= StartBlinkSubscription;

            if (requestCtrl.IsConnected)
            {
                StopBlinkSubscription();
            }
        }

        void Update()
        {
            if (requestCtrl == null || text == null) { return; }

            text.text = requestCtrl.IsConnected ? "Connected" : "Not connected";

            if (requestCtrl.IsConnected)
            {
                text.text += "\n\nWatch the capsule hero and blink!";
            }
        }

        void StartBlinkSubscription()
        {
            Debug.Log("StartBlinkSubscription");

            subscriptionsController.SubscribeTo("blinks", CustomReceiveData);

            requestCtrl.StartPlugin(
                "Blink_Detection",
                new Dictionary<string, object> {
                    { "history_length", 0.2f },
                    { "onset_confidence_threshold", 0.5f },
                    { "offset_confidence_threshold", 0.5f }
                }
            );
        }

        void StopBlinkSubscription()
        {
            Debug.Log("StopBlinkSubscription");

            requestCtrl.StopPlugin("Blink_Detection");

            subscriptionsController.UnsubscribeFrom("blinks", CustomReceiveData);
        }

        void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            if (dictionary.ContainsKey("timestamp"))
            {
                Debug.Log("Blink detected: " + dictionary["timestamp"].ToString());

                if (!blinking)
                {
                    blinking = true;
                    StartCoroutine(Blink(blinkDuration));
                }
            }
        }

        public IEnumerator Blink(float duration)
        {
            Vector3 leftOldScale = leftEye.localScale;
            Vector3 rightOldScale = rightEye.localScale;


            leftEye.localScale = new Vector3(leftOldScale.x, leftOldScale.y * 0.1f, leftOldScale.z);
            rightEye.localScale = new Vector3(rightOldScale.x, rightOldScale.y * 0.1f, rightOldScale.z);

            yield return new WaitForSecondsRealtime(duration);

            leftEye.localScale = leftOldScale;
            rightEye.localScale = rightOldScale;

            blinking = false;
            yield break;
        }
    }
}

