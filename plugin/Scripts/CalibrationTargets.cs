using System;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs{

    [CreateAssetMenu(fileName = "CalibrationTargets", menuName = "Pupil/CalibrationTargets", order = 2)]
    public class CalibrationTargets : ScriptableObject
    {
        public CalibrationSettings.Mode mode;

        [Header("Calibration Targets")]
        [SerializeField]
        int points = 5;
        [SerializeField]
        Vector2[] vectorDepthRadius = new Vector2[1];
        [SerializeField]
        Vector2 centerPoint = new Vector2(0.5f, 0.5f);

        int currentCalibrationPoint;
        int currentCalibrationDepth;
        float radius;
        double offset;

        public int GetTargetCount()
        {
            return points * vectorDepthRadius.Length;
        }

        public float[] GetTargetAt(int idx) //TODO handle idx internally
        {
            currentCalibrationPoint = (int)Mathf.Floor((float)idx/(float)vectorDepthRadius.Length);
            currentCalibrationDepth = idx % vectorDepthRadius.Length;
            Debug.Log($"GetNextTarget {idx},{currentCalibrationPoint},{currentCalibrationDepth}");

            return UpdateCalibrationPoint();            
        }

        private float[] UpdateCalibrationPoint()
        {
            float [] position = new float[] { 0 };
            switch (mode)
            {
                case CalibrationSettings.Mode._3D:
                    position = new float[] { centerPoint.x, centerPoint.y, vectorDepthRadius[currentCalibrationDepth].x };
                    offset = 0.25f * Math.PI;
                    break;
                default:
                    position = new float[] { centerPoint.x, centerPoint.y, vectorDepthRadius[0].x };
                    offset = 0f;
                    break;
            }

            radius = vectorDepthRadius[currentCalibrationDepth].y;

            if (currentCalibrationPoint > 0 && currentCalibrationPoint < points)
            {
                position[0] += radius * (float)Math.Cos(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (points - 1f) + offset);
                position[1] += radius * (float)Math.Sin(2f * Math.PI * (float)(currentCalibrationPoint - 1) / (points - 1f) + offset);
            }

            Debug.Log($"Point: {position[0]} {position[1]} {position[2]}");
            return position;
        }
    }
}