using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    [CreateAssetMenu(fileName = "CalibrationSettings", menuName = "Pupil/CalibrationSettings", order = 1)]
    public class CalibrationSettings : ScriptableObject
    {

        [Header("Time and sample amount per target")]
        public float secondsPerTarget = 1f;
        public float ignoreInitialSeconds = 0.1f;
        public int samplesPerTarget = 40;


        public string PluginName { get { return "HMD_Calibration_3D"; } }
        public string PositionKey { get { return "mm_pos"; } }
        public string DetectionMode { get { return "3d"; } }

        public float SampleRate
        {
            get
            {
                return (float)samplesPerTarget / (secondsPerTarget - ignoreInitialSeconds);
            }
        }

    }
}