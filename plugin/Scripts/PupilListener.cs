using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilListener
    {

        public event Action<PupilData> OnReceivePupilData;

        private RequestController requestCtrl;
        private SubscriptionsController subsCtrl;

        public PupilListener(SubscriptionsController subsCtrl)
        {
            this.subsCtrl = subsCtrl;
            this.requestCtrl = subsCtrl.requestCtrl;

            requestCtrl.OnConnected += Enable;
            requestCtrl.OnDisconnecting += Disable;

            if (subsCtrl.IsConnected)
            {
                Enable();
            }
        }

        ~PupilListener()
        {
            requestCtrl.OnConnected -= Enable;
            requestCtrl.OnDisconnecting -= Disable;

            if (subsCtrl.IsConnected)
            {
                Disable();
            }
        }

        public void Enable()
        {
            subsCtrl.SubscribeTo("pupil.", ReceivePupilData);
        }

        public void Disable()
        {
            subsCtrl.UnsubscribeFrom("pupil.", ReceivePupilData);
        }

        void ReceivePupilData(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame = null)
        {
            PupilData pupilData = ParseMsg(dictionary);

            if (OnReceivePupilData != null)
            {
                OnReceivePupilData(pupilData);
            }
        }

        PupilData ParseMsg(Dictionary<string, object> dictionary)
        {
            PupilData pd = new PupilData();

            pd.EyeIdx = Int32.Parse(Helpers.StringFromDictionary(dictionary, "id"));
            pd.Confidence = Helpers.FloatFromDictionary(dictionary, "confidence");
            pd.Method = Helpers.StringFromDictionary(dictionary, "method");
            
            pd.PupilTimestamp = Helpers.DoubleFromDictionary(dictionary, "timestamp");
            pd.UnityTimestamp = requestCtrl.ConvertToUnityTime(pd.PupilTimestamp);

            pd.NormPos = Helpers.ObjectToVector(dictionary["norm_pos"]);
            pd.Diameter = Helpers.FloatFromDictionary(dictionary, "diameter");

            //+2d gaze mapping
            if (pd.Method.Contains("2d") || pd.Method.Contains("3d"))
            {
                TryExtractEllipse(dictionary, pd);
            }

            //+3d gaze mapping
            if (pd.Method.Contains("3d"))
            {
                pd.ModelId = Helpers.StringFromDictionary(dictionary, "model_id");
                pd.ModelConfidence = Helpers.FloatFromDictionary(dictionary, "model_confidence");
                pd.ModelBirthTimestamp = Helpers.DoubleFromDictionary(dictionary, "model_birth_timestamp");
                pd.Diameter3d = Helpers.FloatFromDictionary(dictionary, "diameter_3d");

                TryExtractCircle3d(dictionary, pd);
                ExtractSphericalCoordinates(dictionary, pd);
                
                TryExtractSphere(dictionary, pd);
                TryExtractProjectedSphere(dictionary, pd);
            }

            return pd;
        }

        bool TryExtractEllipse(Dictionary<string,object> dictionary, PupilData pupilData)
        {
            Dictionary<object,object> subDic = Helpers.DictionaryFromDictionary(dictionary,"ellipse");
            if (subDic == null)
            {
                return false;
            }

            pupilData.EllipseCenter = Helpers.ObjectToVector(subDic["center"]);
            pupilData.EllipseAxis = Helpers.ObjectToVector(subDic["axes"]);
            pupilData.EllipseAngle = (float)(double)subDic["angle"];

            return true;
        }

        bool TryExtractCircle3d(Dictionary<string,object> dictionary, PupilData pupilData)
        {
            Dictionary<object,object> subDic = Helpers.DictionaryFromDictionary(dictionary,"circle_3d");

            if (subDic == null)
            {
                return false;
            }

            pupilData.CircleCenter = Helpers.ObjectToVector(subDic["center"]);
            pupilData.CircleNormal = Helpers.ObjectToVector(subDic["normal"]);
            pupilData.CircleRadius = (float)(double)subDic["radius"];

            return true;
        }

        bool TryExtractSphere(Dictionary<string,object> dictionary, PupilData pupilData)
        {
            Dictionary<object,object> subDic = Helpers.DictionaryFromDictionary(dictionary,"sphere");

            if (subDic == null)
            {
                return false;
            }

            pupilData.SphereCenter = Helpers.ObjectToVector(subDic["center"]);
            pupilData.SphereRadius = (float)(double)subDic["radius"];

            return true;
        }

        bool TryExtractProjectedSphere(Dictionary<string,object> dictionary, PupilData pupilData)
        {
            Dictionary<object,object> subDic = Helpers.DictionaryFromDictionary(dictionary,"projected_sphere");

            if (subDic == null)
            {
                return false;
            }

            pupilData.ProjectedSphereCenter = Helpers.ObjectToVector(subDic["center"]);
            pupilData.ProjectedSphereAxes = Helpers.ObjectToVector(subDic["axes"]);
            pupilData.ProjectedSphereAngle = (float)(double)subDic["angle"];

            return true;
        }

        void ExtractSphericalCoordinates(Dictionary<string,object> dictionary, PupilData pupilData)
        {
            // if circle normals are not available -> theta&phi are no doubles

            // pd.Theta = Helpers.FloatFromDictionary(dictionary, "theta");
            pupilData.Theta = CastToFloat(dictionary["theta"]);

            // pd.Phi = Helpers.FloatFromDictionary(dictionary, "phi");
            pupilData.Phi = CastToFloat(dictionary["phi"]);
        }

        float CastToFloat(object obj)
        {
            Double? d = obj as Double?;
            if (d.HasValue)
            {
                return (float)d.Value;
            }
            else
            {
                return 0f;
            }
        }
    }
}