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

	#region Delegates

	//InspectorGUI repaint
	public delegate void GUIRepaintAction ();
	public delegate void OnCalibrationStartDeleg ();
	public delegate void OnCalibrationEndDeleg ();
	public delegate void OnCalibrationFailedDeleg ();
	public delegate void OnConnectedDelegate ();
	public delegate void OnDisconnectingDelegate ();
	public delegate void OnReceiveDataDelegate (string topic, Dictionary<string,object> dictionary);

	public static event GUIRepaintAction WantRepaint;
	public static event OnCalibrationStartDeleg OnCalibrationStarted;
	public static event OnCalibrationEndDeleg OnCalibrationEnded;
	public static event OnCalibrationFailedDeleg OnCalibrationFailed;
	public static event OnConnectedDelegate OnConnected;
	public static event OnDisconnectingDelegate OnDisconnecting;
	public static event OnReceiveDataDelegate OnReceiveData;

	#endregion

	#region EStatus

	private enum EStatus { Idle, ProcessingGaze, Calibration }
	private static EStatus _dataProcessState = EStatus.Idle;
	private static EStatus DataProcessState
	{
		get { return _dataProcessState; }
		set
		{
			_dataProcessState = value;
			PupilMarker.TryToSetActive(Calibration.Marker,_dataProcessState == EStatus.Calibration);
		}
	}
	private static EStatus previousState = EStatus.Idle;
	public static bool IsIdle
	{
		get { return DataProcessState == EStatus.Idle; }
		set { SetProcessState(!value, EStatus.Idle); }
	}
	public static bool IsGazing
	{
		get { return DataProcessState == EStatus.ProcessingGaze; }
		set { SetProcessState(!value, EStatus.ProcessingGaze); }
	}
	public static bool IsCalibrating
	{
		get { return DataProcessState == EStatus.Calibration; }
		set { SetProcessState(!value, EStatus.Calibration); }
	}

	private static void SetProcessState (bool toOldState, EStatus newState)
	{
		if (toOldState)
			DataProcessState = previousState;
		else
		{
			previousState = DataProcessState;
			DataProcessState = newState;
		}
	}

	#endregion

	#region Recording

	public static bool isRecording = false;
	private static string recordingString;
	public static void StartRecording ()
	{
		isRecording = true;

		recordingString = "Timestamp,Identifier,PupilPositionX,PupilPositionY,PupilPositionZ,UnityWorldPositionX,UnityWorldPositionY,UnityWorldPositionZ\n";
	}

	public static void StopRecording ()
	{
		isRecording = false;
	}

	private static Vector3 unityWorldPosition;
	private static void AddToRecording( string identifier, Vector3 position, bool isViewportPosition = false )
	{
		var timestamp = Time.time;

		if (isViewportPosition)
			unityWorldPosition = Settings.currentCamera.ViewportToWorldPoint (position + Vector3.forward);
		else
			unityWorldPosition = Settings.currentCamera.cameraToWorldMatrix.MultiplyPoint3x4 (position);

//		if (!isViewportPosition)
//			position.y *= -1;				// Pupil y axis is inverted

		recordingString += string.Format ( "{0},{1},{2},{3},{4},{5},{6},{7}\n"
			,timestamp.ToString ("F4")
			,identifier
			,position.x.ToString ("F4"),position.y.ToString ("F4"),position.z.ToString ("F4")
			,unityWorldPosition.x.ToString ("F4"),unityWorldPosition.y.ToString ("F4"),unityWorldPosition.z.ToString ("F4")
		);
	}

	public static void SaveRecording(string toPath)
	{
		string filePath = toPath + "/" + "UnityGazeExport.csv";
		File.WriteAllText(filePath, recordingString);
	}

	#endregion

	#region Gaze Processing

	public static void UpdateGazePostion( string key, float[] position )
	{
		switch (key)
		{
		case PupilSettings.gaze2DLeftEyeKey:
			PupilData._2D.LeftEyePosition.x = position [0];
			PupilData._2D.LeftEyePosition.y = position [1];
			if (isRecording)
				AddToRecording (key, PupilData._2D.LeftEyePosition, true);
//		    UnityEngine.Debug.Log ("Left eye position: " + PupilData._2D.LeftEyePosUDP.ToString());
			break;
		case PupilSettings.gaze2DRightEyeKey:
			PupilData._2D.RightEyePosition.x = position [0];
			PupilData._2D.RightEyePosition.y = position [1];
			if (isRecording)
				AddToRecording (key, PupilData._2D.RightEyePosition, true);
//			UnityEngine.Debug.Log ("Right Eye Position: " + PupilData._2D.RightEyePosUDP.ToString());
			break;
		case PupilSettings.gaze2DKey:
			PupilData._2D.Gaze2DPosUDP.x = position [0];
			PupilData._2D.Gaze2DPosUDP.y = position [1];
			if (isRecording)
				AddToRecording (key, PupilData._2D.Gaze2DPosUDP, true);
//		    UnityEngine.Debug.Log ("Gazepoint 2D: " + PupilData._2D.Gaze2DPosUDP.ToString());
			break;
		default:	// PupilSettings.gaze3DKey
			PupilData._3D.GazePosition.x = position [0] / PupilSettings.PupilUnitScalingFactor;
			PupilData._3D.GazePosition.y = - position [1] / PupilSettings.PupilUnitScalingFactor;
			PupilData._3D.GazePosition.z = position [2] / PupilSettings.PupilUnitScalingFactor;
			if (isRecording)
				AddToRecording (key, PupilData._3D.GazePosition);
//			UnityEngine.Debug.Log ("Gazepoint 3D: " + PupilData._3D.Gaze3DPosUDP.ToString());
			break;
		}
	}

	#endregion

	#region Calibration

	public static void RepaintGUI ()
	{
		if (WantRepaint != null)
			WantRepaint ();
	}

	public static Connection Connection
	{
		get { return Settings.connection; }
	}
	public static bool IsConnected
	{
		get { return Connection.isConnected; }
		set { Connection.isConnected = value; }
	}
	public static IEnumerator Connect(bool retry = false, float retryDelay = 5f)
	{
		yield return new WaitForSeconds (3f);

		while (!IsConnected) 
		{
			Connection.Initialize();

			if (!IsConnected)
            {
				if (retry) 
				{
                    UnityEngine.Debug.Log("Could not connect, Re-trying in 5 seconds ! ");
					yield return new WaitForSeconds (retryDelay);

				} else 
				{
					yield return null;
				}

			} 
			//yield return null;
        }
        UnityEngine.Debug.Log(" Succesfully connected to Pupil Service ! ");

        RepaintGUI();
		if (OnConnected != null)
			OnConnected();
        yield break;
    }

	public static void SubscribeTo (string topic)
	{
		Connection.InitializeSubscriptionSocket (topic);
	}

	public static void UnSubscribeFrom (string topic)
	{
		Connection.CloseSubscriptionSocket (topic);
	}

	public static Calibration Calibration
	{
		get { return Settings.calibration; }
	}
	private static Calibration.Mode _calibrationMode = Calibration.Mode._2D;
	public static Calibration.Mode CalibrationMode
	{
		get { return _calibrationMode; }
		set 
		{
			if (_calibrationMode != value)
			{
				_calibrationMode = value;
			}
		}
	}
	public static Calibration.Type CalibrationType
	{
		get { return Calibration.currentCalibrationType; }
	}

	public static void StartCalibration ()
	{
		if (IsGazing)
			PupilGazeTracker.Instance.StopVisualizingGaze ();
		
		if (OnCalibrationStarted != null)
			OnCalibrationStarted ();
		else
		{
			print ("No 'calibration started' delegate set");
		}

		Settings.calibration.InitializeCalibration ();

		IsCalibrating = true;

		byte[] calibrationData = new byte[ 1 + 2 * sizeof(ushort) + sizeof(float) ];
		calibrationData [0] = (byte) 'C';
		ushort hmdVideoFrameSize = 1000;
		byte[] frameSizeData = System.BitConverter.GetBytes (hmdVideoFrameSize);
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < sizeof(ushort); j++)
			{
				calibrationData [1 + i * sizeof(ushort) + j] = frameSizeData [j];
			}
		}
		float outlierThreshold = 35;
		byte[] outlierThresholdData = System.BitConverter.GetBytes (outlierThreshold);
		for (int i = 0; i < sizeof(float); i++)
		{
			calibrationData [1 + 2 * sizeof(ushort) + i] = outlierThresholdData [i];
		}
		Connection.sendData ( calibrationData );

		_calibrationData.Clear ();

		RepaintGUI ();
	}

	public static void StopCalibration ()
	{
		IsCalibrating = false;
		Settings.connection.sendCommandKey ('c');
	}

	public static void CalibrationFinished ()
	{
		DataProcessState = EStatus.Idle;

		print ("Calibration finished");

//		UnSubscribeFrom ("notify.calibration.successful");
//		UnSubscribeFrom ("notify.calibration.failed");

		if (OnCalibrationEnded != null)
			OnCalibrationEnded ();
		else
		{
			print ("No 'calibration ended' delegate set");
		}
	}

	public static void CalibrationFailed ()
	{
		DataProcessState = EStatus.Idle;

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
		Connection.sendRequestMessage (new Dictionary<string,object> {
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
		if (CalibrationMode == Calibration.Mode._3D)
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

        if (_calibrationData.Count > 40)
            AddCalibrationReferenceData();
    }

	#endregion

	public static void Disconnect()
	{
		if (OnDisconnecting != null)
			OnDisconnecting ();
		
		if (DataProcessState == EStatus.Calibration)
			PupilTools.StopCalibration ();

		// Starting/Stopping eye process is now part of initialization process
		//PupilTools.StopEyeProcesses ();

		Connection.CloseSubscriptionSocket("gaze");

		Connection.CloseSockets();
	}

#region CurrentlyNotSupportedOnHoloLens

	public static void StartPupilServiceRecording (string path)
	{
	}

	public static void StopPupilServiceRecording ()
	{
	}

	public static void StartBinocularVectorGazeMapper ()
	{
	}

	public static void StartFramePublishing ()
	{
	}

	public static void StopFramePublishing ()
	{
	}
		
#endregion

}
