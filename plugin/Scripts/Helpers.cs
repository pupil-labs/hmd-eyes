using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class Helpers
    {
        public static float PupilUnitScalingFactor = 1000;	// Pupil is currently operating in mm
        public const string leftEyeID = "1";
        public const string rightEyeID = "0";

        private static object[] position_o;
        public static Vector3 ObjectToVector(object source)
        {
            position_o = source as object[];
            Vector3 result = Vector3.zero;
            if (position_o.Length != 2 && position_o.Length != 3)
                Debug.Log("Array length not supported");
            else
            {
                result.x = (float)(double)position_o[0];
                result.y = (float)(double)position_o[1];
                if (position_o.Length == 3)
                    result.z = (float)(double)position_o[2];
            }
            return result;
        }
        public static Vector3 Position(object position, bool applyScaling)
        {
            Vector3 result = ObjectToVector(position);
            if (applyScaling)
                result /= PupilUnitScalingFactor;
            return result;
        }

        public static Vector3 VectorFromDictionary(Dictionary<string, object> source, string key)
        {
            if (source.ContainsKey(key))
                return Position(source[key], false);
            else
                return Vector3.zero;
        }

        public static int IntFromDictionary(Dictionary<string, object> source, string key)
        {
            source.TryGetValue(key, out object value_o);
            return (int)value_o;
        }

        public static float FloatFromDictionary(Dictionary<string, object> source, string key)
        {
            return (float)DoubleFromDictionary(source, key);
        }

        public static double DoubleFromDictionary(Dictionary<string, object> source, string key)
        {
            object value_o;
            source.TryGetValue(key, out value_o);
            return (double)value_o;
        }

        public static double TryCastToDouble(object obj)
        {
            Double? d = obj as Double?;
            if (d.HasValue)
            {
                return d.Value;
            }
            else
            {
                return 0f;
            }
        }

        private static object IDo;
        public static string StringFromDictionary(Dictionary<string, object> source, string key)
        {
            string result = "";
            if (source.TryGetValue(key, out IDo))
                result = IDo.ToString();
            return result;
        }

        public static Dictionary<object, object> DictionaryFromDictionary(Dictionary<string, object> source, string key)
        {
            if (source.ContainsKey(key))
                return source[key] as Dictionary<object, object>;
            else
                return null;
        }

        public static string TopicsForDictionary(Dictionary<string, object> dictionary)
        {
            string topics = "";
            foreach (var key in dictionary.Keys)
                topics += key + ",";
            return topics;
        }

        public static Dictionary<object, object> BaseData(Dictionary<string, object> dictionary)
        {
            object o;
            dictionary.TryGetValue("base_data", out o);
            return o as Dictionary<object, object>;
        }
    }
}