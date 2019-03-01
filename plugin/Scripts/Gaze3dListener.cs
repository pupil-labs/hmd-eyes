using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{   
    public class GazeData{

        public enum Mode
        {
            Monocular_0,
            Monocular_1,
            Binocular
        }

        public Mode mode;
        public float confidence;
        public float timestamp; //TODO float in s?

        public Vector2 normPos; //in camera viewport space
        public Vector3 gazePoint3d; //in local camera space
        
        public Vector3 eyeCenter0, eyeCenter1;
        public Vector3 gazeNormal0, gazeNormal1;

        public GazeData(string topic, Dictionary<string, object> dictionary)
        {

            //mode
            if (topic == "gaze.3d.01.")
            {
                mode = Mode.Binocular;
            }
            else if (topic == "gaze.3d.0.")
            {   
                mode = Mode.Monocular_0;
            }
            else if (topic == "gaze.3d.1.")
            {
                mode = Mode.Monocular_1;
            }
            else
            {
                Debug.LogError("GazeData with no matching mode");
                return;
            }
            
            confidence = Helpers.FloatFromDictionary(dictionary, "confidence");       
            timestamp = Helpers.FloatFromDictionary(dictionary, "timestamp");

            normPos = Helpers.Position(dictionary["norm_pos"], false);
           
            gazePoint3d = Helpers.Position (dictionary ["gaze_point_3d"], true);
            gazePoint3d.y *= -1f;    // Pupil y axis is inverted        

            if (mode == Mode.Binocular || mode == Mode.Monocular_0)
            {
                eyeCenter0 = ExtractEyeCenter(dictionary,mode,0);
                gazeNormal0 = ExtractGazeNormal(dictionary,mode,0);
            }
            if (mode == Mode.Binocular || mode == Mode.Monocular_1)
            {
                eyeCenter1 = ExtractEyeCenter(dictionary,mode,1);
                gazeNormal1 = ExtractGazeNormal(dictionary,mode,1);
            }
        }

        private Vector3 ExtractEyeCenter(Dictionary<string, object> dictionary,Mode mode,int eye)
        {

            object vecObj;
            if (mode == Mode.Binocular)
            {
                var binoDic = dictionary["eye_centers_3d"] as Dictionary<string,object>;
                vecObj = binoDic[eye.ToString("d")];
            }
            else
            {
                vecObj = dictionary["eye_center_3d"];
            }
            return Helpers.Position(vecObj,false);
        }

        private Vector3 ExtractGazeNormal(Dictionary<string, object> dictionary,Mode mode,int eye)
        {

            object vecObj;
            if (mode == Mode.Binocular)
            {
                var binoDic = dictionary["gaze_normals_3d"] as Dictionary<string,object>;
                vecObj = binoDic[eye.ToString("d")];
            }
            else
            {
                vecObj = dictionary["gaze_normal_3d"];
            }
            return Helpers.Position(vecObj,false);
        }
    }


    public class Gaze3dListener
    {

        public delegate void Receive3dGazeDel(GazeData gazeData);
        public event Receive3dGazeDel OnReceive3dGaze;
        
        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public Gaze3dListener(SubscriptionsController subsCtrl)
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

        ~Gaze3dListener()
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

            subsCtrl.SubscribeTo("gaze.3d", Receive3DGaze);
        }

        public void Disable()
        {
            Debug.Log("Disabling Gaze Listener");

            subsCtrl.UnsubscribeFrom("gaze.3d", Receive3DGaze);
        }

        void Receive3DGaze(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            
            GazeData gazeData = new GazeData(topic, dictionary);

            if (OnReceive3dGaze != null)
            {
                OnReceive3dGaze(gazeData);
            }
        }
    }
}
