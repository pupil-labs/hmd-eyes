using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class CalibrationSettings : ScriptableObject
    {
        public enum Mode
        {
            _2D,
            _3D
        }

        [System.Serializable]
        public struct Type
        {
            public string name;
            public string pluginName;
            public string positionKey;
            public double[] ref_data;
            public float points;
            public float markerScale;
            public Vector2 centerPoint;
            public Vector2[] vectorDepthRadius;
            public int samplesPerDepth;
        }

        public Type type;
        public Mode mode;

        public int samplesToIgnoreForEyeMovement = 10;

        // public Type CalibrationType2D = new Type () 
		// { 
		// 	name = "2d",
		// 	pluginName = "HMD_Calibration",
		// 	positionKey = "norm_pos",
		// 	ref_data = new double[]{ 0.0, 0.0 },
		// 	points = 8,
		// 	markerScale = 0.05f,
		// 	centerPoint = new Vector2(0.5f,0.5f),
		// 	vectorDepthRadius = new Vector2[] { new Vector2( 2f, 0.07f ) },
		// 	samplesPerDepth = 120
		// };

		// public Type CalibrationType3D = new Type () 
		// { 
		// 	name = "3d",
		// 	pluginName = "HMD_Calibration_3D",
		// 	positionKey = "mm_pos",
		// 	ref_data = new double[]{ 0.0, 0.0, 0.0 },
		// 	points = 10,
		// 	markerScale = 0.04f,
		// 	centerPoint = new Vector2(0,-0.05f),
		// 	vectorDepthRadius = new Vector2[] { new Vector2( 1f, 0.24f ) },
		// 	samplesPerDepth = 40
		// };
    }
}
