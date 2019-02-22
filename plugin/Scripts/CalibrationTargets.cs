using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs{

    [CreateAssetMenu(fileName = "CalibrationTargets", menuName = "Pupil/CalibrationTargets", order = 2)]
    public class CalibrationTargets : ScriptableObject
    {
        [System.Serializable]
        public struct Circle
        {
            public Vector3 center;
            public float radius;
        }

        public CalibrationSettings.Mode mode;

        [Header("Calibration Targets")]
        public List<Circle> circles = new List<Circle>();
        public int points = 5;

        int currentCalibrationPoint;
        int currentCalibrationDepth;
        float radius;
        double offset;

        public int GetTargetCount()
        {
            return points * circles.Count;
        }

        public Vector3 GetLocalTargetPosAt(int idx) //TODO handle idx internally
        {
            currentCalibrationPoint = (int)Mathf.Floor((float)idx/(float)circles.Count);
            currentCalibrationDepth = idx % circles.Count;

            return UpdateCalibrationPoint();            
        }

        private Vector3 UpdateCalibrationPoint()
        {
            //3d offset = 0.25f * Math.PI;?
            Circle circle = circles[currentCalibrationDepth];
            Vector3 position = new Vector3(circle.center.x,circle.center.y,circle.center.z);
            
            radius = circle.radius;

            if (currentCalibrationPoint > 0 && currentCalibrationPoint < points)
            {
                // position[0] += radius * (float)Math.Cos(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (points - 1f) + offset);
                // position[1] += radius * (float)Math.Sin(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (points - 1f) + offset);
                float angle = 360f * (float)(currentCalibrationPoint - 1) / (points - 1f);
                position.x += radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                position.y += radius * Mathf.Sin(Mathf.Deg2Rad * angle);
            }

            return position;
        }
    }
}