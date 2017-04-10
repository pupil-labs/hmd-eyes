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
	[Serializable]
	public class variables : MonoBehaviour{
		public static bool fixLength;
		public static float length;
		public static bool isRecording;
		public static int FPS;
		public static string FilePath;
		public static string pathDirectory;
		public static string pathFileName = "MyRecording";
		public static string pathFileExtension = "mov";
		public static int width = 1280;
		public static int height = 720;
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
		public Circle3d circle_3d = new Circle3d();
		public string topic = "";
		public double diameter = new double();
		public double confidence = new double();
		public string method = "";
		public double model_birth_timestamp = new double();
		public double theta = new double();
		public double[] norm_pos = new double[]{ 0, 0, 0 };
		public Ellipse ellipse = new Ellipse();
		public double model_confidence = new double();
		public int id = 0;
		public double timestamp = new double();
		public Sphere sphere = new Sphere();
		public ProjectedSphere projected_sphere = new ProjectedSphere();
		public double diameter_3d = new double();
		public int model_id = 0;
		public double phi = new double();
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
#region Calibration
namespace Calibration{
	[Serializable]
	public class marker{
		public string name;
		public Rect shape;
		public Color color;
		public bool toggle;
		public bool calibrationPoint;
		public float depth;
		public bool debugCross;
		public float baseSize;
		public Material material;
		public PupilGazeTracker.CalibModes calibMode;
		public Matrix4x4 offsetMatrix = new Matrix4x4 ();
		public Vector3 position{
			get { 
				//TODO : rework the shape&position settings.
				Vector3 _v3 = new Vector3 (shape.x, shape.y, depth);
				return _v3;
			}
			set{
				shape.x = value.x;
				shape.y = value.y;
				depth = value.y;
			}
		}
	}
}
namespace Calibration{
//	[Serializable]
//	public class cameraDetails{
//
//		public double[,] eye_camera_to_world_matrix1 = new double[4, 4];
//		public double[] cal_gaze_points1_3d = new double[]{ };
//		public int kaka;
//		public int a;
//	}

	[Serializable]
	public class data{
//		public cameraDetails args;
//		public int a;
//		public int kaka;
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
//		public v2 bub;
	}
//	[Serializable]
//	public class variables{
//
////		public string eye_camera_to_world_matrix0;
////		public Matrix4x4 eye_camera_to_world_matrix1;
//	}
//	[Serializable]
//	public struct v2{
//		public Vector3 a;
//	}
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
		List<floatArray> ret = new List<floatArray> ();
		foreach (floatArray _fa in list2D) {
			ret.Add (new floatArray (){ axisValues = new float[]{ _fa.axisValues [0], _fa.axisValues [1] } });
		}
		return ret;
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
			return float.Parse( GetValueAsString (key));
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
	public Calibration.data CalibrationData;
	//public Pupil.
//	public Calibration.da CalibraionVariables;

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
				depth = 0.1f
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
			//CalibrationGL.currentMode = CurrentCalibrationMode;
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
		if (Input.GetKeyUp (KeyCode.L)) {
			LineDrawer.Instance.AddLineToMesh (new Vector3[] {
				new Vector3 (UnityEngine.Random.Range (-100, 100), UnityEngine.Random.Range (-100, 100), UnityEngine.Random.Range (-100, 100)),
				new Vector3 (UnityEngine.Random.Range (-100, 100), UnityEngine.Random.Range (-100, 100), UnityEngine.Random.Range (-100, 100))
			}, new Color (UnityEngine.Random.Range (0f, 1f), UnityEngine.Random.Range (0f, 1f), UnityEngine.Random.Range (0f, 1f),UnityEngine.Random.Range (0f, .1f)));
		}
		if (FramePublishingVariables.StreamCameraImages) {//Put this in a function and delegate it to the OnUpdate delegate
			elapsedTime = (float)TimeSpan.FromTicks (DateTime.Now.Ticks - lastTick).TotalSeconds;
			if (elapsedTime >= (1f / FramePublishingVariables.targetFPS)) {//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				AssignTexture (ref FramePublishingVariables.eye0Image, ref FramePublishingVariables.eye0ImageMaterial,  FramePublishingVariables.raw0);
				AssignTexture (ref FramePublishingVariables.eye1Image, ref FramePublishingVariables.eye1ImageMaterial,  FramePublishingVariables.raw1);
//				if (FramePublishingVariables.updateData0) {
//					AssignTexture (ref FramePublishingVariables.eye0Image, ref FramePublishingVariables.eye0ImageMaterial,  FramePublishingVariables.raw0);
//					FramePublishingVariables.updateData0 = false;
//				}
//				if (FramePublishingVariables.updateData1) {
//					AssignTexture (ref FramePublishingVariables.eye1Image, ref FramePublishingVariables.eye1ImageMaterial,  FramePublishingVariables.raw1);
//					FramePublishingVariables.updateData1 = false;
//				}

				print("wihin frame per sec limitation");
				lastTick = DateTime.Now.Ticks;
			}
		}

		if (OnUpdate != null)
			OnUpdate ();


		lock (_queueLock) {
			if (TaskQueue.Count > 0)
				TaskQueue.Dequeue () ();
		}

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


	public void WriteStringToFile(string dataString, string fileName = "defaultFilename"){
		var bytes = System.Text.Encoding.UTF8.GetBytes (dataString);
		File.WriteAllBytes (Application.dataPath + "/" + fileName, bytes);
	}
	public string ReadStringFromFile(string fileName = "defaultFilename"){
		string _str = File.ReadAllText (Application.dataPath + "/" + fileName);
		return _str;
	}

	//private Mesh mes;
	int debugViewFrameIndex = 0;
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
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;

			var pointCloudDrawerGO = new GameObject ("PointCloudDrawer");
			pointCloudDrawerGO.transform.parent = a.GO.transform;
			pointCloudDrawerGO.transform.localPosition = Vector3.zero;
			pointCloudDrawerGO.transform.localRotation = Quaternion.identity;
			pointCloudDrawerGO.AddComponent<PointCloudDrawer> ();

			var lineDrawerGO = new GameObject ("LineDrawer");
			lineDrawerGO.transform.parent = a.GO.transform;
			lineDrawerGO.transform.localPosition = Vector3.zero;
			lineDrawerGO.transform.localRotation = Quaternion.identity;
			lineDrawerGO.AddComponent<LineDrawer> ();
		}
		OnUpdate += CalibrationDebugInteraction;
		isDrawCalibrationDebugInitialized = true;
	}
	public void CalibrationDebugInteraction(){
		#region DebugView.Interactions
		if (Input.anyKey){
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;
			if (Input.GetKey(KeyCode.Alpha1)){
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

			if (Input.GetMouseButton(0))
				print("mouse is any key too");
		}
		if (Input.GetMouseButton(1)){
			var a = (from tr in OffsetTransforms where tr.name == "Debug View Origin Matrix" select tr).FirstOrDefault() as DebugView._Transform;
			a.GO.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"),Input.GetAxis("Mouse X"), 0));
			 
		}
		#endregion
	}
	public void DrawCalibrationDebugView(){

		debugViewFrameIndex++;

		if (!isDrawCalibrationDebugInitialized)
			InitDrawCalibrationDebug ();

		CreateLineMaterial ();
		CreateEye0ImageMaterial ();
		CreateEye1ImageMaterial ();
		CreateEyeSphereMaterial ();



		Vector3 eye0Pos = Pupil.values.EyeCenters3D [0];
		Vector3 eye0Norm = Pupil.values.GazeNormals3D [0];

		Vector3 eye1Pos = Pupil.values.EyeCenters3D [1];
		Vector3 eye1Norm = Pupil.values.GazeNormals3D [1];

		Vector3 gazePoint = Pupil.values.GazePoint3D;
		//print ("before drawing : " + eye0Pos+eye0Norm);

		switch (calibrationDebugCamera) {
		case CalibrationDebugCamera.HMD:
			if (CurrentCalibrationMode == CalibModes._3D){
//			float _fov = fov*Mathf.Deg2Rad;
//			var radianHFOV = 2 * Mathf.Atan (Mathf.Tan (_fov / 2) * Camera.main.aspect);
//			var hFOV = Mathf.Rad2Deg * radianHFOV;

//				DrawCameraFrustum (OffsetTransforms[0].GO.transform.localToWorldMatrix, hFOV, aspectRatios.FULLHD, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));
//
//				DrawCameraFrustum (OffsetTransforms[0].GO.transform.localToWorldMatrix, hFOV, aspectRatios.ONEOONE, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));
//				DrawCameraFrustum (OffsetTransforms[0].GO.transform.localToWorldMatrix, hFOV, aspectRatios.ONEOONE, MinViewDistance, MaxViewDistance, new Color(0f,0.64f,0f));

				DrawDebugSphere ( position: eye0Pos,eyeID: 0,forward: eye0Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//eye0
				DrawDebugSphere ( position: eye1Pos, eyeID:1,forward: eye1Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//eye1


				DrawDebugSphere ( position:gazePoint, eyeID:2, forward:eye1Norm, isEye: false, norm_length: viewDirectionLength, sphereColor: Color.red, norm_color: Color.red, size: DebugViewVariables.EyeSize/2, matrix: OffsetTransforms[0].GO.transform.localToWorldMatrix);//gaze point 3D
			
			}

			break;
		case CalibrationDebugCamera.PUPIL_CAMERA_0:
			if (CurrentCalibrationMode == CalibModes._3D) {
				DrawCameraFrustum (origin: OffsetTransforms[0].GO.transform.localToWorldMatrix, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.1f, maxViewDistance: 10f, frustumColor: Color.blue, drawEye: true);
			}
			break;

		case CalibrationDebugCamera.PUPIL_CAMERA_BOTH:

//			print (Pupil.values.BaseData.Length);
//			if (Pupil.values.BaseData.Length > 1) {
//				
//				double[] _d0 = Pupil.values.BaseData [0].circle_3d.center;
//				DrawCircle (new Vector3 ((float)_d0 [0], (float)_d0 [1], (float)_d0 [2]), OffsetTransforms [1].GO.transform, (float)Pupil.values.BaseData [0].circle_3d.radius );
//
//				double[] _s0 = Pupil.values.BaseData [0].sphere.center;
//				DrawDebugSphere (matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, position: new Vector3 ((float)_s0 [0], (float)_s0 [1] , (float)_s0 [2]), size: (float)Pupil.values.BaseData [0].sphere.radius , sphereColor: Color.grey, forward: Vector3.one);
//
//
//				double[] _d1 = Pupil.values.BaseData [1].circle_3d.center;
//				DrawCircle (new Vector3 ((float)_d1 [0], (float)_d1 [1], (float)_d1 [2]), OffsetTransforms [1].GO.transform, (float)Pupil.values.BaseData [1].circle_3d.radius );
//
//				double[] _s1 = Pupil.values.BaseData [1].sphere.center;
//				DrawDebugSphere (matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix, position: new Vector3 ((float)_s1 [0] , (float)_s1 [1] , (float)_s1 [2] ), size: (float)Pupil.values.BaseData [1].sphere.radius , sphereColor: Color.grey, forward: Vector3.one);
//
//			}

			DrawDebugSphere (position: eye0Pos, eyeID: 0, forward: eye0Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye0
			DrawDebugSphere (position: eye1Pos, eyeID: 1, forward: eye1Norm, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: DebugViewVariables.EyeSize, matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye1


			DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix0, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.001f, maxViewDistance: 60, frustumColor: Color.green, drawEye: true, eyeID: 0, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true);
			DrawCameraFrustum (origin: CalibrationData.eye_camera_to_world_matrix1, fieldOfView: 60, aspect: aspectRatios.ONEOONE, minViewDistance: 0.001f, maxViewDistance: 60f, frustumColor: Color.green, drawEye: true, eyeID: 1, transformOffset: OffsetTransforms [1].GO.transform, drawCameraImage: true);

			float _fov = fov*Mathf.Deg2Rad;
			var radianHFOV = 2 * Mathf.Atan (Mathf.Tan (_fov / 2) * Camera.main.aspect);
			var hFOV = Mathf.Rad2Deg * radianHFOV;
			DrawCameraFrustum (origin: OffsetTransforms [1].GO.transform.localToWorldMatrix, fieldOfView: hFOV, aspect: aspectRatios.FULLVIVE, minViewDistance: 0.001f, maxViewDistance: 100f, frustumColor: Color.gray, drawEye: false, eyeID: 0);

			if (debugViewFrameIndex % 30 == 0) {
				LineDrawer.Instance.AddLineToMesh (new Vector3[]{ eye0Pos, gazePoint }, new Color (1, 1, 1, .2f));//(eye1Pos + (eye1Norm * viewDirectionLength * 100))
				LineDrawer.Instance.AddLineToMesh (new Vector3[]{ eye1Pos, gazePoint }, new Color (1, 1, 1, .2f));
			}

			DrawDebugSphere (position: gazePoint, eyeID: 10, forward: eye1Norm, isEye: false, norm_length: viewDirectionLength, sphereColor: Color.red, norm_color: Color.clear, size: 10, matrix: OffsetTransforms [1].GO.transform.localToWorldMatrix);//eye1

			break;
		}

	}
	public void DrawCircle(Vector3 pos, Transform origin, float size){
		Calibration.marker _circle = new Calibration.marker ();
		_circle.position = pos;
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
	public void DrawCameraFrustum(Matrix4x4 origin, float fieldOfView, aspectRatios aspect, float minViewDistance, float maxViewDistance, Color frustumColor = default(Color), Transform transformOffset = null, bool drawEye = false, int eyeID = 0, bool drawCameraImage = false){

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

		if (drawCameraImage)
			DrawCameraImages (farPlaneRect.verticies, farPlaneRect.width);

		if (drawEye) {
//			Matrix4x4 _cameraSpaceMatrix = new Matrix4x4 ();
//			_cameraSpaceMatrix.SetTRS (new Vector3 (widthMaxView / 2, heightMaxView / 2, maxViewDistance), Quaternion.identity, new Vector3 (widthMaxView, heightMaxView, 1));
//			Vector2 _v2 = new Vector2 ((Mathf.InverseLerp (DebugVariables.value0, DebugVariables.value1, (float)Pupil.values.BaseData [eyeID].projected_sphere.center [0])) - 1f, ((Mathf.InverseLerp (DebugVariables.value2, DebugVariables.value3, (float)Pupil.values.BaseData [eyeID].projected_sphere.center [1])) - 1f));
//			DrawDebugSphere (position: _v2, eyeID: 0, forward: Vector3.one, isEye: true, norm_length: viewDirectionLength, sphereColor: Color.cyan, norm_color: Color.red, size: .1f, matrix: _cameraSpaceMatrix);
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
			if (_pupilDataDict.GetValueAsFloat ("confidence") > 0.6f)
				OnGazePacket ();
			break;
		case "frame.eye.0":
			//if (FramePublishingVariables.updateData0) {
			print("frame eye 0");
				object _eyeFrame0 = new object();
				_pupilDataDict.dictionary.TryGetValue ("extra_frame", out _eyeFrame0);
				FramePublishingVariables.raw0 = (byte[])_eyeFrame0;
				//AssignTexture (ref FramePublishingVariables.eye0Image, ref FramePublishingVariables.eye0ImageMaterial, (byte[])_eyeFrame0);
			//	FramePublishingVariables.updateData0 = true;
			//}
				break;
		case "frame.eye.1":
			print("frame eye 1");
			object _eyeFrame1 = new object();
			_pupilDataDict.dictionary.TryGetValue ("extra_frame", out _eyeFrame1);
			FramePublishingVariables.raw1 = (byte[])_eyeFrame1;
			//AssignTexture (ref FramePublishingVariables.eye1Image, ref FramePublishingVariables.eye1ImageMaterial, (byte[])_eyeFrame1);
			//FramePublishingVariables.updateData0 = true;
//			if (FramePublishingVariables.updateData1) {
//				object _eyeFrame1 = new object();
//				_pupilDataDict.dictionary.TryGetValue ("extra_frame", out _eyeFrame1);
//				FramePublishingVariables.raw1 = (byte[])_eyeFrame1;
//				FramePublishingVariables.updateData1 = true;
//			}
			break;
		case "notify.start_plugin":
			string pluginName = _pupilDataDict.GetValueAsString ("name");
			if (pluginName != null) {
				print ("Plugin " + pluginName + " has started ! ");
				if (pluginName == "Binocular_Vector_Gaze_Mapper") {
					print("it is binocular vector gaze mapper");

					string _strData = _pupilDataDict.GetValueAsString ("args");
					if (_strData != null) {
//						//_pupilDataDict
						print("camera intrinsics data found ! length : " + _strData.Length);
//
						ScheduleTask (new Task (delegate {
							CalibrationData.camera_intrinsics_str = _strData;
							//JsonUtility.FromJson<Calibration.variables> (_strData);
							print ("camera intrinsics data found ! length : " + _strData.Length);
							WriteStringToFile (_strData, "camera_intrinsics");
						}));
//
					}

				}
			}
			break;

//		case "notify.frame_publishing.started":
//			ToastMessage.Instance.DrawToastMessageOnMainThread (new ToastMessage.toastParameters () {
//				ID = 0,
//				text = "frame publishing has started"
//			});
//			break;
		case "notify.meta.doc":
			//var doc = MsgPack.Unpacking.UnpackObject (msg [1].ToByteArray ());
			//print (doc);
			print("doc");
			break;
		}

		if (isOperatorMonitor) {
			Pupil.values.Confidences [0] = (float)Pupil.values.BaseData [0].confidence;
			Pupil.values.Confidences [1] = (float)Pupil.values.BaseData [1].confidence;
		}

	}

	public Vector3 Vector3FromString(string v3String){
		Vector3 _v3 = new Vector3 ();
		string[] _v3Str = v3String.Substring (1, v3String.Length - 2).Split (",".ToCharArray (), 3);
		_v3.x = float.Parse (_v3Str [0]);
		_v3.y = float.Parse (_v3Str [1]);
		_v3.z = float.Parse (_v3Str [2]);
		return _v3;
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
				//print ("string is empty");
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
//		string newStr = "";
//		if (json.IndexOf ("{") < 3)
//			newStr = json.Remove (0, json.IndexOf ("\""));

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
		CalibrationData.eye_camera_to_world_matrix0 = Matrix4x4FromString (o.ToString (), false);
		camera_matricies_dict.TryGetValue ("eye_camera_to_world_matrix1", out o);
		CalibrationData.eye_camera_to_world_matrix1 = Matrix4x4FromString (o.ToString (), false);
	}
	long lastTick;
	float elapsedTime;

	#region Start();
	void Start()
	{
		
		string str = ReadStringFromFile ("camera_intrinsics");
		ReadCalibrationData (str);

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
			OnCalibDebug += DrawCalibrationDebugView;

		//Run the service locally, only if under settings its set to local
		if (connectionMode == 0)
			RunServiceAtPath ();

		_serviceThread = new Thread(NetMQClient);
		_serviceThread.Start();

	}
	#endregion

	#region DebugFunctions
	public void SubscribeTo(string topic){
		if (!Pupil.connectionParameters.toSubscribe.Contains (topic)) {
			Pupil.connectionParameters.toSubscribe.Add (topic);
			print ("adding : " + topic + " to Subscribe ! ");
		}
		
		Pupil.connectionParameters.update = true;
	}
	public void UnSubscribeFrom(string topic){
		if (Pupil.connectionParameters.toSubscribe.Contains (topic)) {
			Pupil.connectionParameters.toSubscribe.Remove (topic);
			print ("removing : " + topic + " from Subscribe ! ");
		}
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
		_sendRequestMessage (new Dictionary<string,object> {{"subject","plugin_started"},{"name", "Frame_Publisher"}});
		SubscribeTo ("frame.");
		print ("frame publish start");
		//_sendRequestMessage (new Dictionary<string,object> { { "subject","frame_publishing.started" } });
	}
	public void StopFramePublishing(){
		if (!calibrationDebugMode && !isOperatorMonitor) {
			UnSubscribeFrom ("frame.");
			FramePublishingVariables.StreamCameraImages = false;
			//_sendRequestMessage (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });
		}
	}
	public void AssignTexture(ref Texture2D _eyeImage, ref Material _mat, byte[] data){
		print (_eyeImage);
		print (_mat);
//		byte[] _bArray;
//		_bArray = (byte[])data;
		_eyeImage.LoadImage (data);
		_mat.mainTexture = _eyeImage;
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
			subscriberSocket.Subscribe ("notify");
			subscriberSocket.Subscribe ("gaze");

			if (DebugVariables.subscribeAll) {
				subscriberSocket.SubscribeToAnyTopic ();
			}
//
			if (DebugVariables.subscribeFrame){
				Pupil.connectionParameters.toSubscribe.Add ("frame.");
				//Pupil.connectionParameters.update = true;
				//subscriberSocket.Subscribe ("frame."); //subscribe for frame data
			
			}
			if (DebugVariables.subscribeGaze)
				subscriberSocket.Subscribe ("gaze"); //subscribe for gaze data

			//if (DebugVariables.subscribeNotify)

			 //subscribe for all notifications



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


						if (Pupil.connectionParameters.update == true && !DebugVariables.subscribeAll){
						//ScheduleTask(new Task(delegate {
							//_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name","Frame_Publisher"}});
							print("net frame subscriptions updated");

							subscriberSocket.Close();
							subscriberSocket = new SubscriberSocket(IPHeader + subport);
							Thread.Sleep(100);
							Pupil.connectionParameters.toSubscribe.ForEach(p=>subscriberSocket.Subscribe(p));
							Pupil.connectionParameters.update = false;
								
							//}));
						}


						string msgType=msg[0].ConvertToString();

						if (DebugVariables.printMessage){
							var m = MsgPack.Unpacking.UnpackObject(msg[1].ToByteArray());
							MsgPack.MessagePackObject map = m.Value;
							print("type : " + msgType + " : " + m);
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
						print("Failed to unpack.");
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
//		print("on gaze");




		if (m_status == EStatus.ProcessingGaze) {
			object eyeID;
			float x, y;
			if (_pupilDataDict.dictionary.TryGetValue ("id", out eyeID)) {
				Calibration.marker _markerLeftEye = CalibrationMarkers.Where (p => p.name == "leftEye").ToList () [0];
				Calibration.marker _markerRightEye = CalibrationMarkers.Where (p => p.name == "rightEye").ToList () [0];
				Calibration.marker _markerGazeCenter = CalibrationMarkers.Where (p => p.name == "gaze").ToList () [0];

				string eyeIDStr = eyeID.ToString ();
				print ("current 2D eye ID : eye " + eyeIDStr);

				if (CurrentCalibrationMode == CalibModes._2D) {
					Vector2 pos_v2 = _pupilDataDict.GetValueAsVector ("norm_pos").v2;
					x = pos_v2.x;
					y = pos_v2.y;
					_eyeGazePos2D.x = (leftEye.gaze2D.x + rightEye.gaze2D.x) * 0.5f;
					_eyeGazePos2D.y = (leftEye.gaze2D.y + rightEye.gaze2D.y) * 0.5f;
					if (eyeIDStr == "0") {
						leftEye.AddGaze (x, y, 0);
						if (OnEyeGaze != null)
							OnEyeGaze (this);
					} else if (eyeIDStr == "1") {
						rightEye.AddGaze (x, y, 1);
						if (OnEyeGaze != null)
							OnEyeGaze (this);
					}

					//print ("gazepacket" + x + " , " + y + " , " + leftEye.gaze2D.ToString("F5"));
					//print ("gazepacket");


					_markerLeftEye.shape.x = GetEyeGaze2D (GazeSource.LeftEye).x;
					_markerLeftEye.shape.y = GetEyeGaze2D (GazeSource.LeftEye).y;
					//_0.toggle = true;

					_markerRightEye.shape.x = GetEyeGaze2D (GazeSource.RightEye).x;
					_markerRightEye.shape.y = GetEyeGaze2D (GazeSource.RightEye).y;
					//_1.toggle = true;

					_markerGazeCenter.shape.x = GetEyeGaze2D (GazeSource.BothEyes).x;
					_markerGazeCenter.shape.y = GetEyeGaze2D (GazeSource.BothEyes).y;
					//_markerGazeCenter.
				}
			}
			if (CurrentCalibrationMode == CalibModes._3D) {

				Pupil.values.EyeCenters3D = _pupilDataDict.GetValueAsVectorArray ("eye_centers_3d", 1f);
				Pupil.values.GazeNormals3D = _pupilDataDict.GetValueAsVectorArray ("gaze_normals_3d", 1f);
				Pupil.values.GazePoint3D = _pupilDataDict.GetValueAsVector ("gaze_point_3d").v3;

//				print ("eye centers : " + Pupil.values.EyeCenters3D [0].ToString ("F5") + " , " + Pupil.values.EyeCenters3D [1].ToString ("F5"));
//				print ("gaze normals : " + Pupil.values.GazeNormals3D [0].ToString ("F5") + " , " + Pupil.values.GazeNormals3D [1].ToString ("F5"));
//				print ("gaze point : " + Pupil.values.GazePoint3D.ToString ("F5"));
				//Vector3 pos_v3 = _pupilDataDict.GetValueAsVector ("mm_pos").v3;
				//var a = JsonUtility.FromJson<_double> ("{\"value\": " + _pupilDataDict.GetValueAsString ("gaze_point_3d") + "}");

				Calibration.marker gaze3D = CalibrationMarkers.Where (p => p.calibMode == CalibModes._3D && !p.calibrationPoint).ToList () [0];

				gaze3D.shape.x = Pupil.values.GazePoint3D.x;
				gaze3D.shape.y = -Pupil.values.GazePoint3D.y;
				gaze3D.depth = Pupil.values.GazePoint3D.z;
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
			_m.shape.width = Mathf.InverseLerp (DefaultCalibrationCount, 0f, _currCalibSamples)*_m.baseSize;//size hardcoded, change this
			_m.shape.x = _currentCalibPointPosition[0];
			_m.shape.y = _currentCalibPointPosition[1];
			_m.depth = _cCalibDetails.depth;//using the height az depth offset
			_m.toggle = true;

			print ("is toggle : " + _m.toggle);
			if (_calibPointTimeOut) {
				print (" is Timing out? : " + _calibPointTimeOut);
				_m.color = new Color (.5f, .5f, .5f, .1f);
				Thread.Sleep (2000);
				_m.color = Color.white;
				_calibPointTimeOut = false;
			}
			print ("is toggle after: " + _m.toggle);


			// Giving the user a short time to focus on the Calibration Point target before starting adding the reference data


			float[] aa = new float[]{ _currentCalibPointPosition [0], _currentCalibPointPosition [1] };
			if (_currentCalibPointPosition.Length > 2) {
				print ("this goes in : X : " + _currentCalibPointPosition [0] + " Y: " + _currentCalibPointPosition [1] + " Z: " + _currentCalibPointPosition [2] + " count : " + _currentCalibPointPosition.Length + " | " + aa [0] + " , " + aa [1]);
			} else {
				print ("this goes in : X : " + _currentCalibPointPosition [0] + " Y: " + _currentCalibPointPosition [1] + " count : " + _currentCalibPointPosition.Length + " | " + aa [0] + " , " + aa [1]);
			}
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
//			ScheduleTask(new Task(delegate {
//				ToastMessage.Instance.DrawToastMessage(new ToastMessage.toastParameters(){ID = 1, text = "Point : " + (_currCalibPoint+1) + "/" + _cPoints.Count() + "  " + ((Mathf.InverseLerp(0f, DefaultCalibrationCount, _currCalibSamples))*100).ToString("F0") + "%" });
//			}));

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

	#region Plugin Control
	public void StartBinocularVectorGazeMapper(){
		_sendRequestMessage (new Dictionary<string,object> {{"subject",""},{"name", "Binocular_Vector_Gaze_Mapper"}});
	}
	#endregion

	#region Recording
	public void OnRecording(){
	}
	public void StartRecording(){
		Recording.variables.CaptureScript = Camera.main.gameObject.AddComponent<FFmpegOut.CameraCapture> ();
		Recording.variables.CaptureScript._width = Recording.variables.width;
		Recording.variables.CaptureScript._height = Recording.variables.height;
		Recording.variables.CaptureScript._recordLength = Recording.variables.length;
		OnUpdate += OnRecording;
	}
	public void StopRecording(){
		Recording.variables.isRecording = false;
		Destroy (Camera.main.gameObject.GetComponent<FFmpegOut.CameraCapture> ());
		Recording.variables.CaptureScript = null;
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
		Pupil.processStatus.eyeProcess0 = false;
		Pupil.processStatus.eyeProcess1 = false;
		if (serviceProcess != null) {
			serviceProcess.Close ();
			serviceProcess.WaitForExit ();
		}
	}
}
