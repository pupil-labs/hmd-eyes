using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class PupilTools : MonoBehaviour
{
	private static PupilSettings Settings
	{
		get { return PupilSettings.Instance; }
	}

	public delegate void GUIRepaintAction ();
//InspectorGUI repaint
	public delegate void OnCalibrationStartDeleg ();
	public delegate void OnCalibrationEndDeleg ();
	public delegate void OnCalibrationFailedDeleg ();
	public delegate void OnConnectedDelegate ();

	public static event GUIRepaintAction WantRepaint;

	public static event OnCalibrationStartDeleg OnCalibrationStarted;
	public static event OnCalibrationEndDeleg OnCalibrationEnded;
	public static event OnCalibrationEndDeleg OnCalibrationFailed;
	public static event OnConnectedDelegate OnConnected;

	#region Recording

	public static void StartPupilServiceRecording (string path)
	{
		var _p = path.Substring (2);

		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","recording.should_start" },
			 {
				"session_name",
				_p
			}
		});

	}

	public static void StopPupilServiceRecording ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","recording.should_stop" } });
	}

	#endregion

	#region Calibration

	public static void RepaintGUI ()
	{
		if (WantRepaint != null)
			WantRepaint ();
	}

	public static IEnumerator Connect(bool retry = false, float retryDelay = 5f)
	{
		yield return new WaitForSeconds (3f);

		var connection = Settings.connection;

		while (!connection.isConnected) 
		{
			connection.InitializeRequestSocket ();

			if (!connection.isConnected)
            {
				if (retry) 
				{
                    UnityEngine.Debug.Log("Could not connect, Re-trying in 5 seconds ! ");
					yield return new WaitForSeconds (retryDelay);

				} else 
				{
					connection.TerminateContext ();
					yield return null;
				}

			} 
			//yield return null;
        }
        UnityEngine.Debug.Log(" Succesfully connected to Pupil Service ! ");

        StartEyeProcesses();
        SetDetectionMode();
        RepaintGUI();
        OnConnected();
        yield break;
    }

	public static void SubscribeTo (string topic)
	{
		Settings.connection.InitializeSubscriptionSocket (topic);
	}

	public static void UnSubscribeFrom (string topic)
	{
		Settings.connection.CloseSubscriptionSocket (topic);
	}

	static PupilSettings.EStatus previousState = PupilSettings.EStatus.Idle;
	public static void StartCalibration ()
	{
		if (OnCalibrationStarted != null)
			OnCalibrationStarted ();
		else
		{
			print ("No 'calibration started' delegate set");
		}

		Settings.calibration.InitializeCalibration ();

		previousState = Settings.DataProcessState;
		Settings.DataProcessState = PupilSettings.EStatus.Calibration;
		SubscribeTo ("notify.calibration.successful");
		SubscribeTo ("notify.calibration.failed");

		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","start_plugin" },
			 {
				"name",
				Settings.calibration.currentCalibrationType.pluginName
			}
		});
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
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
				Settings.calibration.rightEyeTranslation
			},
			{
				"translation_eye1",
				Settings.calibration.leftEyeTranslation
			}
		});

		_calibrationData.Clear ();

		RepaintGUI ();
	}

	public static void StopCalibration ()
	{
		Settings.calibration.currentStatus = Calibration.Status.Stopped;
		Settings.DataProcessState = previousState;
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","calibration.should_stop" } });
	}

	public static void CalibrationFinished ()
	{
		print ("Calibration finished");

		UnSubscribeFrom ("notify.calibration.successful");
		UnSubscribeFrom ("notify.calibration.failed");

		if (OnCalibrationEnded != null)
			OnCalibrationEnded ();
		else
		{
			print ("No 'calibration ended' delegate set");
		}
	}

	public static void CalibrationFailed ()
	{
		if (OnCalibrationFailed != null)
			OnCalibrationFailed ();
		else
		{
			print ("No 'calibration failed' delegate set");
		}
	}

	private static List<Dictionary<string,object>> _calibrationData = new List<Dictionary<string,object>> ();
	public static void AddCalibrationReferenceData ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","calibration.add_ref_data" },
			{
				"ref_data",
				_calibrationData.ToArray ()
			}
		});

		if (Settings.debug.printSampling)
		{
			print ("Sending ref_data");

			string str = "";

			foreach (var element in _calibrationData)
			{
				foreach (var i in element)
				{
					if (i.Key == "norm_pos")
					{
						str += "|| " + i.Key + " | " + ((System.Single[])i.Value) [0] + " , " + ((System.Single[])i.Value) [1];
					} else
					{
						str += "|| " + i.Key + " | " + i.Value.ToString ();
					}
				}
				str += "\n";

			}

			print (str);
		}

		//Clear the current calibration data, so we can proceed to the next point if there is any.
		_calibrationData.Clear ();
	}

	public static void AddCalibrationPointReferencePosition (float[] position, float timestamp)
	{
		if (Settings.calibration.currentMode == Calibration.Mode._3D)
			for (int i = 0; i < position.Length; i++)
				position [i] *= PupilSettings.PupilUnitScalingFactor;
		
		_calibrationData.Add ( new Dictionary<string,object> () {
			{ Settings.calibration.currentCalibrationType.positionKey, position }, 
			{ "timestamp", timestamp },
			{ "id", PupilData.leftEyeID }
		});
		_calibrationData.Add ( new Dictionary<string,object> () {
			{ Settings.calibration.currentCalibrationType.positionKey, position }, 
			{ "timestamp", timestamp },
			{ "id", PupilData.rightEyeID }
		});
	}

	#endregion

	public static void StartEyeProcesses ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.0" },
			{
				"eye_id",
				PupilData.leftEyeID
			}
		});
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.1" },
			{
				"eye_id",
				PupilData.rightEyeID
			}
		});
	}

	public static void StopEyeProcesses ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilData.leftEyeID
			}
		});
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilData.rightEyeID
			}
		});
	}

	public static void StartBinocularVectorGazeMapper ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","" }, { "name", "Binocular_Vector_Gaze_Mapper" } });
	}

	public static void SetDetectionMode()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject", "set_detection_mapping_mode" }, { "mode", Settings.calibration.currentCalibrationType.name } });
	}

	public static void StartFramePublishing ()
	{
		Settings.framePublishing.StreamCameraImages = true;
		Settings.framePublishing.InitializeFramePublishing ();

		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","plugin_started" }, { "name","Frame_Publisher" } });

		SubscribeTo ("frame.");
		//		print ("frame publish start");
		//Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}

	public static void StopFramePublishing ()
	{
		UnSubscribeFrom ("frame.");

		Settings.framePublishing.StreamCameraImages = false;

		//Andre: No sendRequest??
		//Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });
	}
}
