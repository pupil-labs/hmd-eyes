using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class GazeData
    {

        public enum GazeMappingContext
        {
            Monocular_0,
            Monocular_1,
            Binocular
        }

        /// <summary>
        /// MappingContext refers to GazeData being based on binocular or monocular mapping. 
        /// It also indicates the availability of EyeCenter/GazeNormal:
        /// Binocular for both eyes or Monocular for the corresponding eye.
        /// </summary>
        public GazeMappingContext MappingContext { get; private set; }

        /// <summary>
        /// Confidence of the pupil detection and 3d representation of the eye(s).
        /// Used to filter data sets with low confidence (below ~0.6). 
        /// </summary>
        public float Confidence { get; private set; }
        /// <summary>
        /// Pupil time in seconds. 
        /// </summary>
        public double PupilTimestamp { get; private set; }

        /// <summary>
        /// Gaze direction corresponding to the 3d gaze point.
        /// Normalized vector in local camera space. 
        /// </summary>
        public Vector3 GazeDirection { get; private set; }
        /// <summary>
        /// Distance in meters between VR camera and 3d gaze point.
        /// </summary>
        public float GazeDistance { get; private set; }

        /// <summary>
        /// 3d gaze point in local camera space. 
        /// Recommended to use equivalent representation as GazeDirection plus GazeDistance,
        /// as this clearly sperates the angular error from the depth error.
        /// </summary>
        [System.Obsolete("Using the data field GazePoint3d is not recommended. Use GazeDirection and GazeDistance instead.")]
        public Vector3 GazePoint3d { get { return gazePoint3d; } }

        /// <summary> 
        /// Backprojection into viewport, based on camera intrinsics set in Pupil Capture.
        /// Not available with Pupil Service.
        /// </summary>
        public Vector2 NormPos { get; private set; }

        /// <summary>
        /// 3d coordinate of eye center 0 in local camera space. By default eye index 0 corresponds to the right eye.
        /// </summary>
        public Vector3 EyeCenter0 { get { return CheckAvailability(0) ? eyeCenter0 : Vector3.zero; } }
        /// <summary>
        /// 3d coordinate of eye center 1 in local camera space. By default eye index 1 corresponds to the left eye.
        /// </summary>
        public Vector3 EyeCenter1 { get { return CheckAvailability(1) ? eyeCenter1 : Vector3.zero; } }

        /// <summary>
        /// Gaze vector of eye 0 in local camera space. By default eye index 0 corresponds to the right eye.
        /// </summary>
        public Vector3 GazeNormal0 { get { return CheckAvailability(0) ? gazeNormal0 : Vector3.zero; } }
        /// <summary>
        /// Gaze vector of eye 1 in local camera space. By default eye index 1 corresponds to the left eye.
        /// </summary>
        public Vector3 GazeNormal1 { get { return CheckAvailability(1) ? gazeNormal1 : Vector3.zero; } }

        private Vector3 gazePoint3d;
        private Vector3 eyeCenter0, eyeCenter1;
        private Vector3 gazeNormal0, gazeNormal1;

        public GazeData(string topic, Dictionary<string, object> dictionary)
        {
            Parse(topic, dictionary);
        }

        /// <summary>
        /// Check availability of EyeCenter/GazeNormal for corresponding eye. 
        /// </summary>
        public bool IsEyeDataAvailable(int eyeIdx)
        {
            return MappingContext == (GazeMappingContext)eyeIdx || MappingContext == GazeMappingContext.Binocular;
        }

        /// <summary>
        /// Parameterized version of EyeCenter0/1
        /// </summary>
        public Vector3 GetEyeCenter(int eyeIdx)
        {
            if (eyeIdx == 0)
            {
                return EyeCenter0;
            }
            else if (eyeIdx == 1)
            {
                return EyeCenter1;
            }
            else
            {
                Debug.LogWarning($"EyeIdx of {eyeIdx} not supported. Valid options: 0 or 1");
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Parameterized version of GazeNormal0/1
        /// </summary>
        public Vector3 GetGazeNormal(int eyeIdx)
        {
            if (eyeIdx == 0)
            {
                return GazeNormal0;
            }
            else if (eyeIdx == 1)
            {
                return GazeNormal1;
            }
            else
            {
                Debug.LogWarning($"EyeIdx of {eyeIdx} not supported. Valid options: 0 or 1");
                return Vector3.zero;
            }
        }

        private void Parse(string topic, Dictionary<string, object> dictionary)
        {
            if (topic == "gaze.3d.01.")
            {
                MappingContext = GazeMappingContext.Binocular;
            }
            else if (topic == "gaze.3d.0.")
            {
                MappingContext = GazeMappingContext.Monocular_0;
            }
            else if (topic == "gaze.3d.1.")
            {
                MappingContext = GazeMappingContext.Monocular_1;
            }
            else
            {
                Debug.LogError("GazeData with no matching mode");
                return;
            }

            Confidence = Helpers.FloatFromDictionary(dictionary, "confidence");
            PupilTimestamp = Helpers.DoubleFromDictionary(dictionary, "timestamp");

            if (dictionary.ContainsKey("norm_pos"))
            {
                NormPos = Helpers.Position(dictionary["norm_pos"], false);
            }

            gazePoint3d = ExtractAndParseGazePoint(dictionary);
            GazeDirection = gazePoint3d.normalized;
            GazeDistance = gazePoint3d.magnitude;

            if (MappingContext == GazeMappingContext.Binocular || MappingContext == GazeMappingContext.Monocular_0)
            {
                eyeCenter0 = ExtractEyeCenter(dictionary, MappingContext, 0);
                gazeNormal0 = ExtractGazeNormal(dictionary, MappingContext, 0);
            }
            if (MappingContext == GazeMappingContext.Binocular || MappingContext == GazeMappingContext.Monocular_1)
            {
                eyeCenter1 = ExtractEyeCenter(dictionary, MappingContext, 1);
                gazeNormal1 = ExtractGazeNormal(dictionary, MappingContext, 1);
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

        private Vector3 ExtractEyeCenter(Dictionary<string, object> dictionary, GazeMappingContext context, byte eye)
        {

            object vecObj;
            if (context == GazeMappingContext.Binocular)
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

        private Vector3 ExtractGazeNormal(Dictionary<string, object> dictionary, GazeMappingContext context, byte eye)
        {

            object vecObj;
            if (context == GazeMappingContext.Binocular)
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
            if (!IsEyeDataAvailable(eyeIdx))
            {
                Debug.LogWarning("Data not available in this GazeData set. Please check GazeData.IsEyeDataAvailable or GazeData.MappingContext first.");
                return false;
            }

            return true;
        }
    }
}