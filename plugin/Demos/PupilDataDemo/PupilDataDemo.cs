using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PupilLabs.Demos
{

    public class PupilDataDemo : MonoBehaviour
    {
        public SubscriptionsController subsCtrl;
        public Text statusText;

        private RequestController requestCtrl;

        void Awake()
        {
            requestCtrl = subsCtrl.requestCtrl;
        }

        void OnEnable()
        {
            requestCtrl.OnConnected += StartPupilSubscription;
            requestCtrl.OnDisconnecting += StopPupilSubscription;

            if (requestCtrl.IsConnected)
            {
                StartPupilSubscription();
            }
        }

        void OnDisable()
        {
            requestCtrl.OnConnected -= StartPupilSubscription;
            requestCtrl.OnDisconnecting -= StopPupilSubscription;

            if (requestCtrl.IsConnected)
            {
                StopPupilSubscription();
            }
        }

        void Update()
        {
            if (statusText == null) { return; }

            statusText.text = requestCtrl.IsConnected ? "Connected" : "Not connected";

            if (requestCtrl.IsConnected)
            {
                statusText.text += "\n ... but nothing happening here. \nPlease check the console and have a look at the source code to get started.";
            }
        }

        void StartPupilSubscription()
        {
            Debug.Log("StartPupilSubscription");

            subsCtrl.SubscribeTo("pupil", CustomReceiveData);
        }

        void StopPupilSubscription()
        {
            Debug.Log("StopPupilSubscription");

            subsCtrl.UnsubscribeFrom("pupil", CustomReceiveData);
        }

        void CustomReceiveData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            Debug.Log($"Pupil Data received ({topic}) with confidence {dictionary["confidence"]}");
            foreach (var item in dictionary)
            {
                switch (item.Key)
                {
                    case "topic":
                    case "method":
                    case "id":
                        var textForKey = Helpers.StringFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "confidence":
                    case "timestamp":
                    case "diameter":
                        var valueForKey = Helpers.FloatFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "norm_pos":
                        var positionForKey = Helpers.VectorFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "ellipse":
                        var dictionaryForKey = Helpers.DictionaryFromDictionary(dictionary, item.Key);
                        foreach (var pupilEllipse in dictionaryForKey)
                        {
                            switch (pupilEllipse.Key.ToString())
                            {
                                case "angle":
                                    var angle = (float)(double)pupilEllipse.Value;
                                    // Do stuff
                                    break;
                                case "center":
                                case "axes":
                                    var vector = PupilLabs.Helpers.ObjectToVector(pupilEllipse.Value);
                                    // Do stuff
                                    break;
                                default:
                                    break;
                            }
                        }
                        // Do stuff
                        break;
                    default:
                        break;
                }
            }
        }
    }
}