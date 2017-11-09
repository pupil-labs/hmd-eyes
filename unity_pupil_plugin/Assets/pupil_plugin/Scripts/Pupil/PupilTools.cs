using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
	public delegate void OnConnectedDelegate ();

	public static event GUIRepaintAction WantRepaint;

	public static event OnCalibrationStartDeleg OnCalibrationStarted;
	public static event OnCalibrationEndDeleg OnCalibrationEnded;
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
			connection.TryToConnect ();

			if (!connection.isConnected) {

				if (retry) 
				{
					print ("Could not connect, Re-trying in 5 seconds ! ");
					yield return new WaitForSeconds (retryDelay);

				} else 
				{
					connection.TerminateContext ();
					yield break;
				}

			} 
			else
			{
				print (" Succesfully connected to Pupil Service ! ");

				StartEyeProcesses ();
				RepaintGUI ();
				OnConnected ();
				yield break;
			}
			yield return null;
		}
	}

	public static void ClearAndInitiateSubscribe ()
	{
		Settings.dataProcess.state = PupilSettings.EStatus.ProcessingGaze;

		Settings.connection.InitializeSubscriptionSocket ();
	}

	public static void SubscribeTo (string topic)
	{
		if (!Settings.connection.topicList.Contains (topic))
		{
			Settings.connection.topicList.Add (topic);
		}
		ClearAndInitiateSubscribe ();
	}

	public static void UnSubscribeFrom (string topic)
	{
		if (Settings.connection.topicList.Contains (topic))
		{
			Settings.connection.topicList.Remove (topic);
		}
		ClearAndInitiateSubscribe ();
	}


	static int currentCalibrationPoint;
	static int currentCalibrationSamples;
	static Calibration.CalibrationType currentCalibrationType;
	public static int defaultCalibrationCount = 120;
	static float lastTimeStamp = 0;
	public static void InitializeCalibration ()
	{
		print ("Initializing Calibration");

		currentCalibrationPoint = 0;
		currentCalibrationSamples = 0;

		currentCalibrationType = Settings.calibration.currentCalibrationType;

		calibrationMarker.SetActive (true);
		float[] initialPoint = Settings.calibration.GetCalibrationPoint (currentCalibrationPoint);
		calibrationMarker.UpdatePosition (initialPoint);
		calibrationMarker.SetMaterialColor (Color.white);

//		yield return new WaitForSeconds (2f);

		print ("Starting Calibration");

		Settings.calibration.initialized = true;
		Settings.dataProcess.state = PupilSettings.EStatus.Calibration;

		PupilTools.RepaintGUI ();
	}

	public static void StartCalibration ()
	{
		InitializeCalibration ();

		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","start_plugin" },
			 {
				"name",
				currentCalibrationType.pluginName
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
			}
		});

		if (OnCalibrationStarted != null)
			OnCalibrationStarted ();
		else
		{
			print ("No 'calibration started' delegate set");
		}

		_calibrationData.Clear ();
	}
	private static PupilMarker calibrationMarker = new PupilMarker ("Calibraton Marker");
	public static void Calibrate ()
	{
		float[] _currentCalibPointPosition = Settings.calibration.GetCalibrationPoint (currentCalibrationPoint);// .currentCalibrationType.calibPoints [currentCalibrationPoint];
		calibrationMarker.UpdatePosition (_currentCalibPointPosition);

		float t = Settings.connection.GetPupilTimestamp ();

		if (t - lastTimeStamp > 0.02f) // was 0.1, 1000/60 ms wait in old version
		{
			lastTimeStamp = t;

//			print ("its okay to go on");

			//Create reference data to pass on. _cPointFloatValues are storing the float values for the relevant current Calibration mode
			AddCalibrationPointReferencePosition (_currentCalibPointPosition, t, 0);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			AddCalibrationPointReferencePosition (_currentCalibPointPosition, t, 1);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.

			if (Settings.debug.printSampling)
				print ("Point: " + currentCalibrationPoint + ", " + "Sampling at : " + currentCalibrationSamples + ". On the position : " + _currentCalibPointPosition [0] + " | " + _currentCalibPointPosition [1]);

			currentCalibrationSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

			if (currentCalibrationSamples >= defaultCalibrationCount)
			{
				currentCalibrationSamples = 0;
				currentCalibrationPoint++;

				//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
				AddCalibrationReferenceData ();

				if (currentCalibrationPoint >= currentCalibrationType.points)
				{
					StopCalibration ();
				}

			}
		}
	}

	public static void StopCalibration ()
	{
		Settings.calibration.initialized = false;
		Settings.dataProcess.state = PupilSettings.EStatus.Idle;
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","calibration.should_stop" } });

		calibrationMarker.SetActive (false);

//		SetDetectionMode (currentCalibrationType.name);

		if (OnCalibrationEnded != null)
			OnCalibrationEnded ();
		else
		{
			print ("No 'calibration ended' delegate set");
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

	public static void AddCalibrationPointReferencePosition (float[] position, float timestamp, int id)
	{
		_calibrationData.Add ( new Dictionary<string,object> () {
			{ currentCalibrationType.positionKey, position }, 
			{ "timestamp", timestamp },
			{ "id", id }
		});
	}

	#endregion

	public static void StartEyeProcesses ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.0" },
			{
				"eye_id",
				PupilSettings.leftEyeID
			}
		});
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.1" },
			{
				"eye_id",
				PupilSettings.rightEyeID
			}
		});
	}

	public static void StopEyeProcesses ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilSettings.leftEyeID
			}
		});
		Settings.connection.sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilSettings.rightEyeID
			}
		});
	}

	public static void StartBinocularVectorGazeMapper ()
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject","" }, { "name", "Binocular_Vector_Gaze_Mapper" } });
	}

	public static void SetDetectionMode(string mode)
	{
		Settings.connection.sendRequestMessage (new Dictionary<string,object> { { "subject", "set_detection_mapping_mode" }, { "mode", mode } });
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

	public static void SavePupilSettings (ref PupilSettings pupilSettings)
	{
	
		#if UNITY_EDITOR
		AssetDatabase.Refresh ();
		EditorUtility.SetDirty (pupilSettings);
		AssetDatabase.SaveAssets ();
		#endif

	}

	public static bool PupilGazeTrackerExists ()
	{//this could/should be done with .Instance of the singleton type, but for Unity Editor update a FindObjectOfType seems more effective.
	
		if (FindObjectOfType<PupilGazeTracker> () == null)
		{
			return false;
		} else
		{
			return true;
		}
	}

	public static void RunServiceAtPath (bool runEyeProcess = false)
	{
		string servicePath = Settings.pupilServiceApp.servicePath;

		if (File.Exists (servicePath))
		{
		
			if (Process.GetProcessesByName ("pupil_service").Length > 0)
			{
			
				UnityEngine.Debug.LogWarning (" Pupil Service is already running ! ");
			
			} else
			{
			
				Process serviceProcess = new Process ();
				serviceProcess.StartInfo.Arguments = servicePath;
				serviceProcess.StartInfo.FileName = servicePath;
//				serviceProcess.StartInfo.CreateNoWindow = true;
//				serviceProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
//				serviceProcess.StartInfo.UseShellExecute = false;
//				serviceProcess.StartInfo.RedirectStandardOutput = true;     

				if (File.Exists (servicePath))
				{
				
					serviceProcess.Start ();
				
				} else
				{
				
					UnityEngine.Debug.LogWarning ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
			
				}
			}

		} else
		{

			if (servicePath == "")
			{
			
				UnityEngine.Debug.LogWarning ("Pupil Service filename is not specified ! Please configure it under the Pupil plugin settings");
			
			}

		}

	}
}
