using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;


public class OperatorMonitor : MonoBehaviour {

	PupilGazeTracker pupilTracker;
	public Material lineMaterial;
	public Operator.properties properties;
	public List<float> confidenceList0 = new List<float> ();
	public List<float> confidenceList1 = new List<float> ();

	void Awake(){
		pupilTracker = PupilGazeTracker.Instance;
		confidenceList0 = new List<float> ();
		confidenceList1 = new List<float> ();
	}

	void OnPostRender() {
		CreateLineMaterial ();

		lineMaterial.SetPass (0);

		DrawOperatorMonitor ();
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
		DrawGraph (ref confidenceList0, requestUpdate: ref properties.isUpdateGraph, offset: properties.graphPositionOffset0, rotOffset: properties.rotOffset, scale: properties.graphScale, refreshDelay: properties.refreshD, graphLength: properties.eye0GraphLength, confidence: properties.conf0);
		DrawGraph (ref confidenceList1, requestUpdate: ref properties.isUpdateGraph, offset: properties.graphPositionOffset1, rotOffset: properties.rotOffset, scale: properties.graphScale, refreshDelay: properties.refreshD, graphLength: properties.eye1GraphLength, confidence: properties.conf1);
		if (properties.isUpdateGraph)
			properties.isUpdateGraph = false;
	}

	#endregion
}
