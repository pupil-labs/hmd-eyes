// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using UnityEngine.UI;

//TEMP
//using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Threading;
using System.IO;
using System;
using NetMQ;
using NetMQ.Sockets;
//using MsgPack.Serialization;
using System.Linq;
//using System.Linq.Expressions;

//public delegate void Task();

[RequireComponent (typeof(PupilDataReceiver))]
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

	//	Thread _serviceThread;
	//	bool _isDone=false;

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

	//	//changed this
	//	bool _isconnected =false;
	//	public bool IsConnected{
	//		get{ return _isconnected; }
	//		set{_isconnected = value;}
	//	}

	//	[HideInInspector]
	//	public bool _isFullConnected =false;
	//	RequestSocket _requestSocket ;

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

	//	public CalibModeDetails CurrentCalibrationModeDetails{
	//		get{
	//			return CalibrationModes [CurrentCalibrationMode];
	//		}
	//	}
	//
	//	public CalibModes CurrentCalibrationMode{
	//		get {
	//			if (customInspector.calibrationMode == 0) {
	//				return CalibModes._2D;
	//			} else {
	//				return CalibModes._3D;
	//			}
	//		}
	//	}

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

	Process serviceProcess;

	int _gazeFPS = 0;
	int _currentFps = 0;
	DateTime _lastT;

	long lastTick;
	float elapsedTime;

	public int FPS
	{
		get{ return _currentFps; }
	}

	public enum EStatus
	{
		Idle,
		ProcessingGaze,
		Calibration
	}

	[HideInInspector]
	//	public EStatus m_status=EStatus.Idle;



	public PupilGazeTracker ()
	{
		_Instance = this;
	}
		

	//		public void RepaintGUI(){
	//		if (WantRepaint != null)
	//			WantRepaint ();
	//	}
	//

	#region Update

	void Update ()
	{
		if (Settings.framePublishing.StreamCameraImages)
		{
			//Put this in a function and delegate it to the OnUpdate delegate
			elapsedTime = (float)TimeSpan.FromTicks (DateTime.Now.Ticks - lastTick).TotalSeconds;
			if (elapsedTime >= (1f / Settings.framePublishing.targetFPS))
			{
				//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				AssignTexture (ref Settings.framePublishing.eye0Image, ref Settings.framePublishing.eye0ImageMaterial, Settings.framePublishing.raw0);
				AssignTexture (ref Settings.framePublishing.eye1Image, ref Settings.framePublishing.eye1ImageMaterial, Settings.framePublishing.raw1);
				lastTick = DateTime.Now.Ticks;
			}
		}

		if (OnUpdate != null)
			OnUpdate ();


		if (Input.GetKeyUp (KeyCode.C))
		{
			if (PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.Calibration)
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

	void InitializeEyes (ref bool eyeProcess)
	{
		if (!Pupil.processStatus.initialized)
		{
			eyeProcess = true;
			if (Pupil.processStatus.eyeProcess0 && Pupil.processStatus.eyeProcess1)
			{
				Pupil.processStatus.initialized = true;
				//UnSubscribeFrom ("pupil.");
			}
		}
	}

	void OnEnable ()
	{
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
	}

	void OnDisable ()
	{
		PupilGazeTracker._Instance = null;
	}

	#region Start();

	void Start ()
	{
//		print ("Start of pupil gaze tracker");

		Settings = Resources.Load<PupilSettings> ("PupilSettings");

		Settings.framePublishing.StreamCameraImages = false;

		string str = PupilConversions.ReadStringFromFile ("camera_intrinsics");
		PupilConversions.ReadCalibrationData(str,ref PupilData.CalibrationData);

		lastTick = DateTime.Now.Ticks;
		elapsedTime = 0f;

		if (Settings.framePublishing.StreamCameraImages)
			InitializeFramePublishing ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;

		PupilData.calculateMovingAverage = true;

		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null)
		{
			OperatorMonitor.Instantiate ();
		}
		//OnOperatorMonitor += DrawOperatorMonitor;
		if (PupilSettings.Instance.debugView.active)
			debugInstance.StartCalibrationDebugView ();

		PupilGazeTracker.Instance.ProjectName = Application.productName;
	}

	#endregion

	#region frame_publishing.functions

	public void CreateEye0ImageMaterial ()
	{
		if (!Settings.framePublishing.eye0ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			Settings.framePublishing.eye0ImageMaterial = new Material (shader);
			Settings.framePublishing.eye0ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	public void CreateEye1ImageMaterial ()
	{
		if (!Settings.framePublishing.eye1ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			Settings.framePublishing.eye1ImageMaterial = new Material (shader);
			Settings.framePublishing.eye1ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	public void InitializeFramePublishing ()
	{
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		Settings.framePublishing.eye0Image = new Texture2D (100, 100);
		Settings.framePublishing.eye1Image = new Texture2D (100, 100);
	}

	public void AssignTexture (ref Texture2D _eyeImage, ref Material _mat, byte[] data)
	{
		
		_eyeImage.LoadImage (data);
		_mat.mainTexture = _eyeImage;
	
	}

	#endregion

	// Andre: Where was/is this needed??
	//Check platform dependent path for pupil service, only if there is no custom PupilServicePathSet
	public void AdjustPath ()
	{
		
//		InitializePlatformsDictionary ();
//		if (PupilServicePath == "" && PlatformsDictionary.ContainsKey (Application.platform)) {
//			PupilServicePath = PlatformsDictionary [Application.platform] [0];
//			PupilServiceFileName = PlatformsDictionary [Application.platform] [1];
//			print ("Pupil service path is set to the default : " + PupilServicePath);
//		} else if (!PlatformsDictionary.ContainsKey (Application.platform)) {
//			print ("There is no platform default path set for " + Application.platform + ". Please set it under Settings/Platforms!");
//		}

	}

	public void InitializePlatformsDictionary ()
	{
		//		PlatformsDictionary = new Dictionary<RuntimePlatform, string[]> ();
		//		foreach (Platform p in Platforms) {
		//			PlatformsDictionary.Add (p.platform, new string[]{ p.DefaultPath, p.FileName });
		//		}
	}

	//Service is currently stored in Assets/Plugins/pupil_service_versionNumber . This path is hardcoded. See servicePath.
	public void RunServiceAtPath ()
	{
		AdjustPath ();
		string servicePath = PupilServicePath;
		if (File.Exists (servicePath))
		{
			if (Process.GetProcessesByName ("pupil_capture").Length > 0)
			{
				UnityEngine.Debug.LogWarning (" Pupil Capture is already running ! ");
			} else
			{
				serviceProcess = new Process ();
				serviceProcess.StartInfo.Arguments = servicePath;
				serviceProcess.StartInfo.FileName = servicePath;
				if (File.Exists (servicePath))
				{
					serviceProcess.Start ();
				} else
				{
					print ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
				}
			}
		} else
		{
			if (PupilServiceFileName == "")
			{
				print ("Pupil Service filename is not specified, most likely you will have to check if you have it set for the current platform under settings Platforms(DEV opt.)");
			}
		}
	}

	#region packet

	public void StartVisualizingGaze ()
	{
		OnUpdate += VisualizeGaze;

		if (Settings.visualizeGaze)
			CalibrationGL.InitializeVisuals (PupilSettings.EStatus.ProcessingGaze);
	}

	public void StopVisualizingGaze ()
	{
		OnUpdate -= VisualizeGaze;

		CalibrationGL.InitializeVisuals (PupilSettings.EStatus.Idle);
	}

	void VisualizeGaze ()
	{
		if (PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.ProcessingGaze)
		{
			PupilSettings.Calibration.Marker _markerLeftEye = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "LeftEye_2D").ToList () [0];
			PupilSettings.Calibration.Marker _markerRightEye = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "RightEye_2D").ToList () [0];
			PupilSettings.Calibration.Marker _markerGazeCenter = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "Gaze_2D").ToList () [0];

			if (PupilSettings.Instance.calibration.currentCalibrationMode == PupilSettings.Calibration.CalibMode._2D)
			{
				var eyeID = PupilData.eyeID;
				if (eyeID == PupilData.GazeSource.LeftEye || eyeID == PupilData.GazeSource.RightEye)
				{
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				}

				_markerLeftEye.position.x = PupilData._2D.GetEyeGaze (PupilData.GazeSource.LeftEye).x;
				_markerLeftEye.position.y = PupilData._2D.GetEyeGaze (PupilData.GazeSource.LeftEye).y;

				_markerRightEye.position.x = PupilData._2D.GetEyeGaze (PupilData.GazeSource.RightEye).x;
				_markerRightEye.position.y = PupilData._2D.GetEyeGaze (PupilData.GazeSource.RightEye).y;

				_markerGazeCenter.position.x = PupilData._2D.GazePosition.x;
				_markerGazeCenter.position.y = PupilData._2D.GazePosition.y;
			}

			if (PupilSettings.Instance.calibration.currentCalibrationMode == PupilSettings.Calibration.CalibMode._3D)
			{
				PupilSettings.Calibration.Marker gaze3D = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.calibMode == PupilSettings.Calibration.CalibMode._3D && !p.calibrationPoint).ToList () [0];

				gaze3D.position = PupilData._3D.Gaze ();

			}
		} 
	}

	#endregion



	void OnGUI ()
	{
		if (!isOperatorMonitor)
		{
			string str = "Capture Rate=" + FPS;
			str += "\nLeft Eye:" + PupilData._2D.LeftEyePos.ToString ();
			str += "\nRight Eye:" + PupilData._2D.RightEyePos.ToString ();
			GUI.TextArea (new Rect (50, 50, 200, 50), str);
		}

	}

	public void SwitchCalibrationMode ()
	{

		CalibrationGL.InitializeVisuals (PupilSettings.Instance.dataProcess.state);
	
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

		Pupil.processStatus.eyeProcess0 = false;
		Pupil.processStatus.eyeProcess1 = false;

	}
}
