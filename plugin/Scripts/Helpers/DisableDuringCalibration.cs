using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
    public class DisableDuringCalibration : MonoBehaviour
    {

        public CalibrationController controller;
        public bool enableAfterCalibration;

        void OnEnable()
        {
            controller.OnCalibrationStarted += DisableMePls;
            controller.OnCalibrationSucceeded += EnableMePls;
            controller.OnCalibrationFailed += EnableMePls;
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
