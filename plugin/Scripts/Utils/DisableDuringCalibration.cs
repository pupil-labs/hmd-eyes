using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class DisableDuringCalibration : MonoBehaviour
    {

        public CalibrationController controller;
        public bool enableAfterCalibration;

        void Awake()
        {
            controller.OnCalibrationStarted += DisableMePls;
            controller.OnCalibrationRoutineDone += EnableMePls;
        }

        void OnDestroy()
        {
            controller.OnCalibrationStarted -= DisableMePls;
            controller.OnCalibrationRoutineDone -= EnableMePls;
        }

        void EnableMePls()
        {
            if (enableAfterCalibration)
            {
                gameObject.SetActive(true);
            }
        }

        void DisableMePls()
        {
            gameObject.SetActive(false);
        }
    }
}
