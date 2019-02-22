using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{

    [CreateAssetMenu(fileName = "Circle Calibration Targets", menuName = "Pupil/CircleCalibrationTargets", order = 2)]
    public class CircleCalibrationTargets : CalibrationTargets
    {
        [System.Serializable]
        public struct Circle
        {
            public Vector3 center;
            public float radius;
        }

        public List<Circle> circles = new List<Circle>();
        public int points = 5;

        int pointIdx;
        int circleIdx;

        public override int GetTargetCount()
        {
            return points * circles.Count;
        }

        public override Vector3 GetLocalTargetPosAt(int idx) //TODO handle idx internally
        {
            pointIdx = (int)Mathf.Floor((float)idx / (float)circles.Count);
            circleIdx = idx % circles.Count;

            return UpdateCalibrationPoint();
        }

        private Vector3 UpdateCalibrationPoint()
        {
            Circle circle = circles[circleIdx];
            Vector3 position = new Vector3(circle.center.x, circle.center.y, circle.center.z);

            if (pointIdx > 0 && pointIdx < points)
            {
                float angle = 360f * (float)(pointIdx - 1) / (points - 1f);
                position.x += circle.radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                position.y += circle.radius * Mathf.Sin(Mathf.Deg2Rad * angle);
            }

            return position;
        }
    }
}