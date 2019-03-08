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

        [Header("Time and sample amount per target")]
        public float secondsPerTarget = 1f;
        public float ignoreInitialSeconds = 0.1f;
        public int samplesPerTarget = 40;

        [HideInInspector]
        public Mode mode = Mode._3D;

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

        public float SampleRate
        {
            get
            {
                return (float)samplesPerTarget / (secondsPerTarget - ignoreInitialSeconds);
            }
        }
    }
}