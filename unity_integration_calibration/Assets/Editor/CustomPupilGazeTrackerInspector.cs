
/// Custom Inspector Script for the PupilGazeTracker script.
/// There are four custom Style variables exposed from PupilGazeTracker: MainTabsStyle, SettingsLabelsStyle, SettingsValuesStyle, LogoStyle.
/// These are not accessable by default, to gain access, please go to Settings/ShowAll (this toggle will be removed in public version).


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(PupilGazeTracker))]
public class CustomPupilGazeTrackerInspector : Editor {

	PupilGazeTracker pupilTracker;
		
	void OnEnable(){
	
		pupilTracker = (PupilGazeTracker)target;
		pupilTracker.AdjustPath ();

		if (pupilTracker.DrawMenu == null) {
			switch (pupilTracker.tab) {
			case 0:////////MAIN MENU////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawMainMenu;
				break;
			case 1:////////SETTINGS////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawSettings;
				break;
			case 2:////////CALIBRATION////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawCalibration;
				break;
			}
		}

	}

	public override void OnInspectorGUI(){
		


		GUILayout.Space (20);

		////////LABEL WITH STYLE////////
		System.Object logo = Resources.Load("PupilLabsLogo") as Texture;
		GUILayout.Label (logo as Texture, pupilTracker.LogoStyle);
		////////LABEL WITH STYLE////////

		////////DRAW TAB MENU SYSTEM////////
		EditorGUI.BeginChangeCheck();
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		pupilTracker.tab = GUILayout.Toolbar (pupilTracker.tab, new string[]{ "Main Menu", "Settings", "Calibration" });
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		if (EditorGUI.EndChangeCheck ()) {//I delegates are used to assign the relevant menu to be drawn. This way I can fire off something on tab change.
			switch (pupilTracker.tab) {
			case 0:////////MAIN MENU////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawMainMenu;
				break;
			case 1:////////SETTINGS////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawSettings;
				break;
			case 2:////////CALIBRATION////////
				pupilTracker.DrawMenu = null;
				pupilTracker.DrawMenu += DrawCalibration;

				try{
				pupilTracker.CalibrationGameObject2D = GameObject.Find ("Calibrator").gameObject.transform.FindChild ("2D Calibrator").gameObject;
				pupilTracker.CalibrationGameObject3D = GameObject.Find ("Calibrator").gameObject.transform.FindChild ("3D Calibrator").gameObject;
				}
				catch{
					EditorUtility.DisplayDialog ("Pupil Service Warning", "Calibrator prefab cannot be found, or not complete, please add under main camera ! ", "Will Do");
				}


				break;
			}
		}

		if (pupilTracker.DrawMenu != null)
			pupilTracker.DrawMenu ();
		
		////////DRAW TAB MENU SYSTEM////////
		GUILayout.Space (50);

		//Show default Inspector with all exposed variables
		if (pupilTracker.ShowBaseInspector)
			base.OnInspectorGUI ();


	}

	private void DrawMainMenu(){

		////////BUTTONS FOR DEBUGGING TO COMMENT IN PUBLIC VERSION////////
		pupilTracker.isDebugFoldout = EditorGUILayout.Foldout (pupilTracker.isDebugFoldout, "Debug Buttons");
		if (pupilTracker.isDebugFoldout) {
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Start Pupil Service"))
				pupilTracker.RunServiceAtPath ();
			if (GUILayout.Button ("Stop Pupil Service"))
				pupilTracker.StopService ();
			EditorGUILayout.EndHorizontal ();
		}
		////////BUTTONS FOR DEBUGGING TO COMMENT IN PUBLIC VERSION////////

	}
	private void DrawSettings(){

		// test for changes in exposed values
		EditorGUI.BeginChangeCheck();

		pupilTracker.SettingsTab = GUILayout.Toolbar (pupilTracker.SettingsTab, new string[]{ "Service", "Calibration" });
		GUILayout.Space (30);
		////////INPUT FIELDS////////
		switch (pupilTracker.SettingsTab) {
		case 0://SERVICE
			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////BROWSE FOR PUPIL SERVICE PATH
			GUILayout.Label ("Pupil Service App Path : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
			pupilTracker.PupilServicePath = EditorGUILayout.TextArea (pupilTracker.PupilServicePath, pupilTracker.SettingsBrowseStyle, GUILayout.Width (150), GUILayout.Height (22));
			if (GUILayout.Button ("Browse", GUILayout.Width(55), GUILayout.Height(22))) {
				pupilTracker.PupilServicePath = EditorUtility.OpenFolderPanel ("TITLE", pupilTracker.PupilServicePath, "Default Name !");
			}
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Server IP : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
			pupilTracker.ServerIP = EditorGUILayout.TextArea (pupilTracker.ServerIP, pupilTracker.SettingsValuesStyle, GUILayout.Width (150), GUILayout.Height (22));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Service Port : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
			pupilTracker.ServicePort = EditorGUILayout.IntField (pupilTracker.ServicePort, pupilTracker.SettingsValuesStyle, GUILayout.Width(150), GUILayout.Height(22));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (5);//------------------------------------------------------------//

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Show All : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
			pupilTracker.ShowBaseInspector = EditorGUILayout.Toggle (pupilTracker.ShowBaseInspector);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			break;
		case 1://CALIBRATION
			
			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("GameObject for 3D calibration : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(200));
			pupilTracker.CalibrationGameObject3D = (GameObject)EditorGUILayout.ObjectField(pupilTracker.CalibrationGameObject3D, typeof(GameObject), true);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (5);//------------------------------------------------------------//

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("GameObject for 2D calibration : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(200));
			pupilTracker.CalibrationGameObject2D = (GameObject)EditorGUILayout.ObjectField(pupilTracker.CalibrationGameObject2D, typeof(GameObject), true);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Calibration Sample Count : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(175));
			pupilTracker.DefaultCalibrationCount = EditorGUILayout.IntSlider (pupilTracker.DefaultCalibrationCount,1,120);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////
			break;
		}

		/// Accessing the relevant GUIStyles from PupilGazeTracker

		////////INPUT FIELDS////////

		//if change found set scene as dirty, so user will have to save changed values.
		if (EditorGUI.EndChangeCheck())
		{
			EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
		}

		GUILayout.Space (50);

	}

	//TODO: Create 3D calibration method, access 2D calibration method
	private void DrawCalibration(){
		EditorGUI.BeginChangeCheck ();
		pupilTracker.calibrationMode = GUILayout.Toolbar (pupilTracker.calibrationMode, new string[]{ "2D", "3D" });
		if (EditorGUI.EndChangeCheck ()) {
			switch (pupilTracker.calibrationMode) {
			case 0:
				pupilTracker.CalibrationGameObject2D.SetActive (true);
				pupilTracker.CalibrationGameObject3D.SetActive (false);
				break;
			case 1:
				pupilTracker.CalibrationGameObject2D.SetActive (false);
				pupilTracker.CalibrationGameObject3D.SetActive (true);
				break;
			}
		}
		if (GUILayout.Button ("Calibrate", GUILayout.Height (35))) {
			if (Application.isPlaying) {
				pupilTracker.StartCalibration ();
			} else {
				EditorUtility.DisplayDialog ("Pupil service message", "You can only use calibration in playmode", "Understood");
			}
		}
		
	}


	/// Temporary function to create a texture based on a Color value. Most likely will be deleted.
	private Texture2D MakeTexture(int width, int height, Color color, bool border = false, int borderWidth = 2, Color borderColor = default(Color)){

		Color[] pixelArray;
		Color[,] pixelArray2D = new Color[width, height];
		List<Color> pixelList = new List<Color> ();

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				pixelArray2D [i, j] = color;
				if (border) {
					if (i < borderWidth || i > (width - borderWidth))
						pixelArray2D [i, j] = borderColor;
					if (j < borderWidth || j > (height - borderWidth))
						pixelArray2D [i, j] = borderColor;
				}
				pixelList.Add (pixelArray2D [i, j]);
			}
		}

		pixelArray = pixelList.ToArray ();

		Texture2D result = new Texture2D (width, height);
		result.SetPixels (pixelArray);
		result.Apply ();

		return result;
	}

}
