using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;


public class OperatorMonitor : MonoBehaviour {

	static PupilGazeTracker pupilTracker;
	public Material lineMaterial;
	public Operator.properties properties;
	public List<float> confidenceList0 = new List<float> ();
	public List<float> confidenceList1 = new List<float> ();



	public static void Instantiate(){
		GameObject _camGO = new GameObject ();
		pupilTracker = PupilGazeTracker.Instance;
		pupilTracker.OperatorCamera = _camGO.AddComponent<Camera> ();
		pupilTracker.OperatorCamera.stereoTargetEye = StereoTargetEyeMask.None;
		pupilTracker.OperatorCamera.backgroundColor = Color.gray;
		OperatorMonitor _opscript = _camGO.AddComponent<OperatorMonitor> ();
		_camGO.name = "Operator Camera";
		_opscript.properties = pupilTracker.OperatorMonitorProperties;
		_opscript.confidenceList0.Capacity = _opscript.properties.eye0GraphLength + 1;//predefining the assumed list size for better memory allocation
		_opscript.confidenceList1.Capacity = _opscript.properties.eye1GraphLength + 1;
	}

	void Awake(){
		pupilTracker = PupilGazeTracker.Instance;
		confidenceList0 = new List<float> ();
		confidenceList1 = new List<float> ();
	}

	void OnPostRender() {
		CreateLineMaterial ();
		print ("onpostrender on operator monitor");
		lineMaterial.SetPass (0);

		DrawOperatorMonitor ();
	}

	void OnGUI()
	{
//		string str="Capture Rate="+FPS;
//		str += "\nLeft Eye:" + LeftEyePos.ToString ();
//		str += "\nRight Eye:" + RightEyePos.ToString ();
		string str;
		if (pupilTracker._pupilData.eye_centers_3d.zero.Length > 0 && pupilTracker._pupilData.eye_centers_3d.one.Length > 0) {
			str = "Gaze Point : " + pupilTracker._pupilData.gaze_point_3d [0]  + " , " + pupilTracker._pupilData.gaze_point_3d [1]  + " , " + pupilTracker._pupilData.gaze_point_3d [2];
			str += "\nEyeball0:" + pupilTracker._pupilData.eye_centers_3d.zero [0] + " , " + pupilTracker._pupilData.eye_centers_3d.zero [1] + " , " + pupilTracker._pupilData.eye_centers_3d.zero [2];
			str += "\nEyeball1:" + pupilTracker._pupilData.eye_centers_3d.one [0] + " , " + pupilTracker._pupilData.eye_centers_3d.one [1] + " , " + pupilTracker._pupilData.eye_centers_3d.one [2];
		} else {
			str = "Waiting for 3D data.";
		}

//		GUIStyle _style = GUIStyle.none;
//		GUIContent _cont = new GUIContent (str);
//		float height = _style.CalcHeight (_cont, 200);
//		Vector2 _v2 = _style.CalcSize (_cont);
		GUI.TextArea (new Rect (0, 0, Screen.width, 200), str, pupilTracker.Styles.Find(x=>x.name == "OpMon_textArea") );


		//		if (OnCalibrationGL != null)
		//			OnCalibrationGL ();

	}

	void CreateLineMaterial ()
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
	}

	#region operator_monitor.functions

	public void DrawGraph(ref List<float> _confidenceList, ref bool requestUpdate,float confidence = 0.2f, int graphLength = 20, Vector3 offset = default(Vector3), float rotOffset = 0, Vector2 scale = default(Vector2), float refreshDelay = 1f){
		print ("drawing graph");
		Pupil.PupilData _pData = PupilGazeTracker.Instance._pupilData;
		Matrix4x4 _m = new Matrix4x4 ();
		_m.SetTRS (offset, Quaternion.Euler (new Vector3(0,0,rotOffset)), new Vector3 (scale.x, scale.y, 1));

		GL.PushMatrix ();
		lineMaterial.SetPass (0);
		//GL.LoadOrtho ();
		GL.MultMatrix (transform.localToWorldMatrix * _m);
		GL.Begin (GL.LINES);

		//print ("from : " + forward + " to :  " + Quaternion.LookRotation (forward, Vector3.up));


		if (TimeSpan.FromTicks(DateTime.Now.Ticks - properties.graphTime).TotalSeconds > (refreshDelay/100)) {
			requestUpdate = true;
			properties.graphTime = DateTime.Now.Ticks;
		}

		if (requestUpdate) {
			_confidenceList.Insert (0, confidence);
			if (_confidenceList.Count > graphLength)
				_confidenceList.RemoveAt (_confidenceList.Count - 1);
		}
		if (_confidenceList.Count >= graphLength) {
			//print ("this happens");

			for (int i = 0; i < graphLength; i++) {
				GL.Vertex3 (i , 0, 10);
				GL.Vertex3 (i, _confidenceList [i]*100, 10);
			}
		}


		GL.End ();
		GL.PopMatrix ();
	}

	public void DrawOperatorMonitor(){
		CreateLineMaterial ();
		//print (" Drawing operator monitor ! ");
		//TODO: Pass on only the class values!
//		print (properties.conf0 + " conf 1 : ");
//		print (properties.conf1);
		float conf0 = 0; 
		float conf1 = 0;
		if (pupilTracker._pupilData.base_data != null) {
			if (pupilTracker._pupilData.base_data.Length == 2) {
				conf0 = (float)pupilTracker._pupilData.base_data [0].confidence + UnityEngine.Random.Range (0.05f, 0.1f);
				conf1 = (float)pupilTracker._pupilData.base_data [1].confidence + UnityEngine.Random.Range (0.05f, 0.1f);
//				conf0 = (float)pupilTracker._pupilData.base_data [0].confidence + 0.2f;
//				conf1 = (float)pupilTracker._pupilData.base_data [1].confidence + 0.2f;
			} else {
				conf0 = 0.2f;
				conf1 = 0.2f;
			}
		}
		DrawGraph (ref confidenceList0, requestUpdate: ref properties.isUpdateGraph, offset: properties.graphPositionOffset0, rotOffset: properties.rotOffset, scale: properties.graphScale, refreshDelay: properties.refreshD, graphLength: properties.eye0GraphLength, confidence: conf0);
		DrawGraph (ref confidenceList1, requestUpdate: ref properties.isUpdateGraph, offset: properties.graphPositionOffset1, rotOffset: properties.rotOffset, scale: properties.graphScale, refreshDelay: properties.refreshD, graphLength: properties.eye1GraphLength, confidence: conf1);
		if (properties.isUpdateGraph)
			properties.isUpdateGraph = false;
	}

	#endregion
}
