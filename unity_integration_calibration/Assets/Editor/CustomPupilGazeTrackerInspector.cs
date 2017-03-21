
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
	bool isConnected = false;
	bool isCalibrating = false;

	Camera CalibEditorCamera;


	void OnEnable(){
	
		pupilTracker = (PupilGazeTracker)target;
		pupilTracker.AdjustPath ();


//		if (pupilTracker._calibPoints == null) {
//			pupilTracker._calibPoints = new CalibPoints ();
//			Debug.Log ("_calib Points are null");
//		}
			
		EditorApplication.update += CheckConnection;

//		pupilTracker.OnConnected -= OnConnected;
//		pupilTracker.OnConnected += OnConnected;

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
	void OnSceneGUI(){
		//CalibrationPointsEDitor.DrawCalibrationPointsEditor ();

		//	.Popup (1, new string[]{ "asd", "asdfdg" });
	}
	public override void OnInspectorGUI(){
		
		pupilTracker.FoldOutStyle = GUI.skin.GetStyle ("Foldout");
		//pupilTracker.ButtonStyle = GUI.skin.button;
		//pupilTracker.TextField = GUI.skin.textField;

		//EditorGUI.Vector2Field


		//GUI.skin.font = GUI.skin.button.font;
		//EditorGUILayout.fo
		GUILayout.Space (20);

		////////LABEL WITH STYLE////////
		System.Object logo = Resources.Load("PupilLabsLogo") as Texture;
		GUILayout.Label (logo as Texture, pupilTracker.LogoStyle);
		////////LABEL WITH STYLE////////

		if (pupilTracker.connectionMode == 0) {
			GUILayout.Label ("Connecting to localHost", pupilTracker.LogoStyle);
		} else {
			GUILayout.Label ("Connecting to Remote : " + pupilTracker.ServerIP, pupilTracker.LogoStyle);
		}


		////////DRAW TAB MENU SYSTEM////////
		EditorGUI.BeginChangeCheck();

		GUILayout.Label ("DEV", pupilTracker.SettingsLabelsStyle, GUILayout.Width(50));
		pupilTracker.ShowBaseInspector = EditorGUILayout.Toggle (pupilTracker.ShowBaseInspector);

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
		GUILayout.BeginHorizontal ();
		pupilTracker.printSampling = GUILayout.Toggle (pupilTracker.printSampling, "Print At Sampling");
		pupilTracker.printMessage = GUILayout.Toggle (pupilTracker.printMessage, "Print Messages");
		pupilTracker.printMessageType = GUILayout.Toggle (pupilTracker.printMessageType, "Print Messages Type");
		GUILayout.EndHorizontal ();

		if (pupilTracker.isDebugFoldout) {
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Start Pupil Service"))
				pupilTracker.RunServiceAtPath ();
			if (GUILayout.Button ("Stop Pupil Service"))
				pupilTracker.StopService ();
			EditorGUILayout.EndHorizontal ();
			if (GUILayout.Button ("Draw Calibration Points Editor"))
				CalibrationPointsEditor.DrawCalibrationPointsEditor (pupilTracker);
		}
		////////BUTTONS FOR DEBUGGING TO COMMENT IN PUBLIC VERSION////////

	}
	private void DrawSettings(){

		// test for changes in exposed values
		EditorGUI.BeginChangeCheck();
		pupilTracker.SettingsTab = GUILayout.Toolbar (pupilTracker.SettingsTab, new string[]{ "Service", "Calibration" });
		////////INPUT FIELDS////////
		switch (pupilTracker.SettingsTab) {
		case 0://SERVICE
			if (isCalibrating) {
				GUI.enabled = false;
			}

			GUILayout.Space (30);
			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////BROWSE FOR PUPIL SERVICE PATH
			GUILayout.Label ("Pupil Service App Path : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (150));
			pupilTracker.PupilServicePath = EditorGUILayout.TextArea (pupilTracker.PupilServicePath, pupilTracker.SettingsBrowseStyle, GUILayout.Width (150), GUILayout.Height (22));
			if (GUILayout.Button ("Browse")) {
				pupilTracker.PupilServicePath = EditorUtility.OpenFolderPanel ("TITLE", pupilTracker.PupilServicePath, "Default Name !");
			}
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Server IP : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (150));
			pupilTracker.ServerIP = EditorGUILayout.TextArea (pupilTracker.ServerIP, pupilTracker.SettingsValuesStyle, GUILayout.Width (150), GUILayout.Height (22));
			if (GUILayout.Button ("Default")) {
				pupilTracker.ServerIP = "127.0.0.1";
				Repaint ();
				GUI.FocusControl ("");
			}
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Service Port : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (150));
			pupilTracker.ServicePort = EditorGUILayout.IntField (pupilTracker.ServicePort, pupilTracker.SettingsValuesStyle, GUILayout.Width (150), GUILayout.Height (22));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (5);//------------------------------------------------------------//

			GUI.enabled = true;

			GUI.color = new Color (.7f, .7f, .7f, 1f);
			pupilTracker.connectionMode = GUILayout.Toolbar (pupilTracker.connectionMode, new string[]{ "Local", "Remote" }, GUILayout.Height(50));
			GUI.color = Color.white;

			GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Show All : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width(150));
			pupilTracker.ShowBaseInspector = EditorGUILayout.Toggle (pupilTracker.ShowBaseInspector);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			break;
		case 1://CALIBRATION
			
//			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
//			GUILayout.Label ("GameObject for 3D calibration : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (200));
//			pupilTracker.CalibrationGameObject3D = (GameObject)EditorGUILayout.ObjectField (pupilTracker.CalibrationGameObject3D, typeof(GameObject), true);
//			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////
//
//			GUILayout.Space (5);//------------------------------------------------------------//
//
//			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
//			GUILayout.Label ("GameObject for 2D calibration : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (200));
//			pupilTracker.CalibrationGameObject2D = (GameObject)EditorGUILayout.ObjectField (pupilTracker.CalibrationGameObject2D, typeof(GameObject), true);
//			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////
//
			GUILayout.Space (30);

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Calibration Sample Count : ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (175));
			pupilTracker.DefaultCalibrationCount = EditorGUILayout.IntSlider (pupilTracker.DefaultCalibrationCount, 1, 120);
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (10);//------------------------------------------------------------//

			//EditorGUI.indentLevel = 0;

			//pupilTracker.ButtonStyle.
			//////////////////////////CALIBRATION POINTS//////////////////////////
			pupilTracker.CalibrationPointsFoldout = EditorGUILayout.Foldout (pupilTracker.CalibrationPointsFoldout," Calibration Points", pupilTracker.FoldOutStyle);
			GUILayout.Space (10);//------------------------------------------------------------//
			if (pupilTracker.CalibrationPointsFoldout) {
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
				GUILayout.BeginHorizontal ();
				pupilTracker.CalibrationPoints2DFoldout = EditorGUILayout.Foldout (pupilTracker.CalibrationPoints2DFoldout, "2D", pupilTracker.FoldOutStyle);
				if (GUILayout.Button ("Add 2D Calibration Point")) {
					pupilTracker.Add2DCalibrationPoint (new floatArray(){axisValues = new float[]{0f,0f}});
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (7);//------------------------------------------------------------//

				if (pupilTracker.CalibrationPoints2DFoldout) {//////2D POINTS//////
					foreach (floatArray _point in pupilTracker._calibPoints.Get2DList()) {
						int index = pupilTracker._calibPoints.Get2DList().IndexOf (_point);
						GUILayout.BeginHorizontal ();
						//++EditorGUI.indentLevel;
						Vector3 _v2 = new Vector2 (_point.axisValues [0], _point.axisValues [1]);

						_v2 = EditorGUILayout.Vector2Field ("2D Point " + (index + 1) + " : ", _v2);

						_point.axisValues [0] = _v2.x;
						_point.axisValues [1] = _v2.y;

						if (GUILayout.Button ("Remove")) {
							pupilTracker._calibPoints.Remove2DPoint (_point);
							Repaint ();
							break;
						}
						if (GUILayout.Button ("Edit")) {
							CalibrationPointsEditor.DrawCalibrationPointsEditor (pupilTracker, index, 0, new Vector3 (_v2.x, _v2.y, 0));
						}

						GUILayout.EndHorizontal ();
						GUILayout.Space (2);//------------------------------------------------------------//
					}
				}
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
				GUILayout.BeginHorizontal ();
				pupilTracker.CalibrationPoints3DFoldout = EditorGUILayout.Foldout (pupilTracker.CalibrationPoints3DFoldout, "3D", pupilTracker.FoldOutStyle);
				if (GUILayout.Button ("Add 3D Calibration Point")) {
					pupilTracker.Add3DCalibrationPoint (new floatArray(){axisValues = new float[]{0f,0f,0f}});
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (7);//------------------------------------------------------------//
				if (pupilTracker.CalibrationPoints3DFoldout) {//////3D POINTS//////
					foreach (floatArray _point in pupilTracker._calibPoints.Get3DList()) {
						int index = pupilTracker._calibPoints.Get3DList().IndexOf (_point);
						GUILayout.BeginHorizontal ();

						Vector3 _v3 = new Vector3 (_point.axisValues [0], _point.axisValues [1], _point.axisValues [2]);

						//EditorGUI.InspectorTitlebar (Rect (0, 0, 200, 20), new UnityEngine.Object[]());


						_v3 = EditorGUILayout.Vector3Field ("3D Point " + (index + 1) + " :", _v3);

						_point.axisValues [0] = _v3.x;
						_point.axisValues [1] = _v3.y;
						_point.axisValues [2] = _v3.z;

						if (GUILayout.Button ("Remove")) {
							pupilTracker._calibPoints.Remove3DPoint (_point);
							Repaint ();
							break;
						}
						if (GUILayout.Button ("Edit")) {
							CalibrationPointsEditor.DrawCalibrationPointsEditor (pupilTracker, index, 1, _v3);
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (2);//------------------------------------------------------------//
					}
				}
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
			}
			//////////////////////////CALIBRATION POINTS//////////////////////////

			break;
		}

		/// Accessing the relevant GUIStyles from PupilGazeTracker

		////////INPUT FIELDS////////

		//if change found set scene as dirty, so user will have to save changed values.
		if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
		}

		GUILayout.Space (50);

	}

	private void DrawCalibration(){
		EditorGUI.BeginChangeCheck ();

		if (isCalibrating) {
			GUI.enabled = false;
		} else {
			GUI.enabled = true;
		}
			
		pupilTracker.calibrationMode = GUILayout.Toolbar (pupilTracker.calibrationMode, new string[]{ "2D", "3D" });
		GUI.enabled = true;
		if (EditorGUI.EndChangeCheck ()) {
			pupilTracker.SwitchCalibrationMode ();
		}

		if (pupilTracker.calibrationMode == 1) {
			EditorGUI.BeginChangeCheck ();
			pupilTracker.calibrationDebugMode = GUILayout.Toggle (pupilTracker.calibrationDebugMode, "Calibration Debug Mode", "Button");
			if (EditorGUI.EndChangeCheck ()) {
				if (pupilTracker.calibrationDebugMode) {
					pupilTracker.OnCalibDebug -= pupilTracker.DrawCalibrationDebug;
					pupilTracker.OnCalibDebug += pupilTracker.DrawCalibrationDebug;
				} else {
					pupilTracker.OnCalibDebug -= pupilTracker.DrawCalibrationDebug;
				}
			}
			//GUILayout.Label ("Debug Mode ON");
		}




		if (isConnected) {
			if (!isCalibrating) {
				if (GUILayout.Button ("Calibrate", GUILayout.Height (35))) {
					if (Application.isPlaying) {
						pupilTracker.StartCalibration ();
						EditorApplication.update += CheckCalibration;
					} else {
						EditorUtility.DisplayDialog ("Pupil service message", "You can only use calibration in playmode", "Understood");
					}
				}
			} else {
				if (GUILayout.Button ("Stop Calibration", GUILayout.Height (35))) {
					pupilTracker.StopCalibration ();
				}
			}
		} else {
			GUI.enabled = false;
			GUILayout.Button ("Calibrate (Not Connected !)", GUILayout.Height (35));
		}


		GUI.enabled = true;
		
	}

	public void CheckConnection(){
		if (pupilTracker._isFullConnected) {
			Debug.Log ("Connection Established");
			isConnected = true;
			EditorApplication.update = null;
		}
	}
	public void CheckCalibration(){
		Debug.Log ("Editor Update");
		if (pupilTracker.m_status == PupilGazeTracker.EStatus.Calibration) {
			isCalibrating = true;
		} else {
			isCalibrating = false;
			EditorApplication.update = null;
		}
	}



	public static Vector3 Vector3FieldEx(string label, Vector3 value, params GUILayoutOption[] options) {
		GUILayout.Label(label);

		EditorGUILayout.BeginHorizontal();
		++EditorGUI.indentLevel;

		EditorGUILayout.LabelField("X", GUILayout.Width(22));
		value.x = EditorGUILayout.FloatField(value.x);

		--EditorGUI.indentLevel;

		EditorGUILayout.LabelField("Y", GUILayout.Width(22));
		value.y = EditorGUILayout.FloatField(value.y);

		EditorGUILayout.LabelField("Z", GUILayout.Width(22));
		value.z = EditorGUILayout.FloatField(value.z);

		EditorGUILayout.EndHorizontal();

		return value;
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

//	void DrawCalibPointsEditor(){
//
//		GUILayout.Box ("");
//		//Rect R = GUILayoutUtility.GetLastRect ();
//
//		if (Event.current.type == EventType.Repaint) {
//			Rect sceneRect = GUILayoutUtility.GetLastRect ();
//			sceneRect.y += 20;
//
//			CalibEditorCamera.pixelRect = sceneRect;
//			CalibEditorCamera.Render ();
//
//		}
//		Handles.SetCamera (CalibEditorCamera);
//	}






}

