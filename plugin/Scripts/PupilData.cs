using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilData
    {
        public int EyeIdx { get; private set; } //0 or 1 for right/left eye
        public float Confidence { get; private set; } // - is an assessment by the pupil detector on how sure we can be on this measurement. A value of 0 indicates no confidence. 1 indicates perfect confidence. In our experience useful data carries a confidence value greater than ~0.6. A confidence of exactly 0 means that we don’t know anything. So you should ignore the position data.
        public string Method { get; private set; } // indicates what detector was used to detect the pupil

        public double PupilTimestamp { get; private set; } // timestamp of the source image frame
        public double UnityTimestamp { get; private set; }

        public Vector2 NormPos { get; private set; } // position in the eye image frame in normalized coordinates
        public float Diameter { get; private set; } // diameter of the pupil in image pixels as observed in the eye image frame (is not corrected for perspective)

        //2d
        public class PupilEllipse
        {
            public Vector2 Center { get; set; } // center of the pupil in image pixels
            public Vector2 Axis { get; set; } // first and second axis of the pupil ellipse in pixels
            public float Angle { get; set; } // angle of the ellipse in degrees
        }
        public PupilEllipse Ellipse { get; private set; } = new PupilEllipse();

        //3d
        public float ModelConfidence { get; private set; } //- confidence of the current eye model (0-1)
        public string ModelId { get; private set; } //- id of the current eye model. When a slippage is detected the model is replaced and the id changes.
        public double ModelBirthTimestamp { get; private set; }

        public float Diameter3d { get; private set; } //- diameter of the pupil scaled to mm based on anthropomorphic avg eye ball diameter and corrected for perspective.

        public class EyeSphere
        {
            public Vector3 Center { get; set; } // pos of the eyeball sphere is eye pinhole camera 3d space units are scaled to mm.
            public float Radius { get; set; } // radius of the eyeball. This is always 12mm (the anthropomorphic avg.) We need to make this assumption because of the single camera scale ambiguity.
        }
        public EyeSphere Sphere { get; private set; } = new EyeSphere();

        public class PupilCircle
        {
            public Vector3 Center { get; set; } // center of the pupil as 3d circle in eye pinhole camera 3d space units are mm.
            public Vector3 Normal { get; set; } // normals of the pupil as 3d circle. Indicates the direction that the pupil points at in 3d space.
            public float Radius { get; set; } // radius of the pupil as 3d circle. Same as diameter_3d
            public float Theta { get; set; } // CircleNormal3d described in spherical coordinates in radians
            public float Phi { get; set; } // CircleNormal3d described in spherical coordinates in radians
        }
        public PupilCircle Circle { get; private set; } = new PupilCircle();

        public class ProjectedEyeSphere
        {
            public Vector2 Center { get; set; } // center of the 3d sphere projected back onto the eye image frame. Units are in image pixels.
            public Vector2 Axis { get; set; } // first and second axis of the 3d sphere projection.
            public float Angle { get; set; } // angle of the 3d sphere projection. Units are degrees.
        }
        public ProjectedEyeSphere ProjectedSphere { get; private set; } = new ProjectedEyeSphere();

        public PupilData(Dictionary<string, object> dictionary, double unityToPupilTimeOffset)
        {
            ParseDictionary(dictionary, unityToPupilTimeOffset);
        }

        void ParseDictionary(Dictionary<string, object> dictionary, double unityToPupilTimeOffset)
        {
            EyeIdx = System.Int32.Parse(Helpers.StringFromDictionary(dictionary, "id"));
            Confidence = Helpers.FloatFromDictionary(dictionary, "confidence");
            Method = Helpers.StringFromDictionary(dictionary, "method");

            PupilTimestamp = Helpers.DoubleFromDictionary(dictionary, "timestamp");
            UnityTimestamp = PupilTimestamp - unityToPupilTimeOffset;

            NormPos = Helpers.ObjectToVector(dictionary["norm_pos"]);
            Diameter = Helpers.FloatFromDictionary(dictionary, "diameter");

            //+2d
            if (Method.Contains("2d") || Method.Contains("3d"))
            {
                TryExtractEllipse(dictionary);
            }

            //+3d
            if (Method.Contains("3d"))
            {
                ModelId = Helpers.StringFromDictionary(dictionary, "model_id");
                ModelConfidence = Helpers.FloatFromDictionary(dictionary, "model_confidence");
                ModelBirthTimestamp = Helpers.DoubleFromDictionary(dictionary, "model_birth_timestamp");
                Diameter3d = Helpers.FloatFromDictionary(dictionary, "diameter_3d");

                TryExtractCircle3d(dictionary);
                ExtractSphericalCoordinates(dictionary);

                TryExtractSphere(dictionary);
                TryExtractProjectedSphere(dictionary);
            }
        }

        bool TryExtractEllipse(Dictionary<string, object> dictionary)
        {
            Dictionary<object, object> subDic = Helpers.DictionaryFromDictionary(dictionary, "ellipse");
            if (subDic == null)
            {
                return false;
            }

            Ellipse.Center = Helpers.ObjectToVector(subDic["center"]);
            Ellipse.Axis = Helpers.ObjectToVector(subDic["axes"]);
            Ellipse.Angle = (float)(double)subDic["angle"];

            return true;
        }

        bool TryExtractCircle3d(Dictionary<string, object> dictionary)
        {
            Dictionary<object, object> subDic = Helpers.DictionaryFromDictionary(dictionary, "circle_3d");

            if (subDic == null)
            {
                return false;
            }

            Circle.Center = Helpers.ObjectToVector(subDic["center"]);
            Circle.Normal = Helpers.ObjectToVector(subDic["normal"]);
            Circle.Radius = (float)(double)subDic["radius"];

            return true;
        }

        bool TryExtractSphere(Dictionary<string, object> dictionary)
        {
            Dictionary<object, object> subDic = Helpers.DictionaryFromDictionary(dictionary, "sphere");

            if (subDic == null)
            {
                return false;
            }

            Sphere.Center = Helpers.ObjectToVector(subDic["center"]);
            Sphere.Radius = (float)(double)subDic["radius"];

            return true;
        }

        bool TryExtractProjectedSphere(Dictionary<string, object> dictionary)
        {
            Dictionary<object, object> subDic = Helpers.DictionaryFromDictionary(dictionary, "projected_sphere");

            if (subDic == null)
            {
                return false;
            }

            ProjectedSphere.Center = Helpers.ObjectToVector(subDic["center"]);
            ProjectedSphere.Axis = Helpers.ObjectToVector(subDic["axes"]);
            ProjectedSphere.Angle = (float)(double)subDic["angle"];

            return true;
        }

        void ExtractSphericalCoordinates(Dictionary<string, object> dictionary)
        {
            // if circle normals are not available -> theta&phi are no doubles
            Circle.Theta = (float)Helpers.TryCastToDouble(dictionary["theta"]);
            Circle.Phi = (float)Helpers.TryCastToDouble(dictionary["phi"]);
        }
    }
}
