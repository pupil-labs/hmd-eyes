using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs.Demos
{

    public class BlinkDemo : MonoBehaviour
    {
        [SerializeField]
        private PupilLabs.Stream stream;

        public Transform leftEye;
        public Transform rightEye;
        [Range(0.1f, 10)]
        public float blinkDuration = 0.5f;
        private bool blinking = false;

        // Use this for initialization
        void OnEnable()
        {
            stream.OnConnected += StartBlinkSubscription;
            stream.OnDisconnecting += StopBlinkSubscription;

            stream.OnReceiveData += CustomReceiveData;

            //TODO What if already connected?
        }

        void OnDisable()
        {
            stream.OnConnected -= StartBlinkSubscription;
            stream.OnDisconnecting -= StopBlinkSubscription;

            stream.OnReceiveData -= CustomReceiveData;
        }

        void StartBlinkSubscription()
        {
            Debug.Log("StartBlinkSubscription");

            stream.InitializeSubscriptionSocket("blinks");

            stream.Send(new Dictionary<string, object> {
                { "subject", "start_plugin" }
                ,{ "name", "Blink_Detection" }
                ,{
                    "args", new Dictionary<string,object> {
                        { "history_length", 0.2f }
                        ,{ "onset_confidence_threshold", 0.5f }
                        ,{ "offset_confidence_threshold", 0.5f }
                    }
                }
            });
        }

        void StopBlinkSubscription()
        {
            Debug.Log("StopBlinkSubscription");

            stream.Send(new Dictionary<string, object> {
                { "subject","stop_plugin" }
                ,{ "name", "Blink_Detection" }
            });

            stream.CloseSubscriptionSocket("blinks");
        }

        void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            if (topic == "blinks")
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

