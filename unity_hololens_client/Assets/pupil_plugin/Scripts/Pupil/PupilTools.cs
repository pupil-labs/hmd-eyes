using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class PupilTools : MonoBehaviour
{
	static PupilSettings _settings;
	public static PupilSettings Settings
	{
		get
		{
			if (_settings == null)
			{
				_settings = Resources.Load<PupilSettings> ("PupilSettings");
			}

			return _settings;
		}
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

	public static Dictionary<string, object> pupil0Dictionary;
	public static Dictionary<string, object> pupil1Dictionary;
	private static Dictionary<string, object> _gazeDictionary;
	public static Dictionary<string, object> gazeDictionary
	{
		get
		{
			return _gazeDictionary;
		}
		set
		{
			_gazeDictionary = value;
			UpdateGaze ();
			UpdateEyeID ();
		}
	}

	private static string[] gazeKeys = { "gaze_point_3d", "norm_pos", "eye_centers_3d" , "gaze_normals_3d" };
	private static string eyeDataKey;
	private static void UpdateGaze()
	{
		foreach (var key in gazeKeys)
		{
			if (gazeDictionary.ContainsKey (key))
			{
				switch (key)
				{
				case "norm_pos": // 2D case
					eyeDataKey = key + "_" + stringForEyeID(); // we add the identifier to the key
					PupilData.AddGazeToEyeData(eyeDataKey,Position(gazeDictionary[key],false));
					break;
				case "eye_centers_3d":
				case "gaze_normals_3d":
					// in case of eye_centers_3d and gaze_normals_3d, we get an dictionary with one positional object for each eye id (the key)
					if (gazeDictionary [key] is Dictionary<object,object>)
						foreach (var item in (gazeDictionary[key] as Dictionary<object,object>))
						{
							eyeDataKey = key + "_" + item.Key.ToString ();
							PupilData.AddGazeToEyeData (eyeDataKey, Position (item.Value,true));
						}
					break;
				default:
					PupilData.AddGazeToEyeData(key,Position(gazeDictionary[key],true));
					break;
				}
			}
		}
	}

	private static object IDo;
	private static void UpdateEyeID ()
	{
		string id = "";

		if (gazeDictionary != null)
			if (gazeDictionary.TryGetValue ("id", out IDo))
				id = IDo.ToString ();
			
		PupilData.UpdateCurrentEyeID(id);
	}

	public static string stringForEyeID ()
	{
		object IDo;
		if (gazeDictionary == null)
			return null;

		bool isID = gazeDictionary.TryGetValue ("id", out IDo);

		if (isID)
		{
			return IDo.ToString ();

		}
		else
		{
			return null;
		}
	}

	private static object[] position_o;
	private static float[] Position (object position, bool applyScaling)
	{
		position_o = position as object[];
		float[] position_f = new float[position_o.Length];
		for (int i = 0; i < position_o.Length; i++)
		{
			position_f [i] = (float)(double)position_o [i];
		}
		if (applyScaling)
			for (int i = 0; i < position_f.Length; i++)
				position_f [i] /= PupilSettings.PupilUnitScalingFactor;
		return position_f;
	}

	public static float ConfidenceForDictionary(Dictionary<string,object> dictionary)
	{
		object conf0;
		dictionary.TryGetValue ("confidence", out conf0);
		return (float)(double)conf0;
	}

	public static float Confidence (int eyeID)
	{
		if (eyeID == PupilData.rightEyeID)
			return ConfidenceForDictionary (pupil0Dictionary);
		else if (eyeID == PupilData.leftEyeID)
			return ConfidenceForDictionary (pupil1Dictionary); 
		else
			return 0;
	}

	public static Dictionary<object,object> BaseData ()
	{
		object o;
		gazeDictionary.TryGetValue ("base_data", out o);
		return o as Dictionary<object,object>;
	}
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
