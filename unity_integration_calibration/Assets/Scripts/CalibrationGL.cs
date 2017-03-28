using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CalibrationGL{
	static Texture2D _t;
	static PupilGazeTracker pupilTracker;
	static bool isInitialized;
	static Material markerMaterial;

	public static void Init(){
		//return Draw;
		pupilTracker = PupilGazeTracker.Instance;
		_t = Resources.Load ("CalibrationMarker") as Texture2D;
		isInitialized = true;
		CreateEye1ImageMaterial ();
		markerMaterial.mainTexture = _t;
	}

	static void CreateEye1ImageMaterial ()
	{
		if (!markerMaterial)
		{
			Shader shader = Shader.Find ("Sprites/Default");
			markerMaterial = new Material (shader);
			markerMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	public static void Draw(){
		if (!isInitialized)
			Init ();

		GL.PushMatrix ();
		//GL.LoadPixelMatrix (pupilTracker.value1, Screen.width, Screen.height, pupilTracker.value2);
		//GL.LoadPixelMatrix (pupilTracker.value0, pupilTracker.value1, pupilTracker.value2, pupilTracker.value3);

		Matrix4x4 _m = new Matrix4x4 ();
		_m.SetTRS (new Vector3(-.5f,-.5f,.7f), Quaternion.identity, new Vector3(1,1,1));

		GL.MultMatrix (Camera.main.transform.localToWorldMatrix*_m);

		//GL.LoadProjectionMatrix (_m);
		//GL.LoadPixelMatrix(
		//GL.LoadPixelMatrix(0,1,1, 0);
		//GL.LoadOrtho ();


		foreach (Calibration.marker _marker in pupilTracker.CalibrationMarkers) {
			if (_marker.toggle == true)
				Marker (_marker);
		}

		//Marker (pupilTracker.CalibrationMarkers [2]);
		//Calibration.marker _cm = new Calibration.marker();
		//_cm.shape
		Marker (new Calibration.marker(){shape = new Rect(pupilTracker.value0,pupilTracker.value1,.07f,pupilTracker.value2), color = Color.blue});


		GL.PopMatrix ();
	}
	public static void Marker(Calibration.marker _m){
		Rect _r = _m.shape;
		markerMaterial.SetColor ("_Color", _m.color);
		markerMaterial.SetPass (0);
		GL.Begin (GL.QUADS);
		GL.TexCoord2 (0,1);
		GL.Vertex (new Vector3 (_r.x-((_r.width/2)), _r.y-_r.width/2, 0));//BL
		GL.TexCoord2 (1,1);
		GL.Vertex (new Vector3 (_r.x-((_r.width/2)), _r.y+_r.width/2, 0));//TL
		GL.TexCoord2 (1,0);
		GL.Vertex (new Vector3 (_r.x+((_r.width/2)), _r.y+_r.width/2, 0));//TR
		GL.TexCoord2 (0,0);
		GL.Vertex (new Vector3 (_r.x+((_r.width/2)), _r.y-_r.width/2, 0));//BR
		GL.End();
	}
	public static void CalibrationMode(){
		if (!isInitialized)
			Init ();
		foreach (Calibration.marker _m in pupilTracker.CalibrationMarkers) {
			if (_m.name != "Marker") {
				_m.toggle = false;
			} else {
				_m.toggle = true;
			}
		}
		if (pupilTracker.OnCalibrationGL == null)
			pupilTracker.OnCalibrationGL += Draw;
	}
	public static void GazeProcessingMode(){
		if (!isInitialized)
			Init ();
		foreach (Calibration.marker _m in pupilTracker.CalibrationMarkers) {
			if (_m.name != "Marker") {
				_m.toggle = true;
			} else {
				_m.toggle = false;
			}
		}
		if (pupilTracker.OnCalibrationGL == null)
			pupilTracker.OnCalibrationGL += Draw;
	}
}
