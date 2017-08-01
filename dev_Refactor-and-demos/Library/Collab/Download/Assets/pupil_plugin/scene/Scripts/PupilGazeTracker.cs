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

public class JsonHelper{



	/// <summary>
	///Usage:
	///YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
	/// </summary>
	public static T[] getJsonArray<T>(string json)
	{

		string newJson = "{ \"array\": " + json + "}";
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (newJson);
		return wrapper.array;
	}

	//Usage:
	//string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
	public static string arrayToJson<T>(T[] array)
	{
		Wrapper<T> wrapper = new Wrapper<T> ();
		wrapper.array = array;
		return JsonUtility.ToJson (wrapper);
	}

	[Serializable]
	private class Wrapper<T>
	{
		public T[] array;
	}

}



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

	public struct  processStatus{
		public static bool initialized;
		public static bool eyeProcess0;
		public static bool eyeProcess1;
	}
}
namespace Operator{
	[Serializable]
	public class properties{
		public int id;
		[HideInInspector]
		public Vector3 positionOffset = new Vector3 ();
		[HideInInspector]
		public Vector3 rotationOffset = new Vector3 ();
		[HideInInspector]
		public Vector3 scaleOffset = Vector3.one;
		[HideInInspector]
		public Vector2 graphScale = new Vector2 (1, 1);
		[HideInInspector]
		public float gapSize = 1;
		[HideInInspector]
		public int graphLength = 20;
		public float confidence = 0.2f;
		public float refreshDelay;
		[HideInInspector]
		public long graphTime = DateTime.Now.Ticks;
		[HideInInspector]
		public bool update = false;
		[HideInInspector]
		public List<float> confidenceList = new List<float> ();
		[HideInInspector]
		public Camera OperatorCamera;
		[HideInInspector]
		public static properties[] Properties = default(Operator.properties[]);
	}
}
#region Calibration

namespace Calibration{


	[Serializable]
	public class data{
		public string camera_intrinsics_str;
		public Vector3[] cal_ref_points_3d;
		public Vector3[] cal_gaze_points0_3d;
		public Vector3[] cal_gaze_points1_3d;
		public Vector3[] cal_points_3d;
		public Matrix4x4 eye_camera_to_world_matrix0;
		public Matrix4x4 eye_camera_to_world_matrix1;
		public cam_intrinsics camera_intrinsics;
	}

	[Serializable]
	public class cam_intrinsics{
		public double[] resolution;
		public string camera_name;
		public Vector3[] camera_matrix;
		public double[][] dist_coefs;//figure this out if needed.
		public int intt;
	}
}

#endregion

namespace DebugView{
	[Serializable]
	public class _Transform{
		public string name;
		public Vector3 position;
		public Vector3 rotation;
		public Vector3 localScale;
		public GameObject GO;
	}
	[Serializable]
	public class variables{
		public float EyeSize = 24.2f;//official approximation of the size of an avarage human eye(mm). However it may vary from 21 to 27 millimeters.
		[HideInInspector]
		public PupilSettings.Calibration.Marker Circle;
		public bool isDrawPoints = false;
		public bool isDrawLines = false;
		[HideInInspector]
		public GameObject PointCloudGO;
		[HideInInspector]
		public GameObject LineDrawerGO;
		public Mesh DebugEyeMesh;
	}
	[Serializable]
	public class framePublishingVariables{
		public int targetFPS = 20;
		public Texture2D eye0Image;
		public Texture2D eye1Image;
		[HideInInspector]
		public byte[] raw0;
		[HideInInspector]
		public byte[] raw1;
		[HideInInspector]
		public bool StreamCameraImages = false;
		public Material eye0ImageMaterial;
		public Material eye1ImageMaterial;
	}
}

#region DebugVariables
namespace _Debug{
	[Serializable]
	public class Debug_Vars{
		public bool subscribeFrame;
		public bool subscribeGaze;
		public bool subscribeAll;
		public bool subscribeNotify;
		public bool printSampling;
		public bool printMessage;
		public bool printMessageType;
		public float value0;
		public float value1;
		public float value2;
		public float value3;
		public float WorldScaling;
		public bool packetsOnMainThread;
	}
}
#endregion

[Serializable]
public struct floatArray{
	public float[] axisValues;
}


[Serializable]
public class Recorder{
	
	public static GameObject RecorderGO;
	public static bool isRecording;
	public static bool isProcessing;

	public FFmpegOut.FFmpegPipe.Codec codec;
	public FFmpegOut.FFmpegPipe.Resolution resolution;
	public List<int[]> resolutions = new List<int[]> (){ new int[]{ 1920, 1080 }, new int[]{ 1280, 720 }, new int[]{ 640, 480 } };
	public string filePath;
	public bool isFixedRecordingLength;
	public float recordingLength = 10f;
	public bool isCustomPath;

	public static void Start(){
		RecorderGO = new GameObject ("RecorderCamera");
		RecorderGO.transform.parent = Camera.main.gameObject.transform;

		RecorderGO.AddComponent<FFmpegOut.CameraCapture> ();
		Camera c = RecorderGO.GetComponent<Camera> ();
		c.targetDisplay = 1;
		c.stereoTargetEye = StereoTargetEyeMask.None;
		#if UNITY_5_6_OR_NEWER
		c.allowHDR = false;
		c.allowMSAA = false;
		#endif
		c.fieldOfView = 111;
		PupilTools.RepaintGUI ();
	}
	public static void Stop(){
		RecorderGO.GetComponent<FFmpegOut.CameraCapture> ().Stop ();
		PupilTools.RepaintGUI ();
	}
}
	
[RequireComponent(typeof(PupilDataReceiver))]
public class PupilGazeTracker:MonoBehaviour
{

	public PupilSettings Settings;

	static PupilGazeTracker _Instance;
	public static PupilGazeTracker Instance
	{
		get{
			if (_Instance == null) {
//				_Instance = new GameObject("PupilGazeTracker").AddComponent<PupilGazeTracker> ();
			}
			return _Instance;
		}
	}

	public Recorder recorder = new Recorder();

	public string ProjectName;

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
		public Vector2 AddGaze(float x,float y, int eyeID)
		{
//			print ("adding gaze : " + x + " , " + y + "for the eye : " + eyeID);
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

//	Thread _serviceThread;
//	bool _isDone=false;



//	public pupilDataDictionary _pupilDataDict = new pupilDataDictionary();
//	public class pupilDataDictionary
//	{
//		public Dictionary<string, object> dictionary = new Dictionary<string, object>();
//		public object GetValue(string key){
//			object _value;
//			if (dictionary.TryGetValue (key, out _value)) {
//				return _value;
//			} else {
//				return null;
//			}
//		}
//		public string GetValueAsString(string key){	
//			object _strObj = GetValue (key);
//			if (_strObj != null) {
//				return _strObj.ToString ();
//			} else {
//				return "value is null from key : " + key;
//			}
//		}
//		public float GetValueAsFloat(string key){
//			string str = GetValueAsString (key);
//			if (!str.Contains ("null")) {
//				return float.Parse (str);
//			}
//			return 0f;
//		}
//		public int GetValueAsInteger(string key){
//			return int.Parse( GetValue (key).ToString());
//		}
//		public _double GetValueAsVector(string key){
//			string _doubleJson = GetValueAsString (key);
//			if (!_doubleJson.Contains ("null")) {
//				return JsonUtility.FromJson<_double> ("{\"value\": " + GetValueAsString (key) + "}");
//			} else {
//				return new _double (){ value = new double[]{ 0.0, 0.0, 0.0 } };
//			}
//		}
//		public void GetValueAsVectorArray(string key,  ref Vector3[] VectorArray, float converter = 1f, Action action = null){
//			string json = GetValueAsString (key);
//			if (!json.Contains("null")) {
////				print ("Reference : " + VectorArray [0]);
//				json = json.Replace ("0 :", "\"zero\" :");
//				json = json.Replace ("1 :", "\"one\" :");
//				Pupil.eyes3Ddata _3dData = JsonUtility.FromJson<Pupil.eyes3Ddata> (json);
//				Vector3[] tmpArray = new Vector3[] { new Vector3 (((float)_3dData.zero [0]) * converter, ((float)_3dData.zero [1]), (float)_3dData.zero [2]),new Vector3 (((float)_3dData.one [0]) * converter, ((float)_3dData.one [1]), (float)_3dData.one [2])};
//
//				if (VectorArray[0] != tmpArray[0] || VectorArray[1] != tmpArray[1]) {
//					VectorArray = tmpArray;
//					if (action != null)
//						action ();
//				} else {
////					print ("new Vector Array is the same as the old one");
//				}
//			}
//		}
//
//	}

	//[HideInInspector]
	public _Debug.Debug_Vars DebugVariables;
	public DebugView.variables DebugViewVariables;
	[HideInInspector]
	public Calibration.data CalibrationData;


	#region delegates

	public delegate void OnCalibrationStartedDeleg(PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg(PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg(PupilGazeTracker manager);
//	public delegate void OnCalibDataDeleg(PupilGazeTracker manager,object position);
	public delegate void OnCalibrationGLDeleg();
	//public delegate void OnUpdateDeleg(ref Texture2D _eyeImage, ref Material _mat, object data);
	public delegate void OnUpdateDeleg();
//	public delegate void RepaintAction();//InspectorGUI repaint


	public delegate void DrawMenuDeleg ();
//	public delegate void OnSwitchCalibPointDeleg(PupilGazeTracker manager);
	public delegate void OnCalibDebugDeleg();
	public delegate void OnOperatorMonitorDeleg();
	public delegate void OnDrawGizmoDeleg ();

//	public event OnCalibrationStartedDeleg OnCalibrationStarted;
//	public event OnCalibrationDoneDeleg OnCalibrationDone;
	public event OnEyeGazeDeleg OnEyeGaze;
//	public event RepaintAction WantRepaint;

//	public event OnCalibDataDeleg OnCalibData;
	public DrawMenuDeleg DrawMenu;
	public OnCalibrationGLDeleg OnCalibrationGL;
//	public event OnSwitchCalibPointDeleg OnSwitchCalibPoint;

	public OnCalibDebugDeleg OnCalibDebug;
	public OnOperatorMonitorDeleg OnOperatorMonitor;
	public OnDrawGizmoDeleg OnDrawGizmo;
	public OnUpdateDeleg OnUpdate;

	#endregion

	#region calibration_vars


	#endregion
	//FRAME PUBLISHING VARIABLES
	#region frame_publishing_vars

	public DebugView.framePublishingVariables FramePublishingVariables;

	static Material lineMaterial;
	static Material eyeSphereMaterial;

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

	List<Dictionary<string,object>> _calibrationData=new List<Dictionary<string,object>>();
	public Dictionary<string,object> dict = new Dictionary<string, object>();

	[SerializeField]
	Dictionary<string,object>[] _CalibrationPoints
	{
		get{ return _calibrationData.ToArray (); }
	}
		



//	[HideInInspector]
//	public int ServicePort=50020;
	[HideInInspector]
	public int DefaultCalibrationCount=120;
	[HideInInspector]
	public int SamplesCount=4;
	[HideInInspector]
	public float CanvasWidth = 640;
	[HideInInspector]
	public float CanvasHeight=480;
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
	public List<GUIStyle> Styles = new List<GUIStyle>();
	[HideInInspector]
	public GUIStyle FoldOutStyle = new GUIStyle ();
	[HideInInspector]
	public GUIStyle ButtonStyle = new GUIStyle();
	[HideInInspector]
	public GUIStyle TextField = new GUIStyle();
	[HideInInspector]
	public GUIStyle CalibRowStyle = new GUIStyle();


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

	public PupilGazeTracker()
	{
		_Instance = this;
	}
		

//		public void RepaintGUI(){
//		if (WantRepaint != null)
//			WantRepaint ();
//	}
//
	#region Update
	void Update(){

		if (FramePublishingVariables.StreamCameraImages) {//Put this in a function and delegate it to the OnUpdate delegate
			elapsedTime = (float)TimeSpan.FromTicks (DateTime.Now.Ticks - lastTick).TotalSeconds;
			if (elapsedTime >= (1f / FramePublishingVariables.targetFPS)) {//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				AssignTexture (ref FramePublishingVariables.eye0Image, ref FramePublishingVariables.eye0ImageMaterial, FramePublishingVariables.raw0);
				AssignTexture (ref FramePublishingVariables.eye1Image, ref FramePublishingVariables.eye1ImageMaterial, FramePublishingVariables.raw1);
				lastTick = DateTime.Now.Ticks;
			}
		}

		if (OnUpdate != null)
			OnUpdate ();



		if (Input.GetKeyUp (KeyCode.C)) {
			if (PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.Calibration) {
				PupilTools.StopCalibration ();
			} else {
				PupilTools.StartCalibration();
			}
		}
		if (Input.GetKeyUp (KeyCode.R)) {
			
			if (!Recorder.isRecording) {
				Recorder.isRecording = true;
				Recorder.Start ();
			} else {
				Recorder.isRecording = false;
				Recorder.Stop ();
			}

		}
	}
	#endregion

	#region DebugView
	[HideInInspector]
	public DebugView._Transform[] OffsetTransforms;


	[HideInInspector]
	public bool isDrawCalibrationDebugInitialized = false;
	#endregion


	public void WriteStringToFile(string dataString, string fileName = "defaultFilename"){
		var bytes = System.Text.Encoding.UTF8.GetBytes (dataString);
		File.WriteAllBytes (Application.dataPath + "/" + fileName, bytes);
	}
	public string ReadStringFromFile(string fileName = "defaultFilename"){
		if (File.Exists (Application.dataPath + "/" + fileName)) {
			string _str = File.ReadAllText (Application.dataPath + "/" + fileName);
			return _str;
		} else {
			return "file Doesnt exist - null";
		}
	}

	public void InitViewLines(){
		if (LineDrawer.Instance != null) {
			LineDrawer.Instance.Clear ();
			foreach (Vector3 _v3 in CalibrationData.cal_gaze_points0_3d) {
				LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
					points = new Vector3[] {
						PupilData._3D.EyeCenters(0),
						_v3
					},
					color = new Color (1f, 0.6f, 0f, 0.1f)
				});
			}
			foreach (Vector3 _v3 in CalibrationData.cal_gaze_points1_3d) {
				LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {
					points = new Vector3[] {
						PupilData._3D.EyeCenters(1),
						_v3
					},
					color = new Color (1f, 1f, 0f, 0.1f)
				});
			}
			LineDrawer.Instance.Draw();
		}
	}
	//private Mesh mes;
	int debugViewFrameIndex = 0;
	public void InitDrawCalibrationDebug(){
		
		if (OffsetTransforms == null) {
			OffsetTransforms = new DebugView._Transform[]{new DebugView._Transform()};
		} else {
			foreach (DebugView._Transform _t in OffsetTransforms) {
				if (_t.GO == null) {
					_t.GO = new GameObject (_t.name);
					_t.GO.transform.position = _t.position;
					_t.GO.transform.rotation = Quaternion.Euler (_t.rotation);
					_t.GO.transform.localScale = _t.localScale;
				}
			}
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;

			//TODO: Initialize the point clouds outside of the drawer script, for example here, as it is with the line drawer
			DebugViewVariables.PointCloudGO = new GameObject ("PointCloudDrawer");
			DebugViewVariables.PointCloudGO.transform.parent = a.GO.transform;
			DebugViewVariables.PointCloudGO.transform.localPosition = Vector3.zero;
			DebugViewVariables.PointCloudGO.transform.localRotation = Quaternion.identity;
			DebugViewVariables.PointCloudGO.AddComponent<PointCloudDrawer> ();

			DebugViewVariables.LineDrawerGO = new GameObject ("LineDrawer");
			DebugViewVariables.LineDrawerGO.transform.parent = a.GO.transform;
			DebugViewVariables.LineDrawerGO.transform.localPosition = Vector3.zero;
			DebugViewVariables.LineDrawerGO.transform.localRotation = Quaternion.identity;
			DebugViewVariables.LineDrawerGO.AddComponent<LineDrawer> ();

			Invoke("InitViewLines", .7f);
			DebugViewVariables.isDrawLines = true;
			DebugViewVariables.isDrawPoints = true;
		}
		OnUpdate += CalibrationDebugInteraction;
		isDrawCalibrationDebugInitialized = true;
	}

	public void CalibrationDebugInteraction(){
		#region DebugView.Interactions
		if (Input.anyKey){
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;
			if (Input.GetKey(KeyCode.Alpha1)){
			a.GO.transform.position = new Vector3(-7,-9,127);
			a.GO.transform.rotation= Quaternion.Euler( new Vector3(150, -25, -15));
			}
			if (Input.GetKey(KeyCode.Alpha0)){
				a.GO.transform.position = new Vector3(-56,-4,237);
				a.GO.transform.rotation= Quaternion.Euler( new Vector3(62,73, -57));
			}
			if (Input.GetKey(KeyCode.Alpha2)){
				a.GO.transform.position = new Vector3(27.3f,-25f,321.2f);
				a.GO.transform.rotation= Quaternion.Euler( new Vector3(292.6f,0f, 0f));
			}
			if (Input.GetKey(KeyCode.Alpha3)){
				a.GO.transform.position = new Vector3(42f,-24f,300f);
				a.GO.transform.rotation= Quaternion.Euler( new Vector3(0f,190f, 0f));
			}
			if (Input.GetKey(KeyCode.Alpha4)){
				a.GO.transform.position = new Vector3(42f,27f,226f);
				a.GO.transform.rotation= Quaternion.Euler( new Vector3(0f,0f, 0f));
			}
			if (Input.GetKey(KeyCode.Alpha5)){
				a.GO.transform.position = new Vector3(99f,18f,276f);
				a.GO.transform.rotation= Quaternion.Euler( new Vector3(24f,292f, 30f));
			}
			if (Input.GetKey(KeyCode.W))
				a.GO.transform.position += -Camera.main.transform.forward;
			if (Input.GetKey(KeyCode.S))
				a.GO.transform.position += Camera.main.transform.forward;
			if (Input.GetKey(KeyCode.A))
				a.GO.transform.position += Camera.main.transform.right;
			if (Input.GetKey(KeyCode.D))
				a.GO.transform.position += -Camera.main.transform.right;
			if (Input.GetKey(KeyCode.Q))
				a.GO.transform.position += Camera.main.transform.up;
			if (Input.GetKey(KeyCode.E))
				a.GO.transform.position += -Camera.main.transform.up;
			if (Input.GetKeyDown(KeyCode.P)){
				if (DebugViewVariables.isDrawLines || DebugViewVariables.isDrawPoints){
					SetDrawCalibrationLinesNPoints(false);
				}else{
					SetDrawCalibrationLinesNPoints(true);
				}
			}
			if (Input.GetKeyUp (KeyCode.R)) {
				LineDrawer.Instance.Clear ();
				foreach (Vector3 _v3 in CalibrationData.cal_gaze_points0_3d) {
					LineDrawer.Instance.AddLineToMesh (new LineDrawer.param () {points = new Vector3[] {PupilData._3D.EyeCenters(0),_v3},color = new Color (1f, 0.6f, 0f, 0.1f)});
				}
				foreach (Vector3 _v3 in CalibrationData.cal_gaze_points1_3d) {
					LineDrawer.Instance.AddLineToMesh (new LineDrawer.param(){points = new Vector3[] { PupilData._3D.EyeCenters(1), _v3 }, color = new Color(1f, 1f, 0f, 0.1f)}   );
				}
				LineDrawer.Instance.Draw ();
			}

		}
		if (Input.GetMouseButton(1)){
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;
			a.GO.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"),Input.GetAxis("Mouse X"), 0));
			 
		}
		#endregion
	}

	public void SetDrawCalibrationLinesNPoints(bool toggle){
		SetDrawCalibrationLines (toggle);
		SetDrawCalibrationPointCloud (toggle);
	}

	public void SetDrawCalibrationLines(bool toggle){
		DebugViewVariables.isDrawLines = toggle;
		DebugViewVariables.LineDrawerGO.GetComponent<MeshRenderer> ().enabled = toggle;
	}

	public void SetDrawCalibrationPointCloud(bool toggle){
		DebugViewVariables.isDrawPoints = toggle;
		DebugViewVariables.PointCloudGO.GetComponent<MeshRenderer> ().enabled = toggle;
	}

	public void CloseCalibrationDebugView(){
		var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;
		if (a.GO != null)
			a.GO.SetActive (false);
		StopFramePublishing ();
		OnUpdate -= CalibrationDebugInteraction;
		OnCalibDebug -= DrawCalibrationDebugView;
		PupilSettings.Instance.debugView.active = false;
	}

	public void StartCalibrationDebugView(){
		if (DebugViewVariables.DebugEyeMesh != null) {
			var a = (from tr in OffsetTransforms
			         where tr.name == "Debug View Origin Matrix"
			         select tr).FirstOrDefault () as DebugView._Transform;
			if (a.GO != null)
				a.GO.SetActive (true);

			if (OnCalibDebug == null)
				OnCalibDebug += DrawCalibrationDebugView;
//			OnCalibDebug -= DrawCalibrationDebugView;
			OnUpdate -= CalibrationDebugInteraction;
			OnUpdate += CalibrationDebugInteraction;
			InitializeFramePublishing ();
			StartFramePublishing ();
		} else {
			UnityEngine.Debug.LogWarning ("Please assign a Debug Eye Mesh under the Settings Debug View Variables. Accessable in Developer Mode!");
			PupilSettings.Instance.debugView.active = false;
		}
	}
	public void DrawCalibrationDebugView(){

		debugViewFrameIndex++;

		if (!isDrawCalibrationDebugInitialized)
			InitDrawCalibrationDebug ();

		CreateLineMaterial ();
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		CreateEyeSphereMaterial ();



		Vector3 eye0Pos = PupilData._3D.EyeCenters (0);
		Vector3 eye0Norm = PupilData._3D.EyeCenters (0);

		Vector3 eye1Pos = PupilData._3D.EyeCenters (1);
		Vector3 eye1Norm = PupilData._3D.EyeCenters (1);

		Vector3 gazePoint = PupilData._3D.Gaze ();

		////////////////Draw 3D pupils////////////////
		Vector3 _pupil0Center = PupilData._3D.Circle.Center(0);
		Vector3 _pupil1Center = PupilData._3D.Circle.Center(1);
		float _pupil0Radius = (float)PupilData._3D.Circle.Radius (0);
		float _pupil1Radius = (float)PupilData._3D.Circle.Radius (1);
		Vector3 _pupil0Normal = PupilData._3D.Circle.Normal (0);
		DrawDebugSphere (originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, offsetMatrix: CalibrationData.eye_camera_to_world_matrix1, position: _pupil0Center, size: _pupil0Radius, sphereColor: Color.black, forward: _pupil0Normal, wired: false);
		DrawDebugSphere (originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, offsetMatrix: CalibrationData.eye_camera_to_world_matrix0, position: _pupil1Center, size: _pupil1Radius, sphereColor: Color.black, forward: eye0Norm, wired: false);
		////////////////Draw 3D pupils////////////////

		////////////////Draw eye camera frustums////////////////
		DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix0, fieldOfView: 90, aspect: aspectRatios.FOURBYTHREE, minViewDistance: 0.001f, maxViewDistance: 30, frustumColor: Color.black, drawEye: true, eyeID: 1, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true, eyeMaterial: FramePublishingVariables.eye1ImageMaterial, eyeImageRotation: 0);
		DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix1, fieldOfView: 90, aspect: aspectRatios.FOURBYTHREE, minViewDistance: 0.001f, maxViewDistance: 30, frustumColor: Color.white, drawEye: true, eyeID: 0, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true, eyeMaterial: FramePublishingVariables.eye0ImageMaterial, eyeImageRotation: 0);
		////////////////Draw eye camera frustums/////////////////// 

		////////////////Draw 3D eyeballs////////////////
		DrawDebugSphere (position: eye0Pos, eyeID: 0, forward: eye0Norm, isEye: true, norm_length: gazePoint.magnitude * DebugVariables.value0, sphereColor: Color.white, norm_color: Color.red, size: DebugViewVariables.EyeSize, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, wired: false);//eye0
		DrawDebugSphere (position: eye1Pos, eyeID: 1, forward: eye1Norm, isEye: true, norm_length: gazePoint.magnitude * DebugVariables.value0, sphereColor: Color.white, norm_color: Color.red, size: DebugViewVariables.EyeSize, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, wired: false);//eye1
		////////////////Draw 3D eyeballs////////////////

		////////////////Draw HMD camera frustum//////////////// fov 137.7274f
		DrawCameraFrustum (origin: OffsetTransforms [1].GO.transform.localToWorldMatrix, fieldOfView: 111, aspect: aspectRatios.FULLVIVE, minViewDistance: 0.001f, maxViewDistance: 100f, frustumColor: Color.gray, drawEye: false, eyeID: 0);
		////////////////Draw HMD camera frustum////////////////

		////////////////Draw gaze point 3D////////////////
		DrawDebugSphere (position: gazePoint, eyeID: 10, forward: eye1Norm, isEye: false, norm_length: 20, sphereColor: Color.red, norm_color: Color.clear, size: 10, originMatrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye1
		////////////////Draw gaze point 3D////////////////


	}

	public void DrawCircle(Vector3 pos, Matrix4x4 originMatrix, Matrix4x4 offsetMatrix, float size){
		PupilSettings.Calibration.Marker _circle = new PupilSettings.Calibration.Marker ();
		_circle.position = pos;
		_circle.size = size;
		_circle.toggle = true;
		GL.MultMatrix (originMatrix * offsetMatrix);
		CalibrationGL.Marker (_circle);
	}
//	public Texture2D circleTexture;

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
	public void CreateEye0ImageMaterial ()
	{
		if (!FramePublishingVariables.eye0ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			FramePublishingVariables.eye0ImageMaterial = new Material (shader);
			FramePublishingVariables.eye0ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}
	public void CreateEye1ImageMaterial ()
	{
		if (!FramePublishingVariables.eye1ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			FramePublishingVariables.eye1ImageMaterial = new Material (shader);
			FramePublishingVariables.eye1ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
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
		


	public enum aspectRatios{FULLVIVE,HALFVIVE,FULLHD,ONEOONE,FOURBYTHREE};

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
	public void DrawDebugSphere(Matrix4x4 originMatrix = default(Matrix4x4), Vector3 position = default(Vector3), int eyeID = 10, Matrix4x4 offsetMatrix = default(Matrix4x4),  Vector3 forward = default(Vector3), float norm_length = 20, bool isEye = false, Color norm_color = default(Color), Color sphereColor = default(Color), float size = 24.2f, float sizeZ = 1f, bool wired = true){

		eyeSphereMaterial.SetColor ("_Color", sphereColor);
		eyeSphereMaterial.SetPass (0);

		if (originMatrix == default(Matrix4x4))
			originMatrix = Camera.main.transform.localToWorldMatrix;

		Matrix4x4 _m = new Matrix4x4 ();

		//print ("from : " + forward + " to :  " + Quaternion.LookRotation (forward, Vector3.up));

		//TODO: rework this: now Forward vector needed for position assignment, not good!
		if (forward != Vector3.zero) {
			_m.SetTRS (position, Quaternion.LookRotation (forward, Vector3.up), new Vector3 (size, size, size));
		} else {
			//TODO: store the last known position and assign that here
			_m.SetTRS (new Vector3(100*eyeID,0,0), Quaternion.identity, new Vector3 (size, size, size));
			forward = Vector3.forward;
		}

//		if (position == default(Vector3))
//			print ("default vector 3 as position found");

		if (offsetMatrix != default(Matrix4x4))
			_m = offsetMatrix*_m;
		if (wired)
			GL.wireframe = true;
		Graphics.DrawMeshNow(DebugViewVariables.DebugEyeMesh, originMatrix*_m);
		GL.wireframe = false;

		if (isEye) {

			//IRIS//
//			eyeSphereMaterial.SetColor ("_Color", new Color(0,1f,0,.5f));
//			eyeSphereMaterial.SetPass (0);
//			Graphics.DrawMeshNow(DebugViewVariables.DebugEyeMesh, originMatrix*Matrix4x4.TRS (position + (forward * 10.5f), Quaternion.LookRotation (forward, Vector3.up), new Vector3 (10, 10, 3.7f)));
			//IRIS//

		eyeSphereMaterial.SetColor ("_Color", norm_color);
		eyeSphereMaterial.SetPass (0);

			GL.MultMatrix (originMatrix * _m);
			GL.Begin (GL.LINES);
			GL.Vertex (Vector3.zero);
			GL.Vertex (Vector3.forward * norm_length);
			GL.End ();
		}
	}
	#endregion
	#region DebugView.CameraFrustum
	public void DrawCameraFrustum(Matrix4x4 origin, float fieldOfView, aspectRatios aspect, float minViewDistance, float maxViewDistance, Color frustumColor = default(Color), Transform transformOffset = null, bool drawEye = false, int eyeID = 0, bool drawCameraImage = false, int eyeImageRotation = 0, Material eyeMaterial = default(Material)){

		lineMaterial.SetColor ("_Color", frustumColor);
		lineMaterial.SetPass (0);

		Matrix4x4 offsetMatrix = new Matrix4x4 ();

		if (origin == default(Matrix4x4))
			origin = Camera.main.transform.localToWorldMatrix;

		if (transformOffset == null) {
			offsetMatrix.SetTRS (Vector3.zero, Quaternion.identity, Vector3.one );
		} else {
			offsetMatrix = transformOffset.localToWorldMatrix;
		}

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
		case aspectRatios.ONEOONE:
			aspectRatio = 1f;
			break;
		case aspectRatios.FOURBYTHREE:
			aspectRatio = 1.3333f;
			break;
		}
		//Vector3 up = origin.up;
		Rect3D farPlaneRect = new Rect3D ();
		Rect3D nearPlaneRect = new Rect3D ();

		GL.MultMatrix (offsetMatrix*origin);

		GL.Begin (GL.LINES);
		float ratio =  Mathf.Sin( ((fieldOfView/2)*Mathf.PI)/180 )/Mathf.Sin(  (   (  ((180-fieldOfView)/2)*Mathf.PI   )/180    ) );

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
		GL.Color (Color.red);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.right*30);
		//Y
		GL.Color (Color.green);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.up*30);
		//Z
		GL.Color (Color.blue);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.forward*30);
		//Draw Gizmo
		GL.End ();
		#endregion

		if (drawCameraImage)
			DrawCameraImages (eyeMaterial, farPlaneRect.verticies, farPlaneRect.width, eyeImageRotation);
//		if (drawEye) {
//			float flipper = 1;
//			if (eyeID == 1)
//				flipper = -1;
//
//			float scaler = widthMaxView / 640 / 24.2f;//scaling
//
//			Matrix4x4 _imageSpaceMatrix = offsetMatrix * origin * Matrix4x4.TRS (new Vector3(flipper*(widthMaxView/2), -flipper*(heightMaxView/2),maxViewDistance), Quaternion.identity, Vector3.one*24.2f);
//			float eyeCenterX = 0f;
//			float eyeCenterY = 0f;
//			eyeCenterX = (float)Pupil.values.BaseData [eyeID].projected_sphere.center [0];
//			eyeCenterY = (float)Pupil.values.BaseData [eyeID].projected_sphere.center [1];
//			GL.wireframe = true;
//			Graphics.DrawMeshNow (DebugViewVariables.DebugEyeMesh, _imageSpaceMatrix * Matrix4x4.Translate (  new Vector3 (-flipper*eyeCenterX* scaler, flipper*eyeCenterY* scaler, 0)   ));
//			GL.wireframe = false;
//		}

		GL.PopMatrix ();


	}	
	#endregion
	#region DebugView.CameraFrustum.CameraImages
	void DrawCameraImages(Material eyeMaterial, Vector3[] drawPlane, float width, int offset = 0){
		float[] _f = new float[]{ 0, 1, 1, 0, 0, 1, 1, 0, 0 };
		eyeMaterial.SetPass (0);
		GL.Begin (GL.QUADS);
		for (int j = drawPlane.Count () - 1; j > -1; j--) {
			int ind = (drawPlane.Count () - 1) - j + offset;
			GL.TexCoord2 (_f [ind], _f [ind + 1]);
			GL.Vertex3 (-drawPlane [j].x, drawPlane [j].y, drawPlane [j].z);
		}
		GL.End ();
	}	
	#endregion

	void InitializeEyes(ref bool eyeProcess){
		if (!Pupil.processStatus.initialized) {
			eyeProcess = true;
			if (Pupil.processStatus.eyeProcess0 && Pupil.processStatus.eyeProcess1) {
				Pupil.processStatus.initialized = true;
				//UnSubscribeFrom ("pupil.");
			}
		}
	}




	public Vector3[] Vector3ArrayFromString(string v3StringArray){
		List<Vector3> _v3List = new List<Vector3> ();
		List<float> _v3TempList = new List<float> ();
		string[] _v3StringArray = v3StringArray.Split ("],[".ToCharArray());
		foreach (string s in _v3StringArray) {
			if (s != "") {
				_v3TempList.Add (float.Parse (s));
			}
			if (_v3TempList.Count == 3) {
				_v3List.Add (new Vector3 (_v3TempList [0], _v3TempList [1], _v3TempList [2]));
				_v3TempList.Clear ();
			}

		}
		return _v3List.ToArray ();
	}
	public Matrix4x4 Matrix4x4FromString(string matrixString, bool column = true, float scaler = 1f){
		Matrix4x4 _m = new Matrix4x4 ();
		List<Vector4> _v4List = new List<Vector4> ();
		List<float> _v4TempList = new List<float> ();
		string[] _matrixStringArray = matrixString.Split ("],[".ToCharArray ());
		int ind = 0;
		foreach (string s in _matrixStringArray) {
			if (s != "")
				_v4TempList.Add (float.Parse (s));
			if (_v4TempList.Count == 4) {
				_v4List.Add (new Vector4 (_v4TempList [0], _v4TempList [1], _v4TempList [2], _v4TempList [3]));
				_v4TempList.Clear ();
				if (column) {
					_m.SetColumn (ind, _v4List.LastOrDefault ());
				} else {
					_m.SetRow (ind, _v4List.LastOrDefault ());
				}
				ind++;
			}
		}
		return _m;
	}
	public Dictionary<string, object> DictionaryFromJSON(string json){
		List<string> keys = new List<string> ();
		List<string> values = new List<string> ();
		Dictionary<string,object> dict = new Dictionary<string, object> ();

		string[] a = json.Split ("\"".ToCharArray(), 50);

		int ind = 0;
		foreach (string s in a) {
			if (s.Contains (":") && !s.Contains ("{")) {
				//print (s);
				keys.Add (a [ind - 1]);
				a [ind] = a [ind].Replace (":", "");
				a [ind] = a [ind].Substring (0, a [ind].Length - 2);
				a [ind] = a [ind].Replace (" ", "");
				a [ind] = a [ind].Replace ("}", "");
				values.Add (a [ind]);
			}
			ind++;
		}


		for (int i = 0; i < keys.Count; i++) {
			dict.Add (keys [i], values [i]);
		}

		return dict;
	}

	public void ReadCalibrationData(string str){

		if (!str.Contains ("null")) {
		
			Dictionary<string, object> camera_matricies_dict = DictionaryFromJSON (str);
			//print (camera_matricies_dict.Count);

			CalibrationData = JsonUtility.FromJson<Calibration.data> (str);
			object o;
			camera_matricies_dict.TryGetValue ("cal_gaze_points0_3d", out o);
			CalibrationData.cal_gaze_points0_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_gaze_points1_3d", out o);
			CalibrationData.cal_gaze_points1_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_ref_points_3d", out o);
			CalibrationData.cal_ref_points_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_points_3d", out o);
			CalibrationData.cal_points_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("eye_camera_to_world_matrix0", out o);
			CalibrationData.eye_camera_to_world_matrix0 = Matrix4x4FromString (o.ToString (), false) * Matrix4x4.Scale (new Vector3 (1, -1, 1));
			camera_matricies_dict.TryGetValue ("eye_camera_to_world_matrix1", out o);
			CalibrationData.eye_camera_to_world_matrix1 = Matrix4x4FromString (o.ToString (), false) * Matrix4x4.TRS (Vector3.zero, Quaternion.Euler (0, 0, 0), new Vector3 (1, -1, 1));
		
		}

	}
	long lastTick;
	float elapsedTime;

	public byte[] doubleArrayToByteArray(double[] doubleArray){

		byte[] _bytes_blockcopy;

		_bytes_blockcopy = new byte[doubleArray.Length*8];

		Buffer.BlockCopy(doubleArray, 0, _bytes_blockcopy, 0, doubleArray.Length*8 );

		return _bytes_blockcopy;

	}


	void OnEnable(){
		
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		
	}

	void OnDisable(){
		PupilGazeTracker._Instance = null;
	}

	#region Start();
	void Start()
	{

		string str = ReadStringFromFile ("camera_intrinsics");
		ReadCalibrationData (str);

		lastTick = DateTime.Now.Ticks;
		elapsedTime = 0f;

		CalibrationGL.InitializeVisuals(PupilSettings.EStatus.ProcessingGaze);

		if (FramePublishingVariables.StreamCameraImages)
			InitializeFramePublishing ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		
		leftEye = new EyeData (SamplesCount);
		rightEye= new EyeData (SamplesCount);

		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null) {
			OperatorMonitor.Instantiate ();
		}
			//OnOperatorMonitor += DrawOperatorMonitor;
		if (PupilSettings.Instance.debugView.active)
			StartCalibrationDebugView ();

		PupilGazeTracker.Instance.ProjectName = Application.productName;


	}
	#endregion

	#region frame_publishing.functions
	public void InitializeFramePublishing(){
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		FramePublishingVariables.eye0Image = new Texture2D (100,100);
		FramePublishingVariables.eye1Image = new Texture2D (100,100);
	}
	public void StartFramePublishing(){
		FramePublishingVariables.StreamCameraImages = true;
		PupilTools._sendRequestMessage (new Dictionary<string,object> {{"subject","plugin_started"},{"name", "Frame_Publisher"}});
		PupilTools.SubscribeTo ("frame.");
//		print ("frame publish start");
		//_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}
	public void StopFramePublishing(){
		if (!PupilSettings.Instance.debugView.active && !isOperatorMonitor) {
			PupilTools.UnSubscribeFrom ("frame.");
			FramePublishingVariables.StreamCameraImages = false;
			//_sendRequestMessage (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });
		}
	}

	public void AssignTexture(ref Texture2D _eyeImage, ref Material _mat, byte[] data){
		
		_eyeImage.LoadImage (data);
		_mat.mainTexture = _eyeImage;
	
	}

	#endregion


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
			if (Process.GetProcessesByName ("pupil_capture").Length > 0) {
				UnityEngine.Debug.LogWarning (" Pupil Capture is already running ! ");
			} else {
				serviceProcess = new Process ();
				serviceProcess.StartInfo.Arguments = servicePath;
				serviceProcess.StartInfo.FileName = servicePath;
				if (File.Exists (servicePath)) {
					serviceProcess.Start ();
				} else {
					print ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
				}
			}
		} else{
			if (PupilServiceFileName == "") {
				print ("Pupil Service filename is not specified, most likely you will have to check if you have it set for the current platform under settings Platforms(DEV opt.)");
			}
		}
	}





	public void InitializePlatformsDictionary(){
		PlatformsDictionary = new Dictionary<RuntimePlatform, string[]> ();
		foreach (Platform p in Platforms) {
			PlatformsDictionary.Add (p.platform, new string[]{ p.DefaultPath, p.FileName });
		}
	}

	#region packet
	public struct _double{
		public double[] value;
		public Vector2 v2{get{ return new Vector2 ((float)value [0], (float)value [1]); }}
		public Vector3 v3{get{ return new Vector3 ((float)value [0], (float)value [1], (float)value [2]); }}
		public Vector3 convertedV3{get{ return new Vector3 (-(float)value [0], (float)value [1], (float)value [2]); }}
	}

	void OnUpdateEyeCenter(){//This happens on MainThread

		InitViewLines ();
	
	}

	void VisualizeGaze(){


		if (PupilSettings.Instance.dataProcess.state == PupilSettings.EStatus.ProcessingGaze) {
			object eyeID;
			float x, y;
			if ( PupilData._2D.ID() != null ) {
				PupilSettings.Calibration.Marker _markerLeftEye = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "LeftEye_2D").ToList () [0];
				PupilSettings.Calibration.Marker _markerRightEye = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "RightEye_2D").ToList () [0];
				PupilSettings.Calibration.Marker _markerGazeCenter = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.name == "Gaze_2D").ToList () [0];

				string eyeIDStr = PupilData._2D.ID();

				if (PupilSettings.Instance.calibration.currentCalibrationMode == PupilSettings.Calibration.CalibMode._2D) {
					Vector2 pos_v2 = PupilData._2D.Norm_Pos ();//_pupilDataDict.GetValueAsVector ("norm_pos").v2;
					x = pos_v2.x;
					y = pos_v2.y;
//				_eyeGazePos2D.x = (leftEye.gaze2D.x + rightEye.gaze2D.x) * 0.5f;
//				_eyeGazePos2D.y = (leftEye.gaze2D.y + rightEye.gaze2D.y) * 0.5f;


					if (eyeIDStr == "0") {
						leftEye.AddGaze (x, y, 0);
						if (OnEyeGaze != null)
							OnEyeGaze (this);
					} else if (eyeIDStr == "1") {
						rightEye.AddGaze (x, y, 1);
						if (OnEyeGaze != null)
							OnEyeGaze (this);
					}

					_markerLeftEye.position.x = GetEyeGaze2D (GazeSource.LeftEye).x;
					_markerLeftEye.position.y = GetEyeGaze2D (GazeSource.LeftEye).y;

					_markerRightEye.position.x = GetEyeGaze2D (GazeSource.RightEye).x;
					_markerRightEye.position.y = GetEyeGaze2D (GazeSource.RightEye).y;

					_markerGazeCenter.position.x = PupilData._2D.Gaze ().x;
					_markerGazeCenter.position.y = PupilData._2D.Gaze ().y;
				}
			}
			if (PupilSettings.Instance.calibration.currentCalibrationMode == PupilSettings.Calibration.CalibMode._3D) {

				PupilSettings.Calibration.Marker gaze3D = PupilSettings.Instance.calibration.CalibrationMarkers.Where (p => p.calibMode == PupilSettings.Calibration.CalibMode._3D && !p.calibrationPoint).ToList () [0];

				gaze3D.position = PupilData._3D.Gaze ();

			}
		} 
	}

	#endregion



	public IEnumerator InitializeCalibration(){
	
		print ("Initializing Calibration");

		PupilSettings pupilSettings = PupilSettings.Instance;

		pupilSettings.calibration.currCalibPoint = 0;
		pupilSettings.calibration.currCalibSamples = 0;

		pupilSettings.calibration.marker.position.x = pupilSettings.calibration.currentCalibrationType.calibPoints [0][0];
		pupilSettings.calibration.marker.position.y = pupilSettings.calibration.currentCalibrationType.calibPoints [0][1];
		pupilSettings.calibration.marker.position.z = pupilSettings.calibration.currentCalibrationType.depth;

		CalibrationGL.InitializeVisuals (PupilSettings.EStatus.Calibration);

		yield return new WaitForSeconds (2f);

		print ("Starting Calibration");

		pupilSettings.calibration.initialized = true;
		pupilSettings.dataProcess.state = PupilSettings.EStatus.Calibration;

		PupilTools.RepaintGUI ();

	}

	float lastTimeStamp = 0;

	public void Calibrate(){

		PupilSettings pupilSettings = PupilSettings.Instance;

		// Get the current calibration information from the PupilSettings class
		PupilSettings.CalibrationType currentCalibrationType = pupilSettings.calibration.currentCalibrationType;

		float[] _currentCalibPointPosition = pupilSettings.calibration.currentCalibrationType.calibPoints [pupilSettings.calibration.currCalibPoint];




		float alphaRatio = Mathf.InverseLerp (DefaultCalibrationCount, 0f, pupilSettings.calibration.currCalibSamples);//*_m.baseSize;//size hardcoded, change this
		PupilSettings.Calibration.Marker marker = pupilSettings.calibration.CalibrationMarkers.Where (p => p.calibrationPoint && p.calibMode == PupilSettings.Instance.calibration.currentCalibrationMode).ToList () [0];
		marker.color = new Color (1f, 1f, 1f, alphaRatio);
		marker.position.x = _currentCalibPointPosition [0];
		marker.position.y = _currentCalibPointPosition [1];
		marker.position.z = currentCalibrationType.depth;//using the height az depth offset
		marker.toggle = true;

		float t = PupilTools.GetPupilTimestamp ();

		if (t - lastTimeStamp > 0.1f) {
			lastTimeStamp = t;

			print ("its okay to go on");

			//Create reference data to pass on. _cPointFloatValues are storing the float values for the relevant current Calibration mode

			var ref0 = new Dictionary<string,object> () {
				{ currentCalibrationType.positionKey,_currentCalibPointPosition }, {
					"timestamp",
					t
				},
				 {
					"id",
					0
				}
			};
			var ref1 = new Dictionary<string,object> () {
				{ currentCalibrationType.positionKey,_currentCalibPointPosition },
				 {
					"timestamp",
					t
				},
				 {
					"id",
					1
				}
			};


			_calibrationData.Add (ref0);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			_calibrationData.Add (ref1);//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.

			if (pupilSettings.debug.printSampling)
				print ("Sampling at : " + pupilSettings.calibration.currCalibSamples + ". On the position : " + _currentCalibPointPosition [0] + " | " + _currentCalibPointPosition [1]);

			pupilSettings.calibration.currCalibSamples++;//Increment the current calibration sample. (Default sample amount per calibration point is 120)

			if (pupilSettings.calibration.currCalibSamples >= DefaultCalibrationCount) {
			
				pupilSettings.calibration.currCalibSamples = 0;
				pupilSettings.calibration.currCalibPoint++;

				//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
				PupilTools._sendRequestMessage (new Dictionary<string,object> {
					{ "subject","calibration.add_ref_data" }, {
					"ref_data",
					_CalibrationPoints
				}
			});



				if(	pupilSettings.debug.printSampling){
					print("Sending ref_data");

					string str = "";

					foreach(var element in _CalibrationPoints){

						foreach(var i in element){
							if (i.Key == "norm_pos"){
								str += "|| " + i.Key + " | " + ((Single[])i.Value)[0] + " , " + ((Single[])i.Value)[1];
							}else{
								str += "|| " + i.Key + " | " + i.Value.ToString();
							}
						}
						str += "\n";

					}

					print(str);
				}

			//Clear the current calibration data, so we can proceed to the next point if there is any.
				_calibrationData.Clear ();

				if (pupilSettings.calibration.currCalibPoint >= currentCalibrationType.calibPoints.Count){
				
				PupilTools.StopCalibration();

				}

			}
		}


	}

	void OnGUI()
	{
		if (!isOperatorMonitor) {
			string str = "Capture Rate=" + FPS;
			str += "\nLeft Eye:" + LeftEyePos.ToString ();
			str += "\nRight Eye:" + RightEyePos.ToString ();
			GUI.TextArea (new Rect (0, 0, 200, 50), str);
		}

	}

	public void SwitchCalibrationMode(){

		CalibrationGL.InitializeVisuals (PupilSettings.Instance.dataProcess.state);
	
	}

	#region Plugin Control
	public void StartBinocularVectorGazeMapper(){
		PupilTools._sendRequestMessage (new Dictionary<string,object> {{"subject",""},{"name", "Binocular_Vector_Gaze_Mapper"}});
	}
	#endregion

	#region Recording
	public void OnRecording(){
	}

	#endregion

	public static T ByteArrayToObject<T>(byte[] arrayOfBytes){
		if (arrayOfBytes == null || arrayOfBytes.Length < 1)
			return default(T);

		BinaryFormatter binaryFormatter = new BinaryFormatter ();

		T obj = (T)binaryFormatter.Deserialize (new MemoryStream (arrayOfBytes));

		return obj;
	}

	void OnApplicationQuit(){

		#if UNITY_EDITOR // Operator window will only be available in Editor mode
		if (OperatorWindow.Instance != null)
			OperatorWindow.Instance.Close ();
		#endif

		Pupil.processStatus.eyeProcess0 = false;
		Pupil.processStatus.eyeProcess1 = false;

	}
}
