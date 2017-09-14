using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;


public static class PupilData{

	public static Dictionary<string, object> gazeDictionary;
	public static Dictionary<string, object> pupil0Dictionary;
	public static Dictionary<string, object> pupil1Dictionary;

	private static object o;

	public static double Diameter(){
	
		return new double ();

	}

	public static class _3D{
	

		public static class Circle
		{
	
			public static Vector3 Center(int eyeID){

				return Vector3.zero;

			}

			public static double Radius(int eyeID){

				return 0.0;

			}

			public static Vector3 Normal(int eyeID){

				return Vector3.zero;

			}

		}

		public static Vector3 EyeCenters(int eyeID){

			return Vector3.zero;

		}

		public static Vector3 EyeNormals(int eyeID){

			return Vector3.zero;

		}

		public static Vector3 Gaze(){

			object o = new object();
			object[] gaze_point_3d_o;

			Vector3 _v3 = new Vector3();

			if (gazeDictionary.TryGetValue ("gaze_point_3d", out o)) {

				gaze_point_3d_o = o as object[];

				_v3.Set ((float)(double)gaze_point_3d_o [0], (float)(double)gaze_point_3d_o [1], (float)(double)gaze_point_3d_o [2]);

			}

			return _v3;

		}

	}
		
	public class _2D
	{
		private static int eyeID;

		public _2D(int _eyeID){
		
			eyeID = _eyeID;

		}

		public static Vector2 Norm_Pos(){

			object o = new object();
			object[] norm_pos_o;

			Vector2 _v2 = new Vector2();

			if (gazeDictionary.TryGetValue ("norm_pos", out o)) {
				
				norm_pos_o = o as object[];

				_v2.Set ((float)(double)norm_pos_o [0], (float)(double)norm_pos_o [1]);

			}

			return _v2;

		}

		public static Vector2 Left_Pos(){

			return new Vector2 (0, 0);

		}

		public static Vector2 Right_Pos(){

			return new Vector2 (0, 0);

		}

		public static Vector2 Gaze(){

			return (Left_Pos() + Right_Pos()) * 0.5f;

		}

		public static string ID(){
		
			object IDo;
			bool isID = gazeDictionary.TryGetValue ("id", out IDo);

			if (isID) {
				
				return IDo.ToString ();

			} else {
				
				return null;

			}

		}


	}











	



	public static double Confidence(int eyeID){
		object confO;
		if (eyeID == 0) {
			PupilData.pupil0Dictionary.TryGetValue ("confidence", out confO);
		} else {
			PupilData.pupil1Dictionary.TryGetValue ("confidence", out confO);
		}
		return (double)confO;
	}

	public static Dictionary<object,object> BaseData(){
		gazeDictionary.TryGetValue ("base_data", out o);
		return o as Dictionary<object,object>;
	}

}

[CreateAssetMenu(fileName = "PupilSettings")]
public class PupilSettings:ScriptableObject{

	static PupilSettings _instance = null;
	public static PupilSettings Instance{
		get{
			if (_instance == null)
				_instance = PupilTools.GetPupilSettings ();
			return _instance;
		}
	}

	public enum EStatus
	{
		Idle,
		ProcessingGaze,
		Calibration
	}



	[Serializable]
	public struct CalibrationType{
		public string name;
		public string pluginName;
		public string positionKey;
		public double[] ref_data;
		public float depth;
		public List<float[]> calibPoints;
	}

	[Serializable]
	public class Calibration{

		[Serializable]
		public class Marker{

			public string name;
			public Vector3 position;
			public float size;
			public Color color;
			public bool toggle;
			public bool calibrationPoint;
			public Material material;
			public PupilSettings.Calibration.CalibMode calibMode;

		}

		public enum CalibMode
		{
			_2D,
			_3D
		};

		private CalibrationType CalibrationType2D = new CalibrationType (){ 
			name = "2D",
			pluginName = "HMD_Calibration",
			positionKey = "norm_pos",
			ref_data = new double[]{0.0,0.0},
			depth = 0.1f,
			calibPoints = new List<float[]>(){
				new float[]{0.2f,0.2f},
				new float[]{0.35f,0.35f},
				new float[]{0.2f,0.5f},
				new float[]{0.35f,0.5f},
				new float[]{0.2f,0.8f},
				new float[]{0.35f,0.65f},
				new float[]{0.5f,0.8f},
				new float[]{0.5f,0.65f},
				new float[]{0.8f,0.8f},
				new float[]{0.65f,0.65f},
				new float[]{0.8f,0.5f},
				new float[]{0.65f,0.5f},
				new float[]{0.8f,0.2f},
				new float[]{0.65f,0.35f},
				new float[]{0.5f,0.2f},
				new float[]{0.5f,0.35f},
				new float[]{0.5f,0.5f}
			}
		};

		private CalibrationType CalibrationType3D = new CalibrationType (){ 
			name = "3D",
			pluginName = "HMD_Calibration_3D",
			positionKey = "mm_pos",
			ref_data = new double[]{0.0,0.0, 0.0},
			depth = 100f,
			calibPoints = new List<float[]>(){
				new float[]{0f,0f, 100f},
				new float[]{-40,-40, 100f},
				new float[]{-40,-0f, 100f},
				new float[]{40,-0f, 100f},
				new float[]{-20,-20, 100f},
				new float[]{-40,40, 100f},
				new float[]{0f,40, 100f},
				new float[]{0f,-40, 100f},
				new float[]{-20,20, 100f},
				new float[]{40,40, 100f},
				new float[]{20,20, 100f},
				new float[]{40,-40, 100f},
				new float[]{20,-20, 100f}
//				new float[]{0f,0f, 100f}
			}
		};
				

		public CalibMode currentCalibrationMode;

		public CalibrationType currentCalibrationType{
			
			get{
				
				if (currentCalibrationMode == CalibMode._2D) {
					
					return CalibrationType2D;

				} else {
					
					return CalibrationType3D;

				}

			}

		}

		public int currCalibPoint;
		public int currCalibSamples;

		Marker _marker = null;
		public Marker marker{
			get{
				if (_marker.name == "") {
					
					_marker = CalibrationMarkers.Where (p => p.calibrationPoint && p.calibMode == PupilSettings.Instance.calibration.currentCalibrationMode).ToList () [0];
					Debug.Log ("getting");
				
				}
//				Debug.Log (_marker);

				return _marker;

			}
			set{ 
				_marker = value;
			}
		}

		public bool initialized = false;
		public Marker[] CalibrationMarkers;

	}

	[Serializable]
	public class Connection{
		public bool isConnected = false;
		public bool isAutorun;
		public string IP;
		public string IPHeader;
		public int PORT;
		public string subport;
		public bool isLocal;
		public List<string> topicList;

		public SubscriberSocket subscribeSocket = null;
	}

	[Serializable]
	public class PupilServiceApp
	{
		public string servicePath;
	}
		
	[Serializable]
	public class DataProcess
	{
		public EStatus state;
		public string benchMarkString;
	}

	[Serializable]
	public class CustomGUIVariables
	{
		[Serializable]
		public class Tabs{
			
			public int mainTab;

		}

		[Serializable]
		public class Bools
		{

			public bool isAdvanced;
			
		}

		public Tabs tabs;
		public Bools bools;

	}

	[Serializable]
	public class DebugView
	{
		public bool active = false;
	}

	[Serializable]
	public class DebugVars
	{
		public bool printSampling;
		public bool printMessage;
		public bool printMessageType;
	}

	[Serializable]
	public class FramePublishing{
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

	public DebugVars debug;
	public DataProcess dataProcess;
	public Connection connection;
	public PupilServiceApp pupilServiceApp;
	public Calibration calibration;
	public CustomGUIVariables customGUIVariables;
	public DebugView debugView;
	public FramePublishing framePublishing;
	public bool visualizeGaze;

	public List<GUIStyle> GUIStyles;

}

