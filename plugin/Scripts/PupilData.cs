using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class PupilData
    {
        public string Id { get; set; } //0 or 1 for left/right eye
        public float Confidence { get; set; } // - is an assessment by the pupil detector on how sure we can be on this measurement. A value of 0 indicates no confidence. 1 indicates perfect confidence. In our experience useful data carries a confidence value greater than ~0.6. A confidence of exactly 0 means that we don’t know anything. So you should ignore the position data.
        public string Method { get; set; } // indicates what detector was used to detect the pupil

        public double PupilTimestamp { get; set; } // timestamp of the source image frame
        public double UnityTimestamp { get; set; }
        
        public Vector2 NormPos { get; set; } // position in the eye image frame in normalized coordinates
        public float Diameter { get; set; } // diameter of the pupil in image pixels as observed in the eye image frame (is not corrected for perspective)

        //2d gaze mapping
        public Vector2 EllipseCenter { get; set; } // center of the pupil in image pixels
        public Vector2 EllipseAxis { get; set; } // first and second axis of the pupil ellipse in pixels
        public float EllipseAngle { get; set; } // angle of the ellipse in degrees

        //3d gaze mapping
        public float Diameter3d { get; set; } //- diameter of the pupil scaled to mm based on anthropomorphic avg eye ball diameter and corrected for perspective.
        public float ModelConfidence { get; set; } //- confidence of the current eye model (0-1)
        public string ModelId { get; set; } //- id of the current eye model. When a slippage is detected the model is replaced and the id changes.
        public double ModelBirthTimestamp { get; set; }
        public Vector3 SphereCenter { get; set; } // pos of the eyeball sphere is eye pinhole camera 3d space units are scaled to mm.
        public float SphereRadius { get; set; } // radius of the eyeball. This is always 12mm (the anthropomorphic avg.) We need to make this assumption because of the single camera scale ambiguity.
        public Vector3 CircleCenter { get; set; } // center of the pupil as 3d circle in eye pinhole camera 3d space units are mm.
        public Vector3 CircleNormal { get; set; } // normals of the pupil as 3d circle. Indicates the direction that the pupil points at in 3d space.
        public float CircleRadius { get; set; } // radius of the pupil as 3d circle. Same as diameter_3d
        public float Theta { get; set; } // CircleNormal3d described in spherical coordinates in radians
        public float Phi { get; set; } // CircleNormal3d described in spherical coordinates in radians
        public Vector2 ProjectedSphereCenter { get; set; } // center of the 3d sphere projected back onto the eye image frame. Units are in image pixels.
        public Vector2 ProjectedSphereAxes { get; set; } // first and second axis of the 3d sphere projection.
        public float ProjectedSphereAngle { get; set; } // angle of the 3d sphere projection. Units are degrees.
    }
}
