using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
	public class Calibration
	{	
		public RequestController requestCtrl;
		public SubscriptionsController subsCtrl;

		public delegate void CalibrationDel();
		public event CalibrationDel OnCalibrationStarted;
		public event CalibrationDel OnCalibrationEnded;
		public event CalibrationDel OnCalibrationFailed;

		public bool IsCalibrating { get; set; }

		private void ReceiveResponse(string topic, Dictionary<string, object> dictionary, byte[] thirdFrame)
		{
			//TODO
		}

		//TODO where to set?	
		public float[] rightEyeTranslation; 
		public float[] leftEyeTranslation;
        
		CalibrationSettings settings;
		public void StartCalibration (CalibrationSettings settings)
		{
			this.settings = settings; //TODO temp, where to set?

			requestCtrl.SetPupilTimestamp (Time.time);

			if (OnCalibrationStarted != null)
			{
				OnCalibrationStarted ();
			}
			
			IsCalibrating = true;
			
			subsCtrl.SubscribeTo ("notify.calibration.successful",ReceiveResponse);
			subsCtrl.SubscribeTo ("notify.calibration.failed",ReceiveResponse);
			// subsCtrl.SubscribeTo ("pupil."); //TODO why?

			requestCtrl.StartPlugin(settings.pluginName);
				
			requestCtrl.Send (new Dictionary<string,object> {
				{ "subject","calibration.should_start" },
				{
					"hmd_video_frame_size",
					new float[] {
						1000,
						1000
					}
				},
				{
					"outlier_threshold",
					35
				},
				{
					"translation_eye0",
					rightEyeTranslation
				},
				{
					"translation_eye1",
					leftEyeTranslation
				}
			});

			_calibrationData.Clear ();
		}
		
		public void AddCalibrationPointReferencePosition (float[] position, float timestamp)
		{
			if (settings.mode == CalibrationSettings.Mode._3D)
				for (int i = 0; i < position.Length; i++)
					position [i] *= Helpers.PupilUnitScalingFactor;

			_calibrationData.Add ( new Dictionary<string,object> () {
				{ settings.positionKey, position }, 
				{ "timestamp", timestamp },
				{ "id", int.Parse(Helpers.rightEyeID) }
			});
			_calibrationData.Add ( new Dictionary<string,object> () {
				{ settings.positionKey, position }, 
				{ "timestamp", timestamp },
				{ "id", int.Parse(Helpers.leftEyeID) } 
			});
		}

		private List<Dictionary<string,object>> _calibrationData = new List<Dictionary<string,object>> ();
		public void AddCalibrationReferenceData ()
		{
			requestCtrl.Send (new Dictionary<string,object> {
				{ "subject","calibration.add_ref_data" },
				{
					"ref_data",
					_calibrationData.ToArray ()
				}
			});


			//Clear the current calibration data, so we can proceed to the next point if there is any.
			_calibrationData.Clear ();
		}

		public void StopCalibration ()
		{
			IsCalibrating = false;
			requestCtrl.Send (new Dictionary<string,object> { { "subject","calibration.should_stop" } });
		}

		
	}
}

			// if (Settings.debug.printSampling)
			// {
			// 	print ("Sending ref_data");

			// 	string str = "";

			// 	foreach (var element in _calibrationData)
			// 	{
			// 		foreach (var i in element)
			// 		{
			// 			if (i.Key == "norm_pos")
			// 			{
			// 				str += "|| " + i.Key + " | " + ((System.Single[])i.Value) [0] + " , " + ((System.Single[])i.Value) [1];
			// 			} else
			// 			{
			// 				str += "|| " + i.Key + " | " + i.Value.ToString ();
			// 			}
			// 		}
			// 		str += "\n";

			// 	}

			// 	print (str);
			// }