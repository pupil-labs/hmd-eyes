// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System;
using Pupil;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PupilGazeTracker:MonoBehaviour
{
	public PupilSettings Settings;

	static PupilGazeTracker _Instance;
	public static PupilGazeTracker Instance
	{
		get
		{
			if (_Instance == null)
			{
				_Instance = new GameObject ("PupilGazeTracker").AddComponent<PupilGazeTracker> ();
			}
			return _Instance;
		}
	}

	static PupilGazeTrackerDebug _debugInstance;
	public PupilGazeTrackerDebug debugInstance
	{
		get
		{
			if (_debugInstance == null)
			{
				_debugInstance = new GameObject ("").AddComponent<PupilGazeTrackerDebug> ();
			}
			return _debugInstance;
		}
	}

	public string ProjectName;

	#region delegates

	public delegate void OnCalibrationStartedDeleg (PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg (PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg (PupilGazeTracker manager);
	public delegate void OnCalibrationGLDeleg ();
	public delegate void OnUpdateDeleg ();
	public delegate void DrawMenuDeleg ();
	public delegate void OnCalibDebugDeleg ();
	public delegate void OnOperatorMonitorDeleg ();
	public delegate void OnDrawGizmoDeleg ();

	public event OnEyeGazeDeleg OnEyeGaze;

	public DrawMenuDeleg DrawMenu;
	public OnCalibrationGLDeleg OnCalibrationGL;
	public OnCalibDebugDeleg OnCalibDebug;
	public OnOperatorMonitorDeleg OnOperatorMonitor;
	public OnDrawGizmoDeleg OnDrawGizmo;
	public OnUpdateDeleg OnUpdate;

	#endregion

	#region operator_monitor_vars

	[HideInInspector]
	public bool isOperatorMonitor;

	public Operator.properties[] OperatorMonitorProperties;

	#endregion

	//	[HideInInspector]
	//	public int ServicePort=50020;
	[HideInInspector]
	public float CanvasWidth = 640;
	[HideInInspector]
	public float CanvasHeight = 480;
	//	[HideInInspector]
	//	public int ServiceStartupDelay = 7000;//Time to allow the Service to start before connecting to Server.
	//	bool _serviceStarted = false;
	//	bool _calibPointTimeOut = true;

	//CUSTOM EDITOR VARIABLES

	[HideInInspector]
	public bool saved = false;

	[HideInInspector]
	public int SettingsTab;

	[HideInInspector]
	public int Codec = 1;

	//[HideInInspector]
	//public bool AdvancedSettings;
	[HideInInspector]
	public string PupilServicePath = "";
	[HideInInspector]
	public string PupilServiceFileName = "";

	[HideInInspector]
	public List<GUIStyle> Styles = new List<GUIStyle> ();
	[HideInInspector]
	public GUIStyle FoldOutStyle = new GUIStyle ();
	[HideInInspector]
	public GUIStyle ButtonStyle = new GUIStyle ();
	[HideInInspector]
	public GUIStyle TextField = new GUIStyle ();
	[HideInInspector]
	public GUIStyle CalibRowStyle = new GUIStyle ();

	int _currentFps = 0;
	DateTime _lastT;

	public int FPS
	{
		get{ return _currentFps; }
	}

	public PupilGazeTracker ()
	{
		_Instance = this;
	}
		
	#region Update
	void Update ()
	{
		Settings.framePublishing.UpdateEyeTextures ();

		if (PupilTools.DataProcessState == EStatus.Calibration)
		{
			PupilTools.Calibration.UpdateCalibration ();
		} 

		PupilTools.Connection.UpdateSubscriptionSockets ();

		if (Input.GetKeyUp (KeyCode.C))
		{
			if (PupilTools.DataProcessState == EStatus.Calibration)
			{
				PupilTools.StopCalibration ();
			} else
			{
				PupilTools.StartCalibration ();
			}
		}
#if !UNITY_WSA
		if (Input.GetKeyUp (KeyCode.R))
		{
			if (PupilTools.IsConnected)
			{
				if (!Recorder.isRecording)
				{
					Recorder.isRecording = true;
					Recorder.Start ();
				} else
				{
					Recorder.isRecording = false;
					Recorder.Stop ();
				}
			} else
				print ("Can not start recording without connection to pupil service");
		}
#endif

		if (Instance.OnUpdate != null)
			Instance.OnUpdate ();
	}

	#endregion

	public virtual void OnDrawGizmos ()
	{
		if (Instance.OnDrawGizmo != null)
			Instance.OnDrawGizmo ();
	}

	public void OnRenderObject ()
	{
		if (Instance.OnCalibDebug != null)
			Instance.OnCalibDebug ();

		if (Instance.OnCalibrationGL != null)
			Instance.OnCalibrationGL ();
	}

	void OnEnable ()
	{
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
	}

	void OnDisable ()
	{
		CloseShop ();

		PupilGazeTracker._Instance = null;
		var pupilSettings = PupilSettings.Instance;
		SavePupilSettings (ref pupilSettings);
	}

	public static void SavePupilSettings (ref PupilSettings pupilSettings)
	{
#if UNITY_EDITOR
		AssetDatabase.Refresh ();
		EditorUtility.SetDirty (pupilSettings);
		AssetDatabase.SaveAssets ();
#endif

	}
	#region Start();

	void Start ()
	{
//		print ("Start of pupil gaze tracker");

		Settings = PupilSettings.Instance;


		string str = PupilConversions.ReadStringFromFile ("camera_intrinsics");
		PupilConversions.ReadCalibrationData(str,ref PupilData.CalibrationData);

		Settings.framePublishing.StreamCameraImages = false;
		if (Settings.framePublishing.StreamCameraImages)
			Settings.framePublishing.InitializeFramePublishing ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;

#if !UNITY_WSA
		PupilData.calculateMovingAverage = true;
#endif
		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null)
		{
			OperatorMonitor.Instantiate ();
		}
		//OnOperatorMonitor += DrawOperatorMonitor;
		if (Settings.debugView.active)
			debugInstance.StartCalibrationDebugView ();

		PupilGazeTracker.Instance.ProjectName = Application.productName;

		PupilTools.IsConnected = false;
		PupilTools.DataProcessState = EStatus.Idle;

		var relativeRightEyePosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.RightEye) - UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye);
		PupilTools.Calibration.rightEyeTranslation = new float[] { relativeRightEyePosition.z*PupilSettings.PupilUnitScalingFactor, 0, 0 };
		var relativeLeftEyePosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.LeftEye) - UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye);
		PupilTools.Calibration.leftEyeTranslation = new float[] { relativeLeftEyePosition.z*PupilSettings.PupilUnitScalingFactor, 0, 0 };

#if !UNITY_WSA
		if (PupilTools.Connection.isAutorun)
			RunConnect ();
#endif
	}

	#endregion

	//Check platform dependent path for pupil service, only if there is no custom PupilServicePathSet
	[Serializable]
	public struct Platform
	{
		public RuntimePlatform platform;
		public string DefaultPath;
		public string FileName;
	}
	[HideInInspector]
	public Platform[] Platforms;
	[HideInInspector]
	public Dictionary<RuntimePlatform, string[]> PlatformsDictionary;
	public void AdjustPath ()
	{
		PlatformsDictionary = new Dictionary<RuntimePlatform, string[]> ();
		foreach (Platform p in Platforms) 
		{
			PlatformsDictionary.Add (p.platform, new string[]{ p.DefaultPath, p.FileName });
		}
		if (PupilServicePath == "" && PlatformsDictionary.ContainsKey (Application.platform)) 
		{
			PupilServicePath = PlatformsDictionary [Application.platform] [0];
			PupilServiceFileName = PlatformsDictionary [Application.platform] [1];
			print ("Pupil service path is set to the default : " + PupilServicePath);
		} 
		else if (!PlatformsDictionary.ContainsKey (Application.platform)) 
		{
			print ("There is no platform default path set for " + Application.platform + ". Please set it under Settings/Platforms!");
		}
	}

	public void RunConnect()
	{
#if !UNITY_WSA
		if (PupilTools.Connection.isLocal)
			RunServiceAtPath ();
#endif
		
		StartCoroutine (PupilTools.Connect (retry: true, retryDelay: 5f));
	}

	public static void RunServiceAtPath (bool runEyeProcess = false)
	{
#if !UNITY_WSA
		string servicePath = PupilSettings.Instance.pupilServiceApp.servicePath;
		if (File.Exists (servicePath))
		{
			if ( (Process.GetProcessesByName ("pupil_capture").Length > 0) || (Process.GetProcessesByName ("pupil_service").Length > 0) )
			{
				UnityEngine.Debug.LogWarning (" Pupil Capture/Service is already running ! ");
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
#else
		print("Process can not be started in UWP environment");
#endif
	}

	#region packet
	PupilMarker _markerLeftEye;
	PupilMarker _markerRightEye;
	PupilMarker _markerGazeCenter;
	PupilMarker _gaze3D;

	public void StartVisualizingGaze ()
	{
		Instance.OnUpdate += VisualizeGaze;

        PupilSettings.Instance.currentCamera = Camera.main;

        if ( !PupilMarker.TryToReset(_markerLeftEye) )
			_markerLeftEye= new PupilMarker("LeftEye_2D",Color.green);
		if ( !PupilMarker.TryToReset(_markerRightEye) )
			_markerRightEye = new PupilMarker("RightEye_2D",Color.blue);
		if ( !PupilMarker.TryToReset(_markerGazeCenter) )
			_markerGazeCenter = new PupilMarker("Gaze_2D",Color.red);
		if ( !PupilMarker.TryToReset(_gaze3D) )
			_gaze3D = new PupilMarker("Gaze_3D", Color.yellow);

		PupilTools.DataProcessState = EStatus.ProcessingGaze;
		PupilTools.SubscribeTo("gaze");
	}

	

	public void StopVisualizingGaze ()
	{
		Instance.OnUpdate -= VisualizeGaze;

		_markerLeftEye.SetActive (false);
		_markerRightEye.SetActive (false);
		_markerGazeCenter.SetActive (false);
		_gaze3D.SetActive (false);

		PupilTools.UnSubscribeFrom("gaze");
	}

	void VisualizeGaze ()
	{
		if (PupilTools.DataProcessState == EStatus.ProcessingGaze)
		{
			if (PupilTools.CalibrationMode == Calibration.Mode._2D)
			{
				var eyeID = PupilData.currentEyeID;
				if (eyeID == GazeSource.LeftEye || eyeID == GazeSource.RightEye)
				{
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				}

				_markerLeftEye.UpdatePosition(PupilData._2D.GetEyeGaze (GazeSource.LeftEye));
				_markerRightEye.UpdatePosition (PupilData._2D.GetEyeGaze (GazeSource.RightEye));
				_markerGazeCenter.UpdatePosition (PupilData._2D.GetEyeGaze (GazeSource.BothEyes));
			}
			else if (PupilTools.CalibrationMode == Calibration.Mode._3D)
			{
				_gaze3D.UpdatePosition(PupilData._3D.GazePosition);
			}
		} 
	}

	#endregion

	void OnGUI ()
    {
#if !UNITY_WSA
		if (!isOperatorMonitor)
		{
			string str = "Capture Rate=" + FPS;
			str += "\nLeft Eye:" + PupilData._2D.GetEyeGaze(GazeSource.LeftEye).ToString ();
			str += "\nRight Eye:" + PupilData._2D.GetEyeGaze(GazeSource.RightEye).ToString ();
			GUI.TextArea (new Rect (50, 50, 200, 50), str);
		}
#endif
	}

#region Recording

	public void OnRecording ()
	{
	}

#endregion

	void OnApplicationQuit()
	{
		CloseShop ();
	}

	public void CloseShop ()
	{
#if UNITY_EDITOR // Operator window will only be available in Editor mode
		if (OperatorWindow.Instance != null)
			OperatorWindow.Instance.Close ();
#endif

		PupilTools.Disconnect ();
			
		StopAllCoroutines ();
#if !UNITY_WSA
		if (Recorder.isRecording)
		{
			Recorder.Stop ();
		}
#endif
		PupilTools.RepaintGUI ();

		processStatus.eyeProcess0 = false;
		processStatus.eyeProcess1 = false;
	}
}
