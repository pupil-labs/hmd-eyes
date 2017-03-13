// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.IO;
using MsgPack.Serialization;

namespace Pupil
{
	//Pupil data types based on Yuta Itoh sample hosted in https://github.com/pupil-labs/hmd-eyes
	[Serializable]
	public class ProjectedSphere
	{
		public double[] axes = new double[] {0,0};
		public double angle;
		public double[] center = new double[] {0,0};
	}
	[Serializable]
	public class Sphere
	{
		public double radius;
		public double[] center = new double[] {0,0,0};
	}
	[Serializable]
	public class Circle3d
	{
		public double radius;
		public double[] center = new double[] {0,0,0};
		public double[] normal = new double[] {0,0,0};
	}
	[Serializable]
	public class Ellipse
	{
		public double[] axes = new double[] {0,0};
		public double angle;
		public double[] center = new double[] {0,0};
	}
	[Serializable]
	public class PupilData3D
	{
		public double diameter;
		public double confidence;
		public ProjectedSphere projected_sphere = new ProjectedSphere();
		public double theta;
		public int model_id;
		public double timestamp;
		public double model_confidence;
		public string method;
		public double phi;
		public Sphere sphere = new Sphere();
		public double diameter_3d;
		public double[] norm_pos = new double[] { 0, 0, 0 };
		public int id;
		public double model_birth_timestamp;
		public Circle3d circle_3d = new Circle3d();
		public Ellipse ellipese = new Ellipse();
		public double gaze_point_3d_x;
		public float gaze_point_3d_y;
		public float gaze_point_3d_z;
		public float a;
	}
}

public class PupilGazeTracker:MonoBehaviour
{
	static PupilGazeTracker _Instance;
	public static PupilGazeTracker Instance
	{
		get{
			if (_Instance == null) {
				_Instance = new GameObject("PupilGazeTracker").AddComponent<PupilGazeTracker> ();
			}
			return _Instance;
		}
	}
	class MovingAverage
	{
		List<float> samples=new List<float>();
		int length=5;

		public MovingAverage(int len)
		{
			length=len;
		}
		public float AddSample(float v)
		{
			samples.Add (v);
			while (samples.Count > length) {
				samples.RemoveAt (0);
			}
			float s = 0;
			for (int i = 0; i < samples.Count; ++i)
				s += samples [i];

			return s / (float)samples.Count;

		}
	}
	class EyeData
	{
		MovingAverage xavg;
		MovingAverage yavg;

		public EyeData(int len)
		{
			 xavg=new MovingAverage(len);
			 yavg=new MovingAverage(len);
		}
		public Vector2 gaze=new Vector2();
		public Vector2 AddGaze(float x,float y)
		{
			gaze.x = xavg.AddSample (x);
			gaze.y = yavg.AddSample (y);
			return gaze;
		}
	}
	EyeData leftEye;
	EyeData rightEye;

	Vector2 _eyePos;

	Thread _serviceThread;
	bool _isDone=false;
	Pupil.PupilData3D _pupilData;

	public delegate void OnCalibrationStartedDeleg(PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg(PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg(PupilGazeTracker manager);
	//public delegate void OnCalibDataDeleg(PupilGazeTracker manager,float x,float y);
	public delegate void OnCalibDataDeleg(PupilGazeTracker manager,object position);
	public delegate void DrawMenuDeleg ();

	public event OnCalibrationStartedDeleg OnCalibrationStarted;
	public event OnCalibrationDoneDeleg OnCalibrationDone;
	public event OnEyeGazeDeleg OnEyeGaze;
	public event OnCalibDataDeleg OnCalibData;
	public DrawMenuDeleg DrawMenu;

	bool _isconnected =false;
	RequestSocket _requestSocket ;

	List<Dictionary<string,object>> _calibrationData=new List<Dictionary<string,object>>();

	[SerializeField]
	Dictionary<string,object>[] _CalibrationPoints
	{
		get{ return _calibrationData.ToArray (); }
	}


	Vector2[] _calibPoints;
	int _calibSamples;
	int _currCalibPoint=0;
	int _currCalibSamples=0;

	[Serializable]
	public struct floatArray{
		public float[] axisValues;
	}
	public floatArray[] CalibPoints2D;
	public floatArray[] CalibPoints3D;

	public floatArray[] GetCalibPoints{
		get{ 
			return CalibrationModes [CurrentCalibrationMode].calibrationPoints;
		}
	}

	Dictionary<CalibModes,CalibModeDetails> CalibrationModes;


	public enum CalibModes{
		_2D,
		_3D
	};
	public struct CalibModeDetails
	{
		//public List<float[]> calibrationPoints;
		public floatArray[] calibrationPoints;
	}

	public string ServerIP = "127.0.0.1";
	public int ServicePort=50020;
	public int DefaultCalibrationCount=120;
	public int SamplesCount=4;
	public float CanvasWidth = 640;
	public float CanvasHeight=480;

	public int ServiceStartupDelay = 7000;//Time to allow the Service to start before connecting to Server.
	bool _serviceStarted = false;

	//CUSTOM EDITOR VARIABLES
	public int tab = 0;
	public int SettingsTab;
	public int calibrationMode = 0;
	public CalibModes CurrentCalibrationMode{
		get {
			if (calibrationMode == 0) {
				return CalibModes._2D;
			} else {
				return CalibModes._3D;
			}
		}
	}
	public GameObject CalibrationGameObject3D;
	public GameObject CalibrationGameObject2D;

	public bool isDebugFoldout;
	public bool ShowBaseInspector;
	public string PupilServicePath = "";
	public string PupilServiceFileName = "";
//	public string DefaultPathMac = "~/Applications/Pupil/";
//	public string DefaultPathWin = "~Program Files/Pupil Service";
//	public string DefaultPathLinux = "$HOME";
//	public string ServiceFileNameMac = "Service.app";
//	public string ServiceFileNameWin = "pupil_service.exe";
//	public string ServiceFileNameLinux = "pupil_service";
	//public RuntimePlatform platforms;

	public GUIStyle MainTabsStyle = new GUIStyle ();
	public GUIStyle SettingsLabelsStyle = new GUIStyle ();
	public GUIStyle SettingsValuesStyle = new GUIStyle ();
	public GUIStyle SettingsBrowseStyle = new GUIStyle ();
	public GUIStyle LogoStyle = new GUIStyle ();

	[Serializable]
	public struct Platform
	{
		public RuntimePlatform platform;
		public string DefaultPath;
		public string FileName;
	}
	public Platform[] Platforms;
	public Dictionary<RuntimePlatform, string[]> PlatformsDictionary;
	//CUSTOM EDITOR VARIABLES

	Process serviceProcess;

	int _gazeFPS = 0;
	int _currentFps = 0;
	DateTime _lastT;

	object _dataLock;

	public int FPS
	{
		get{ return _currentFps; }
	}

	enum EStatus
	{
		Idle,
		ProcessingGaze,
		Calibration
	}

	EStatus m_status=EStatus.Idle;

	public enum GazeSource
	{
		LeftEye,
		RightEye,
		BothEyes
	}

	public Vector2 NormalizedEyePos
	{
		get{ return _eyePos; }
	}

	public Vector2 EyePos
	{
		get{ return new Vector2((_eyePos.x-0.5f)*CanvasWidth,(_eyePos.y-0.5f)*CanvasHeight); }
	}
	public Vector2 LeftEyePos
	{
		get{ return leftEye.gaze; }
	}
	public Vector2 RightEyePos
	{
		get{ return rightEye.gaze; }
	}

	public Vector2 GetEyeGaze(GazeSource s)
	{
		if (s == GazeSource.RightEye)
			return RightEyePos;
		if (s == GazeSource.LeftEye)
			return LeftEyePos;
		return NormalizedEyePos;
	}
	
	public double Confidence
	{
		get
		{
			if (_pupilData == null){return 0;}
			return _pupilData.confidence;
		}
	}

	public PupilGazeTracker()
	{
		_Instance = this;
	}

	void Start()
	{
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		leftEye = new EyeData (SamplesCount);
		rightEye= new EyeData (SamplesCount);

		_dataLock = new object ();

		CalibrationModes = new Dictionary<CalibModes, CalibModeDetails> { 
			{
				CalibModes._2D,
				new CalibModeDetails (){ calibrationPoints = CalibPoints2D }
			},
			{
				CalibModes._3D,
				new CalibModeDetails (){ calibrationPoints = CalibPoints3D }
			}
		};

		RunServiceAtPath ();

		_serviceThread = new Thread(NetMQClient);
		_serviceThread.Start();

	}
	void OnDestroy()
	{
		if (m_status == EStatus.Calibration)
			StopCalibration ();
		_isDone = true;
		_serviceThread.Join();

	}

	NetMQMessage _sendRequestMessage(Dictionary<string,object> data)
	{
		NetMQMessage m = new NetMQMessage ();
		m.Append ("notify."+data["subject"]);

		using (var byteStream = new MemoryStream ()) {
			var ctx=new SerializationContext();
			ctx.CompatibilityOptions.PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.None;
			var ser= MessagePackSerializer.Get<object>(ctx);
			ser.Pack (byteStream, data);
			m.Append (byteStream.ToArray ());
		}

		_requestSocket.SendMultipartMessage (m);

		NetMQMessage recievedMsg;
		recievedMsg=_requestSocket.ReceiveMultipartMessage ();

		return recievedMsg;
	}

	float GetPupilTimestamp()
	{
		_requestSocket.SendFrame ("t");
		NetMQMessage recievedMsg=_requestSocket.ReceiveMultipartMessage ();
		return float.Parse(recievedMsg[0].ConvertToString());
	}

	void NetMQClient()
	{
		
		//thanks for Yuta Itoh sample code to connect via NetMQ with Pupil Service
		string IPHeader = ">tcp://" + ServerIP + ":";
		var timeout = new System.TimeSpan(0, 0, 1); //1sec

		// Necessary to handle this NetMQ issue on Unity editor
		// https://github.com/zeromq/netmq/issues/526
		AsyncIO.ForceDotNet.Force();
		NetMQConfig.ManualTerminationTakeOver();
		NetMQConfig.ContextCreate(true);

		string subport="";
		print ("Connect to the server: " + IPHeader + ServicePort + ".");
		Thread.Sleep (ServiceStartupDelay);

		_requestSocket = new RequestSocket(IPHeader + ServicePort);

		_requestSocket.SendFrame("SUB_PORT");
		_isconnected = _requestSocket.TryReceiveFrameString(timeout, out subport);
		print (_isconnected + " isconnected");
		_lastT = DateTime.Now;

		if (_isconnected)
		{
			//_serviceStarted = true;
			StartProcess ();
			var subscriberSocket = new SubscriberSocket( IPHeader + subport);

			subscriberSocket.Subscribe("gaze"); //subscribe for gaze data
			subscriberSocket.Subscribe("notify."); //subscribe for all notifications
			_setStatus(EStatus.ProcessingGaze);
			var msg = new NetMQMessage();
			while ( _isDone == false)
			{
				_isconnected = subscriberSocket.TryReceiveMultipartMessage(timeout,ref(msg));
				if (_isconnected)
				{
					try
					{
						string msgType=msg[0].ConvertToString();
						//Debug.Log(msgType);
						if(msgType=="gaze")
						{
							var message = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
							//print(message);
							MsgPack.MessagePackObject mmap = message.Value;
							lock (_dataLock)
							{
								_pupilData = JsonUtility.FromJson<Pupil.PupilData3D>(mmap.ToString());
								if(_pupilData.confidence>0.5f)
								{
									OnPacket(_pupilData);
								}
							}
						}
						//Debug.Log(message);
					}
					catch
					{
					//	Debug.Log("Failed to unpack.");
					}
				}
				else
				{
					print("Failed to receive a message.");
					Thread.Sleep(500);
				}
			}

			StopProcess ();
			subscriberSocket.Close();
		}
		else
		{
			print ("Failed to connect the server.");
			//If needed here could come a retry connection.
		}

		//Can only send request via IPC if the connection has been established, otherwise we are facing, errors and potential freezing.
		if (_serviceStarted && _isconnected)
			StopService ();
		
		_requestSocket.Close ();
		// Necessary to handle this NetMQ issue on Unity editor
		// https://github.com/zeromq/netmq/issues/526
		print("ContextTerminate.");
		NetMQConfig.ContextTerminate();



	}

	void _setStatus(EStatus st)
	{
		if(st==EStatus.Calibration)
		{
			_calibrationData.Clear ();
			_currCalibPoint = 0;
			_currCalibSamples = 0;
		}

		m_status = st;
	}

	public void StopService(){
		print ("Stopping service");
		_sendRequestMessage (new Dictionary<string,object> { { "subject","service_process.should_stop" }, { "eye_id",1 } });
		_sendRequestMessage (new Dictionary<string,object> { { "subject","service_process.should_stop" }, { "eye_id",0 } });
	}

	//Check platform dependent path for pupil service, only if there is no custom PupilServicePathSet
	public void AdjustPath(){
		InitializePlatformsDictionary ();
		if (PupilServicePath == "" && PlatformsDictionary.ContainsKey (Application.platform)) {
			PupilServicePath = PlatformsDictionary [Application.platform] [0];
			PupilServiceFileName = PlatformsDictionary [Application.platform] [1];
			print ("Pupil service path is set to the default : " + PupilServicePath);
		} else if (!PlatformsDictionary.ContainsKey (Application.platform)) {
			print ("There is no platform default path set for " + Application.platform + ". Please set it under Settings/Platforms!");
		}

	}

	//Service is currently stored in Assets/Plugins/pupil_service_versionNumber . This path is hardcoded. See servicePath.
	public void RunServiceAtPath(){
		AdjustPath ();
		string servicePath = PupilServicePath + "/";
		if (Directory.Exists (servicePath) && PupilServiceFileName != "") {
			serviceProcess = new Process ();
			serviceProcess.StartInfo.Arguments = servicePath;
			serviceProcess.StartInfo.FileName = servicePath + PupilServiceFileName;
			if (File.Exists (servicePath + PupilServiceFileName)) {
				serviceProcess.Start ();
				_serviceStarted = true;
			} else {
				print ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
			}
		} else{
			if (PupilServiceFileName == "") {
				print ("Pupil Service filename is not specified, most likely you will have to check if you have it set for the current platform under settings Platforms(DEV opt.)");
			}
			if (!Directory.Exists (servicePath)){
				print ("Pupil Service directory incorrect, please change under Settings");
			}
		}
	}

	public void StartProcess()
	{
		_sendRequestMessage (new Dictionary<string,object> {{"subject","eye_process.should_start.0"},{"eye_id",0}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_start.1"},{"eye_id",1}});
	}
	public void StopProcess()
	{
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",0}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",1}});
	}

	public void StartCalibration3D(){
		//This might be different for 3DbiocularCalibration
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name","HMD_Calibration"}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_start"},{"hmd_video_frame_size",new float[]{1000,1000}},{"outlier_threshold",35}});
		_setStatus (EStatus.Calibration);
	}

	public void StartCalibration()
	{
		print ("Calibrating!");
		//calibrate using default 9 points and 120 samples for each target
		StartCalibration (new Vector2[] {new Vector2 (0.5f, 0.5f), new Vector2 (0.2f, 0.2f), new Vector2 (0.2f, 0.5f),
			new Vector2 (0.2f, 0.8f), new Vector2 (0.5f, 0.8f), new Vector2 (0.8f, 0.8f),
			new Vector2 (0.8f, 0.5f), new Vector2 (0.8f, 0.2f), new Vector2 (0.5f, 0.2f)
		},DefaultCalibrationCount);
	}
	public void StartCalibration(Vector2[] calibPoints,int samples)
	{
		
		_calibPoints = calibPoints;
		_calibSamples = samples;

		_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name","HMD_Calibration"}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_start"},{"hmd_video_frame_size",new float[]{1000,1000}},{"outlier_threshold",35}});
		_setStatus (EStatus.Calibration);

		if (OnCalibrationStarted != null)
			OnCalibrationStarted (this);


	}
	public void StopCalibration()
	{
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_stop"}});
		if (OnCalibrationDone != null)
			OnCalibrationDone(this);
		_setStatus (EStatus.ProcessingGaze);
	}

	//we add function to this OnCalibData delegate in the PupilCalibMarker script
//	void _CalibData(float x,float y)
//	{
//		if(OnCalibData!=null)
//			OnCalibData (this,x, y);
//	}
//	void _CalibData(float x,float y, float z)
//	{
//		if(OnCalibData!=null)
//			OnCalibData (this,x, y, z);
//	}
	void _CalibData(float [] axisValues){
		//TODO:
		//object _o = axisValues.Length == 3 ? new Vector3 (axisValues [0], axisValues [1], axisValues [2]) : new Vector2 (axisValues [0], axisValues [1]);
		object _o;
		if (axisValues.Length == 3) {
			_o = new Vector3 (axisValues [0], axisValues [1], axisValues [2]);
		} else {
			_o = new Vector2 (axisValues [0], axisValues [1]);
		}
		if (OnCalibData != null)
			OnCalibData (this, _o);
	}

	public void InitializePlatformsDictionary(){
		PlatformsDictionary = new Dictionary<RuntimePlatform, string[]> ();
		foreach (Platform p in Platforms) {
			PlatformsDictionary.Add (p.platform, new string[]{ p.DefaultPath, p.FileName });
		}
	}


	void OnPacket(Pupil.PupilData3D data)
	{
		//add new frame
		_gazeFPS++;
		var ct=DateTime.Now;
		if((ct-_lastT).TotalSeconds>1)
		{
			_lastT=ct;
			_currentFps=_gazeFPS;
			_gazeFPS=0;
		}

		if (m_status == EStatus.ProcessingGaze) { //gaze processing stage

			float x,y;
			x = (float) data.norm_pos [0];
			y = (float)data.norm_pos [1];
			_eyePos.x = (leftEye.gaze.x + rightEye.gaze.x) * 0.5f;
			_eyePos.y = (leftEye.gaze.y + rightEye.gaze.y) * 0.5f;
			if (data.id == 0) {
				leftEye.AddGaze (x, y);
				if (OnEyeGaze != null)
					OnEyeGaze (this);
			} else if (data.id == 1) {
				rightEye.AddGaze (x, y);
				if (OnEyeGaze != null)
					OnEyeGaze (this);
			}


		} else if (m_status == EStatus.Calibration) {//gaze calibration stage
			float t=GetPupilTimestamp();

			floatArray[] _cPoints = GetCalibPoints;
			float[] _cPointFloatValues = _cPoints [_currCalibPoint].axisValues;


			var ref0=new Dictionary<string,object>(){{"norm_pos",_cPointFloatValues},{"timestamp",t},{"id",0}};
			var ref1=new Dictionary<string,object>(){{"norm_pos",_cPointFloatValues},{"timestamp",t},{"id",1}};

			//keeping this until the new calibration method is not yet tested
//			var ref0=new Dictionary<string,object>(){{"norm_pos",new float[]{_calibPoints[_currCalibPoint].x,_calibPoints[_currCalibPoint].y}},{"timestamp",t},{"id",0}};
//			var ref1=new Dictionary<string,object>(){{"norm_pos",new float[]{_calibPoints[_currCalibPoint].x,_calibPoints[_currCalibPoint].y}},{"timestamp",t},{"id",1}};

			//If OnCalibData delegate has assigned function from the Calibration Marker, assign the current calibration position to it.
			_CalibData (_cPointFloatValues);

			_calibrationData.Add (ref0);
			_calibrationData.Add (ref1);
			//Increment the current calibration sample. (Default sample amount per calibration point is 120)
			_currCalibSamples++;

			print ("Sampling at : " + _currCalibSamples);
			//give a small timeout per sample.
			Thread.Sleep (1000 / 60);

			//If the current calibration sample is bigger or equal to the desired sampling (so we accomplished sampling for this calibration point),
			//null the current sample and step to next calbration point.
			//Also prepare calibration data for sending, and send it.
			if (_currCalibSamples >= _calibSamples) {
				_currCalibSamples = 0;
				_currCalibPoint++;

				//reformat the calibration data for sending.
				string pointsData="[";
				int index = 0;
				foreach (var v in _calibrationData) {
					pointsData+= JsonUtility.ToJson(v);//String.Format("{'norm_pos':({0},{1}),'timestamp':{2},'id':{3}}",v.norm_pos[0],v.norm_pos[1],v.timestamp,v.id);
					++index;
					if (index != _calibrationData.Count) {
						pointsData += ",";
					}
				}
				pointsData += "]";

			//	pointsData = JsonUtility.ToJson (_CalibrationPoints);
				//Debug.Log (pointsData);

				//Send the current relevant calibration data for the current calibration point.
				_sendRequestMessage (new Dictionary<string,object> {{"subject","calibration.add_ref_data"},{"ref_data",_CalibrationPoints}});

				//Clear the current calibration data, so we can proceed to the next point if there is any.
				_calibrationData.Clear ();

				//Stop calibration if we accomplished all required calibration target.
				if (_currCalibPoint >= _cPoints.Length) {
					StopCalibration ();
				}
			}
		}
	}

	void OnGUI()
	{
		string str="Capture Rate="+FPS;
		str += "\nLeft Eye:" + LeftEyePos.ToString ();
		str += "\nRight Eye:" + RightEyePos.ToString ();
		GUI.TextArea (new Rect (0, 0, 200, 50), str);
	}
}
