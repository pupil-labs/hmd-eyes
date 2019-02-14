using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs.Demos
{

    public class PupilDemo : MonoBehaviour
    {
        public PupilLabs.SubscriptionsController subsCtrl;
        public PupilLabs.RequestController requestCtrl;

        void OnEnable()
        {
            requestCtrl.OnConnected += StartPupilSubscription;
            requestCtrl.OnDisconnecting += StopPupilSubscription;

            if (subsCtrl.IsConnected)
            {
                StartPupilSubscription();
            }
        }

        void OnDisable()
        {
            requestCtrl.OnConnected -= StartPupilSubscription;
            requestCtrl.OnDisconnecting -= StopPupilSubscription;

            if (subsCtrl.IsConnected)
            {
                StopPupilSubscription();
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

            Debug.Log($"Topic {topic} received");
            foreach (var item in dictionary)
            {
                switch (item.Key)
                {
                    case "topic":
                    case "method":
                    case "id":
                        var textForKey = PupilLabs.Helpers.StringFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "confidence":
                    case "timestamp":
                    case "diameter":
                        var valueForKey = PupilLabs.Helpers.FloatFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "norm_pos":
                        var positionForKey = PupilLabs.Helpers.VectorFromDictionary(dictionary, item.Key);
                        // Do stuff
                        break;
                    case "ellipse":
                        var dictionaryForKey = PupilLabs.Helpers.DictionaryFromDictionary(dictionary, item.Key);
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