using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public abstract class CalibrationTargets : ScriptableObject
    {
        public abstract int GetTargetCount();
        public abstract Vector3 GetLocalTargetPosAt(int idx); //unity camera space 
    }
}
