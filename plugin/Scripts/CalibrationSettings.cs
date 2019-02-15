using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    [CreateAssetMenu(fileName = "CalibrationSettings", menuName = "Pupil/CalibrationSettings", order = 1)]
    public class CalibrationSettings : ScriptableObject
    {
        public enum Mode
        {
            _2D,
            _3D
        }

        public Mode mode;

        public string pluginName;
        public string positionKey;
        public float points = 5;
        public float markerScale = 0.03f;
        public Vector2 centerPoint = new Vector2(0.5f,0.5f);
        public Vector2[] vectorDepthRadius;
        public int samplesPerDepth = 40;
        public int samplesToIgnoreForEyeMovement = 10;
    }
}
