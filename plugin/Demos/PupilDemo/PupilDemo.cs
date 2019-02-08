using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs.Demos
{

    public class PupilDemo : MonoBehaviour
    {
        public PupilLabs.Stream stream;

        void OnEnable()
        {
            stream.OnConnected += StartPupilSubscription;
            stream.OnDisconnecting += StopPupilSubscription;

            if (stream.IsConnected)
            {
                StartPupilSubscription();
            }
        }

        void OnDisable()
        {
            stream.OnConnected -= StartPupilSubscription;
            stream.OnDisconnecting -= StopPupilSubscription;

            if (stream.IsConnected)
            {
                StopPupilSubscription();
            }
        }

        void StartPupilSubscription()
        {

            Debug.Log("StartPupilSubscription");

            stream.InitializeSubscriptionSocket("pupil.", CustomReceiveData);
        }

        void StopPupilSubscription()
        {

            Debug.Log("StopPupilSubscription");

            stream.CloseSubscriptionSocket("pupil.", CustomReceiveData);
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