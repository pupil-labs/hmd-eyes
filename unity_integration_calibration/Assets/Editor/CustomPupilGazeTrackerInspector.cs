
/// Custom Inspector Script for the PupilGazeTracker script.
/// There are four custom Style variables exposed from PupilGazeTracker: MainTabsStyle, SettingsLabelsStyle, SettingsValuesStyle, LogoStyle.
/// These are not accessable by default, to gain access, please go to Settings/ShowAll (this toggle will be removed in public version).


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Input;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.VR.WSA.Sharing;
using UnityEngine.VR.WSA.WebCam;
using UnityEditor;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(PupilGazeTracker))]
public class CustomPupilGazeTrackerInspector : Editor {

	PupilGazeTracker pupilTracker;

//	private static bool isMouseDown = false;
	bool isConnected = false;
	bool isCalibrating = false;
	string tempServerIP;

	Camera CalibEditorCamera;


	void OnEnable(){


		pupilTracker = (PupilGazeTracker)target;
		pupilTracker.AdjustPath ();

		tempServerIP = pupilTracker.ServerIP;
			
		EditorApplication.update += CheckConnection;

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
			}
		}

	}


	public override void OnInspectorGUI(){

//		Event e = Event.current;
//
//		switch (e.type) {
//		case EventType.MouseUp:
//			isMouseDown = false;
//			break;
//		case EventType.MouseDown:
//			isMouseDown = true;
//			break;
//		}
			
//		pupilTracker.FoldOutStyle = GUI.skin.GetStyle ("Foldout");

		GUILayout.Space (20);

		////////LABEL WITH STYLE////////
		System.Object logo = Resources.Load("PupilLabsLogo") as Texture;
		GUILayout.Label (logo as Texture, pupilTracker.Styles[9]);
		////////LABEL WITH STYLE////////

		GUILayout.Space (20);

		////////DRAW TAB MENU SYSTEM////////
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.BeginHorizontal ();

		//////////////////////////////////////STATUS FIELD//////////////////////////////////////
		if (isConnected) {
			GUI.color = Color.green;
			if (pupilTracker.connectionMode == 0) {
				GUILayout.Label ("Connected to localHost", pupilTracker.Styles[11]);
			} else {
				GUILayout.Label ("Connected to Remote : " + pupilTracker.ServerIP, pupilTracker.Styles[11]);
			}

		} else {
			if (pupilTracker.connectionMode == 0) {
				GUILayout.Label ("Connecting to localHost", pupilTracker.Styles[11]);
			} else {
				GUILayout.Label ("Connecting to Remote : " + pupilTracker.ServerIP, pupilTracker.Styles[11]);
			}
		}
		GUI.color = Color.white;

		Texture2D eyeIcon = Resources.Load("eye") as Texture2D;
		if (Pupil.processStatus.eyeProcess0) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilTracker.Styles[12], GUILayout.Width (20), GUILayout.Height (20));
		GUILayout.Space (5);
		if (Pupil.processStatus.eyeProcess1) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilTracker.Styles[12], GUILayout.Width (20), GUILayout.Height (20));
		GUI.color = Color.white;

		EditorGUILayout.EndHorizontal ();
		//////////////////////////////////////STATUS FIELD\//////////////////////////////////////

		//////////////////////////////////////DEVELOPER MODE TOGGLE//////////////////////////////////////
		GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////
		GUI.skin.label.fontSize = 10;
		GUILayout.Label ("developer mode", GUILayout.Width(75));
		GUI.skin.label = null;
		pupilTracker.AdvancedSettings = EditorGUILayout.Toggle (pupilTracker.AdvancedSettings);
		GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////
		//////////////////////////////////////DEVELOPER MODE TOGGLE\//////////////////////////////////////
		//base.OnInspectorGUI ();
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		pupilTracker.tab = GUILayout.Toolbar (pupilTracker.tab, new string[]{ "Main", "Settings"}, GUILayout.Height(35));
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
			}
		}

		if (pupilTracker.DrawMenu != null)
			pupilTracker.DrawMenu ();
		
		////////DRAW TAB MENU SYSTEM////////
		GUILayout.Space (50);

		GUI.skin = null;
	}
	public float y;
	private void DrawMainMenu(){

		Event e = Event.current;

		GUILayout.Space (10);

		////////////////////////////CALIBRATE BUTTON////////////////////////////
		if (isConnected) {
			if (!isCalibrating) {
				if (GUILayout.Button ("Calibrate", GUILayout.Height (50))) {
					if (Application.isPlaying) {
						pupilTracker.StartCalibration ();
						EditorApplication.update += CheckCalibration;
					} else {
						EditorUtility.DisplayDialog ("Pupil service message", "You can only use calibration in playmode", "Understood");
					}
				}
			} else {
				if (GUILayout.Button ("Stop Calibration", GUILayout.Height (50))) {
					pupilTracker.StopCalibration ();
				}
			}
		} else {
			GUI.enabled = false;
			GUILayout.Button ("Calibrate (Not Connected !)", GUILayout.Height (50));
		}
		GUI.enabled = true;
		////////////////////////////CALIBRATE BUTTON////////////////////////////

		GUILayout.Space (5);

		////////////////////////////RECORDING BUTTON////////////////////////////
		EditorGUI.BeginChangeCheck ();
		Recording.variables.isRecording = GUILayout.Toggle (Recording.variables.isRecording, "Recording", "Button", GUILayout.Height(50));
		GUI.backgroundColor = Color.white;
		if (EditorGUI.EndChangeCheck ()) {
			if (Recording.variables.isRecording) {
				pupilTracker.StartRecording ();
				EditorApplication.update += CheckRecording;
			} else {
				pupilTracker.StopRecording ();
			}
		}
		////////////////////////////RECORDING BUTTON////////////////////////////

		GUILayout.Space (5);

		////////////////////////////OPERATOR MONITOR BUTTON////////////////////////////
		EditorGUI.BeginChangeCheck ();
		pupilTracker.isOperatorMonitor = GUILayout.Toggle (pupilTracker.isOperatorMonitor, "Operator Monitor", "Button", GUILayout.MinWidth (100), GUILayout.Height (50));
		if (EditorGUI.EndChangeCheck ()) {
			if (pupilTracker.isOperatorMonitor) {
				pupilTracker.CloseCalibrationDebugView();
				//				Debug.Log("instantiate operator monitor");
				OperatorMonitor.Instantiate ();
			} else {
				if (pupilTracker.OperatorMonitorProperties[0].OperatorCamera != null)
					OperatorMonitor.Instance.ExitOperatorMonitor ();				
			}
		}
		////////////////////////////OPERATOR MONITOR BUTTON////////////////////////////

		GUILayout.Space (10);

		GUI.skin = default(GUISkin);

		GUILayout.Space (100);

		GUI.depth = 0;
		GUI.color = Color.white;
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		GUI.depth = 1;
		GUI.color = Color.white;

		if (pupilTracker.AdvancedSettings) {
			if (GUILayout.Button ("IsConnected")) {
				isConnected = true;
			}
			pupilTracker.DebugVariables.packetsOnMainThread = GUILayout.Toggle (pupilTracker.DebugVariables.packetsOnMainThread, "Process Packets on Main Thread", "Button", GUILayout.MinWidth (100));

			GUI.backgroundColor = Color.white;
			GUILayout.Space (10);

			pupilTracker.DebugVariables.printSampling = GUILayout.Toggle (pupilTracker.DebugVariables.printSampling, "Print Sampling", "Button");

			pupilTracker.DebugVariables.printMessage = GUILayout.Toggle (pupilTracker.DebugVariables.printMessage, "Print Msg", "Button");

			pupilTracker.DebugVariables.printMessageType = GUILayout.Toggle (pupilTracker.DebugVariables.printMessageType, "Print Msg Types", "Button");

			pupilTracker.DebugVariables.subscribeAll = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeAll, "Subscribe to all", "Button");

			pupilTracker.DebugVariables.subscribeFrame = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeFrame, "Subscribe to frame.", "Button");

			pupilTracker.DebugVariables.subscribeGaze = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeGaze, "Subscribe to gaze.", "Button");

			pupilTracker.DebugVariables.subscribeNotify = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeNotify, "Subscribe to notifications.", "Button");

		}


	}
	private void DrawSettings(){

		GUILayout.Space (10);

		// test for changes in exposed values
		EditorGUI.BeginChangeCheck();
		pupilTracker.SettingsTab = GUILayout.Toolbar (pupilTracker.SettingsTab, new string[] {
			"pupil app",
			"calibration",
			"recording"
		}, GUILayout.Height(30));
		////////INPUT FIELDS////////
		switch (pupilTracker.SettingsTab) {
		case 0://PUPIL APP
			if (isCalibrating) {
				GUI.enabled = false;
			}

			GUILayout.Space (10);

			////////////////////////////CONNECTION MODE////////////////////////////
			EditorGUI.BeginChangeCheck ();
			//GUI.color = new Color (.7f, .7f, .7f, 1f);
			pupilTracker.connectionMode = GUILayout.Toolbar (pupilTracker.connectionMode, new string[]{ "Local", "Remote" }, GUILayout.Height (30), GUILayout.MinWidth (25));
			GUI.color = Color.white;
			if (EditorGUI.EndChangeCheck ()) {
				if (pupilTracker.connectionMode == 0) {
					tempServerIP = pupilTracker.ServerIP;
					pupilTracker.ServerIP = "127.0.0.1";
				} else {
					pupilTracker.ServerIP = tempServerIP;
				}
			}
			////////////////////////////CONNECTION MODE////////////////////////////
			GUILayout.Space (5);
			if (pupilTracker.connectionMode == 0) {//LOCAL CONNECTION MODE//
				////////////////////////////PUPIL APP PATH////////////////////////////
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("path : ", pupilTracker.Styles [6], GUILayout.MinWidth (50));
				pupilTracker.PupilServicePath = EditorGUILayout.TextArea (pupilTracker.PupilServicePath, pupilTracker.Styles [7], GUILayout.MinWidth (100), GUILayout.Height (22));
				if (GUILayout.Button ("Browse")) {
					pupilTracker.PupilServicePath = EditorUtility.OpenFilePanel ("Select Pupil service application file", pupilTracker.PupilServicePath, "exe");
				}
				GUILayout.EndHorizontal ();
				////////////////////////////PUPIL APP PATH////////////////////////////
			}

			if (pupilTracker.connectionMode == 1) {//---------REMOTE CONNECTION MODE---------//

				if (pupilTracker.AdvancedSettings){//ADVANCED SETTING
					
					////////////////////////////SERVICE PORT////////////////////////////
					/// 
					GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
					//
					GUILayout.Label ("Service Port : ", pupilTracker.Styles[6], GUILayout.MinWidth (50));
					pupilTracker.ServicePort = EditorGUILayout.IntField (pupilTracker.ServicePort, pupilTracker.Styles[8], GUILayout.MinWidth (100), GUILayout.Height (22));
					//
					GUILayout.EndHorizontal ();//---------HORIZONTAL GROUP\---------//
					///
					////////////////////////////SERVICE PORT\////////////////////////////
					base.OnInspectorGUI ();
					GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line

				}//ADVANCED SETTING\

				GUILayout.Space (5);

				////////////////////////////SERVER IP ADDRESS////////////////////////////
				/// 
				GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
				//
				GUILayout.Label ("IP : ", pupilTracker.Styles [6], GUILayout.MinWidth (50));
				//
				pupilTracker.ServerIP = EditorGUILayout.TextArea (pupilTracker.ServerIP, pupilTracker.Styles[8], GUILayout.MinWidth (50), GUILayout.Height (22));
				if (GUILayout.Button ("Default")) {
					pupilTracker.ServerIP = "127.0.0.1";
					Repaint ();
					GUI.FocusControl ("");
				}
				//
				GUI.enabled = true;
				//
				GUILayout.EndHorizontal ();//---------HORIZONTAL GROUP\---------//
				///
				////////////////////////////SERVER IP ADDRESS\////////////////////////////


				GUI.enabled = true;

			}//---------REMOTE CONNECTION MODE\---------//


			break;
		case 1://CALIBRATION
			

			GUILayout.Space (20);

			////////////////////////////2D-3D TOGGLE BAR////////////////////////////
			EditorGUI.BeginChangeCheck ();
			pupilTracker.calibrationMode = GUILayout.Toolbar (pupilTracker.calibrationMode, new string[]{ "2D", "3D" });
			GUI.enabled = true;
			if (EditorGUI.EndChangeCheck ()) {
				pupilTracker.SwitchCalibrationMode ();
			}
			////////////////////////////2D-3D TOGGLE BAR////////////////////////////

			////////////////////////////CALIBRATION DEBUG MODE////////////////////////////
			if (isCalibrating || !isConnected || pupilTracker.calibrationMode != 1)
				GUI.enabled = false;

			EditorGUI.BeginChangeCheck ();
			pupilTracker.calibrationDebugMode = GUILayout.Toggle (pupilTracker.calibrationDebugMode, "Calibration Debug Mode", "Button");
			GUI.enabled = true;
			if (EditorGUI.EndChangeCheck ()) {
				if (pupilTracker.calibrationDebugMode) {
					if (pupilTracker.OperatorMonitorProperties [0].OperatorCamera != null)
						OperatorMonitor.Instance.ExitOperatorMonitor ();
					pupilTracker.StartCalibrationDebugView ();

				} else {
					pupilTracker.CloseCalibrationDebugView ();
				}
			}

			if (pupilTracker.calibrationDebugMode) {
				//				pupilTracker.calibrationDebugCamera = (PupilGazeTracker.CalibrationDebugCamera) EditorGUILayout.EnumPopup (pupilTracker.calibrationDebugCamera);
				GUILayout.BeginHorizontal ();
				EditorGUI.BeginChangeCheck ();
				pupilTracker.DebugViewVariables.isDrawLines = GUILayout.Toggle (pupilTracker.DebugViewVariables.isDrawLines, " Draw Debug Lines ", "Button");
				pupilTracker.DebugViewVariables.isDrawPoints = GUILayout.Toggle (pupilTracker.DebugViewVariables.isDrawPoints, " Draw Debug Points ", "Button");
				if (EditorGUI.EndChangeCheck ()) {
					pupilTracker.SetDrawCalibrationPointCloud (pupilTracker.DebugViewVariables.isDrawPoints);
					pupilTracker.SetDrawCalibrationLines (pupilTracker.DebugViewVariables.isDrawLines);
				}
				GUILayout.EndHorizontal ();
			}

			GUI.enabled = true;
			////////////////////////////CALIBRATION DEBUG MODE////////////////////////////

			GUILayout.Space (20);

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Samples ", pupilTracker.Styles[6], GUILayout.MinWidth (35));
			pupilTracker.DefaultCalibrationCount = EditorGUILayout.IntSlider (pupilTracker.DefaultCalibrationCount, 1, 120, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (10);//------------------------------------------------------------//

			if (pupilTracker.AdvancedSettings){

				base.OnInspectorGUI ();
			}
			break;
		}


		//if change found set scene as dirty, so user will have to save changed values.
		if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
		}

		GUILayout.Space (10);

	}

	public void CheckConnection(){
		if (Pupil.processStatus.eyeProcess0 || Pupil.processStatus.eyeProcess1) {
			if (Pupil.processStatus.initialized) {
				EditorApplication.update -= CheckConnection;
			}
			isConnected = true;
		}
		Repaint ();
	}
	public void CheckCalibration(){
//		Debug.Log ("Editor Update : Check Calibration");
		if (pupilTracker.m_status == PupilGazeTracker.EStatus.Calibration) {
			isCalibrating = true;
		} else {
			isCalibrating = false;
			EditorApplication.update -= CheckCalibration;
		}
	}
	public void CheckRecording(){
		if (!Recording.variables.isRecording) {
			EditorApplication.update -= CheckRecording;
			Repaint ();
		}
	}
		
	void OnApplicationQuit(){
		EditorApplication.update = null;
	}


}

