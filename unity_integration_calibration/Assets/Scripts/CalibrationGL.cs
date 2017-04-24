using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CalibrationGL{
	static Texture2D _t;
	static PupilGazeTracker pupilTracker;
	static bool isInitialized;
	static Material markerMaterial;
	//public static PupilGazeTracker.CalibModes currentMode;

	public static void Init(){
		//return Draw;
		pupilTracker = PupilGazeTracker.Instance;
		_t = Resources.Load ("CalibrationMarker") as Texture2D;
		isInitialized = true;
		CreateEye1ImageMaterial ();
		markerMaterial.mainTexture = _t;
		//currentMode = pupilTracker.CurrentCalibrationMode;
	}

	static void CreateEye1ImageMaterial ()
	{
		if (!markerMaterial)
		{
			Shader shader = Shader.Find ("Particles/Alpha Blended");
			markerMaterial = new Material (shader);
			markerMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	public static void Draw(){
		if (!isInitialized)
			Init ();

		GL.PushMatrix ();

		//TODO : set this matrix once only!!!
			Matrix4x4 _m = new Matrix4x4 ();
			//_m.SetTRS (new Vector3 (-.5f, -.5f, .7f), Quaternion.identity, new Vector3 (1, 1, 1));
		_m.SetTRS (new Vector3 (-.5f, -.5f, .7f), Quaternion.identity , new Vector3 (1, 1, 1));

			GL.MultMatrix (Camera.main.transform.localToWorldMatrix * _m);
		foreach (Calibration.marker _marker in pupilTracker.CalibrationMarkers) {
			if (_marker.toggle == true) {
				Marker (_marker);
			}
		}

		GL.PopMatrix ();

	}
	public static void Marker(Calibration.marker _m){
		Rect _r = _m.shape;
		if (_m.material != null) {
			_m.material.SetPass (0);
		} else {
			//_m.color.a = 1;
			markerMaterial.SetColor ("_TintColor", _m.color);
			markerMaterial.SetPass (0);
		}

		GL.Begin (GL.QUADS);
		GL.TexCoord2 (0,1);
		GL.Vertex (new Vector3 (_r.x-((_r.width/2)), _r.y-_r.width/2, _m.depth));//BL
		GL.TexCoord2 (1,1);
		GL.Vertex (new Vector3 (_r.x-((_r.width/2)), _r.y+_r.width/2, _m.depth));//TL
		GL.TexCoord2 (1,0);
		GL.Vertex (new Vector3 (_r.x+((_r.width/2)), _r.y+_r.width/2, _m.depth));//TR
		GL.TexCoord2 (0,0);
		GL.Vertex (new Vector3 (_r.x+((_r.width/2)), _r.y-_r.width/2, _m.depth));//BR
		GL.End();

	}

	public static void SetMode(PupilGazeTracker.EStatus status){
		if (!isInitialized)
			Init ();
		foreach (Calibration.marker _m in pupilTracker.CalibrationMarkers) {
			_m.toggle = false;

			if (_m.calibMode == pupilTracker.CurrentCalibrationMode) {
				if (_m.calibrationPoint && status == PupilGazeTracker.EStatus.Calibration) {
					_m.toggle = true;
				}
				if (!_m.calibrationPoint && status == PupilGazeTracker.EStatus.ProcessingGaze) {
					_m.toggle = true;
				}
			}
		}
		if (pupilTracker.OnCalibrationGL == null)
			pupilTracker.OnCalibrationGL += Draw;
	}
	//TODO: Merge these functions (CalibrationMode & GazeProcessingMode)
//	public static void CalibrationMode(){
//		if (!isInitialized)
//			Init ();
//		foreach (Calibration.marker _m in pupilTracker.CalibrationMarkers) {
//			if (_m.name != ("Marker"+pupilTracker.CurrentCalibrationModeDetails.name)) {
//				_m.toggle = false;
//			} else {
//				_m.toggle = true;
//			}
//		}
//		if (pupilTracker.OnCalibrationGL == null)
//			pupilTracker.OnCalibrationGL += Draw;
//	}
//
//
//	public static void GazeProcessingMode(PupilGazeTracker.CalibModes _mode){
//		if (!isInitialized)
//			Init ();
//		foreach (Calibration.marker _m in pupilTracker.CalibrationMarkers) {
//			if (_m.name != "Marker") {
//				_m.toggle = true;
//			} else {
//				_m.toggle = false;
//			}
//		}
//		if (pupilTracker.OnCalibrationGL == null)
//			pupilTracker.OnCalibrationGL += Draw;
//	}

}
