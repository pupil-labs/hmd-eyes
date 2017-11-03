// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Threading;
using System.IO;
using System;

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

	public Recorder recorder = new Recorder ();

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

	Process serviceProcess;

	int _gazeFPS = 0;
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

		if (Settings.dataProcess.state == PupilSettings.EStatus.Calibration)
		{
			if (Settings.calibration.initialized)
				PupilTools.Calibrate ();
		}
		else if (Settings.connection.subscribeSocket != null)
			Settings.connection.subscribeSocket.Poll ();

		if (Input.GetKeyUp (KeyCode.C))
		{
			if (Settings.dataProcess.state == PupilSettings.EStatus.Calibration)
			{
				PupilTools.StopCalibration ();
			} else
			{
				PupilTools.StartCalibration ();
			}
		}
		if (Input.GetKeyUp (KeyCode.R))
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

		}

		if (OnUpdate != null)
			OnUpdate ();
	}

	#endregion

	public virtual void OnDrawGizmos ()
	{
		if (OnDrawGizmo != null)
			OnDrawGizmo ();
	}

	public void OnRenderObject ()
	{
		if (OnCalibDebug != null)
			OnCalibDebug ();

		if (OnCalibrationGL != null)
			OnCalibrationGL ();
	}

	void OnEnable ()
	{
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
	}

	void OnDisable ()
	{
		PupilGazeTracker._Instance = null;
		var pupilSettings = PupilSettings.Instance;
		PupilTools.SavePupilSettings (ref pupilSettings);
	}

	#region Start();

	void Start ()
	{
//		print ("Start of pupil gaze tracker");

		Settings = PupilTools.Settings;


		string str = PupilConversions.ReadStringFromFile ("camera_intrinsics");
		PupilConversions.ReadCalibrationData(str,ref PupilData.CalibrationData);

		Settings.framePublishing.StreamCameraImages = false;
		if (Settings.framePublishing.StreamCameraImages)
			Settings.framePublishing.InitializeFramePublishing ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;

		PupilData.calculateMovingAverage = true;

		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null)
		{
			OperatorMonitor.Instantiate ();
		}
		//OnOperatorMonitor += DrawOperatorMonitor;
		if (Settings.debugView.active)
			debugInstance.StartCalibrationDebugView ();

		PupilGazeTracker.Instance.ProjectName = Application.productName;

		Settings.connection.isConnected = false;
		Settings.dataProcess.state = PupilSettings.EStatus.Idle;

		if (Settings.connection.isAutorun)
			RunConnect ();
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
		if (Settings.connection.isLocal)
			PupilTools.RunServiceAtPath ();
		
		StartCoroutine (PupilTools.Connect (retry: true, retryDelay: 5f));
	}

	#region packet
	PupilMarker _markerLeftEye = new PupilMarker("LeftEye_2D");
	PupilMarker _markerRightEye = new PupilMarker("RightEye_2D");
	PupilMarker _markerGazeCenter = new PupilMarker("Gaze_2D");
	PupilMarker _gaze3D= new PupilMarker("Gaze_3D");

	public void StartVisualizingGaze ()
	{
		OnUpdate += VisualizeGaze;

		bool isCalibrationMode2D = Settings.calibration.currentCalibrationMode == Calibration.CalibMode._2D;
		_markerLeftEye.SetActive (isCalibrationMode2D);
		_markerLeftEye.SetMaterialColor (Color.green);
		_markerRightEye.SetActive (isCalibrationMode2D);
		_markerRightEye.SetMaterialColor (Color.blue);
		_markerGazeCenter.SetActive (isCalibrationMode2D);
		_markerGazeCenter.SetMaterialColor (Color.red);
		_gaze3D.SetActive (!isCalibrationMode2D);
		if (isCalibrationMode2D)
			PupilTools.SubscribeTo("gaze");
		else
			PupilTools.SubscribeTo("pupil.");
			
	}

	public void StopVisualizingGaze ()
	{
		OnUpdate -= VisualizeGaze;

		_markerLeftEye.SetActive (false);
		_markerRightEye.SetActive (false);
		_markerGazeCenter.SetActive (false);
		_gaze3D.SetActive (false);

		bool isCalibrationMode2D = Settings.calibration.currentCalibrationMode == Calibration.CalibMode._2D;
		if (isCalibrationMode2D)
			PupilTools.UnSubscribeFrom("gaze");
		else
			PupilTools.UnSubscribeFrom("pupil.");
	}

	void VisualizeGaze ()
	{
		if (Settings.dataProcess.state == PupilSettings.EStatus.ProcessingGaze)
		{
			if (Settings.calibration.currentCalibrationMode == Calibration.CalibMode._2D)
			{
				var eyeID = PupilData.eyeID;
				if (eyeID == PupilData.GazeSource.LeftEye || eyeID == PupilData.GazeSource.RightEye)
				{
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				}

				_markerLeftEye.UpdatePosition(PupilData._2D.GetEyeGaze (PupilData.GazeSource.LeftEye));
				_markerRightEye.UpdatePosition (PupilData._2D.GetEyeGaze (PupilData.GazeSource.RightEye));
				_markerGazeCenter.UpdatePosition (PupilData._2D.GetEyeGaze (PupilData.GazeSource.BothEyes));
			}

			if (Settings.calibration.currentCalibrationMode == Calibration.CalibMode._3D)
			{
				_gaze3D.position = PupilData._3D.Gaze ();
			}
		} 
	}

	#endregion

	void OnGUI ()
	{
		if (!isOperatorMonitor)
		{
			string str = "Capture Rate=" + FPS;
			str += "\nLeft Eye:" + PupilData._2D.GetEyeGaze(PupilData.GazeSource.LeftEye).ToString ();
			str += "\nRight Eye:" + PupilData._2D.GetEyeGaze(PupilData.GazeSource.RightEye).ToString ();
			GUI.TextArea (new Rect (50, 50, 200, 50), str);
		}

	}

	#region Recording

	public void OnRecording ()
	{
	}

	#endregion

	void OnApplicationQuit ()
	{

		#if UNITY_EDITOR // Operator window will only be available in Editor mode
		if (OperatorWindow.Instance != null)
			OperatorWindow.Instance.Close ();
		#endif

		if (Settings.dataProcess.state == PupilSettings.EStatus.Calibration)
			PupilTools.StopCalibration ();

		PupilTools.StopEyeProcesses ();

		Thread.Sleep (1);

		Settings.connection.CloseSockets();
			
		StopAllCoroutines ();

		PupilTools.RepaintGUI ();

		Pupil.processStatus.eyeProcess0 = false;
		Pupil.processStatus.eyeProcess1 = false;

	}
}
