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

            pd.Id = Helpers.StringFromDictionary(dictionary, "id");
            pd.Confidence = Helpers.FloatFromDictionary(dictionary, "confidence");
            pd.Method = Helpers.StringFromDictionary(dictionary, "method");

            pd.PupilTimestamp = Helpers.DoubleFromDictionary(dictionary, "timestamp");
            pd.UnityTimestamp = requestCtrl.ConvertToUnityTime(pd.PupilTimestamp);
            // pd.Index = Helpers.IntFromDictionary(dictionary, "index");

            pd.NormPos = Helpers.ObjectToVector(dictionary["norm_pos"]);
            pd.Diameter = Helpers.FloatFromDictionary(dictionary, "diameter");

            Debug.Log(Helpers.TopicsForDictionary(dictionary));
            // Debug.Log(Helpers.TopicsForDictionary());
            // foreach(var obj in (dictionary["circle_3d"] as Dictionary<object, object>).Keys)
            // {
            //     Debug.Log((string)obj);
            // }

            return pd;
            // //+2d gaze mapping
            // pd.EllipseCenter2d = Helpers.ObjectToVector(dictionary["2d_ellipse_center"]);
            // pd.EllipseAxis = Helpers.ObjectToVector(dictionary["2d_ellipse_axis"]);
            // pd.EllipseAngle2d = Helpers.FloatFromDictionary(dictionary, "2d_ellipse_angle");

            // //+3d gaze mapping
            // pd.ModelId = Helpers.IntFromDictionary(dictionary, "model_id");
            // pd.ModelConfidence = Helpers.FloatFromDictionary(dictionary, "model_confidence");
            // pd.Diameter3d = Helpers.FloatFromDictionary(dictionary, "diameter_3d ");

            // pd.SphereCenter = Helpers.ObjectToVector(dictionary["sphere_center"]);
            // pd.SphereRadius = Helpers.FloatFromDictionary(dictionary, "sphere_radius");
            
            // pd.CircleCenter3d = Helpers.ObjectToVector(dictionary["circle_3d_center"]);
            // pd.CircleNormal3d = Helpers.ObjectToVector(dictionary["circle_3d_normal"]);
            // pd.CircleRadius3d = Helpers.FloatFromDictionary(dictionary, "circle_3d_radius");
            // pd.Theta = Helpers.FloatFromDictionary(dictionary, "theta");
            // pd.Phi = Helpers.FloatFromDictionary(dictionary, "phi");

            // pd.ProjectedSphereCenter = Helpers.ObjectToVector(dictionary["projected_sphere_center"]);
            // pd.ProjectedSphereAxis = Helpers.ObjectToVector(dictionary["projected_sphere_axis"]);
            // pd.ProjectedSphereAngle = Helpers.FloatFromDictionary(dictionary, "projected_sphere_angle");

            // return pd;
        }
    }
}