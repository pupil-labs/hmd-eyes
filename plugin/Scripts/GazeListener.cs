using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeListener
    {

        public delegate void Receive2dDel(string id, Vector3 pos, float confidence);
        public delegate void Receive3dGazeTargetDel(Vector3 gazeTarget, float confidence);
        public delegate void Receive3dGazeVectorDel(string id, Vector3 eyeCenter, Vector3 gazeNormals);
        public event Receive2dDel OnReceive2dGazeTarget;
        public event Receive3dGazeTargetDel OnReceive3dGazeTarget;
        public event Receive3dGazeVectorDel OnReceive3dGazeVevctor;

        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public GazeListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
            this.requestCtrl = subsCtrl.requestCtrl;

            requestCtrl.OnConnected += Enable;
            requestCtrl.OnDisconnecting += Disable;

            if (requestCtrl.IsConnected)
            {
                Enable();
            }
        }

        ~GazeListener()
        {
            requestCtrl.OnConnected -= Enable;
            requestCtrl.OnDisconnecting -= Disable;

            if (requestCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            Debug.Log("Enabling Gaze Listener");

            subsCtrl.SubscribeTo("gaze.2d", Process2DGaze);
            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
        }

        public void Disable()
        {
            Debug.Log("Disabling Gaze Listener");

            subsCtrl.UnsubscribeFrom("gaze.2d", Process2DGaze);
            subsCtrl.UnsubscribeFrom("gaze.3d", Receive3DGaze);
        }

        void Receive3DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            Debug.Log($"3D Gaze msg topic: {topic}");

            float confidence = 0f;
            string confidenceKey = "confidence";
            if (dictionary.ContainsKey(confidenceKey))
            {
                confidence = Helpers.FloatFromDictionary(dictionary, confidenceKey);
            }
            
            string key = "gaze_point_3d";
            if (dictionary.ContainsKey(key))
            {
                var position3D = Helpers.Position (dictionary [key], true);
                position3D.y *= -1f;    // Pupil y axis is inverted

                
                if (OnReceive3dGazeTarget != null)
                {
                    OnReceive3dGazeTarget(position3D,confidence);
                }
            }

            if (dictionary.ContainsKey("eye_centers_3d") && dictionary.ContainsKey("gaze_normals_3d"))
            {
                // if (dictionary ["eye_centers_3d"] is Dictionary<object,object>)
                //     foreach (var item in (dictionary["eye_centers_3d"] as Dictionary<object,object>))
                //     {
                //         eyeDataKey = key + "_" + item.Key.ToString ();
                //         var position = Position (item.Value, true);
                //         position.y *= -1f;							// Pupil y axis is inverted
                //         PupilData.AddGazeToEyeData (eyeDataKey,position);
                //     }
                // break;
            }

        }

        void Process2DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            string key = "norm_pos";
            if (dictionary.ContainsKey(key))
            {
                string eyeId = Helpers.StringFromDictionary(dictionary, "id");
                var position2D = Helpers.Position(dictionary[key], false);
                //TODO project into world? or naming regarind viewport

                float confidence = 0f;
                string confidenceKey = "confidence";
                if (dictionary.ContainsKey(confidenceKey))
                {
                    confidence = Helpers.FloatFromDictionary(dictionary, confidenceKey);
                }

                if (OnReceive2dGazeTarget != null)
                {
                    OnReceive2dGazeTarget(eyeId, position2D, confidence);
                }
            }
            else
            {
                Debug.LogWarning("2d gaze received without norm_pos data");
            }
        }
    }
}
