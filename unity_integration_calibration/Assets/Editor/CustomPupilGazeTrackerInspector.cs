
/// Custom Inspector Script for the PupilGazeTracker script.
/// There are four custom Style variables exposed from PupilGazeTracker: MainTabsStyle, SettingsLabelsStyle, SettingsValuesStyle, LogoStyle.
/// These are not accessable by default, to gain access, please go to Settings/ShowAll (this toggle will be removed in public version).



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

	}

	public override void OnInspectorGUI(){
		


		GUILayout.Space (20);

		//At first I have declared my Styles here, now they are stored in the PupilGazeTracker (target), as public accessable Styles. This is subject to delete.
		////////STYLES////////
//		GUIStyle parameterStyle = new GUIStyle();
//		parameterStyle = GUI.skin.textArea;
//		parameterStyle.alignment = TextAnchor.MiddleCenter;
//		//parameterStyle.normal.background = MakeTexture (100, 200, new Color (1, 1, 1, 0.2f));
//		parameterStyle.fontSize = 15;
//
//		GUIStyle parameterLabelStyle = new GUIStyle ();
//		parameterLabelStyle.alignment = TextAnchor.MiddleLeft;
//		//parameterLabelStyle.normal.background = MakeTexture (600, 1, new Color (1, 1, 1, 0.5f));
//		parameterLabelStyle.fontSize = 15;
//
//		GUIStyle logoStyle = new GUIStyle ();
//		logoStyle.alignment = TextAnchor.UpperCenter;
		////////STYLES////////


		////////LABEL WITH STYLE////////
		Object logo = Resources.Load("PupilLabsLogo") as Texture;
		GUILayout.Label (logo as Texture, pupilTracker.LogoStyle);
		////////LABEL WITH STYLE////////

		////////DRAW TAB MENU SYSTEM////////
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
		pupilTracker.tab = GUILayout.Toolbar (pupilTracker.tab, new string[]{ "Main Menu", "Settings", "Calibration" });
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

		GUILayout.Space (30);

		switch (pupilTracker.tab) {
		////////MAIN MENU////////
		case 0:
			DrawMainMenu ();
			break;
		////////SETTINGS////////
		case 1:
			DrawSettings ();
			break;
		////////CALIBRATION////////
		case 2:
			DrawCalibration ();
			break;
		}
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

		////////INPUT FIELDS////////

		/// Accessing the relevant GUIStyles from PupilGazeTracker
		GUILayout.BeginHorizontal();
		GUILayout.Label ("Server IP : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
		pupilTracker.ServerIP = EditorGUILayout.TextArea (pupilTracker.ServerIP, pupilTracker.SettingsValuesStyle, GUILayout.Width (150), GUILayout.Height (22));
		GUILayout.EndHorizontal ();

		EditorGUILayout.Separator ();

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Service Port : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
		pupilTracker.ServicePort = EditorGUILayout.IntField (pupilTracker.ServicePort, pupilTracker.SettingsValuesStyle, GUILayout.Width(150), GUILayout.Height(22));
		GUILayout.EndHorizontal ();

		GUILayout.Space (5);

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Calibration Count : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
		pupilTracker.DefaultCalibrationCount = EditorGUILayout.IntSlider (pupilTracker.DefaultCalibrationCount,1,120);
		GUILayout.EndHorizontal ();

		EditorGUILayout.Separator ();
		GUILayout.Space (5);

		GUILayout.BeginHorizontal();
		GUILayout.Label ("Show All : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
		pupilTracker.ShowBaseInspector = EditorGUILayout.Toggle (pupilTracker.ShowBaseInspector);
		GUILayout.EndHorizontal ();

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
		pupilTracker.calibrationMode = GUILayout.Toolbar (pupilTracker.calibrationMode, new string[]{ "2D", "3D" });

		GUILayout.Button("Calibrate", GUILayout.Height(35));
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
