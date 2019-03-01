using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
	public class DisableOnCalibrationStart : MonoBehaviour {

		public CalibrationController controller;

		void OnEnable () 
		{
			controller.OnCalibrationStarted += DisableMePls;
		}

		void OnDisable ()
		{
			controller.OnCalibrationStarted -= DisableMePls;
		}

		void DisableMePls()
		{
			gameObject.SetActive(false);
		}
	}
}
