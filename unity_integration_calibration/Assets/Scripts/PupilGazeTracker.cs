// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
//TEMP	
//using UnityEditor;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Net;
using System.Threading;
using System.IO;
using NetMQ;
using NetMQ.Sockets;
using System;
using MsgPack.Serialization;
using System.Linq;

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
	public class eyes3Ddata{
		public double[] zero = new double[]{0,0,0};
		public double[] one = new double[]{ 0, 0, 0 };
	}

	[Serializable]
	public class BaseData{
		public Circle3d circle_3d;
		public string topic;
		public double diameter;
		public double confidence;
		public string method;
		public double model_birth_timestamp;
		public double theta;
		public double[] norm_pos = new double[]{ 0, 0, 0 };
		public Ellipse ellipse;
		public double model_confidence;
		public int id;
		public double timestamp;
		public Sphere sphere;
		public ProjectedSphere projected_sphere;
		public double diameter_3d;
		public int model_id;
		public double phi;
	}
	[Serializable]
	public class PupilData{
		public int id;
		public eyes3Ddata gaze_normals_3d;
		public eyes3Ddata eye_centers_3d;
		public double[] gaze_point_3d = new double[]{ 0, 0, 0 };
		public string topic;
		public double confidence;
		public double timestamp;
		public double[] norm_pos = new double[]{ 0, 0, 0 };
		public BaseData[] base_data;
	}

}
[Serializable]
public struct floatArray{
	public float[] axisValues;
}
	
[Serializable]
public class CalibPoints
{
	public List<floatArray> list3D;
	public List<floatArray> list2D;
	public void SetVector(List<floatArray> floatArrayList, Vector3 _v3, int index){
		floatArrayList [index].axisValues [0] = _v3.x;
		floatArrayList [index].axisValues [1] = _v3.y;
		try{
		floatArrayList [index].axisValues [2] = _v3.z;
		}catch{}
	}
	public object GetVector(List<floatArray> floatArrayList, int index){
		if (floatArrayList [index].axisValues.Count() == 3) 
			return new Vector3 (floatArrayList [index].axisValues [0], floatArrayList [index].axisValues [1], floatArrayList [index].axisValues [2]);
		if (floatArrayList [index].axisValues.Count() == 2) 
			return new Vector3 (floatArrayList [index].axisValues [0], floatArrayList [index].axisValues [1], 0);
		return new object ();
	}

	public List<floatArray> Get2DList(){
		if (list2D == null)
			list2D = new List<floatArray> ();
		return list2D;
	}
	public List<floatArray> Get3DList(){
		if (list3D == null)
			list3D = new List<floatArray> ();
		return list3D;
	}
	public List<floatArray> GetActiveList(PupilGazeTracker.CalibModes currentCalibMode){
		if (currentCalibMode == PupilGazeTracker.CalibModes._2D)
			return list2D;
		if (currentCalibMode == PupilGazeTracker.CalibModes._3D)
			return list3D;
		return new List<floatArray> ();
	}
	public string[] GetPointNames(PupilGazeTracker.CalibModes currentCalibMode){
		List<string> _s = new List<string> ();
		int i = 0;
		foreach (floatArray _fA in GetActiveList(currentCalibMode)) {
			i++;
			_s.Add (currentCalibMode.ToString ().Substring (1) + " Calibration Point " + i);
		}
		return _s.ToArray ();
	}
	public void Remove2DPoint(floatArray incoming){
		if (list2D == null)
			return;
		list2D.Remove (incoming);
	}
	public void Remove3DPoint(floatArray incoming){
		if (list3D == null)
			return;
		list3D.Remove (incoming);
	}
	public void Add3DPoint(floatArray incoming){
		if (list3D == null)
			list3D = new List<floatArray> ();
		list3D.Add (incoming);
	}
	public void Add2DPoint(floatArray incoming){
		if (list2D == null)
			list2D = new List<floatArray> ();
		list2D.Add (incoming);
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
		MovingAverage zavg;

		public EyeData(int len)
		{
			 xavg=new MovingAverage(len);
			 yavg=new MovingAverage(len);
			 zavg=new MovingAverage(len);
		}
		public Vector2 gaze2D = new Vector2 ();
		public Vector2 AddGaze(float x,float y)
		{
			gaze2D.x = xavg.AddSample (x);
			gaze2D.y = yavg.AddSample (y);
			return gaze2D;
		}

		public Vector3 gaze3D = new Vector3 ();
		public Vector3 AddGaze(float x, float y,float z)
		{
			gaze3D.x = xavg.AddSample (x);
			gaze3D.y = yavg.AddSample (y);
			gaze3D.z = zavg.AddSample (z);
			return gaze3D;
		}
	}
	EyeData leftEye;
	EyeData rightEye;

	Vector2 _eyeGazePos2D;
	Vector3 _eyeGazePos3D;

	Thread _serviceThread;
	bool _isDone=false;

	Pupil.PupilData _pupilData;

	public delegate void OnCalibrationStartedDeleg(PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg(PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg(PupilGazeTracker manager);
	//public delegate void OnCalibDataDeleg(PupilGazeTracker manager,float x,float y);
	public delegate void OnCalibDataDeleg(PupilGazeTracker manager,object position);
	public delegate void DrawMenuDeleg ();
//	public delegate void OnConnectedDeleg (PupilGazeTracker manager);
	public delegate void OnSwitchCalibPointDeleg(PupilGazeTracker manager);
	public delegate void OnCalibDebugDeleg();

	public event OnCalibrationStartedDeleg OnCalibrationStarted;
	public event OnCalibrationDoneDeleg OnCalibrationDone;
	public event OnEyeGazeDeleg OnEyeGaze;
	public event OnCalibDataDeleg OnCalibData;
	public DrawMenuDeleg DrawMenu;
//	public event OnConnectedDeleg OnConnected;
	public event OnSwitchCalibPointDeleg OnSwitchCalibPoint;

	public OnCalibDebugDeleg OnCalibDebug;

	bool _isconnected =false;
	public bool _isFullConnected =false;
	RequestSocket _requestSocket ;

	List<Dictionary<string,object>> _calibrationData=new List<Dictionary<string,object>>();
	public Dictionary<string,object> dict = new Dictionary<string, object>();

	[SerializeField]
	Dictionary<string,object>[] _CalibrationPoints
	{
		get{ return _calibrationData.ToArray (); }
	}
		
	//Vector2[] _calibPoints;
	int _calibSamples;
	int _currCalibPoint=0;
	int _currCalibSamples=0;

	//private static CalibPoints _cpoints;
	//public List<floatArray> CalibPoints2D;
	public CalibPoints _calibPoints = new CalibPoints();

	//TODO: replace this
	public floatArray[] GetCalibPoints{
		get{ 
			return CalibrationModes [CurrentCalibrationMode].calibrationPoints.ToArray ();
		}
	}

	public Dictionary<CalibModes,CalibModeDetails> CalibrationModes{
		get{
			Dictionary<CalibModes, CalibModeDetails> _calibModes = new Dictionary<CalibModes, CalibModeDetails> ();
			_calibModes.Add (CalibModes._2D, new CalibModeDetails () {
				calibrationPoints = _calibPoints.Get2DList(),
				calibPlugin = "HMD_Calibration",
				positionKey = "norm_pos"
			});
			_calibModes.Add (CalibModes._3D, new CalibModeDetails () {
				calibrationPoints = _calibPoints.Get3DList(),
				calibPlugin = "HMD_Calibration_3D",
				positionKey = "mm_pos"
			});
			return _calibModes;
		}
	}


	public enum CalibModes{
		_2D,
		_3D
	};
	public struct CalibModeDetails
	{
		public List<floatArray> calibrationPoints;
		public string positionKey;//A string that refers to a value in the ref_data in 2D its norm_pos in 3D its mm_pos
		public string calibPlugin;//Currently containing HMD_CALIBRATION and HMD_CALIBRATION_3D
		public Type type;
	}

	public string ServerIP = "127.0.0.1";
	public int ServicePort=50020;
	public int DefaultCalibrationCount=120;
	public int SamplesCount=4;
	public float CanvasWidth = 640;
	public float CanvasHeight=480;

	public int ServiceStartupDelay = 7000;//Time to allow the Service to start before connecting to Server.
	bool _serviceStarted = false;
	bool _calibPointTimeOut = true;

	public struct DebugVars{
		public bool printMessage;	
	}
	public DebugVars debugVars = new DebugVars();

	public bool printSampling;
	public bool printMessage;
	public bool printMessageType;
	//CUSTOM EDITOR VARIABLES

	public Vector2 Calibration2DScale;
	public bool saved = false;

	public int editedCalibIndex = 0;

	public bool CalibrationPointsFoldout;
	public bool CalibrationPoints2DFoldout;
	public bool CalibrationPoints3DFoldout;
	public int tab = 0;
	public int SettingsTab;
	public int calibrationMode = 0;
	public bool calibrationDebugMode = false;
	public int connectionMode = 0;
	public CalibModeDetails CurrentCalibrationModeDetails{
		get{
			return CalibrationModes [CurrentCalibrationMode];
		}
	}
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

	public GUIStyle MainTabsStyle = new GUIStyle ();
	public GUIStyle SettingsLabelsStyle = new GUIStyle ();
	public GUIStyle SettingsValuesStyle = new GUIStyle ();
	public GUIStyle SettingsBrowseStyle = new GUIStyle ();
	public GUIStyle LogoStyle = new GUIStyle ();
	public GUIStyle FoldOutStyle = new GUIStyle ();
	public GUIStyle ButtonStyle = new GUIStyle();
	public GUIStyle TextField = new GUIStyle();

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

	public enum EStatus
	{
		Idle,
		ProcessingGaze,
		Calibration
	}

	public EStatus m_status=EStatus.Idle;

	public enum GazeSource
	{
		LeftEye,
		RightEye,
		BothEyes
	}

	public Vector2 NormalizedEyePos2D
	{
		get{ return _eyeGazePos2D; }
	}

	public Vector2 NormalizedEyePos3D
	{
		get{ return _eyeGazePos3D; }
	}
	public Vector2 EyePos2D
	{
		get{ return new Vector2((_eyeGazePos2D.x-0.5f)*CanvasWidth,(_eyeGazePos2D.y-0.5f)*CanvasHeight); }
	}
	public Vector2 EyePos3D
	{
		get{ return new Vector3 (_eyeGazePos3D.x, _eyeGazePos3D.y, _eyeGazePos3D.z); }
	}
	public Vector2 LeftEyePos
	{
		get{ return leftEye.gaze2D; }
	}
	public Vector2 RightEyePos
	{
		get{ return rightEye.gaze2D; }
	}


		
	public Vector2 GetEyeGaze2D(GazeSource s){
		if (s == GazeSource.RightEye)
			return RightEyePos;
		if (s == GazeSource.LeftEye)
			return LeftEyePos;
		return NormalizedEyePos2D;
	}
	public Vector3 GetEyeGaze3D(GazeSource s){
		return _eyeGazePos3D;
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


	void Update(){
		//CalibrationDebugMode.Instantiate ();
		//CalibrationDebugMode.OnRenderObject();
	}
	public static int lineCount = 100;
	public static float radius = 3.0f;

	static Material lineMaterial;

	static void CreateLineMaterial ()
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 0);
		}
	}
	public void DrawCalibrationDebug(){
		CreateLineMaterial ();
		// Apply the line material
		lineMaterial.SetPass (0);

		DrawCameraFrustum (Camera.main.transform, fov, aspectRatios.FULLHD, MinViewDistance, MaxViewDistance);
		GL.wireframe = true;
		DrawDebugSphere (Camera.main.transform,  Vector3.zero, 0);//eye0
		DrawDebugSphere (Camera.main.transform,  Vector3.zero, 1);//eye1
		GL.wireframe = false;
	}
	public void OnRenderObject(){
		if (OnCalibDebug != null)
			OnCalibDebug ();
	}
	public float fov = 60;
	public float MinViewDistance = 20;
	public float MaxViewDistance = 200;
	public float scale = 1;
	public float cameraGizmoLength = 50;
	public Mesh DebugEyeMesh;

	public enum aspectRatios{FULLVIVE,HALFVIVE,FULLHD};

	public class Rect3D
	{
		public float width;
		public float height;
		public float zOffset;
		public float scale;
		public Vector3[] verticies = new Vector3[4];

		public void SetPosition(){
			verticies [0] = new Vector3 (-(width / 2)*scale, -(height / 2)*scale, zOffset);
			verticies [1] = new Vector3 ((width / 2)*scale, -(height / 2)*scale, zOffset);
			verticies [2] = new Vector3 ((width / 2)*scale, (height / 2)*scale, zOffset);
			verticies [3] = new Vector3 (-(width / 2)*scale, (height / 2)*scale, zOffset);
		}
		public void Draw(float _width, float _height, float _zOffset, float _scale){
			width = _width;
			height = _height;
			zOffset = _zOffset;
			scale = _scale;
			SetPosition ();
			for (int i = 0; i <= verticies.Count() - 1; i++) {
				GL.Vertex (verticies [i]);
				if (i != verticies.Count() - 1) {
					GL.Vertex (verticies [i + 1]);
				} else {
					GL.Vertex (verticies [0]);
				}
			}
		}
	}

	public void DrawDebugSphere(Transform matrix, Vector3 origin, int eyeID){
		CreateLineMaterial ();
		Matrix4x4 _m = new Matrix4x4 ();
		_m.SetTRS (new Vector3 (10, 0, 0), Quaternion.identity, new Vector3 (20, 20, 20));
		//Graphics.DrawMeshNow(DebugEyeMesh, Matrix4x4.TRS(origin, Quaternion.identity, new Vector3(20,20,20)));
		Graphics.DrawMeshNow(DebugEyeMesh, Camera.main.transform.localToWorldMatrix*_m);

		//Graphics.DrawMeshNow()
	}

	public void DrawCameraFrustum(Transform origin, float fov, aspectRatios aspect, float minViewDistance, float maxViewDistance){
		GL.PushMatrix ();

		float aspectRatio = 1;

		switch (aspect) {
		case aspectRatios.FULLHD:
			aspectRatio = 1.7777f;
			break;
		case aspectRatios.FULLVIVE:
			aspectRatio = 1.8f;
			break;
		case aspectRatios.HALFVIVE:
			aspectRatio = 0.9f;
			break;
		}
		Vector3 up = origin.up;
		Rect3D farPlaneRect = new Rect3D ();
		Rect3D nearPlaneRect = new Rect3D ();



		GL.MultMatrix (origin.localToWorldMatrix);

		GL.Begin (GL.LINES);
		float ratio =  Mathf.Sin( ((fov/2)*Mathf.PI)/180 )/Mathf.Sin(  (   (  ((180-fov)/2)*Mathf.PI   )/180    ) );

		float widthMinView = (ratio * minViewDistance * 2) * -1;
		float heightMinView = widthMinView/aspectRatio;
		float widthMaxView = (ratio * maxViewDistance * 2) * -1;
		float heightMaxView = widthMaxView/aspectRatio;


		nearPlaneRect.Draw (widthMinView, heightMinView, minViewDistance, 1);
		farPlaneRect.Draw (widthMaxView, heightMaxView, maxViewDistance, 1);

		//ConnectRectangles
		for (int i = 0; i < nearPlaneRect.verticies.Count (); i++) {
			GL.Vertex (nearPlaneRect.verticies[i]);
			GL.Vertex (farPlaneRect.verticies[i]);
		}

		//Draw Gizmo
		//X
		GL.Color (Color.red);
		GL.Vertex(Vector3.zero);
		GL.Vertex(origin.right*cameraGizmoLength);
		//Y
		GL.Color (Color.green);
		GL.Vertex(Vector3.zero);
		GL.Vertex(-origin.up*cameraGizmoLength);
		//Z
		GL.Color (Color.blue);
		GL.Vertex(Vector3.zero);
		GL.Vertex(origin.forward*cameraGizmoLength);
		//Draw Gizmo





//
//		Vector3 fovR = origin.forward;
//		fovR = Quaternion.AngleAxis ((fov / 2), up) * fovR;
//		
//		Vector3 fovL = origin.forward;
//		fovL = Quaternion.AngleAxis (-(fov / 2), up) * fovL;
//
//
//		//FOV right and left
//		GL.Vertex (new Vector3(0,0,0));
//		GL.Vertex (fovR*1000);
//
//		GL.Vertex (new Vector3(0,0,0));
//		GL.Vertex (fovL*1000);
//		//FOV right and left
//
		GL.End ();

		GL.PopMatrix ();
	}

	void Start()
	{
		_pupilData = new Pupil.PupilData ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		leftEye = new EyeData (SamplesCount);
		rightEye= new EyeData (SamplesCount);

		_dataLock = new object ();

//		CalibrationModes = new Dictionary<CalibModes, CalibModeDetails> { 
//			{
//				CalibModes._2D,
//				new CalibModeDetails (){ calibrationPoints = CalibPoints2D, calibPlugin = "HMD_Calibration", positionKey = "norm_pos" }
//			},
//			{
//				CalibModes._3D,
//				new CalibModeDetails (){ calibrationPoints = CalibPoints3D, calibPlugin = "HMD_Calibration_3D", positionKey = "mm_pos" }
//			}
//		};

		//Run the service locally, only if under settings its set to local
		if (connectionMode == 0)
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

	public void NullDelegates(){
		OnCalibrationStarted = null;
		OnCalibrationDone = null;
		OnCalibData = null;
		OnSwitchCalibPoint = null;
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
		print ("Connecting to the server: " + IPHeader + ServicePort + ".");
		Thread.Sleep (ServiceStartupDelay);

		_requestSocket = new RequestSocket(IPHeader + ServicePort);

		_requestSocket.SendFrame("SUB_PORT");
		_isconnected = _requestSocket.TryReceiveFrameString(timeout, out subport);
		_lastT = DateTime.Now;

		if (_isconnected)
		{
			_serviceStarted = true;
			try{
				StartProcess ();
			}
			catch{
				print ("Couldn't start process");
			}
				
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
					_isFullConnected = true;
					try
					{
						string msgType=msg[0].ConvertToString();
						if (printMessageType){
							print(msgType);
						}
						if(msgType=="gaze")
						{
							var message = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
							if (printMessage){
								print(message);
							}
							MsgPack.MessagePackObject mmap = message.Value;
							lock (_dataLock)
							{
								try{
									string jsonData = mmap.ToString();
									jsonData = jsonData.Replace ("0 :", "\"zero\" :");//Replacing the stringless keys in the json to a version that is recognizable by Unity's
									jsonData = jsonData.Replace ("1 :", "\"one\" :");//JsonUtility.
									_pupilData = JsonUtility.FromJson<Pupil.PupilData>(jsonData);//Be aware that errors in Json will not show on this Thread. For testing run on the MainThread
								}
								catch{
									print("Assigning the _pupilData with the JsonUtility has Failed!");
								}
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

		if (m_status == EStatus.Calibration)
			StopCalibration ();
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
		try{
		_sendRequestMessage (new Dictionary<string,object> {{"subject","eye_process.should_start.0"},{"eye_id",0}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_start.1"},{"eye_id",1}});
		}
		catch{
			print ("cannot start process");
		}
	}
	public void StopProcess()
	{
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",0}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",1}});
	}

	public void StartCalibration(){
		
		CalibModeDetails _currCalibModeDetails = CalibrationModes [CurrentCalibrationMode];
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name",_currCalibModeDetails.calibPlugin}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_start"},{"hmd_video_frame_size",new float[]{1000,1000}},{"outlier_threshold",35}});
		_setStatus (EStatus.Calibration);
		
		if (OnCalibrationStarted != null)
			OnCalibrationStarted (this);
		
	}
	public void StopCalibration()
	{
		_setStatus (EStatus.ProcessingGaze);
		print ("Calibration Stopping !");
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_stop"}});
		if (OnCalibrationDone != null)
			OnCalibrationDone(this);
		
	}

	//we add function to this OnCalibData delegate in the PupilCalibMarker script
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


	void OnPacket(Pupil.PupilData data)
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
			float x,y,z;

			switch (CurrentCalibrationMode) {
			case CalibModes._2D:
				x = (float)data.norm_pos [0];
				y = (float)data.norm_pos [1];
				_eyeGazePos2D.x = (leftEye.gaze2D.x + rightEye.gaze2D.x) * 0.5f;
				_eyeGazePos2D.y = (leftEye.gaze2D.y + rightEye.gaze2D.y) * 0.5f;
				if (data.id == 0) {
					leftEye.AddGaze (x, y);
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				} else if (data.id == 1) {
					rightEye.AddGaze (x, y);
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				}
				break;
			case CalibModes._3D:
				x = (float)data.gaze_point_3d [0];
				y = (float)data.gaze_point_3d [1];
				z = (float)data.gaze_point_3d [2];
				_eyeGazePos3D.x = x;
				_eyeGazePos3D.y = y;
				_eyeGazePos3D.z = z;
				break;
			}

		} else if (m_status == EStatus.Calibration) {//gaze calibration stage
			float t=GetPupilTimestamp();

			floatArray[] _cPoints = GetCalibPoints;
			float[] _cPointFloatValues = _cPoints [_currCalibPoint].axisValues;

//			print (_cPointFloatValues.Count());
//			print (_cPointFloatValues [0] + " , " + _cPointFloatValues [1]);

			CalibModeDetails _cCalibDetails = CalibrationModes[CurrentCalibrationMode];

//			print ("Calibration points amount for calibration method " + CurrentCalibrationMode + " is : " + _cPoints.Length + ". Current Calibration sample is : " + _currCalibSamples);
			//If OnCalibData delegate has assigned function from the Calibration Marker, assign the current calibration position to it.
			_CalibData (_cPointFloatValues);


			// Giving the user a short time to focus on the Calibration Point target before starting adding the reference data
			if (_calibPointTimeOut) {
				Thread.Sleep (1000);
				_calibPointTimeOut = false;
			}

			//Create reference data to pass on. _cPointFloatValues are storing the float values for the relevant current Calibration mode
			var ref0=new Dictionary<string,object>(){{_cCalibDetails.positionKey,_cPointFloatValues},{"timestamp",t},{"id",0}};
			var ref1=new Dictionary<string,object>(){{_cCalibDetails.positionKey,_cPointFloatValues},{"timestamp",t},{"id",1}};

			//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			_calibrationData.Add (ref0);
			_calibrationData.Add (ref1);

			//Increment the current calibration sample. (Default sample amount per calibration point is 120)
			_currCalibSamples++;

			//Debugging
			if (printSampling) {
				print ("Sampling at : " + _currCalibSamples);
			}

			//give a small timeout per sample.
			Thread.Sleep (1000 / 60);

			// SWITCHING CALIBRATION POINT
			//If the current calibration sample is bigger or equal to the desired sampling (so we accomplished sampling for this calibration point),
			//null the current sample and step to next calbration point.
			//Also prepare calibration data for sending, and send it.
			if (_currCalibSamples >= DefaultCalibrationCount) {
				_calibPointTimeOut = true;
				_currCalibSamples = 0;
				_currCalibPoint++;

					
				//reformat the calibration data for sending.
				string pointsData="[";
				int _index = 0;
				foreach (var v in _calibrationData) {
					pointsData += JsonUtility.ToJson (v);//String.Format("{'norm_pos':({0},{1}),'timestamp':{2},'id':{3}}",v.norm_pos[0],v.norm_pos[1],v.timestamp,v.id);
					//print(pointsData);
					++_index;
					if (_index != _calibrationData.Count) {
						pointsData += ",";
					}
				}
				pointsData += "]";

			//	pointsData = JsonUtility.ToJson (_CalibrationPoints);

//				foreach (Dictionary<string,object> _d in _calibrationData) {
//					object _o = _d.ElementAt (0);
//					float[] _f = (float[])_o;
//					print (_f);
//				}

				//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
				_sendRequestMessage (new Dictionary<string,object> {{"subject","calibration.add_ref_data"},{"ref_data",_CalibrationPoints}});
				//Clear the current calibration data, so we can proceed to the next point if there is any.
				_calibrationData.Clear ();

				//Stop calibration if we accomplished all required calibration targets.
				if (_currCalibPoint >= _cPoints.Length) {
					StopCalibration ();
				}
				OnSwitchCalibPoint (this);
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
	public void Add3DCalibrationPoint(floatArray _f){
		CalibrationPoints3DFoldout = true;
		_calibPoints.Add3DPoint (_f);
	}
	public void Add2DCalibrationPoint(floatArray _f){
		CalibrationPoints2DFoldout = true;
		_calibPoints.Add2DPoint (_f);
	}
	public void AddCalibrationPoint(){
		if (CurrentCalibrationMode == PupilGazeTracker.CalibModes._2D) {
			Add2DCalibrationPoint (new floatArray (){ axisValues = new float[]{ 0f, 0f } });
		} else {
			Add3DCalibrationPoint (new floatArray (){ axisValues = new float[]{ 0f, 0f, 0f } });
		}
	}
	public void RemoveCalibrationPoint(List<floatArray> _f, int index){
		_f.RemoveAt (index);
	}

	public void SwitchCalibrationMode(){
		switch (calibrationMode) {
		case 0://On switched to 2D Calibration mode
			CalibrationGameObject2D.transform.FindChild ("Calib").gameObject.GetComponent<PupilCalibMarker> ().AssignDelegates ();
			CalibrationGameObject2D.SetActive (true);
			CalibrationGameObject3D.SetActive (false);
			Camera.main.orthographic = true;
			//Debug.Log ("Check if this happens more than once");
			break;
		case 1://On switched to 3D Calibration mode
			CalibrationGameObject3D.transform.FindChild ("Calib Marker 3D").gameObject.GetComponent<PupilCalibMarker3D> ().AssignDelegates ();
			CalibrationGameObject2D.SetActive (false);
			CalibrationGameObject3D.SetActive (true);
			Camera.main.orthographic = false;
			break;
		}
	
	}

}
