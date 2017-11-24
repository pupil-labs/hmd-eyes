
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

#if !UNITY_WSA
	PupilGazeTracker pupilTracker;

	private PupilSettings pupilSettings;

//	bool isConnected = false;
	bool isEyeProcessConnected = false;
	string tempServerIP;

	Camera CalibEditorCamera;
//	PupilGazeTracker.CustomInspector cInspector;

	void OnEnable(){



	}
	void OnDisable(){
		PupilTools.WantRepaint -= this.Repaint;
		Repaint ();
	}

	public static void AutoRunLayout(){

		PupilSettings pupilSettings = PupilTools.Settings;

		////////////////////////////TOGGLE AUTO MODE////////////////////////////

		PupilTools.Settings.connection.isAutorun = GUILayout.Toggle (PupilTools.Settings.connection.isAutorun, "Autorun Pupil Service", "Button");

		if (!PupilTools.Settings.connection.isAutorun && Application.isPlaying) {
		
			GUILayout.BeginHorizontal ();

			if (GUILayout.Button ("Start")) {

				Debug.Log ("start button before connect");
				PupilTools.Connect ();
				Debug.Log ("start button after connect");

//				if (PupilTools.Settings.connection.isLocal)
//					PupilTools.RunServiceAtPath ();
//
//
//				PupilDataReceiver.Instance.StartCoroutine (PupilDataReceiver.Instance.Connect (retry: true, retryDelay: 5f));



//				if (PupilTools.Settings.connection.isLocal) {
//				
//					PupilTools.RunServiceAtPath ();
//					PupilDataReceiver.Instance.StartCoroutine (PupilDataReceiver.Instance.Connect (retry: true, retryDelay: 5f));
//				
//				} else {
//				
//					PupilDataReceiver.Instance.StartCoroutine (PupilDataReceiver.Instance.Connect (retry: true, retryDelay: 5f));
//				
//				}

			}

			if (GUILayout.Button ("Stop"))
				PupilTools.StopEyeProcesses ();

			GUILayout.EndHorizontal ();
		}

		////////////////////////////TOGGLE AUTO MODE////////////////////////////
	}

	public override void OnInspectorGUI()
	{
		if (pupilTracker == null) 
		{
			Debug.Log ("fos");
			pupilTracker = (PupilGazeTracker)target;
//			pupilTracker.AdjustPath ();

			PupilTools.WantRepaint += this.Repaint;

			if (pupilTracker.Settings == null) 
			{
				pupilTracker.Settings = Resources.Load<PupilSettings> ("PupilSettings");
				pupilSettings = pupilTracker.Settings;
			} 
			else 
			{
				pupilSettings = pupilTracker.Settings;
			}

			tempServerIP = PupilTools.Settings.connection.IP;
			if (pupilTracker.DrawMenu == null) 
			{
				switch (pupilSettings.customGUIVariables.tabs.mainTab) 
				{
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
//		PupilSettings pupilSettings = PupilTools.GetPupilSettings ();
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

		GUILayout.Space (20);

		////////LABEL WITH STYLE////////
		System.Object logo = Resources.Load("pupil_labs_logotype") as Texture;
		GUILayout.Label (logo as Texture, pupilSettings.GUIStyles[0]);
		////////LABEL WITH STYLE////////

		GUILayout.Space (50);

		////////DRAW TAB MENU SYSTEM////////
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.BeginHorizontal ();

		//////////////////////////////////////STATUS FIELD//////////////////////////////////////
		if (PupilTools.Settings.connection.isConnected) {
			GUI.color = Color.green;
			if (PupilTools.Settings.connection.isLocal) {
				GUILayout.Label ("localHost ( Connected )", pupilSettings.GUIStyles[1]);
			} else {
				GUILayout.Label ("remote " + PupilTools.Settings.connection.IP + " ( Connected )" , pupilSettings.GUIStyles[1]);
			}

		} else {
			if (PupilTools.Settings.connection.isLocal) {
				GUILayout.Label ("localHost ( Not Connected )", pupilSettings.GUIStyles[1]);
			} else {
				GUILayout.Label ("remote " + PupilTools.Settings.connection.IP + " ( Not Connected )" , pupilSettings.GUIStyles[1]);
			}
		}
		GUI.color = Color.white;

		Texture2D eyeIcon = Resources.Load("eye") as Texture2D;
		if (Pupil.processStatus.eyeProcess0) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilSettings.GUIStyles[2], GUILayout.Width (20), GUILayout.Height (20));
		GUILayout.Space (5);
		if (Pupil.processStatus.eyeProcess1) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilSettings.GUIStyles[2], GUILayout.Width (20), GUILayout.Height (20));
		GUI.color = Color.white;

		EditorGUILayout.EndHorizontal ();
		//////////////////////////////////////STATUS FIELD\//////////////////////////////////////

		//////////////////////////////////////DEVELOPER MODE TOGGLE//////////////////////////////////////
		GUILayout.BeginHorizontal();////////////////////HORIZONTAL////////////////////

		GUI.skin.button.fontSize = 9;
		pupilSettings.customGUIVariables.bools.isAdvanced = GUILayout.Toggle (pupilSettings.customGUIVariables.bools.isAdvanced, "developer mode", "Button", GUILayout.Width(90));
		//GUI.skin.button.fontSize = 13;
		GUI.skin.button.fontSize = 12;

		GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////
		//////////////////////////////////////DEVELOPER MODE TOGGLE\//////////////////////////////////////
		//base.OnInspectorGUI ();
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		pupilSettings.customGUIVariables.tabs.mainTab = GUILayout.Toolbar (pupilSettings.customGUIVariables.tabs.mainTab, new string[]{ "Main", "Settings"}, GUILayout.Height(35));
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		if (EditorGUI.EndChangeCheck ()) {//I delegates are used to assign the relevant menu to be drawn. This way I can fire off something on tab change.
			switch (pupilSettings.customGUIVariables.tabs.mainTab) {
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
		if (PupilTools.Settings.connection.isConnected) 
		{
			if (PupilTools.Settings.DataProcessState != PupilSettings.EStatus.Calibration) 
			{
				if (GUILayout.Button ("Calibrate", GUILayout.Height (50))) 
				{
					if (Application.isPlaying) 
					{
						PupilTools.StartCalibration ();
						//EditorApplication.update += CheckCalibration;
					} else {
						EditorUtility.DisplayDialog ("Pupil service message", "You can only use calibration in playmode", "Understood");
					}
				}
			} else 
			{
				if (GUILayout.Button ("Stop Calibration", GUILayout.Height (50))) 
				{
					PupilTools.StopCalibration ();
				}
			}
		} else 
		{
			GUI.enabled = false;
			GUILayout.Button ("Calibrate (Not Connected !)", GUILayout.Height (50));
		}
		GUI.enabled = true;
		////////////////////////////CALIBRATE BUTTON////////////////////////////

		GUILayout.Space (5);

		////////////////////////////RECORDING BUTTON////////////////////////////
		if (PupilTools.Settings.connection.isConnected)
		{
			EditorGUI.BeginChangeCheck ();

			if (Recorder.isProcessing)
			{
				GUI.enabled = false;
				Recorder.isRecording = GUILayout.Toggle (Recorder.isRecording, "Processing ... ", "Button", GUILayout.Height (50));
			} else
			{
				Recorder.isRecording = GUILayout.Toggle (Recorder.isRecording, "Recording", "Button", GUILayout.Height (50));
			}

			GUI.enabled = true;
			GUI.backgroundColor = Color.white;
			if (EditorGUI.EndChangeCheck ())
			{
				if (Recorder.isRecording)
				{
					Recorder.Start ();
					EditorApplication.update += CheckRecording;
					EditorUtility.SetDirty (target);
				} else
				{
					Recorder.Stop ();
				}
			}
		}
		else 
		{
			GUI.enabled = false;
			GUILayout.Button ("Recording (Not Connected !)", GUILayout.Height (50));
		}
		GUI.enabled = true;
		////////////////////////////RECORDING BUTTON////////////////////////////

		/// 
		GUILayout.Space (5);

		////////////////////////////OPERATOR MONITOR BUTTON////////////////////////////
		EditorGUI.BeginChangeCheck ();
		pupilTracker.isOperatorMonitor = GUILayout.Toggle (pupilTracker.isOperatorMonitor, "Operator Monitor", "Button", GUILayout.MinWidth (100), GUILayout.Height (50));
		if (EditorGUI.EndChangeCheck ()) {
			if (pupilTracker.isOperatorMonitor) {
				pupilTracker.debugInstance.CloseCalibrationDebugView();
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

		if (pupilSettings.customGUIVariables.bools.isAdvanced) {
			if (GUILayout.Button ("IsConnected")) {
				PupilTools.Settings.connection.isConnected = true;
			}
//			pupilTracker.debugInstance.DebugVariables.packetsOnMainThread = GUILayout.Toggle (pupilTracker.debugInstance.DebugVariables.packetsOnMainThread, "Process Packets on Main Thread", "Button", GUILayout.MinWidth (100));

			GUI.backgroundColor = Color.white;
			GUILayout.Space (10);

			pupilSettings.debug.printSampling = GUILayout.Toggle (pupilSettings.debug.printSampling, "Print Sampling", "Button");

			pupilSettings.debug.printMessage = GUILayout.Toggle (pupilSettings.debug.printMessage, "Print Msg", "Button");

			pupilSettings.debug.printMessageType = GUILayout.Toggle (pupilSettings.debug.printMessageType, "Print Msg Types", "Button");

//			pupilTracker.DebugVariables.subscribeAll = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeAll, "Subscribe to all", "Button");
//
//			pupilTracker.DebugVariables.subscribeFrame = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeFrame, "Subscribe to frame.", "Button");
//
//			pupilTracker.DebugVariables.subscribeGaze = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeGaze, "Subscribe to gaze.", "Button");
//
//			pupilTracker.DebugVariables.subscribeNotify = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeNotify, "Subscribe to notifications.", "Button");

		}


	}
	private void DrawSettings(){

		PupilSettings pupilSettings = PupilTools.Settings;

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
			if (PupilTools.Settings.DataProcessState == PupilSettings.EStatus.Calibration) {
				GUI.enabled = false;
			}

			GUILayout.Space (10);

			AutoRunLayout ();

			GUILayout.Space (20);

			////////////////////////////CONNECTION MODE////////////////////////////
			EditorGUI.BeginChangeCheck ();
			//GUI.color = new Color (.7f, .7f, .7f, 1f);

			PupilTools.Settings.connection.isLocal = Convert.ToBoolean (GUILayout.Toolbar (Convert.ToInt32 (PupilTools.Settings.connection.isLocal), new string[] {
				"Remote",
				"Local"
			}, GUILayout.Height (30), GUILayout.MinWidth (25)));
			//pupilTracker.customInspector.connectionMode = GUILayout.Toolbar (pupilTracker.customInspector.connectionMode, new string[]{ "Local", "Remote" }, GUILayout.Height (30), GUILayout.MinWidth (25));
			GUI.color = Color.white;
			if (EditorGUI.EndChangeCheck ()) {
				if (PupilTools.Settings.connection.isLocal) {
					tempServerIP = PupilTools.Settings.connection.IP;
					PupilTools.Settings.connection.IP = "127.0.0.1";
				} else {
					PupilTools.Settings.connection.IP = tempServerIP;
				}
			}

			////////////////////////////CONNECTION MODE////////////////////////////
			GUILayout.Space (5);
			if (PupilTools.Settings.connection.isLocal) {//LOCAL CONNECTION MODE//
				////////////////////////////PUPIL APP PATH////////////////////////////
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("path : ", pupilSettings.GUIStyles[5], GUILayout.MinWidth (50));

				PupilTools.Settings.pupilServiceApp.servicePath = EditorGUILayout.TextArea (PupilTools.Settings.pupilServiceApp.servicePath, pupilSettings.GUIStyles[6], GUILayout.MinWidth (100), GUILayout.Height (22));
				if (GUILayout.Button ("Browse")) {
					PupilTools.Settings.pupilServiceApp.servicePath = EditorUtility.OpenFilePanel ("Select Pupil service application file", PupilTools.Settings.pupilServiceApp.servicePath, "exe");
				}
				GUILayout.EndHorizontal ();
				////////////////////////////PUPIL APP PATH////////////////////////////
			}

			if (pupilSettings.customGUIVariables.bools.isAdvanced){//ADVANCED SETTING

				////////////////////////////SERVICE PORT////////////////////////////
				/// 
				GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
				//
				GUILayout.Label ("Service Port : ", pupilSettings.GUIStyles[3], GUILayout.MinWidth (50));
				PupilTools.Settings.connection.PORT = EditorGUILayout.IntField (PupilTools.Settings.connection.PORT, pupilSettings.GUIStyles[4], GUILayout.MinWidth (100), GUILayout.Height (22));
				//
				GUILayout.EndHorizontal ();//---------HORIZONTAL GROUP\---------//
				///
				////////////////////////////SERVICE PORT\////////////////////////////
				base.OnInspectorGUI ();
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line

			}//ADVANCED SETTING\

			if (!PupilTools.Settings.connection.isLocal) {//---------REMOTE CONNECTION MODE---------//



				GUILayout.Space (5);

				////////////////////////////SERVER IP ADDRESS////////////////////////////
				/// 

				GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
				//
				GUILayout.Label ("IP : ", pupilSettings.GUIStyles[5], GUILayout.MinWidth (50));
				//

//				pupilTracker.Settings = (PupilSettings)EditorGUILayout.ObjectField (pupilTracker.Settings);
//				pupilTracker.Settings.a = EditorGUILayout.TextArea (pupilTracker.Settings.a, pupilTracker.Styles[8], GUILayout.MinWidth (50), GUILayout.Height (22));
				PupilTools.Settings.connection.IP = EditorGUILayout.TextArea (PupilTools.Settings.connection.IP, pupilSettings.GUIStyles[4], GUILayout.MinWidth (50), GUILayout.Height (22));
				if (GUILayout.Button ("Default")) {
					PupilTools.Settings.connection.IP = "127.0.0.1";
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
			

//			GUILayout.Space (20);
//
//			////////////////////////////2D-3D TOGGLE BAR////////////////////////////
//			EditorGUI.BeginChangeCheck ();
//			var calibrationMode = (Calibration.Mode)GUILayout.Toolbar ((int)pupilSettings.calibration.currentMode, new string[] {
//				"2D",
//				"3D"
//			});
//			if (calibrationMode != pupilSettings.calibration.currentMode)
//			{
//				pupilSettings.calibration.SetMode (calibrationMode);
//			}
//			GUI.enabled = true;
//			EditorGUI.EndChangeCheck ();
//			////////////////////////////2D-3D TOGGLE BAR////////////////////////////

			////////////////////////////CALIBRATION DEBUG MODE////////////////////////////
			if (PupilTools.Settings.DataProcessState == PupilSettings.EStatus.Calibration || !isEyeProcessConnected || (int)pupilSettings.calibration.currentMode != 1) {
			} else {
				
				//GUI.enabled = false;

				EditorGUI.BeginChangeCheck ();

				pupilSettings.debugView.active = GUILayout.Toggle (pupilSettings.debugView.active, "Calibration Debug Mode", "Button");
				GUI.enabled = true;
				if (EditorGUI.EndChangeCheck ()) {
					if (pupilSettings.debugView.active) {
						if (pupilTracker.OperatorMonitorProperties [0].OperatorCamera != null)
							OperatorMonitor.Instance.ExitOperatorMonitor ();
						pupilTracker.debugInstance.StartCalibrationDebugView ();

					} else {
						pupilTracker.debugInstance.CloseCalibrationDebugView ();
					}
				}
			}

			if (pupilSettings.debugView.active) {
				//				pupilTracker.calibrationDebugCamera = (PupilGazeTracker.CalibrationDebugCamera) EditorGUILayout.EnumPopup (pupilTracker.calibrationDebugCamera);
				GUILayout.BeginHorizontal ();
				EditorGUI.BeginChangeCheck ();
				pupilTracker.debugInstance.DebugViewVariables.isDrawLines = GUILayout.Toggle (pupilTracker.debugInstance.DebugViewVariables.isDrawLines, " Draw Debug Lines ", "Button");
				pupilTracker.debugInstance.DebugViewVariables.isDrawPoints = GUILayout.Toggle (pupilTracker.debugInstance.DebugViewVariables.isDrawPoints, " Draw Debug Points ", "Button");
				if (EditorGUI.EndChangeCheck ()) {
					pupilTracker.debugInstance.SetDrawCalibrationPointCloud (pupilTracker.debugInstance.DebugViewVariables.isDrawPoints);
					pupilTracker.debugInstance.SetDrawCalibrationLines (pupilTracker.debugInstance.DebugViewVariables.isDrawLines);
				}
				GUILayout.EndHorizontal ();
			}

			GUI.enabled = true;
//			////////////////////////////CALIBRATION DEBUG MODE////////////////////////////
//
//			GUILayout.Space (20);
//
//			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
//			GUILayout.Label ("Samples per depth", pupilSettings.GUIStyles[3], GUILayout.MinWidth (35));
//			pupilSettings.calibration.currentCalibrationType.samplesPerDepth = EditorGUILayout.IntSlider (pupilSettings.calibration.currentCalibrationType.samplesPerDepth, 1, 120, GUILayout.ExpandWidth(true));
//			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (10);//------------------------------------------------------------//

			if (pupilSettings.customGUIVariables.bools.isAdvanced){

				base.OnInspectorGUI ();
			}
			break;
		case 2://RECORDING

			GUILayout.Space (20);

			GUILayout.BeginHorizontal ();
			PupilTools.Settings.recorder.resolution = (FFmpegOut.FFmpegPipe.Resolution)EditorGUILayout.EnumPopup (PupilTools.Settings.recorder.resolution);
			PupilTools.Settings.recorder.codec = (FFmpegOut.FFmpegPipe.Codec)EditorGUILayout.EnumPopup (PupilTools.Settings.recorder.codec);//  GUILayout.Toolbar (pupilTracker.Codec, new string[] {
			GUILayout.EndHorizontal();

//			GUILayout.BeginHorizontal ();
//			PupilTools.Settings.recorder.isFixedRecordingLength = GUILayout.Toggle (PupilTools.Settings.recorder.isFixedRecordingLength, "fixed length", "Button", GUILayout.Width (90));
//			if (PupilTools.Settings.recorder.isFixedRecordingLength) {
//				PupilTools.Settings.recorder.recordingLength = EditorGUILayout.FloatField (PupilTools.Settings.recorder.recordingLength);
//			}
//			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			PupilTools.Settings.recorder.isCustomPath = GUILayout.Toggle (PupilTools.Settings.recorder.isCustomPath, "CustomPath", "Button", GUILayout.Width (90));
			if (EditorGUI.EndChangeCheck ()) {
				if (PupilTools.Settings.recorder.isCustomPath) {
					PupilTools.Settings.recorder.filePath = EditorUtility.OpenFolderPanel ("Select the output folder", PupilTools.Settings.recorder.filePath, "");
				}
			}
			if (PupilTools.Settings.recorder.isCustomPath) {
				GUIStyle centeredStyle = new GUIStyle (GUI.skin.textField);
				centeredStyle.alignment = TextAnchor.MiddleCenter;
				centeredStyle.margin = new RectOffset (0, 0, 3, 0);
				centeredStyle.fixedHeight = 20;
				PupilTools.Settings.recorder.filePath = GUILayout.TextField (PupilTools.Settings.recorder.filePath, centeredStyle);
				if (GUILayout.Button ("Browse", GUILayout.Width(60))) {
					PupilTools.Settings.recorder.filePath = EditorUtility.OpenFolderPanel ("Select the output folder", PupilTools.Settings.recorder.filePath, "");
				}
			}
			GUILayout.EndHorizontal ();

			break;
		}


		//if change found set scene as dirty, so user will have to save changed values.
		if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
		{
			EditorSceneManager.MarkSceneDirty (EditorSceneManager.GetActiveScene ());
		}

		GUILayout.Space (10);

	}

//	public void CheckConnection(){
//		if (Pupil.processStatus.eyeProcess0 || Pupil.processStatus.eyeProcess1) {
//			if (Pupil.processStatus.initialized) {
//				EditorApplication.update -= CheckConnection;
//			}
//			isEyeProcessConnected = true;
//		}
//
//		Repaint ();
//	}
		
	public void CheckRecording()
	{
		if (!Recorder.isRecording) 
		{
			EditorApplication.update -= CheckRecording;
			Repaint ();
		}
	}
		
	void OnApplicationQuit(){
		EditorApplication.update = null;
	}

#endif
}

