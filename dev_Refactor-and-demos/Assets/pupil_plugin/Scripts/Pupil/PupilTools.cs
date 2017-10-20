using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PupilTools : MonoBehaviour
{
	static PupilSettings _pupilSettings;
	public static PupilSettings pupilSettings
	{
		get
		{
			if (_pupilSettings == null)
			{
				foreach (var item in Resources.LoadAll<PupilSettings> (""))
				{
					_pupilSettings = item;
				}
				//			print (pupilSettings);	
			}

			return _pupilSettings;
		}
	}

	public delegate void GUIRepaintAction ();
//InspectorGUI repaint
	public delegate void OnCalibrationStartDeleg ();

	public delegate void OnCalibrationEndDeleg ();

	public static event GUIRepaintAction WantRepaint;

	public static event OnCalibrationStartDeleg OnCalibrationStarted;
	public static event OnCalibrationEndDeleg OnCalibrationEnded;


	#region Recording

	public static void StartPupilServiceRecording (string path)
	{
		var _p = path.Substring (2);

		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","recording.should_start" },
			 {
				"session_name",
				_p
			}
		});

	}

	public static void StopPupilServiceRecording ()
	{
		_sendRequestMessage (new Dictionary<string,object> { { "subject","recording.should_stop" } });
	}

	#endregion


	#region Calibration

	public static void RepaintGUI ()
	{
		if (WantRepaint != null)
			WantRepaint ();
	}

	public static SubscriberSocket ClearAndInitiateSubscribe ()
	{
		if (pupilSettings.connection.subscribeSocket != null)
		{
			
			pupilSettings.connection.subscribeSocket.Close ();

		}

		SubscriberSocket _subscriberSocket = new SubscriberSocket (pupilSettings.connection.IPHeader + pupilSettings.connection.subport);

		//André: Is this necessary??
		_subscriberSocket.Options.SendHighWatermark = PupilSettings.numberOfMessages;// 6;

		//André: PupilSettings got overwritten every time, adding "pupil." after I removed it..
		foreach (var topic in pupilSettings.connection.topicList)
		{
			_subscriberSocket.Subscribe (topic);
		}

		return _subscriberSocket;

	}

	public static void SubscribeTo (string topic)
	{
		if (!pupilSettings.connection.topicList.Contains (topic))
		{
			
			pupilSettings.connection.topicList.Add (topic);

		}

		pupilSettings.connection.subscribeSocket = ClearAndInitiateSubscribe ();
	}

	public static void UnSubscribeFrom (string topic)
	{

		if (pupilSettings.connection.topicList.Contains (topic))
		{
			pupilSettings.connection.topicList.Remove (topic);
		}

		pupilSettings.connection.subscribeSocket = ClearAndInitiateSubscribe ();

	}


	static int currCalibPoint;
	static int currCalibSamples;
	public static int defaultCalibrationCount = 120;
	static float lastTimeStamp = 0;
	public static void InitializeCalibration ()
	{
		print ("Initializing Calibration");

		currCalibPoint = 0;
		currCalibSamples = 0;

		calibrationMarker = pupilSettings.calibration.CalibrationMarkers.Where (p => p.calibrationPoint && p.calibMode == pupilSettings.calibration.currentCalibrationMode).ToList () [0];

		float[] initialPoint = pupilSettings.calibration.GetCalibrationPoint (currCalibPoint);
		calibrationMarker.UpdatePosition (initialPoint[0], initialPoint[1]);
		calibrationMarker.SetMaterialColor (Color.white);

//		yield return new WaitForSeconds (2f);

		ResetMarkerVisuals(PupilSettings.EStatus.Calibration);

		print ("Starting Calibration");

		pupilSettings.calibration.initialized = true;
		pupilSettings.dataProcess.state = PupilSettings.EStatus.Calibration;

		PupilTools.RepaintGUI ();
	}

	public static void ResetMarkerVisuals(PupilSettings.EStatus status)
	{
		foreach (PupilSettings.Marker _m in PupilSettings.Instance.calibration.CalibrationMarkers) 
		{
			_m.SetActive(false);

			if (_m.calibMode == PupilSettings.Instance.calibration.currentCalibrationMode)
			{
				if (_m.calibrationPoint && status == PupilSettings.EStatus.Calibration) 
				{
					_m.SetActive(true);
				}

				if (!_m.calibrationPoint && status == PupilSettings.EStatus.ProcessingGaze) 
				{
					_m.SetActive (true);
				}
			}
		}
	}

	public static void StartCalibration ()
	{
		InitializeCalibration ();

		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","start_plugin" },
			 {
				"name",
				pupilSettings.calibration.currentCalibrationType.pluginName
			}
		});
		_sendRequestMessage (new Dictionary<string,object> {
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
	private static PupilSettings.Marker calibrationMarker;
	public static void Calibrate ()
	{
		// Get the current calibration information from the PupilSettings class
		PupilSettings.CalibrationType currentCalibrationType = pupilSettings.calibration.currentCalibrationType;

		float[] _currentCalibPointPosition = pupilSettings.calibration.GetCalibrationPoint (currCalibPoint);// .currentCalibrationType.calibPoints [currCalibPoint];
		calibrationMarker.UpdatePosition (_currentCalibPointPosition [0], _currentCalibPointPosition [1]);

		float t = GetPupilTimestamp ();

		if (t - lastTimeStamp > 0.02f) // was 0.1, 1000/60 ms wait in old version
		{
			lastTimeStamp = t;

//			print ("its okay to go on");

			//Create reference data to pass on. _cPointFloatValues are storing the float values for the relevant current Calibration mode
			AddCalibrationPointReferencePosition (currentCalibrationType.positionKey, _currentCalibPointPosition, t, 0);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			AddCalibrationPointReferencePosition (currentCalibrationType.positionKey, _currentCalibPointPosition, t, 1);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.

			if (pupilSettings.debug.printSampling)
				print ("Point: " + currCalibPoint + ", " + "Sampling at : " + currCalibSamples + ". On the position : " + _currentCalibPointPosition [0] + " | " + _currentCalibPointPosition [1]);

			currCalibSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

			if (currCalibSamples >= defaultCalibrationCount)
			{
				currCalibSamples = 0;
				currCalibPoint++;

				//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
				AddCalibrationReferenceData ();

				if (currCalibPoint >= currentCalibrationType.points)
				{
					StopCalibration ();
				}

			}
		}
	}

	public static void StopCalibration ()
	{
		pupilSettings.calibration.initialized = false;
		pupilSettings.dataProcess.state = PupilSettings.EStatus.ProcessingGaze;
		_sendRequestMessage (new Dictionary<string,object> { { "subject","calibration.should_stop" } });

		ResetMarkerVisuals(PupilSettings.EStatus.Idle);

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
		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","calibration.add_ref_data" },
			{
				"ref_data",
				_calibrationData.ToArray ()
			}
		});

		if (pupilSettings.debug.printSampling)
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

	public static void AddCalibrationPointReferencePosition (string calibrationType, float[] position, float timestamp, int id)
	{
		_calibrationData.Add ( new Dictionary<string,object> () {
			{ calibrationType, position }, 
			{ "timestamp", timestamp },
			{ "id", id }
		});
	}

	#endregion


	public static NetMQMessage _sendRequestMessage (Dictionary<string,object> data)
	{
		NetMQMessage m = new NetMQMessage ();

		m.Append ("notify." + data ["subject"]);
		m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

		PupilDataReceiver.Instance._requestSocket.SendMultipartMessage (m);

		NetMQMessage recievedMsg;
		recievedMsg = PupilDataReceiver.Instance._requestSocket.ReceiveMultipartMessage ();

		return recievedMsg;
	}

	public static float GetPupilTimestamp ()
	{
		PupilDataReceiver.Instance._requestSocket.SendFrame ("t");
		NetMQMessage recievedMsg = PupilDataReceiver.Instance._requestSocket.ReceiveMultipartMessage ();
		return float.Parse (recievedMsg [0].ConvertToString ());
	}

	public static void StartEyeProcesses ()
	{
		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.0" },
			{
				"eye_id",
				PupilSettings.leftEyeID
			}
		});
		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_start.1" },
			{
				"eye_id",
				PupilSettings.rightEyeID
			}
		});
	}

	public static void StopEyeProcesses ()
	{
		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilSettings.leftEyeID
			}
		});
		_sendRequestMessage (new Dictionary<string,object> {
			{ "subject","eye_process.should_stop" },
			 {
				"eye_id",
				PupilSettings.rightEyeID
			}
		});
	}

	public static void StartBinocularVectorGazeMapper ()
	{
		_sendRequestMessage (new Dictionary<string,object> { { "subject","" }, { "name", "Binocular_Vector_Gaze_Mapper" } });
	}

	public static void SetDetectionMode(string mode)
	{
		_sendRequestMessage (new Dictionary<string,object> { { "subject", "set_detection_mapping_mode" }, { "mode", mode } });
	}

	public static void StartFramePublishing ()
	{
		pupilSettings.framePublishing.StreamCameraImages = true;

		_sendRequestMessage (new Dictionary<string,object> { { "subject","plugin_started" }, { "name","Frame_Publisher" } });

		SubscribeTo ("frame.");
		//		print ("frame publish start");
		//_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}

	public static void StopFramePublishing ()
	{
		UnSubscribeFrom ("frame.");

		pupilSettings.framePublishing.StreamCameraImages = false;

		//Andre: No sendRequest??
		//_sendRequestMessage (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });
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

	public static void Connect ()
	{
		if (pupilSettings.connection.isLocal)
			RunServiceAtPath ();

		PupilDataReceiver.Instance.RunConnect ();

	}

	public static void RunServiceAtPath (bool runEyeProcess = false)
	{

		string servicePath = pupilSettings.pupilServiceApp.servicePath;

		if (File.Exists (servicePath))
		{
		
			if (Process.GetProcessesByName ("pupil_capture").Length > 0)
			{
			
				UnityEngine.Debug.LogWarning (" Pupil Capture is already running ! ");
			
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
