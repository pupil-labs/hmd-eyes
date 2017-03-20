using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CalibrationPointsEditor : EditorWindow {

	private static PupilGazeTracker pupilTracker;

	public static void DrawCalibrationPointsEditor(PupilGazeTracker _pupilTracker){
		SceneView.onSceneGUIDelegate -= OnScene;
		SceneView.onSceneGUIDelegate += OnScene;
		SceneView.RepaintAll ();
		pupilTracker = _pupilTracker;
		Debug.Log ("Calibration Points Editor Started !");
		Debug.Log (pupilTracker.CurrentCalibrationMode);
	}

	private static void OnScene(SceneView sceneView){
		Handles.PositionHandle (new Vector3 (0, 0, 0), Quaternion.identity);


		//pupilTracker.CurrentCalibrationMode

		Handles.BeginGUI ();
		pupilTracker.editedCalibIndex = EditorGUILayout.Popup (pupilTracker.editedCalibIndex, new string[]{ "asd", "asdfdg" });
		Handles.EndGUI ();

	}
}