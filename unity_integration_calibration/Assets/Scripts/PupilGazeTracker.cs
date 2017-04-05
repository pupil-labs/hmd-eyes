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
using System;
using NetMQ;
using NetMQ.Sockets;

using MsgPack.Serialization;
using System.Linq;

public delegate void Task();


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



namespace Recording{
	public class variables : MonoBehaviour{
		public static bool fixLength;
		public static float length;
		public static bool isRecording;
		public static int FPS;
		public static string FilePath;
		public static string pathDirectory;
		public static string pathFileName = "MyRecording";
		public static string pathFileExtension = "mov";
		public static FFmpegOut.CameraCapture CaptureScript;
	}
}
namespace Pupil
{
	[Serializable]
	public static class connectionParameters{
		public static List<string> toSubscribe = new List<string> (){ "notify.", "gaze" };
		public static bool update = false;
	}
	[Serializable]
	public static class values{
		public static Vector3[] EyeCenters3D = new Vector3[2];
		public static Vector3[] GazeNormals3D = new Vector3[2];
		public static Vector3 GazePoint3D = new Vector3();
		public static float Diameter = 0f;
		//TODO: get the confidences
		public static float[] Confidences = new float[]{ 0f, 0f };
		public static BaseData[] BaseData = new BaseData[]{ new BaseData (), new BaseData () };
	}
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
//	[Serializable]
//	public class BaseDataArray{
//		public BaseData[] base_data_array;
//	}
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
	public struct  processStatus{
		public static bool eyeProcess0;
		public static bool eyeProcess1;
	}
}
namespace Operator{
	[Serializable]
	public class properties{
		public int id;
		public Vector3 positionOffset = new Vector3 ();
		public Vector3 rotationOffset = new Vector3 ();
		public Vector3 scaleOffset = Vector3.one;
		public Vector2 graphScale = new Vector2 (1, 1);
		public float gapSize = 1;
		public int graphLength = 20;
		public float confidence = 0.2f;
		public float refreshDelay;
		public long graphTime = DateTime.Now.Ticks;
		public bool update = false;
		public List<float> confidenceList = new List<float> ();
		public Camera OperatorCamera;
		public static properties[] Properties = default(Operator.properties[]);
	}
}
namespace Calibration{
	[Serializable]
	public class marker{
		public string name;
		public Rect shape;
		public Color color;
		public bool toggle;
		public bool calibrationPoint;
		public Material material;
		public PupilGazeTracker.CalibModes calibMode;
		public Matrix4x4 offsetMatrix = new Matrix4x4 ();
		public Vector3 position{
			get { 
				//TODO : rework the shape&position settings.
				Vector3 _v3 = new Vector3 (shape.x, shape.y, shape.height);
				return _v3;
			}
			set{
				shape.x = value.x;
				shape.y = value.y;
				shape.height = value.y;
			}
		}
	}
}
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
		public Calibration.marker Circle;
	}
	[Serializable]
	public class framePublishingVariables{
		public int targetFPS = 20;
		public Texture2D eye0Image;
		public Texture2D eye1Image;
		public bool drawImage0 = false;
		public bool drawImage1 = false;
		public bool updateData0 = false;
		public bool updateData1 = false;
		public byte[] raw0;
		public byte[] raw1;
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
public class CalibPoints
{
	//TODO: rework this!
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
	public List<floatArray> Get3DList(bool _convert = false){
		List<floatArray> _tmpList = new List<floatArray> ();
		if (list3D == null)
			list3D = new List<floatArray> ();
		if (_convert) {
			foreach (floatArray _fa in list3D) {
				_tmpList.Add (new floatArray (){ axisValues = new float[]{ -_fa.axisValues [0], -_fa.axisValues [1], _fa.axisValues [2] } });
			}
		} else {
			_tmpList = list3D;
		}
		return _tmpList;
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

	//public Pupil.PupilData _pupilData;
	public pupilDataDictionary _pupilDataDict = new pupilDataDictionary();
	public class pupilDataDictionary
	{
		public Dictionary<string, object> dictionary = new Dictionary<string, object>();
		public object GetValue(string key){
			object _value;
			if (dictionary.TryGetValue (key, out _value)) {
				return _value;
			} else {
				return null;
			}
		}
		public string GetValueAsString(string key){	
			object _strObj = GetValue (key);
			if (_strObj != null) {
				return _strObj.ToString ();
			} else {
				return "value is null from key : " + key;
			}
		}
		public float GetValueAsFloat(string key){
			return float.Parse( GetValue (key).ToString());
		}
		public int GetValueAsInteger(string key){
			return int.Parse( GetValue (key).ToString());
		}
		public _double GetValueAsVector(string key){
			string _doubleJson = GetValueAsString (key);
			if (!_doubleJson.Contains ("null")) {
				return JsonUtility.FromJson<_double> ("{\"value\": " + GetValueAsString (key) + "}");
			} else {
				return new _double (){ value = new double[]{ 0.0, 0.0, 0.0 } };
			}
		}
		public Vector3[] GetValueAsVectorArray(string key, float converter = 1f){
			string json = GetValueAsString (key);
			if (!json.Contains("null")) {
				json = json.Replace ("0 :", "\"zero\" :");
				json = json.Replace ("1 :", "\"one\" :");
				Pupil.eyes3Ddata _3dData = JsonUtility.FromJson<Pupil.eyes3Ddata> (json);
				return new Vector3[] {
					new Vector3 (((float)_3dData.zero [0])*converter, ((float)_3dData.zero [1])*converter, (float)_3dData.zero [2]),
					new Vector3 (((float)_3dData.one [0])*converter, ((float)_3dData.one [1])*converter, (float)_3dData.one [2])
				};
			} else {
				return new Vector3[]{ Vector3.zero, Vector3.zero };
			}
		}

	}
	public int graphstyle = 0;
	public _Debug.Debug_Vars DebugVariables;
	public DebugView.variables DebugViewVariables;
	#region delegates

	public delegate void OnCalibrationStartedDeleg(PupilGazeTracker manager);
	public delegate void OnCalibrationDoneDeleg(PupilGazeTracker manager);
	public delegate void OnEyeGazeDeleg(PupilGazeTracker manager);
	public delegate void OnCalibDataDeleg(PupilGazeTracker manager,object position);
	public delegate void OnCalibrationGLDeleg();
	//public delegate void OnUpdateDeleg(ref Texture2D _eyeImage, ref Material _mat, object data);
	public delegate void OnUpdateDeleg();


	public delegate void DrawMenuDeleg ();
	public delegate void OnSwitchCalibPointDeleg(PupilGazeTracker manager);
	public delegate void OnCalibDebugDeleg();
	public delegate void OnOperatorMonitorDeleg();
	public delegate void OnDrawGizmoDeleg ();

	public event OnCalibrationStartedDeleg OnCalibrationStarted;
	public event OnCalibrationDoneDeleg OnCalibrationDone;
	public event OnEyeGazeDeleg OnEyeGaze;
	public event OnCalibDataDeleg OnCalibData;
	public DrawMenuDeleg DrawMenu;
	public OnCalibrationGLDeleg OnCalibrationGL;
	public event OnSwitchCalibPointDeleg OnSwitchCalibPoint;

	public OnCalibDebugDeleg OnCalibDebug;
	public OnOperatorMonitorDeleg OnOperatorMonitor;
	public OnDrawGizmoDeleg OnDrawGizmo;
	public OnUpdateDeleg OnUpdate;

	#endregion

	#region calibration_vars
	//Use status!!!!!!!!!!!

	[HideInInspector]
	public bool isCalibrating = false;



	public Calibration.marker[] CalibrationMarkers;

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

	//changed this
	bool _isconnected =false;
	public bool IsConnected{
		get{ return _isconnected; }
		set{_isconnected = value;}
	}

	[HideInInspector]
	public bool _isFullConnected =false;
	RequestSocket _requestSocket ;

	List<Dictionary<string,object>> _calibrationData=new List<Dictionary<string,object>>();
	public Dictionary<string,object> dict = new Dictionary<string, object>();

	[SerializeField]
	Dictionary<string,object>[] _CalibrationPoints
	{
		get{ return _calibrationData.ToArray (); }
	}
		
	int _calibSamples;
	int _currCalibPoint=0;
	int _currCalibSamples=0;

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
				name = "2D",
				calibrationPoints = _calibPoints.Get2DList(),
				calibPlugin = "HMD_Calibration",
				positionKey = "norm_pos",
				ref_data = new double[]{0.0,0.0},
				depth = 0
			});
			_calibModes.Add (CalibModes._3D, new CalibModeDetails () {
				name = "3D",
				calibrationPoints = _calibPoints.Get3DList(_convert: true),
				calibPlugin = "HMD_Calibration_3D",
				positionKey = "mm_pos",
				ref_data = new double[]{0.0,0.0,0.0},
				depth = 100
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
		public string name;
		public List<floatArray> calibrationPoints;
		public string positionKey;//A string that refers to a value in the ref_data in 2D its norm_pos in 3D its mm_pos
		public string calibPlugin;//Currently containing HMD_CALIBRATION and HMD_CALIBRATION_3D
		public Type type;
		public double[] ref_data;
		public float depth;
	}

	[HideInInspector]
	public string ServerIP = "127.0.0.1";
	[HideInInspector]
	public int ServicePort=50020;
	[HideInInspector]
	public int DefaultCalibrationCount=120;
	[HideInInspector]
	public int SamplesCount=4;
	[HideInInspector]
	public float CanvasWidth = 640;
	[HideInInspector]
	public float CanvasHeight=480;
	[HideInInspector]
	public int ServiceStartupDelay = 7000;//Time to allow the Service to start before connecting to Server.
	bool _serviceStarted = false;
	bool _calibPointTimeOut = true;

	//CUSTOM EDITOR VARIABLES

	[HideInInspector]
	public Vector2 Calibration2DScale;
	[HideInInspector]
	public bool saved = false;
	[HideInInspector]
	public int editedCalibIndex = 0;
	[HideInInspector]
	public bool CalibrationPointsFoldout;
	[HideInInspector]
	public bool CalibrationPoints2DFoldout;
	[HideInInspector]
	public bool CalibrationPoints3DFoldout;
	[HideInInspector]
	public int tab = 0;
	[HideInInspector]
	public int SettingsTab;
	private int _cMode;
	public int calibrationMode{
		get{ return _cMode; }
		set{
			_cMode = value;
			CalibrationGL.currentMode = CurrentCalibrationMode;
		}
	}
	[HideInInspector]
	public bool calibrationDebugMode = false;
	[HideInInspector]
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

	[HideInInspector]
	public GameObject CalibrationGameObject3D;
	[HideInInspector]
	public GameObject CalibrationGameObject2D;

	[HideInInspector]
	public bool isDebugFoldout;
	[HideInInspector]
	public bool ShowBaseInspector;
	[HideInInspector]
	public string PupilServicePath = "";
	[HideInInspector]
	public string PupilServiceFileName = "";

	public List<GUIStyle> Styles = new List<GUIStyle>();

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

	[HideInInspector]
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
//	public Vector2 EyePos2D
//	{
//		get{ return new Vector2((_eyeGazePos2D.x-0.5f)*CanvasWidth,(_eyeGazePos2D.y-0.5f)*CanvasHeight); }
//	}
//	public Vector2 EyePos3D
//	{
//		get{ return new Vector3 (_eyeGazePos3D.x, _eyeGazePos3D.y, _eyeGazePos3D.z); }
//	}
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




//	public Vector3 GetEyeGaze3D(GazeSource s){
//		return _eyeGazePos3D;
//	}

//	public double Confidence
//	{
//		get
//		{
//			if (_pupilData == null){return 0;}
//			return _pupilData.confidence;
//		}
//	}

	public PupilGazeTracker()
	{
		_Instance = this;
	}

	//private int toastIndex = 0;
	private Queue<Task> TaskQueue = new Queue<Task> ();
	private object _queueLock = new object ();

	public void ScheduleTask(Task newTask){
		lock (_queueLock) {
			if (TaskQueue.Count < 150)
				TaskQueue.Enqueue (newTask);
		}
	}

	#region Update
	void Update(){

		if (FramePublishingVariables.StreamCameraImages && Pupil.processStatus.eyeProcess0 && Pupil.processStatus.eyeProcess1) {
			elapsedTime = (float)TimeSpan.FromTicks (DateTime.Now.Ticks - lastTick).TotalSeconds;
			if (elapsedTime >= (1f / FramePublishingVariables.targetFPS)) {//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				lastTick = DateTime.Now.Ticks;
				//FramePublishingVariables.updateData = true;
				//FramePublishingVariables.drawImage0 = true;
				//if (FramePublishingVariables.drawImage0)
				AssignTexture (ref FramePublishingVariables.eye0Image, ref FramePublishingVariables.eye0ImageMaterial, ref FramePublishingVariables.drawImage0, FramePublishingVariables.raw0);

				//if (FramePublishingVariables.drawImage1)
				AssignTexture (ref FramePublishingVariables.eye1Image, ref FramePublishingVariables.eye1ImageMaterial, ref FramePublishingVariables.drawImage1, FramePublishingVariables.raw1);
				FramePublishingVariables.updateData0 = true;
				FramePublishingVariables.updateData1 = true;
			}
		}
		if (OnUpdate != null)
			OnUpdate ();


		lock (_queueLock) {
			if (TaskQueue.Count > 0)
				TaskQueue.Dequeue () ();
		}

		if (Input.GetKeyUp (KeyCode.X)) {
			Pupil.values.EyeCenters3D [0].x += 1000;
			Pupil.connectionParameters.toSubscribe.Add ("frame.");
			print ("x down : " + Pupil.connectionParameters.toSubscribe.Count);
			//StartProcess ();
			//subscriberSocket.Subscribe ("frame.");
		}

		if (Input.GetKeyUp (KeyCode.Z)) {
			ScheduleTask (new Task (delegate {
				subscriberSocket.Unsubscribe ("frame.");
				subscriberSocket.Unsubscribe ("frame");
				subscriberSocket.Unsubscribe ("frame.eye.0");
			}));
		}
		if (Input.GetKeyUp (KeyCode.A)) {
			Pupil.connectionParameters.toSubscribe.Add ("frame.");
			Pupil.connectionParameters.update = true;
//			Pupil.connectionParameters.toUnSubscribe.Add ("frame.");
			//StopFramePublishing();
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
	public DebugView._Transform[] OffsetTransforms;


	public enum CalibrationDebugCamera
	{
		HMD,
		PUPIL_CAMERA_0,
		PUPIL_CAMERA_1,
		PUPIL_CAMERA_BOTH
	}
	public CalibrationDebugCamera calibrationDebugCamera = CalibrationDebugCamera.PUPIL_CAMERA_0;
	[HideInInspector]
	public bool isDrawCalibrationDebugInitialized = false;
	#endregion
	public void InitDrawCalibrationDebug(){
		if (OffsetTransforms == null) {
			OffsetTransforms = new DebugView._Transform[]{new DebugView._Transform()};
		} else {
			foreach (DebugView._Transform _t in OffsetTransforms) {
				_t.GO = new GameObject (_t.name);
				_t.GO.transform.position = _t.position;
				_t.GO.transform.rotation = Quaternion.Euler (_t.rotation);
				_t.GO.transform.localScale = _t.localScale;
			}
		}
		isDrawCalibrationDebugInitialized = true;
	}
	public void DrawCalibrationDebug(){

		if (!isDrawCalibrationDebugInitialized)
			InitDrawCalibrationDebug ();

		CreateLineMaterial ();
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		CreateEyeSphereMaterial ();



		Vector3 eye0Pos = Pupil.values.EyeCenters3D [0] * DebugVariables.WorldScaling;
		Vector3 eye0Norm = Pupil.values.GazeNormals3D [0];

		Vector3 eye1Pos = Pupil.values.EyeCenters3D [1] * DebugVariables.WorldScaling;
		Vector3 eye1Norm = Pupil.values.GazeNormals3D [1];

		Vector3 gazePoint = Pupil.values.GazePoint3D * DebugVariables.WorldScaling;
		//print ("before drawing : " + eye0Pos+eye0Norm);

		switch (calibrationDebugCamera) {
		case CalibrationDebugCamera.HMD:
			if (CurrentCalibrationMode == CalibModes._3D){
			float _fov = fov*Mathf.Deg2Rad;
			var radianHFOV = 2 * Mathf.Atan (Mathf.Tan (_fov / 2) * Camera.main.aspect);
			var hFOV = Mathf.Rad2Deg * radianHFOV;

				DrawCameraFrustum (OffsetTransforms[0].GO.transform, hFOV, aspectRatios.FULLHD, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));

				DrawCameraFrustum (OffsetTransforms[0].GO.transform, hFOV, aspectRatios.ONEOONE, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));
				DrawCameraFrustum (OffsetTransforms[0].GO.transform, hFOV, aspectRatios.ONEOONE, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));

				DrawDebugSphere ( position: eye0Pos,eyeID: 0,forward: eye0Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//eye0
				DrawDebugSphere ( position: eye1Pos, eyeID:1,forward: eye1Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//eye1


				DrawDebugSphere ( position:gazePoint, eyeID:2, forward:eye1Norm, isEye: false, norm_length: viewDirectionLength, sphereColor: Color.red, norm_color: Color.red, size: DebugViewVariables.EyeSize/2, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//gaze point 3D
			
			}

			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_0:
			if (CurrentCalibrationMode == CalibModes._3D) {
				DrawCameraFrustum (origin: OffsetTransforms[0].GO.transform, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.1f, maxViewDistance: 10f, frustumColor: Color.blue, drawEye: true);
			}
			break;
//		case CalibrationDebugCamera.PUPIL_CAMERA_1:
////			if (CurrentCalibrationMode == CalibModes._3D) {
////				Pupil.Sphere _s;
////				if (_pupilData.base_data != null) {
////					print ("inside pupil camera 0 with .base data");
////					_s = _pupilData.base_data [1].sphere;
////					print (_s.center [0] + " , " + _s.center [1] + " , " + _s.center [2]);
////				} else {
////					_s = new Pupil.Sphere ();
////				}
////				Vector3 _v3Pos = new Vector3 ((float)_s.center [0] * DebugVariables.WorldScaling, (float)_s.center [1] * DebugVariables.WorldScaling, (float)_s.center [2] * DebugVariables.WorldScaling);
////				DrawDebugSphere (matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix, forward: Vector3.one, eyeID: 10, position: _v3Pos, size: ((float)_s.radius) * DebugVariables.WorldScaling, sphereColor: Color.green);
////				DrawCameraFrustum (origin: OffsetTransforms[0].GO.transform, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.1f, maxViewDistance: 10f, frustumColor: Color.blue, drawEye: true);
////			}
//			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_BOTH:
			
			double[] _d0 = Pupil.values.BaseData [0].circle_3d.center;
			DrawCircle (new Vector3 ((float)_d0 [0], (float)_d0 [1], (float)_d0 [2]), OffsetTransforms [1].GO.transform, (float)Pupil.values.BaseData [0].circle_3d.radius * DebugVariables.WorldScaling);

			double[] _s0 = Pupil.values.BaseData [0].sphere.center;
			DrawDebugSphere (matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, position: new Vector3 ((float)_s0 [0] * DebugVariables.WorldScaling, (float)_s0 [1] * DebugVariables.WorldScaling, (float)_s0 [2] * DebugVariables.WorldScaling), size: (float)Pupil.values.BaseData [0].sphere.radius * DebugVariables.WorldScaling, sphereColor: Color.grey, forward: Vector3.one);

			if (Pupil.values.BaseData.Length > 1) {
				double[] _d1 = Pupil.values.BaseData [1].circle_3d.center;
				DrawCircle (new Vector3 ((float)_d1 [0], (float)_d1 [1], (float)_d1 [2]), OffsetTransforms [1].GO.transform, (float)Pupil.values.BaseData [1].circle_3d.radius * DebugVariables.WorldScaling);

				double[] _s1 = Pupil.values.BaseData [1].sphere.center;
				DrawDebugSphere (matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, position: new Vector3 ((float)_s1 [0] * DebugVariables.WorldScaling, (float)_s1 [1] * DebugVariables.WorldScaling, (float)_s1 [2] * DebugVariables.WorldScaling), size: (float)Pupil.values.BaseData [1].sphere.radius * DebugVariables.WorldScaling, sphereColor: Color.grey, forward: Vector3.one);

			}

			DrawDebugSphere (position: eye0Pos, eyeID: 0, forward: eye0Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye0
			DrawDebugSphere (position: eye1Pos, eyeID: 1, forward: eye1Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye1


			DrawCameraFrustum (origin: OffsetTransforms [1].GO.transform, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.001f, maxViewDistance: 100f, frustumColor: Color.red, drawEye: true, eyeID: 0);

			break;
		}



		//print ("after drawing");

	}
	public void DrawCircle(Vector3 pos, Transform origin, float size){
		Calibration.marker _circle = new Calibration.marker ();
		_circle.position = pos * DebugVariables.WorldScaling;
		_circle.shape.width = size;
		GL.MultMatrix (origin.localToWorldMatrix);
		CalibrationGL.Marker (_circle);
	}
	public Texture2D circleTexture;
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
	public float fov = 60;
	public float MinViewDistance = 20;
	public float MaxViewDistance = 200;
	public float scale = 1;
	public float viewDirectionLength = 20;
	public float cameraGizmoLength = 20;
	public Mesh DebugEyeMesh;

	public enum aspectRatios{FULLVIVE,HALFVIVE,FULLHD,ONEOONE};

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
	public void DrawDebugSphere(Matrix4x4 matrix = default(Matrix4x4), Vector3 position = default(Vector3), int eyeID = 10, Matrix4x4 offsetMatrix = default(Matrix4x4),  Vector3 forward = default(Vector3), float norm_length = 20, bool isEye = false, Color norm_color = default(Color), Color sphereColor = default(Color), float size = 24.2f){

		eyeSphereMaterial.SetColor ("_Color", sphereColor);
		eyeSphereMaterial.SetPass (0);

		if (matrix == default(Matrix4x4))
			matrix = Camera.main.transform.localToWorldMatrix;

		Matrix4x4 _m = new Matrix4x4 ();

		//print ("from : " + forward + " to :  " + Quaternion.LookRotation (forward, Vector3.up));

		//TODO: rework this: now Forward vector needed for position assignment, not good!
		if (forward != Vector3.zero) {
			_m.SetTRS (position, Quaternion.LookRotation (forward, Vector3.up), new Vector3 (size, size, size));
		} else {
			_m.SetTRS (new Vector3(100*eyeID,0,0), Quaternion.identity, new Vector3 (size, size, size));
		}

		if (offsetMatrix != default(Matrix4x4))
			_m = offsetMatrix*_m;

		GL.wireframe = true;
		Graphics.DrawMeshNow(DebugEyeMesh, matrix*_m);
		GL.wireframe = false;


		eyeSphereMaterial.SetColor ("_Color", norm_color);
		eyeSphereMaterial.SetPass (0);
		if (isEye) {
			GL.MultMatrix (matrix * _m);
			GL.Begin (GL.LINES);
			GL.Vertex (Vector3.zero);
			GL.Vertex (Vector3.forward * norm_length);
			GL.End ();
		}
	}
	#endregion
	#region DebugView.CameraFrustum
	public void DrawCameraFrustum(Transform origin, float fieldOfView, aspectRatios aspect, float minViewDistance, float maxViewDistance, Color frustumColor = default(Color), Transform transformOffset = null, bool drawEye = false, int eyeID = 0){

		lineMaterial.SetColor ("_Color", frustumColor);
		lineMaterial.SetPass (0);

		Matrix4x4 offsetMatrix = new Matrix4x4 ();

		if (origin == null)
			origin = Camera.main.transform;

		if (transformOffset == null) {
			offsetMatrix.SetTRS (Vector3.zero, Quaternion.identity, Vector3.one);
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
		}
		Vector3 up = origin.up;
		Rect3D farPlaneRect = new Rect3D ();
		Rect3D nearPlaneRect = new Rect3D ();

		GL.MultMatrix (origin.localToWorldMatrix * offsetMatrix);

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
		GL.Vertex(Vector3.right*cameraGizmoLength);
		//Y
		GL.Color (Color.green);
		GL.Vertex(Vector3.zero);
		GL.Vertex(-Vector3.up*cameraGizmoLength);
		//Z
		GL.Color (Color.blue);
		GL.Vertex(Vector3.zero);
		GL.Vertex(Vector3.forward*cameraGizmoLength);
		//Draw Gizmo
		GL.End ();
		#endregion

		DrawCameraImages (farPlaneRect.verticies, farPlaneRect.width);

		if (drawEye) {
			Matrix4x4 _cameraSpaceMatrix = new Matrix4x4 ();
			_cameraSpaceMatrix.SetTRS (new Vector3 (widthMaxView / 2, heightMaxView / 2, maxViewDistance), Quaternion.identity, new Vector3 (widthMaxView, heightMaxView, 1));
			Vector2 _v2 = new Vector2 ((Mathf.InverseLerp (DebugVariables.value0, DebugVariables.value1, (float)Pupil.values.BaseData [eyeID].projected_sphere.center [0])) - 1f, ((Mathf.InverseLerp (DebugVariables.value2, DebugVariables.value3, (float)Pupil.values.BaseData [eyeID].projected_sphere.center [1])) - 1f));
			DrawDebugSphere (position: _v2, eyeID: 0, forward: Vector3.one, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: .1f, matrix: _cameraSpaceMatrix);
		}

		GL.PopMatrix ();
	}	
	#endregion
	#region DebugView.CameraFrustum.CameraImages
	void DrawCameraImages(Vector3[] drawPlane, float width){
		float[] _f = new float[]{ 0, 1, 1, 0, 0 };
		FramePublishingVariables.eye0ImageMaterial.SetPass (0);
		GL.Begin (GL.QUADS);
		for (int j = drawPlane.Count () - 1; j > -1; j--) {
			int ind = (drawPlane.Count () - 1) - j;
			GL.TexCoord2 (_f [ind], _f [ind + 1]);
			GL.Vertex3 (drawPlane [j].x, drawPlane [j].y, drawPlane [j].z);
		}
		GL.End ();
	}	
	#endregion

	public void ProcessPackets(){

		object messageType = _pupilDataDict.GetValueAsString ("messageType");
		if (DebugVariables.printMessageType)
			print (messageType);

		string base_data_json = _pupilDataDict.GetValueAsString ("base_data");
		if (!base_data_json.Contains ("null")) {
			Pupil.values.BaseData = JsonHelper.getJsonArray<Pupil.BaseData> (base_data_json);
			//print ("base_data request returned valid data");
		}


		//Pupil.values.EyeCenters3D = JsonHelper

		//Pupil.eyes3Ddata i = 

		switch((string)messageType){
		case "notify.calibration.started":
			ScheduleTask(new Task(delegate {
				ToastMessage.Instance.DrawToastMessage(new ToastMessage.toastParameters(){text = CurrentCalibrationMode.ToString().Substring(1) + " Calibration started"});
			}));
			break;
		case "notify.calibration.failed":
			ScheduleTask(new Task(delegate {
				ToastMessage.Instance.DrawToastMessage(new ToastMessage.toastParameters(){text = CurrentCalibrationMode.ToString().Substring(1) + " Calibration failed"});
			}));
			break;
		case "notify.calibration.success":
			ScheduleTask(new Task(delegate {
				ToastMessage.Instance.DrawToastMessage(new ToastMessage.toastParameters(){text = CurrentCalibrationMode.ToString().Substring(1) + " Calibration successful"});
			}));
			break;
		case "notify.eye_process.started":
			if (_pupilDataDict.GetValueAsInteger ("eye_id") == 0) {
				Pupil.processStatus.eyeProcess0 = true;
			}
			if (_pupilDataDict.GetValueAsInteger ("eye_id") == 1) {
				Pupil.processStatus.eyeProcess1 = true;
			}
			break;

		case "notify.eye_process.stopped":
			if (_pupilDataDict.GetValueAsInteger ("eye_id") == 0) {
				Pupil.processStatus.eyeProcess0 = false;
			}
			if (_pupilDataDict.GetValueAsInteger ("eye_id") == 1) {
				Pupil.processStatus.eyeProcess1 = false;
			}
			break;

		case "pupil.0":
			_gazeFPS++;
			var ct=DateTime.Now;
			if((ct-_lastT).TotalSeconds>1)
			{
				_lastT=ct;
				_currentFps=_gazeFPS;
				_gazeFPS=0;
			}
			break;
		case "gaze":
			if (_pupilDataDict.GetValueAsFloat ("confidence") > 0.35f)
				OnGazePacket ();
			break;
		case "frame.eye.0":
			if (FramePublishingVariables.updateData0) {
				object _eyeFrame0 = new object();
				_pupilDataDict.dictionary.TryGetValue ("extra_frame", out _eyeFrame0);
				FramePublishingVariables.raw0 = (byte[])_eyeFrame0;
				FramePublishingVariables.updateData0 = false;
			}
				break;
		case "frame.eye.1":
			if (FramePublishingVariables.updateData1) {
				object _eyeFrame1 = new object();
				_pupilDataDict.dictionary.TryGetValue ("extra_frame", out _eyeFrame1);
				FramePublishingVariables.raw1 = (byte[])_eyeFrame1;
				FramePublishingVariables.updateData1 = false;
			}
			break;
//		case "notify.frame_publishing.started":
//			ToastMessage.Instance.DrawToastMessageOnMainThread (new ToastMessage.toastParameters () {
//				ID = 0,
//				text = "frame publishing has started"
//			});
//			break;
//		case "notify.meta.doc":
//			var doc = MsgPack.Unpacking.UnpackObject (msg [1].ToByteArray ());
//			print (doc);
//			break;
		}


	}
	long lastTick;
	float elapsedTime;
	#region Start();
	void Start()
	{
		lastTick = DateTime.Now.Ticks;
		elapsedTime = 0f;
		//_pupilDataDict = new pupilDataDictionary ();
		//OnAllPackets += ProcessPackets;
		//OperatorMonitorProperties.OperatorCamera = null;
		ToastMessage.Instance.DrawToastMessage (new ToastMessage.toastParameters (){ text = "" });//Initialize toast messages;

		//CalibrationGL.GazeProcessingMode ();
		CalibrationGL.SetMode(EStatus.ProcessingGaze);

//		float[] _cPointFloatValues = new float[]{0f,0f,0f};

		if (FramePublishingVariables.StreamCameraImages)
			InitializeFramePublishing ();

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
		leftEye = new EyeData (SamplesCount);
		rightEye= new EyeData (SamplesCount);

		_dataLock = new object ();

		//make sure that if the toggles are on it functions as the toggle requires it
		if (isOperatorMonitor && OnOperatorMonitor == null) {
			OperatorMonitor.Instantiate ();
		}
			//OnOperatorMonitor += DrawOperatorMonitor;
		if (calibrationDebugMode && OnCalibDebug == null)
			OnCalibDebug += DrawCalibrationDebug;

		//Run the service locally, only if under settings its set to local
		if (connectionMode == 0)
			RunServiceAtPath ();

		_serviceThread = new Thread(NetMQClient);
		_serviceThread.Start();

	}
	#endregion

	#region DebugFunctions
	public void SubscribeTo(string topic){
		if (!Pupil.connectionParameters.toSubscribe.Contains (topic))
			Pupil.connectionParameters.toSubscribe.Add (topic);
		
		Pupil.connectionParameters.update = true;
	}
	public void UnSubscribeFrom(string topic){
		if (Pupil.connectionParameters.toSubscribe.Contains (topic))
			Pupil.connectionParameters.toSubscribe.Remove (topic);

		Pupil.connectionParameters.update = true;
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
		//if (Pupil.processStatus.eyeProcess0 || Pupil.processStatus.eyeProcess1) {//only allow to send/receive messages when at least one camera is connected and functioning
			NetMQMessage m = new NetMQMessage ();
			m.Append ("notify." + data ["subject"]);

			using (var byteStream = new MemoryStream ()) {
				var ctx = new SerializationContext ();
				ctx.CompatibilityOptions.PackerCompatibilityOptions = MsgPack.PackerCompatibilityOptions.None;
				var ser = MessagePackSerializer.Get<object> (ctx);
				ser.Pack (byteStream, data);
				m.Append (byteStream.ToArray ());
			}

			_requestSocket.SendMultipartMessage (m);

			NetMQMessage recievedMsg;
			recievedMsg = _requestSocket.ReceiveMultipartMessage ();

			return recievedMsg;
		//} else {
		//} return new NetMQMessage ();
	}

	float GetPupilTimestamp()
	{
		_requestSocket.SendFrame ("t");
		NetMQMessage recievedMsg=_requestSocket.ReceiveMultipartMessage ();
		return float.Parse(recievedMsg[0].ConvertToString());
	}



	#region frame_publishing.functions
	public void InitializeFramePublishing(){
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		FramePublishingVariables.eye0Image = new Texture2D (100,100);
		FramePublishingVariables.eye1Image = new Texture2D (100,100);
	}
	public void StartFramePublishing(){
		FramePublishingVariables.StreamCameraImages = true;
		_sendRequestMessage (new Dictionary<string,object> {{"subject","start_plugin"},{"name", "Frame_Publisher"}});
		SubscribeTo ("frame.");
		print ("frame publish start");
		//_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}
	public void StopFramePublishing(){
		if (!calibrationDebugMode && !isOperatorMonitor) {
			UnSubscribeFrom ("frame.");
			FramePublishingVariables.StreamCameraImages = false;
			_sendRequestMessage (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });
		}
	}
	public void AssignTexture(ref Texture2D _eyeImage, ref Material _mat, ref bool drawEye, object data){
		byte[] _bArray;
		_bArray = (byte[])data;
		_eyeImage.LoadImage (_bArray);
		_mat.mainTexture = _eyeImage;
		drawEye = false;
	}
	#endregion

	#region NetMQ
	public SubscriberSocket subscriberSocket;
	void NetMQClient()
	{



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


			if (DebugVariables.subscribeAll) {
				subscriberSocket.SubscribeToAnyTopic ();
			}

			if (DebugVariables.subscribeFrame){
				Pupil.connectionParameters.toSubscribe.Add ("frame.");
				//Pupil.connectionParameters.update = true;
				subscriberSocket.Subscribe ("frame."); //subscribe for frame data
			
			}
			if (DebugVariables.subscribeGaze)
				subscriberSocket.Subscribe ("gaze"); //subscribe for gaze data

			if (DebugVariables.subscribeNotify)
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
						
						//ScheduleTask(new Task(delegate {
							
						if (Pupil.connectionParameters.update == true){
							_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name","Frame_Publisher"}});

							//_sendRequestMessage ( new Dictionary<string,object> {{"subject","notify.eye_process.started"},{"eye_id",0}});
							print("stuff");
							subscriberSocket.Close();
							subscriberSocket = new SubscriberSocket(IPHeader + subport);
							Thread.Sleep(100);
//							foreach(string _s in Pupil.connectionParameters.toSubscribe){
//								Thread.Sleep(1000);
//								subscriberSocket.Subscribe(_s);
//							}
							Pupil.connectionParameters.toSubscribe.ForEach(p=>subscriberSocket.Subscribe(p));
//							subscriberSocket.Subscribe("gaze");
//							Thread.Sleep(1000);
//							subscriberSocket.Subscribe("frame.");
//							Thread.Sleep(1000);
							Pupil.connectionParameters.update = false;
						}
//							print("before the to subscribe stuff");
//
//							if (Pupil.connectionParameters.toSubscribe.Count > 0) {
//								print("withing the to subscribe stuff");
//								subscriberSocket.Subscribe("frame.");
//								Pupil.connectionParameters.toSubscribe.RemoveAt(0);
//							}
//							if (Pupil.connectionParameters.toUnSubscribe.Count > 0) {
//								print("withing the to unsubscribe stuff");
//								subscriberSocket.Unsubscribe(Pupil.connectionParameters.toUnSubscribe[0]);
//								Pupil.connectionParameters.toUnSubscribe.RemoveAt(0);
//							}
//							print("after the to subscribe stuff");
						//}));

						string msgType=msg[0].ConvertToString();

						if (DebugVariables.printMessage){
							var m = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
							MsgPack.MessagePackObject map = m.Value;
							print(m);
						}



						#region NetMQ.message_handling

						Dictionary<MsgPack.MessagePackObject, MsgPack.MessagePackObject> mpoDict = MessagePackSerializer.Create<Dictionary<MsgPack.MessagePackObject, MsgPack.MessagePackObject>>().Unpack(new MemoryStream(msg[1].ToByteArray()));
						Dictionary<String, object> dict = mpoDict.ToDictionary(kv => (String)kv.Key, kv => (object)kv.Value);
						dict.Add("messageType", msgType);

						if (msg.FrameCount>2){
							dict.Add("extra_frame", msg[2].Buffer);
						}

						_pupilDataDict.dictionary = dict;
						if (DebugVariables.packetsOnMainThread){
							ScheduleTask(new Task(delegate {
								ProcessPackets();
							}));
						}else{
							ProcessPackets();
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
		CalibrationGL.SetMode (st);
		m_status = st;
	}
	public void To3DMethod(){
		_sendRequestMessage (new Dictionary<string,object> { { "subject","set_detection_mapping_mode" }, { "mode","3d" } });
	}
	public void To2DMethod(){
		_sendRequestMessage (new Dictionary<string,object> { { "subject","set_detection_mapping_mode" }, { "mode","2d" } });
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

	public void StartCalibration(){

		CalibrationGL.CalibrationMode ();

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
		public Vector3 convertedV3{get{ return new Vector3 (-(float)value [0], -(float)value [1], (float)value [2]); }}
	}

	void OnGazePacket(){
		//print ("base data count : " + _pupilDataDict.GetValueAsString ("base_data"));
		//Pupil.BaseDataArray baseData = JsonUtility.FromJson<Pupil.BaseDataArray> ("{ \"base_data_array\" :" + _pupilDataDict.GetValueAsString ("base_data") + "}");
		//print (baseData.base_data_array[0].id);
		//print (_pupilDataDict.GetValue ("eye_centers_3d"));

		if (m_status == EStatus.ProcessingGaze) {
			float x, y;
			if (CurrentCalibrationMode == CalibModes._2D) {
				Vector2 pos_v2 = _pupilDataDict.GetValueAsVector ("norm_pos").v2;
				x = pos_v2.x;
				y = pos_v2.y;
				_eyeGazePos2D.x = (leftEye.gaze2D.x + rightEye.gaze2D.x) * 0.5f;
				_eyeGazePos2D.y = (leftEye.gaze2D.y + rightEye.gaze2D.y) * 0.5f;
				if (Pupil.values.BaseData[0].id == 0) {
					leftEye.AddGaze (x, y);
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				} else if (Pupil.values.BaseData[0].id == 1) {
					rightEye.AddGaze (x, y);
					if (OnEyeGaze != null)
						OnEyeGaze (this);
				}

				//print ("gazepacket" + x + " , " + y + " , " + leftEye.gaze2D.ToString("F5"));
				print ("gazepacket");

				Calibration.marker _0 = CalibrationMarkers.Where (p => p.name == "leftEye").ToList () [0];
				_0.shape.x = LeftEyePos.x;
				_0.shape.y = LeftEyePos.x;
				//_0.toggle = true;

				Calibration.marker _1 = CalibrationMarkers.Where (p => p.name == "rightEye").ToList () [0];
				_1.shape.x = GetEyeGaze2D (GazeSource.RightEye).x;
				_1.shape.y = GetEyeGaze2D (GazeSource.RightEye).y;
				//_1.toggle = true;

				Calibration.marker _2 = CalibrationMarkers.Where (p => p.name == "gaze").ToList () [0];
				_2.shape.x = GetEyeGaze2D (GazeSource.BothEyes).x;
				_2.shape.y = GetEyeGaze2D (GazeSource.BothEyes).y;
			}
			if (CurrentCalibrationMode == CalibModes._3D) {

				Pupil.values.EyeCenters3D = _pupilDataDict.GetValueAsVectorArray ("eye_centers_3d", -1f);
				Pupil.values.GazeNormals3D = _pupilDataDict.GetValueAsVectorArray ("gaze_normals_3d", -1f);
				Pupil.values.GazePoint3D = _pupilDataDict.GetValueAsVector ("gaze_point_3d").convertedV3;

//				print ("eye centers : " + Pupil.values.EyeCenters3D [0].ToString ("F5") + " , " + Pupil.values.EyeCenters3D [1].ToString ("F5"));
//				print ("gaze normals : " + Pupil.values.GazeNormals3D [0].ToString ("F5") + " , " + Pupil.values.GazeNormals3D [1].ToString ("F5"));
//				print ("gaze point : " + Pupil.values.GazePoint3D.ToString ("F5"));
				//Vector3 pos_v3 = _pupilDataDict.GetValueAsVector ("mm_pos").v3;
				//var a = JsonUtility.FromJson<_double> ("{\"value\": " + _pupilDataDict.GetValueAsString ("gaze_point_3d") + "}");

				Calibration.marker gaze3D = CalibrationMarkers.Where (p => p.calibMode == CalibModes._3D && !p.calibrationPoint).ToList () [0];

				//gaze3D.shape.x = pos_v3.x;
				//gaze3D.shape.y = pos_v3.y;
				//gaze3D.shape.height = pos_v3.z;
			}
		} else if (m_status == EStatus.Calibration) {
			CalibModeDetails _cCalibDetails = CalibrationModes[CurrentCalibrationMode];
			float t = GetPupilTimestamp ();
			floatArray[] _cPoints = GetCalibPoints;
			float[] _currentCalibPointPosition = _cPoints [_currCalibPoint].axisValues;

//			if (CurrentCalibrationMode == CalibModes._3D) {
//				_currentCalibPointPosition = new float[] {
//					-_cPoints [_currCalibPoint].axisValues [0],
//					-_cPoints [_currCalibPoint].axisValues [1],
//					_cPoints [_currCalibPoint].axisValues [2]
//				};
//			} else {
//				_currentCalibPointPosition = new float[] {
//					_cPoints [_currCalibPoint].axisValues [0],
//					_cPoints [_currCalibPoint].axisValues [1],
//					_cPoints [_currCalibPoint].axisValues [2]
//				};
//			}

			Calibration.marker _m = CalibrationMarkers.Where (p => p.calibrationPoint && p.calibMode == CurrentCalibrationMode).ToList()[0];
			_m.shape.x = _currentCalibPointPosition[0];
			_m.shape.y = _currentCalibPointPosition[1];
			_m.shape.height = _cCalibDetails.depth;//using the height az depth offset
			_m.toggle = true;

			// Giving the user a short time to focus on the Calibration Point target before starting adding the reference data
			if (_calibPointTimeOut) {
				Thread.Sleep (1000);
				_calibPointTimeOut = false;
			}

			print ("this goes in : X : " + _currentCalibPointPosition [0] + " Y: " + _currentCalibPointPosition [1] + " Z: " + _currentCalibPointPosition [2]);
			//Create reference data to pass on. _cPointFloatValues are storing the float values for the relevant current Calibration mode
			var ref0=new Dictionary<string,object>(){{_cCalibDetails.positionKey,_currentCalibPointPosition},{"timestamp",t},{"id",0}};
			var ref1=new Dictionary<string,object>(){{_cCalibDetails.positionKey,_currentCalibPointPosition},{"timestamp",t},{"id",1}};

			//Adding the calibration reference data to the list that wil;l be passed on, once the required sample amount is met.
			_calibrationData.Add (ref0);
			_calibrationData.Add (ref1);

			//Increment the current calibration sample. (Default sample amount per calibration point is 120)
			_currCalibSamples++;

			//Debugging
			if (DebugVariables.printSampling) {
				print ("Sampling at : " + _currCalibSamples);
			}
			ScheduleTask(new Task(delegate {
				ToastMessage.Instance.DrawToastMessage(new ToastMessage.toastParameters(){ID = 1, text = "Point : " + (_currCalibPoint+1) + "/" + _cPoints.Count() + "  " + ((Mathf.InverseLerp(0f, DefaultCalibrationCount, _currCalibSamples))*100).ToString("F0") + "%" });
			}));

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

				//Send the current relevant calibration data for the current calibration point. _CalibrationPoints returns _calibrationData as an array of a Dictionary<string,object>.
				_sendRequestMessage (new Dictionary<string,object> {{"subject","calibration.add_ref_data"},{"ref_data",_CalibrationPoints}});
				//Clear the current calibration data, so we can proceed to the next point if there is any.
				_calibrationData.Clear ();

				//Stop calibration if we accomplished all required calibration targets.
				if (_currCalibPoint >= _cPoints.Length) {
					StopCalibration ();
				}
			}

		}
	}

	#endregion
	void OnGUI()
	{
		if (!isOperatorMonitor) {
			string str = "Capture Rate=" + FPS;
			str += "\nLeft Eye:" + LeftEyePos.ToString ();
			str += "\nRight Eye:" + RightEyePos.ToString ();
			GUI.TextArea (new Rect (0, 0, 200, 50), str);
		}

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

		CalibrationGL.SetMode (m_status);
	
	}

	#region Recording
	public void OnRecording(){
	}
	public void StartRecording(){
		Recording.variables.CaptureScript = Camera.main.gameObject.AddComponent<FFmpegOut.CameraCapture> ();
		OnUpdate += OnRecording;
	}
	public void StopRecording(){
		Recording.variables.isRecording = false;
		Destroy (Camera.main.gameObject.GetComponent<FFmpegOut.CameraCapture> ());
		Recording.variables.CaptureScript = null;
	}
	#endregion

	void OnApplicationQuit(){
		Pupil.processStatus.eyeProcess0 = false;
		Pupil.processStatus.eyeProcess1 = false;
		if (serviceProcess != null) {
			serviceProcess.Close ();
			serviceProcess.WaitForExit ();
		}
	}
}
