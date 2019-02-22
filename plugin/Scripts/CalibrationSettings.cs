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

        public string PluginName
        {
            get
            {
                if (mode == Mode._2D)
                {
                    return "HMD_Calibration";
                }
                else
                {
                    return "HMD_Calibration_3D";
                }
            }
        }

        public string PositionKey
        {
            get
            {
                if (mode == Mode._2D)
                {
                    return "norm_pos";
                }
                else
                {
                    return "mm_pos";
                }
            }
        }
        
        [Header("Time and sample amount per Target")]
        public float secondsPerTarget = 1f;
        public float ignoreInitialSeconds = 0.1f;
        public int samplesPerTarget = 40;

        [Header("Calibration Targets")]
        public float points = 5;
        public Vector2[] vectorDepthRadius;
        public Vector2 centerPoint = new Vector2(0.5f, 0.5f);

        public float SampleRate
        {
            get
            {
                return (float)samplesPerTarget / (secondsPerTarget - ignoreInitialSeconds);
            }
        }
    }
}