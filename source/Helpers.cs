using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class Helpers
    {
        public static bool Is3DCalibrationSupported(PupilLabs.Connection connection)
        {
            List<int> versionNumbers = connection.PupilVersionNumbers;
            if (versionNumbers.Count > 0)
                if (versionNumbers[0] >= 1)
                    return true;

            Debug.Log("Pupil version below 1 detected. V1 is required for 3D calibration");
            return false;
        }
    }
}