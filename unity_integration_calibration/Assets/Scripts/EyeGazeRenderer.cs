using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class EyeGazeRenderer : MonoBehaviour
{
	
	[System.Serializable]
	public class Options3D{
	[Range(-250,250)]
	public float xOffset;

	[Range(-250,250)]
	public float yOffset;

	[Range(-250,500)]
	public float zOffset;

	[Range(-1.0f,3.0f)]
	public float scalex = 1;

	[Range(-1.0f,3.0f)]
	public float scaley = 1;

	[Range(-1.0f,3.0f)]
	public float scalez = 1;

	[Range(-1.0f,3.0f)]
	public float scaleAll = 1;
	}

	public Options3D _3DOptions = new Options3D ();
	public RectTransform gaze;

	public PupilGazeTracker.GazeSource Gaze;


//	int FPSoffset = 0;
//	int Updatecount = 0;
//	public string errordata = "";
//	int Packetcount = 0;
//	float valueX = -1;
//	int highestOffset = 0;
//	int threshold = 10;
//	List<float> overThreshold = new List<float>();

	// Script initialization
	void Start() {	
		
		if (gaze == null)
			gaze = this.GetComponent<RectTransform> ();
	}

	void Update() {
		if (PupilGazeTracker.Instance.CurrentCalibrationMode == PupilGazeTracker.CalibModes._2D) {
			if (gaze == null)
				return;
			Canvas c = gaze.GetComponentInParent<Canvas> ();
			Vector2 g2 = PupilGazeTracker.Instance.GetEyeGaze2D (Gaze);
			gaze.localPosition = new Vector3 ((g2.x - 0.5f) * c.pixelRect.width, (g2.y - 0.5f) * c.pixelRect.height, 0);
		} else {
			Vector3 g3 = PupilGazeTracker.Instance.GetEyeGaze3D (Gaze);
			gameObject.transform.localPosition = new Vector3 (((g3.x)*_3DOptions.scalex*_3DOptions.scaleAll)+_3DOptions.xOffset,((g3.y)*_3DOptions.scaleAll*_3DOptions.scaley)+_3DOptions.yOffset,((g3.z)*_3DOptions.scaleAll*_3DOptions.scalez)+_3DOptions.zOffset);

//			if (g3.x != 0) {
//				if (valueX != g3.x) {
//					string _toAdd = g3.x.ToString ("0000.0");
//					//_toAdd = _toAdd.
//					errordata += "Update Offset : " + FPSoffset + ", X : " + _toAdd + " || ";
//					if (FPSoffset > highestOffset) {
//						highestOffset = FPSoffset;
//					}
//					if (FPSoffset > threshold) {
//						overThreshold.Add (FPSoffset);
//					}
//					FPSoffset = -1;
//					Packetcount++;
//				}
//				print (g3.x);
//			} else {
//				FPSoffset = -1;
//			}
//			valueX = g3.x;
//
//			Updatecount++;
//			FPSoffset++;
		}
	}
	void OnApplicationQuit(){
//		errordata += "||| Time Elapsed : " + Time.timeSinceLevelLoad + ", Update Count vs. Packet Count : " + Updatecount + " vs. " + Packetcount + ", Highest Offset : " + highestOffset + 
//			", Packet over offset threshold amount(10) : " + overThreshold.Count + ", Namely : ";
//		foreach (float _f in overThreshold) {
//			errordata += _f + " , ";
//		}
//		errordata+="|||";
//		print (errordata);
	}
}