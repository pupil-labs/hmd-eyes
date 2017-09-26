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
		pupilTracker = PupilGazeTracker.Instance;
		_t = Resources.Load ("CalibrationMarker") as Texture2D;
		Debug.Log (pupilTracker.Settings.dataProcess.state);
//		InitializeVisuals (pupilTracker.Settings.dataProcess.state);
		isInitialized = true;
		CreateEye1ImageMaterial ();
		markerMaterial.mainTexture = _t;
	}

	static void CreateEye1ImageMaterial ()
	{


		if (!markerMaterial){
			
			Shader shader = Shader.Find ("Particles/Alpha Blended");
			markerMaterial = new Material (shader);
			markerMaterial.hideFlags = HideFlags.HideAndDontSave;
		
		}


	}

	public static void Draw(){

		if (!isInitialized)
			Init ();

		GL.PushMatrix ();

		Matrix4x4 _m = new Matrix4x4 ();
		_m.SetTRS (new Vector3 (-.5f, -.5f, .7f), Quaternion.identity , new Vector3 (1, 1, 1));


//		Quaternion.Euler( Camera.main.transform.up )
//		new Vector3 (-.5f, -.5f, .7f)
		GL.MultMatrix (Camera.main.transform.localToWorldMatrix * _m);

		foreach (PupilSettings.Calibration.Marker _marker in PupilSettings.Instance.calibration.CalibrationMarkers) {
		
			if (_marker.toggle == true)
				Marker (_marker);
		
		}

		GL.PopMatrix ();

	}

	public static void Marker(PupilSettings.Calibration.Marker _m){
		
		if (_m.material != null) {
			
			_m.material.SetPass (0);

		} else {
			
			markerMaterial.SetColor ("_TintColor", _m.color);
			markerMaterial.SetPass (0);

		}

		GL.Begin (GL.QUADS);
		GL.TexCoord2 (0,1);
		GL.Vertex (new Vector3 (_m.position.x-((_m.size/2)), _m.position.y-_m.size/2, _m.position.z));//BL
		GL.TexCoord2 (1,1);
		GL.Vertex (new Vector3 (_m.position.x-((_m.size/2)), _m.position.y+_m.size/2, _m.position.z));//TL
		GL.TexCoord2 (1,0);
		GL.Vertex (new Vector3 (_m.position.x+((_m.size/2)), _m.position.y+_m.size/2, _m.position.z));//TR
		GL.TexCoord2 (0,0);
		GL.Vertex (new Vector3 (_m.position.x+((_m.size/2)), _m.position.y-_m.size/2, _m.position.z));//BR
		GL.End();

	}

	public static void InitializeVisuals(PupilSettings.EStatus status){

		if (!isInitialized)
			Init ();
		
		foreach (PupilSettings.Calibration.Marker _m in PupilSettings.Instance.calibration.CalibrationMarkers) {
		
			_m.toggle = false;


			if (_m.calibMode == PupilSettings.Instance.calibration.currentCalibrationMode) {
			
				if (_m.calibrationPoint && status == PupilSettings.EStatus.Calibration) {
				
					_m.toggle = true;
				
				}

				if (!_m.calibrationPoint && status == PupilSettings.EStatus.ProcessingGaze) 
				{
					_m.toggle = true;
				
				}
			
			}
		
		
		}

		if (pupilTracker.OnCalibrationGL == null)
		
			pupilTracker.OnCalibrationGL += Draw;
	
	
	}

}
