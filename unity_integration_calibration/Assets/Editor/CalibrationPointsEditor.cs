using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CalibrationPointsEditor : EditorWindow {

	private static PupilGazeTracker pupilTracker;

	private static Vector3 _v3 = new Vector3 (0, 0, 0);
	private static Vector2 _2DScale;

	public static void DrawCalibrationPointsEditor(PupilGazeTracker _pupilTracker, int index, int mode, Vector3 vector){
		DrawCalibrationPointsEditor (_pupilTracker);
		pupilTracker.editedCalibIndex = index;
		pupilTracker.calibrationMode = mode;
		_2DScale = pupilTracker.Calibration2DScale;
		_v3 = vector;
		if (mode == 0)
			_v3 = convertToWorldSpace (_v3, _2DScale.x, _2DScale.y);
	}
	public static void DrawCalibrationPointsEditor(PupilGazeTracker _pupilTracker){
		SceneView.onSceneGUIDelegate -= OnScene;
		SceneView.onSceneGUIDelegate += OnScene;
		SceneView.RepaintAll ();
		pupilTracker = _pupilTracker;
		Debug.Log ("Calibration Points Editor Started !");
		pupilTracker.editedCalibIndex = 0;
	}

	private static void OnScene(SceneView sceneView){
		Tools.current = Tool.None;
		bool updatePosition;
		PupilGazeTracker.CalibModes _calibMode = pupilTracker.CurrentCalibrationMode;
		string[] _calibPointNames = pupilTracker._calibPoints.GetPointNames (_calibMode);
		PupilGazeTracker.CalibModeDetails _currCalibModeDetails = pupilTracker.CurrentCalibrationModeDetails;
		List<floatArray> _activeList = pupilTracker._calibPoints.GetActiveList (_calibMode);
		//Debug.Log (_currCalibModeDetails.calibrationPoints);

		Vector3 _tmpV3 = Handles.PositionHandle (_v3, Quaternion.identity);
		if (_tmpV3 == _v3) {
			updatePosition = false;
		} else {
			_2DScale = pupilTracker.Calibration2DScale;
			_v3 = _tmpV3;
			updatePosition = true;
		}
		Debug.Log (_tmpV3);
		///////////////////////////GUI///////////////////////////
		Handles.BeginGUI ();

		EditorGUI.BeginChangeCheck ();
		pupilTracker.calibrationMode = GUILayout.Toolbar (pupilTracker.calibrationMode, new string[]{ "2D", "3D" });
		if (EditorGUI.EndChangeCheck ()) {
			Debug.Log ("change happens");
//			sceneView.Repaint ();
			pupilTracker.SwitchCalibrationMode();
			_calibMode = pupilTracker.CurrentCalibrationMode;
			_activeList = pupilTracker._calibPoints.GetActiveList (_calibMode);
			if (pupilTracker.editedCalibIndex > _activeList.Count - 1)
				pupilTracker.editedCalibIndex = _activeList.Count - 1;
			
			_v3 = (Vector3)pupilTracker._calibPoints.GetVector (_activeList, pupilTracker.editedCalibIndex);
			if (pupilTracker.calibrationMode == 0)
				_v3 = convertToWorldSpace (_v3,_2DScale.x, _2DScale.y);
		}
			
		EditorGUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Add Calibration Point")) {
			pupilTracker.AddCalibrationPoint ();
			pupilTracker.editedCalibIndex = _activeList.Count - 1;
			_v3 = (Vector3)pupilTracker._calibPoints.GetVector (_activeList, pupilTracker.editedCalibIndex);
			if (pupilTracker.calibrationMode == 0)
				_v3 = convertToWorldSpace (_v3,_2DScale.x, _2DScale.y);
		}
		if (GUILayout.Button ("Remove Calibration Point")) {
			pupilTracker.RemoveCalibrationPoint (_activeList, pupilTracker.editedCalibIndex);
			pupilTracker.editedCalibIndex--;
			_v3 = (Vector3)pupilTracker._calibPoints.GetVector (_activeList, pupilTracker.editedCalibIndex);
			if (pupilTracker.calibrationMode == 0)
				_v3 = convertToWorldSpace (_v3,_2DScale.x, _2DScale.y);
		}
		if (GUILayout.Button ("Exit Editor")) {
			SceneView.onSceneGUIDelegate -= OnScene;
			Tools.current = Tool.Move;
		}
		EditorGUILayout.EndHorizontal ();

		EditorGUI.BeginChangeCheck ();
			pupilTracker.editedCalibIndex = EditorGUILayout.Popup (pupilTracker.editedCalibIndex, _calibPointNames);
		if (EditorGUI.EndChangeCheck ()) {
			_v3 = (Vector3)pupilTracker._calibPoints.GetVector (_activeList, pupilTracker.editedCalibIndex);
			if (pupilTracker.calibrationMode == 0)
				_v3 = convertToWorldSpace (_v3, _2DScale.x, _2DScale.y);
		}


		if (pupilTracker.editedCalibIndex <= (_activeList.Count - 1)) {
			
			if (pupilTracker.calibrationMode == 0) {
				Vector3 _v3Norm = convertToNormalSpace(_v3,_2DScale.x, _2DScale.y);
				pupilTracker._calibPoints.SetVector (_activeList, _v3Norm, pupilTracker.editedCalibIndex);
			} else {
				pupilTracker._calibPoints.SetVector (_activeList, _v3, pupilTracker.editedCalibIndex);
			}


		} else {
			pupilTracker.editedCalibIndex = _activeList.Count - 1;
		}
		
		Handles.EndGUI ();
		///////////////////////////GUI///////////////////////////

	}
	static Vector3 convertToNormalSpace(Vector3 _v3In, float xMax, float yMax){
		float xAbs = _v3In.x + xMax;
		float x = Mathf.InverseLerp (0f, xMax * 2, xAbs);

		float yAbs = _v3In.y + yMax;
		float y = Mathf.InverseLerp (0f, yMax * 2, yAbs);

		Vector3 _v3Out = new Vector3 (x, y, 0);
		return _v3Out;
	}
	static Vector3 convertToWorldSpace(Vector3 _v3In, float xMax, float yMax){
		//float xAbs = _v3In.x + xMax;
		float x = Mathf.Lerp (0f, xMax*2, _v3In.x)-xMax;
		float y = Mathf.Lerp (0f, yMax*2, _v3In.y)-yMax;

		Vector3 _v3Out = new Vector3 (x, y, 0);
		return _v3Out;
	}
}