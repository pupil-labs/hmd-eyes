using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeData
    {

        public enum GazeDataMode
        {
            Monocular_0,
            Monocular_1,
            Binocular
        }

        public GazeDataMode Mode { get; }
        public float Confidence { get; }
        public float Timestamp { get; }

        public Vector2 NormPos { get; } //in camera viewport space
        public Vector3 GazeDirection { get; }
        public float GazeDistance { get; }
        [System.Obsolete("GazePoint3d is deprecated. Use GazeDirection (and GazeDistance) instead.")]
        public Vector3 GazePoint3d { get { return gazePoint3d; } } //in local camera space

        public Vector3 EyeCenter0 { get { return CheckAvailability(0) ? eyeCenter0 : Vector3.zero; } }
        public Vector3 EyeCenter1 { get { return CheckAvailability(1) ? eyeCenter1 : Vector3.zero; } }
        public Vector3 GazeNormal0 { get { return CheckAvailability(0) ? gazeNormal0 : Vector3.zero; } }
        public Vector3 GazeNormal1 { get { return CheckAvailability(1) ? gazeNormal1 : Vector3.zero; } }

        private Vector3 gazePoint3d;
        private Vector3 eyeCenter0, eyeCenter1;
        private Vector3 gazeNormal0, gazeNormal1;

        public GazeData(string topic, Dictionary<string, object> dictionary)
        {

            if (topic == "gaze.3d.01.")
            {
                Mode = GazeDataMode.Binocular;
            }
            else if (topic == "gaze.3d.0.")
            {
                Mode = GazeDataMode.Monocular_0;
            }
            else if (topic == "gaze.3d.1.")
            {
                Mode = GazeDataMode.Monocular_1;
            }
            else
            {
                Debug.LogError("GazeData with no matching mode");
                return;
            }

            Confidence = Helpers.FloatFromDictionary(dictionary, "confidence");
            Timestamp = Helpers.FloatFromDictionary(dictionary, "timestamp");

            NormPos = Helpers.Position(dictionary["norm_pos"], false);
            gazePoint3d = ExtractAndParseGazePoint(dictionary);
            GazeDirection = gazePoint3d.normalized;
            GazeDistance = gazePoint3d.magnitude;
            
            if (Mode == GazeDataMode.Binocular || Mode == GazeDataMode.Monocular_0)
            {
                eyeCenter0 = ExtractEyeCenter(dictionary, Mode, 0);
                gazeNormal0 = ExtractGazeNormal(dictionary, Mode, 0);
            }
            if (Mode == GazeDataMode.Binocular || Mode == GazeDataMode.Monocular_1)
            {
                eyeCenter1 = ExtractEyeCenter(dictionary, Mode, 1);
                gazeNormal1 = ExtractGazeNormal(dictionary, Mode, 1);
            }
        }

        private Vector3 ExtractAndParseGazePoint(Dictionary<string, object> dictionary)
        {
            Vector3 gazePos = Helpers.Position(dictionary["gaze_point_3d"], true);
            gazePos.y *= -1f;    // Pupil y axis is inverted    

            // correct/flip pos if behind viewer
            float angle = Vector3.Angle(Vector3.forward, gazePos);
            if (angle >= 90f)
            {
                gazePos *= -1f;
            }

            return gazePos;
        }

        private Vector3 ExtractEyeCenter(Dictionary<string, object> dictionary, GazeDataMode mode, byte eye)
        {

            object vecObj;
            if (mode == GazeDataMode.Binocular)
            {
                var binoDic = dictionary["eye_centers_3d"] as Dictionary<object, object>;
                vecObj = binoDic[eye];
            }
            else
            {
                vecObj = dictionary["eye_center_3d"];
            }
            Vector3 eyeCenter = Helpers.Position(vecObj, true);
            eyeCenter.y *= -1;
            return eyeCenter;
        }

        private Vector3 ExtractGazeNormal(Dictionary<string, object> dictionary, GazeDataMode mode, byte eye)
        {

            object vecObj;
            if (mode == GazeDataMode.Binocular)
            {
                var binoDic = dictionary["gaze_normals_3d"] as Dictionary<object, object>;
                vecObj = binoDic[eye];
            }
            else
            {
                vecObj = dictionary["gaze_normal_3d"];
            }
            Vector3 gazeNormal = Helpers.Position(vecObj, false);
            gazeNormal.y *= -1f;
            return gazeNormal;
        }

        private bool CheckAvailability(int eyeIdx)
        {
            if (Mode != (GazeDataMode)eyeIdx && Mode != GazeDataMode.Binocular)
            {
                Debug.LogWarning("Data not available in this GazeData set. Please check GazeData.Mode first.");
                return false;
            }

            return true;
        }
    }
}