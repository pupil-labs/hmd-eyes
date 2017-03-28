// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using UnityEngine.UI;
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
		//private static double[] defaultDoubleArray = new double[]{ 0, 0, 0 };
		public Vector3 GetVector3(double[] _d3 = default(double[]), float wScale = 1.0f){
			if (_d3.Length > 0)
				return new Vector3 ((float)_d3 [0]*wScale, (float)_d3 [1]*wScale, (float)_d3 [2]*wScale);
			return Vector3.zero;
		}
		public Vector2 GetVector2(double[] _d2){
			if (_d2.Length > 0)
				return new Vector2 ((float)_d2 [0], (float)_d2 [1]);
			return Vector2.zero;
		}
	}

}
namespace Operator{
	[Serializable]
	public class properties{
		public Vector3 graphPositionOffset0 = new Vector3 ();
		public Vector3 graphPositionOffset1 = new Vector3 ();
		public int eye0GraphLength = 20;
		public int eye1GraphLength = 20;
		public float conf0 = 0.2f;
		public float conf1 = 0.5f;
		public float rotOffset = 0;
		public Vector2 graphScale = new Vector2 (1, 1);
		public float refreshD;
		public long graphTime = DateTime.Now.Ticks;
		public bool isUpdateGraph = false;
	}
}
namespace Calibration{
	[Serializable]
	public class marker{
		public string name;
		public Rect shape;
		public Color color;
		public bool toggle;
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

	public Pupil.PupilData _pupilData;

	#region delegates

	public delegate void OnCalibrationStartedDeleg(PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg(PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg(PupilGazeTracker manager);
	//public delegate void OnCalibDataDeleg(PupilGazeTracker manager,float x,float y);
	public delegate void OnCalibDataDeleg(PupilGazeTracker manager,object position);
	public delegate void OnCalibrationGLDeleg();

	public delegate void DrawMenuDeleg ();
//	public delegate void OnConnectedDeleg (PupilGazeTracker manager);
	public delegate void OnSwitchCalibPointDeleg(PupilGazeTracker manager);
	public delegate void OnCalibDebugDeleg();
	public delegate void OnOperatorMonitorDeleg();
	public delegate void OnDrawGizmoDeleg ();

	//TODO: confirm that this does not cause freezing
	//public delegate void OnFramePublishingDeleg(Color[] _texture);

	public event OnCalibrationStartedDeleg OnCalibrationStarted;
	public event OnCalibrationDoneDeleg OnCalibrationDone;
	public event OnEyeGazeDeleg OnEyeGaze;
	public event OnCalibDataDeleg OnCalibData;
	public DrawMenuDeleg DrawMenu;
	public OnCalibrationGLDeleg OnCalibrationGL;
//	public event OnConnectedDeleg OnConnected;
	public event OnSwitchCalibPointDeleg OnSwitchCalibPoint;
	//public event OnFramePublishingDeleg OnFramePublish;

	public OnCalibDebugDeleg OnCalibDebug;
	public OnOperatorMonitorDeleg OnOperatorMonitor;
	public OnDrawGizmoDeleg OnDrawGizmo;

	#endregion

	#region calibration_vars
	//Use status!!!!!!!!!!!
	public bool isCalibrating = false;

	public float value0;
	public float value1;
	public float value2;
	public float value3;
	public Calibration.marker[] CalibrationMarkers;

	#endregion
	//FRAME PUBLISHING VARIABLES
	#region frame_publishing_vars
	public static int lineCount = 100;
	public float EyeSize = 24.2f;//official approximation of the size of an avarage human eye(mm). However it may vary from 21 to 27 millimeters.

	static Material lineMaterial;
	static Material eye0ImageMaterial;
	static Material eye1ImageMaterial;
	static Material eyeSphereMaterial;

	public string pluginName;

	public int framePublishFramePerSecondLimit = 20;

	public Texture2D eye0Image;
	public Texture2D eye1Image;
	#endregion

	#region operator_monitor_vars
	public bool isOperatorMonitor;
	public Camera OperatorCamera;
	public StereoTargetEyeMask targetMask;
	public Operator.properties OperatorMonitorProperties;
	#endregion

	//changed this
	bool _isconnected =false;
	public bool IsConnected{
		get{ return _isconnected; }
		set{
			GameObject go = new GameObject ();
			_isconnected = value;
		}
	}

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

	public float WorldScaling; 

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
	public GUIStyle CalibRowStyle = new GUIStyle();

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

	private int toastIndex = 0;
	#region Update
	void Update(){
		if (Input.GetKeyUp (KeyCode.X)) {
			subscriberSocket.Subscribe ("frame.");
		}

		if (Input.GetKeyUp (KeyCode.Z)) {
			subscriberSocket.Unsubscribe ("frame.");
		}
		if (Input.GetKeyUp (KeyCode.A)) {
			StopFramePublishing();
//			_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
//			_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name", "Frame_Publisher"}, {"args","{}"}});
			//_sendRequestMessage (new Dictionary<string,object> { { "subject","eye_process.started" }, { "eye_id",0 } });
			//ToastMessage.Instance.DrawToastMessage (new ToastMessage.toastParameters(){delay = 2, fadeOutSpeed = 2, text = "toast message", ID = 0});
			//ToastMessage.Instance.DrawToastMessageOnMainThread (new ToastMessage.toastParameters(){delay = 2, fadeOutSpeed = 2, text = "posted to Main Thread toast message", ID = 1});
		}
		if (Input.GetKeyUp (KeyCode.B)) {
			_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name", "Frame_Publisher"}, {"args","{}"}});
		}
		if (Input.GetKeyUp (KeyCode.D)) {
			_sendRequestMessage ( new Dictionary<string,object> {{"subject","stop_plugin"},{"name", "Frame_Publisher"}, { "eye_id",0 }});
		}
		if (Input.GetKeyUp (KeyCode.E)) {
			_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.stopped" } });
		}

		if (Input.GetKeyUp (KeyCode.C)) {
			if (m_status == EStatus.Calibration)
				StopCalibration ();
			else {
				StartCalibration ();	
			}

		}

		//CalibrationDebugMode.Instantiate ();
		//CalibrationDebugMode.OnRenderObject();
	}
	#endregion

	#region DebugView
	public Transform DebugViewTransform;
	public bool StreamCameraImages = true;

	public enum CalibrationDebugCamera
	{
		HMD,
		PUPIL_CAMERA_0,
		PUPIL_CAMERA_1,
		PUPIL_CAMERA_BOTH
	}
	public CalibrationDebugCamera calibrationDebugCamera = CalibrationDebugCamera.PUPIL_CAMERA_0;
	#endregion
	public void DrawCalibrationDebug(){

		CreateLineMaterial ();
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		CreateEyeSphereMaterial ();



		Vector3 eye0Pos = _pupilData.GetVector3(_pupilData.eye_centers_3d.zero, WorldScaling);
		Vector3 eye0Norm = _pupilData.GetVector3(_pupilData.gaze_normals_3d.zero, 1);

		Vector3 eye1Pos = _pupilData.GetVector3(_pupilData.eye_centers_3d.one, WorldScaling);
		Vector3 eye1Norm = _pupilData.GetVector3(_pupilData.gaze_normals_3d.one, 1);

		Vector3 gazePoint = _pupilData.GetVector3 (_pupilData.gaze_point_3d, WorldScaling);
		//print ("before drawing : " + eye0Pos+eye0Norm);

		switch (calibrationDebugCamera) {
		case CalibrationDebugCamera.HMD:

			float _fov = fov*Mathf.Deg2Rad;
			var radianHFOV = 2 * Mathf.Atan (Mathf.Tan (_fov / 2) * Camera.main.aspect);
			var hFOV = Mathf.Rad2Deg * radianHFOV;

			DrawCameraFrustum (DebugViewTransform, hFOV, aspectRatios.FULLHD, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));

			DrawDebugSphere (DebugViewTransform, eye0Pos, 0, eye0Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: EyeSize);//eye0
			DrawDebugSphere (DebugViewTransform, eye1Pos, 1, eye1Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: EyeSize);//eye1

			DrawDebugSphere (DebugViewTransform, gazePoint, 2, eye1Norm, isEye: false, norm_length: viewDirectionLength, sphereColor: Color.red, norm_color: Color.red, size: EyeSize/2);//gaze point 3D


			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_0:
			Pupil.Sphere _s;
			//_pupilData.base_data
			if (_pupilData.base_data != null) {
				print ("inside pupil camera 0 with .base data");
				_s = _pupilData.base_data [0].sphere;
				print (_s.center [0] + " , " + _s.center [1] + " , " + _s.center [2]);
			} else {
				_s = new Pupil.Sphere ();
			}
			Vector3 _v3Pos = new Vector3 ((float)_s.center [0] * WorldScaling, (float)_s.center [1] * WorldScaling, (float)_s.center [2] * WorldScaling);
			//DrawDebugSphere (DebugViewTransform, _v3Pos, 10, size: ((float)_s.radius) * WorldScaling, sphereColor: Color.green);
			DrawDebugSphere (matrix: DebugViewTransform,forward: Vector3.one, eyeID: 10,position: _v3Pos, size: ((float)_s.radius) * WorldScaling, sphereColor: Color.green);
			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_1:
			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_BOTH:
			break;
		}



		//print ("after drawing");

	}
	public Material CreateLineMaterial ()
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
			lineMaterial.SetInt ("_ZWrite", 1);
		}
		return lineMaterial;
	}
	static void CreateEye0ImageMaterial ()
	{
		if (!eye0ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			eye0ImageMaterial = new Material (shader);
			eye0ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	static void CreateEye1ImageMaterial ()
	{
		if (!eye1ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			eye1ImageMaterial = new Material (shader);
			eye1ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	static void CreateEyeSphereMaterial ()
	{
		if (!eyeSphereMaterial)
		{
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			eyeSphereMaterial = new Material (shader);
			eyeSphereMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			eyeSphereMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			eyeSphereMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			eyeSphereMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			eyeSphereMaterial.SetInt ("_ZWrite", 1);
			//eyeSphereMaterial.
		}
	}

	public virtual void OnDrawGizmos(){
		if (OnDrawGizmo != null)
			OnDrawGizmo ();
	}
	public void OnRenderObject(){
		if (OnCalibDebug != null)
			OnCalibDebug ();

		if (OnCalibrationGL != null)
			OnCalibrationGL ();
	}
	public float fov = 60;
	public float MinViewDistance = 20;
	public float MaxViewDistance = 200;
	public float scale = 1;
	public float viewDirectionLength = 20;
	public float cameraGizmoLength = 20;
	public Mesh DebugEyeMesh;

	//public Dictionary<string, Vector3> DebugObjectPositions = new Dictionary<string, Vector3>(){{"eye0", new Vector3(0,0,0)},{"eye1", new Vector3(0,0,0)} };

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
		public void Draw(float _width, float _height, float _zOffset, float _scale, bool drawCameraImage = false){
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
	#region DebugView.DrawDebugSphere
	public void DrawDebugSphere(Transform matrix, Vector3 position, int eyeID, Vector3 forward = default(Vector3), float norm_length = 20, bool isEye = false, Color norm_color = default(Color), Color sphereColor = default(Color), float size = 24.2f){

		eyeSphereMaterial.SetColor ("_Color", sphereColor);
		eyeSphereMaterial.SetPass (0);

		if (matrix == null)
			matrix = Camera.main.transform;

		Matrix4x4 _m = new Matrix4x4 ();

		//print ("from : " + forward + " to :  " + Quaternion.LookRotation (forward, Vector3.up));

		if (forward != Vector3.zero) {
			_m.SetTRS (position, Quaternion.LookRotation (forward, Vector3.up), new Vector3 (size, size, size));
		} else {
			_m.SetTRS (new Vector3(100*eyeID,0,0), Quaternion.identity, new Vector3 (size, size, size));
		}
		GL.wireframe = true;
		Graphics.DrawMeshNow(DebugEyeMesh, matrix.localToWorldMatrix*_m);
		GL.wireframe = false;


		eyeSphereMaterial.SetColor ("_Color", norm_color);
		eyeSphereMaterial.SetPass (0);
		if (isEye) {
			GL.MultMatrix (matrix.localToWorldMatrix * _m);
			GL.Begin (GL.LINES);
			GL.Vertex (Vector3.zero);
			GL.Vertex (Vector3.forward * norm_length);
			GL.End ();
		}
	}
	#endregion
	#region DebugView.CameraFrustum
	public void DrawCameraFrustum(Transform origin, float fov, aspectRatios aspect, float minViewDistance, float maxViewDistance, Color frustumColor = default(Color)){

		lineMaterial.SetColor ("_Color", frustumColor);
		lineMaterial.SetPass (0);

		if (origin == null)
			origin = Camera.main.transform;

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
		farPlaneRect.Draw (widthMaxView, heightMaxView, maxViewDistance, 1, true);

		#region DebugView.CameraFrustum.ConnectRectangles
		//ConnectRectangles
		for (int i = 0; i < nearPlaneRect.verticies.Count (); i++) {
			GL.Vertex (nearPlaneRect.verticies[i]);
			GL.Vertex (farPlaneRect.verticies[i]);
		}
		GL.End ();
		#endregion

		lineMaterial.SetColor ("_Color", Color.white);
		lineMaterial.SetPass (0);

		#region DebugView.CameraFrustum.Gizmo
		GL.Begin(GL.LINES);
		//Draw Gizmo
		//X
		//lineMaterial.color = Color.red;
		GL.Color (Color.red);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.right*cameraGizmoLength);
		//Y
		//lineMaterial.color = Color.green;
		GL.Color (Color.green);
		GL.Vertex(Vector3.zero);
		GL.Vertex(-Vector3.up*cameraGizmoLength);
		//Z
		//lineMaterial.color = Color.blue;
		GL.Color (Color.blue);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.forward*cameraGizmoLength);
		//Draw Gizmo
		GL.End ();
		#endregion




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

		DrawCameraImages (farPlaneRect.verticies, farPlaneRect.width);
		GL.PopMatrix ();
	}	
	#endregion
	#region DebugView.CameraFrustum.CameraImages
	void DrawCameraImages(Vector3[] drawPlane, float width){



		float[] _f = new float[]{ 0, 1, 1, 0, 0 };
		for (int i = 0; i < 2; i++) {
			if (i == 0) {
				eye0ImageMaterial.SetPass (0);
			} else {
				eye1ImageMaterial.SetPass (0);
			}
			GL.Begin (GL.QUADS);
			for (int j = drawPlane.Count ()-1; j > -1 ; j--) {
				int ind = (drawPlane.Count()-1)-j;
				float widthScaling = 0;
				GL.TexCoord2 (_f [ind], _f [ind + 1]);
			if (j == drawPlane.Count () - 1 || j == 0) widthScaling = width / 2;
				GL.Vertex3 (drawPlane [j].x+widthScaling-(i*(width/2)), drawPlane [j].y, drawPlane [j].z);
			}
			GL.End ();
		}

	}	
	#endregion
	#region Start();
	void Start()
	{
		OperatorCamera = null;
		ToastMessage.Instance.DrawToastMessage (new ToastMessage.toastParameters (){ text = "" });//Initialize toast messages;

		CalibrationGL.GazeProcessingMode ();

		float[] _cPointFloatValues = new float[]{0f,0f,0f};
		eye0Image = new Texture2D (100,100);
		eye1Image = new Texture2D (100,100);

		_pupilData = new Pupil.PupilData () {gaze_point_3d = new double[]{ 10.0, 10.0, 10.0 },gaze_normals_3d = new Pupil.eyes3Ddata () {one = new double[] {1,0,0},zero = new double[] {
					1,
					0,
					1
				}
			},
			eye_centers_3d = new Pupil.eyes3Ddata () {
				one = new double[] {
					100,
					0,
					0
				},
				zero = new double[] {
					-100,
					0,
					0
				}
			}
		};



		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		leftEye = new EyeData (SamplesCount);
		rightEye= new EyeData (SamplesCount);

		_dataLock = new object ();

		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null) {
			OperatorMonitor.Instant ();
		}
			//OnOperatorMonitor += DrawOperatorMonitor;
		if (calibrationDebugMode && OnCalibDebug == null)
			OnCalibDebug += DrawCalibrationDebug;
		
		//OnFramePublish += AssignTexture;

		//ToastMessage.Instance.DrawAthing ("my first toast message", 0, delay: 3f, fadeOutSpeed: 5f);

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
	#endregion
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



	#region frame_publishing.functions
	public void StartFramePublishing(){
		//_sendRequestMessage (new Dictionary<string,object> {{"subject","start_plugin"},{"name", "Frame_Publisher"}});
		_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}
	public void StopFramePublishing(){
		_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.stopped" } });
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","stop_plugin"},{"name", "Frame_Publisher"}, {"args","{}"}});
	}
	public void AssignTexture(object data){
		//TODO: work on redundancy here!
		Dictionary<int,byte[]> _dic = (Dictionary<int,byte[]>)data;
		byte[] _bArray;

		if (_dic.ContainsKey (0)) {
			_dic.TryGetValue (0, out _bArray);
			//eye0Image = new Texture2D(100,100);
			eye0Image.LoadImage (_bArray);
			try{
				eye0ImageMaterial.mainTexture = eye0Image;
			}catch{
			}
		} else {
			_dic.TryGetValue (1, out _bArray);
			//eye1Image = new Texture2D(100,100);
			eye1Image.LoadImage (_bArray);
			try{
				eye1ImageMaterial.mainTexture = eye1Image;
			}catch{
			}
		}
	}
	#endregion

	#region NetMQ
	private SubscriberSocket subscriberSocket;
	void NetMQClient()
	{
		long lastTick = DateTime.Now.Ticks;
		float elapsedTime;

		bool updateEye0 = false;
		bool updateEye1 = false;

		//ToastMessage.Instance.DrawToastMessageOnMainThread (new ToastMessage.toastParameters(){delay = 2, fadeOutSpeed = 2, text = "posted to Main Thread toast message", ID = 0});
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
			
			try{
				Thread.Sleep(1000);
				StartProcess ();
			}
			catch{
				print ("Couldn't start process");
			}
				
			//var subscriberSocket = new SubscriberSocket( IPHeader + subport);
			subscriberSocket = new SubscriberSocket( IPHeader + subport);

			subscriberSocket.Subscribe("gaze"); //subscribe for gaze data
			subscriberSocket.Subscribe("notify."); //subscribe for all notifications
			//subscriberSocket.Subscribe("frame."); //subscribe for all notifications
			//subscriberSocket.Subscribe()
			//subscriberSocket.SubscribeToAnyTopic();

			_setStatus(EStatus.ProcessingGaze);
			var msg = new NetMQMessage();



			while ( _isDone == false)
			{
				_isconnected = subscriberSocket.TryReceiveMultipartMessage(timeout,ref(msg));
				if (_isconnected)
				{
					//print ("isconnected");
					_isFullConnected = true;

					try
					{
//						if (!_serviceStarted){
//							StopFramePublishing();
//						}
						string msgType=msg[0].ConvertToString();
						if (printMessage){
							var m = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
							MsgPack.MessagePackObject map = m.Value;
							print(m);
						}
						if (printMessageType){
							print(msgType);
							//ToastMessage.Instance.DrawToastMessageOnMainThread(new ToastMessage.toastParameters(){ID = 0,text = msgType});
						}

						elapsedTime = (float)TimeSpan.FromTicks(DateTime.Now.Ticks - lastTick).TotalSeconds;
						if (elapsedTime >= (1f/framePublishFramePerSecondLimit)){//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
							lastTick = DateTime.Now.Ticks;
							updateEye0 = true;
							updateEye1 = true;
						}
						#region NetMQ.message_handling
						switch(msgType){
						case "pupil.0":
//							var pupil0 =MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
//							MsgPack.MessagePackObject pupil0_data = pupil0.Value;
//							//print(pupil0_data);
							_gazeFPS++;
							var ct=DateTime.Now;
							if((ct-_lastT).TotalSeconds>1)
							{
								_lastT=ct;
								_currentFps=_gazeFPS;
								_gazeFPS=0;
							}
							break;
//						case "frame.eye.0":
//							if (calibrationDebugMode && StreamCameraImages){
//								var eye0Data = msg[2].Buffer;
//								if (updateEye0){
//									MainThread.Call(AssignTexture, new Dictionary<int,byte[]>{{0,eye0Data}});
//									updateEye0=false;
//								}
//							}
//							break;
//						case "frame.eye.1":
//							if (calibrationDebugMode && StreamCameraImages){
//								var eye1Data = msg[2].Buffer;
//								if (updateEye1){
//									MainThread.Call(AssignTexture, new Dictionary<int,byte[]>{{1,eye1Data}});
//									updateEye1=false;
//								}
//							}
//							break;
//						case "notify.frame_publishing.started":
//							ToastMessage.Instance.DrawToastMessageOnMainThread(new ToastMessage.toastParameters(){ID = 0,text = "frame publishing has started"});
//							break;
////						case "notify.meta.doc":
////							var doc = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
////							print(doc);
////							break;
						}

						if(msgType=="gaze")
						{
//							if (!_serviceStarted){
//								_serviceStarted = true;
//								print("Service started ! ");
//							}
							var message = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
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
						#endregion
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
	#endregion
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
		string servicePath = PupilServicePath;
		if (File.Exists (servicePath)) {
			serviceProcess = new Process ();
			serviceProcess.StartInfo.Arguments = servicePath;
			serviceProcess.StartInfo.FileName = servicePath;
			if (File.Exists (servicePath)) {
				serviceProcess.Start ();
			} else {
				print ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
			}
		} else{
			if (PupilServiceFileName == "") {
				print ("Pupil Service filename is not specified, most likely you will have to check if you have it set for the current platform under settings Platforms(DEV opt.)");
			}
//			if (!Directory.Exists (servicePath)){
//				print ("Pupil Service directory incorrect, please change under Settings");
//			}
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

		CalibrationGL.CalibrationMode ();

		//print (CalibrationMarkers.Where (p => p.name == "Marker") as Calibration.marker);
		//print (CalibrationMarkers.Where (p => p.name == "Marker").ToList()[0]);

		CalibModeDetails _currCalibModeDetails = CalibrationModes [CurrentCalibrationMode];
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name",_currCalibModeDetails.calibPlugin}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_start"},{"hmd_video_frame_size",new float[]{1000,1000}},{"outlier_threshold",35}});
		_setStatus (EStatus.Calibration);
		
		if (OnCalibrationStarted != null)
			OnCalibrationStarted (this);
		
	}
	public void StopCalibration()
	{
		CalibrationGL.GazeProcessingMode ();
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

	#region packet
	void OnPacket(Pupil.PupilData data)
	{
//		Application.logMessageReceivedThreaded ();
		//print ("OnPacket begin");
		//add new frame


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

				Calibration.marker _0 = CalibrationMarkers.Where (p => p.name == "leftEye").ToList () [0];
				_0.shape.x = GetEyeGaze2D (GazeSource.LeftEye).x;
				_0.shape.y = GetEyeGaze2D (GazeSource.LeftEye).y;
				_0.toggle = true;

				Calibration.marker _1 = CalibrationMarkers.Where (p => p.name == "rightEye").ToList () [0];
				_1.shape.x = GetEyeGaze2D (GazeSource.RightEye).x;
				_1.shape.y = GetEyeGaze2D (GazeSource.RightEye).y;
				_1.toggle = true;

				Calibration.marker _2 = CalibrationMarkers.Where (p => p.name == "gaze").ToList () [0];
				_2.shape.x = GetEyeGaze2D (GazeSource.BothEyes).x;
				_2.shape.y = GetEyeGaze2D (GazeSource.BothEyes).y;
				_2.toggle = true;

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

			#region packet.calibration
		} else if (m_status == EStatus.Calibration) {//gaze calibration stage
			float t=GetPupilTimestamp();

			//TODO: Call this once!
			floatArray[] _cPoints = GetCalibPoints;
			float[] _tmp = _cPoints [_currCalibPoint].axisValues;

			//print("1 : " + _cPoints.Length + " , " + _cPoints[11].axisValues.Length);

			float[] _cPointFloatValues = new float[]{0f,0f,0f};
			float[] _cPointFloatValuesBase = new float[]{(float)_tmp[0], (float)_tmp[1], (float)_tmp[2]};
			//print("2");
			if (CurrentCalibrationMode == CalibModes._2D){
				_cPointFloatValues = new float[]{_tmp[0], _tmp[1], -100};
				//print("calib mode 2D on packet");
			}else{
				print("calib mode 3D on packet");
				//_cPointFloatValues = new float[]{_tmp[0]/WorldScaling, _tmp[1]/WorldScaling, _tmp[2]/WorldScaling};
			}

//			print (_cPointFloatValues.Count());
//			print (_cPointFloatValues [0] + " , " + _cPointFloatValues [1]);

			CalibModeDetails _cCalibDetails = CalibrationModes[CurrentCalibrationMode];

//			print ("Calibration points amount for calibration method " + CurrentCalibrationMode + " is : " + _cPoints.Length + ". Current Calibration sample is : " + _currCalibSamples);
			//If OnCalibData delegate has assigned function from the Calibration Marker, assign the current calibration position to it.

			if (CurrentCalibrationMode == CalibModes._2D){
				Calibration.marker _m = CalibrationMarkers.Where (p => p.name == "Marker").ToList()[0];
				_m.shape.x = _tmp[0];
				_m.shape.y = _tmp[1];
				_m.toggle = true;
			}
			_CalibData (_cPointFloatValuesBase);


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
		#endregion
	}
	#endregion
	void OnGUI()
	{
		string str="Capture Rate="+FPS;
		str += "\nLeft Eye:" + LeftEyePos.ToString ();
		str += "\nRight Eye:" + RightEyePos.ToString ();
		GUI.TextArea (new Rect (0, 0, 200, 50), str);

//		if (OnCalibrationGL != null)
//			OnCalibrationGL ();

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
			Add2DCalibrationPoint (new floatArray (){ axisValues = new float[]{ 0f, 0f, 0f } });
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
