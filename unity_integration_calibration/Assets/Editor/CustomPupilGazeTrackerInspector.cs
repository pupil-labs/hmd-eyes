
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

	private static bool isMouseDown = false;
	bool isConnected = false;
	bool isCalibrating = false;
	string tempServerIP;

	Camera CalibEditorCamera;


	void OnEnable(){


		pupilTracker = (PupilGazeTracker)target;
		pupilTracker.AdjustPath ();

		tempServerIP = pupilTracker.ServerIP;

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

	}
	public override void OnInspectorGUI(){
		Event e = Event.current;
		var controlID = GUIUtility.GetControlID (FocusType.Passive);

		switch (e.type) {
		case EventType.MouseUp:
			isMouseDown = false;
			break;
		case EventType.MouseDown:
			isMouseDown = true;
			break;
		}

//		Debug.Log (isMouseDown);
		pupilTracker.FoldOutStyle = GUI.skin.GetStyle ("Foldout");

		GUILayout.Space (20);

		////////LABEL WITH STYLE////////
		System.Object logo = Resources.Load("PupilLabsLogo") as Texture;
		GUILayout.Label (logo as Texture, pupilTracker.LogoStyle);
		////////LABEL WITH STYLE////////

		if (isConnected) {
			GUI.color = Color.green;
			if (pupilTracker.connectionMode == 0) {
				GUILayout.Label ("Connected to localHost", pupilTracker.LogoStyle);
			} else {
				GUILayout.Label ("Connected to Remote : " + pupilTracker.ServerIP, pupilTracker.LogoStyle);
			}

		} else {
			if (pupilTracker.connectionMode == 0) {
				GUILayout.Label ("Connecting to localHost", pupilTracker.LogoStyle);
			} else {
				GUILayout.Label ("Connecting to Remote : " + pupilTracker.ServerIP, pupilTracker.LogoStyle);
			}
		}
		GUI.color = Color.white;

		////////DRAW TAB MENU SYSTEM////////
		EditorGUI.BeginChangeCheck();

		EditorGUILayout.BeginHorizontal ();

		GUILayout.Label ("DEV", pupilTracker.SettingsLabelsStyle, GUILayout.Width(50));
		pupilTracker.ShowBaseInspector = EditorGUILayout.Toggle (pupilTracker.ShowBaseInspector);

		Texture2D eyeIcon = Resources.Load("eye") as Texture2D;
		if (Pupil.processStatus.eyeProcess0) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilTracker.LogoStyle, GUILayout.Width (20), GUILayout.Height (20));
		GUILayout.Space (5);
		if (Pupil.processStatus.eyeProcess1) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilTracker.LogoStyle, GUILayout.Width (20), GUILayout.Height (20));
		GUI.color = Color.white;

		EditorGUILayout.EndHorizontal ();

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

//				try{
//				pupilTracker.CalibrationGameObject2D = GameObject.Find ("Calibrator").gameObject.transform.FindChild ("2D Calibrator").gameObject;
//				pupilTracker.CalibrationGameObject3D = GameObject.Find ("Calibrator").gameObject.transform.FindChild ("3D Calibrator").gameObject;
//				}
//				catch{
//					EditorUtility.DisplayDialog ("Pupil Service Warning", "Calibrator prefab cannot be found, or not complete, please add under main camera ! ", "Will Do");
//				}


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
	public float y;
	private void DrawMainMenu(){

		Event e = Event.current;

		////////////////////////////RECORDING////////////////////////////
	
		string RecLabel = "RECORD";
		string fixedLengthLabel = "Fixed Length";
		bool recorderButtonsEnabled = true;
		if (!Application.isPlaying) {
			GUI.enabled = false;
			RecLabel = "Recording is only available in Play mode !";
		}
		Rect _r = GUILayoutUtility.GetLastRect();
		GUI.DrawTexture(new Rect(15,_r.y+10,_r.size.x, 120 ), Resources.Load("recorder_bck") as Texture2D);
		GUI.backgroundColor = default(Color);
		Rect R = new Rect (40, 400, 90, 90);

		if (Recording.variables.isRecording) {
			recorderButtonsEnabled = false;
			RecLabel = "Recording !";
			R = new Rect (45, 405, 82, 82);
			GUI.color = new Color(.75f,.79f,.75f,1);
		}
		if (R.Contains(e.mousePosition) && isMouseDown) {
			//Debug.Log ("contains and down");
			R = new Rect (47, 406, 80, 80);
			GUI.color = new Color(.72f,.7f,.7f,1);
		}
		EditorGUI.BeginChangeCheck ();
		Recording.variables.isRecording = GUI.Toggle ( R,Recording.variables.isRecording, Resources.Load ("rec") as Texture2D, pupilTracker.Styles[3]);
		GUI.backgroundColor = Color.white;
		if (EditorGUI.EndChangeCheck ()) {
			if (Recording.variables.isRecording) {
				pupilTracker.StartRecording ();
				EditorApplication.update += CheckRecording;
			} else {
				pupilTracker.StopRecording ();
			}
		}
		GUILayout.Space (20);
		GUI.Label (new Rect(167,425,200,20), RecLabel, pupilTracker.LogoStyle);

		//////////////BUTTONS//////////////
		if (!recorderButtonsEnabled)
			GUI.enabled = false;
		GUI.skin.button.fontSize = 7;
		if (GUI.Button (new Rect (135, 382, 60, 20), "Browse")) {
			Recording.variables.FilePath = EditorUtility.SaveFilePanel("Please give your desired filename and path", Recording.variables.pathDirectory, Recording.variables.pathFileName, "mov");
		}

		//GUI.color = Color.white;

		//GUI.contentColor = Color.white;
		if (!Recording.variables.fixLength)
			fixedLengthLabel = "Manual Length";
		Recording.variables.fixLength = GUI.Toggle (new Rect (194, 382, 70, 20), Recording.variables.fixLength, fixedLengthLabel, "Button");
		if (Recording.variables.fixLength) {
			//GUI.skin.FindStyle ("FloatField").alignment = TextAnchor.MiddleCenter;
			Recording.variables.length = EditorGUI.FloatField (new Rect (197, 402, 64, 10), Recording.variables.length, pupilTracker.Styles [4]);
		} else {
			Recording.variables.length = 10;
		}

		//if ())
		GUI.skin.textField.alignment = TextAnchor.MiddleCenter;
		Recording.variables.width = EditorGUI.IntField(new Rect (135, 475, 50, 15), Recording.variables.width);
		Recording.variables.height = EditorGUI.IntField(new Rect (190, 475, 50, 15), Recording.variables.height);
		//Recording.variables.height = EditorGUI.IntField(new Rect (190, 475, 50, 15), Recording.variables.height);
		GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
//		if (GUI.Button (new Rect (150, 400, 200, 20), "Browse", pupilTracker.Styles[4])) {
//			Recording.variables.FilePath = EditorUtility.OpenFilePanel ("Please give your desired filename and path", Recording.variables.FilePath, "mov");
//		}

		GUI.skin.button.fontSize = 10;
		GUI.enabled = true;
		GUILayout.Space (120);
		////////////////////////////RECORDING////////////////////////////
		GUI.skin = default(GUISkin);
		//y =EditorGUILayout.FloatField(y);
		//GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		GUI.depth = 0;
		GUI.color = Color.white;
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		GUI.depth = 1;
		GUI.color = Color.white;

		if (GUILayout.Button ("IsConnected")) {
			isConnected = true;
		}

		//GUILayout.EndHorizontal ();
		GUI.backgroundColor = Color.white;
		GUILayout.Space (10);

		EditorGUI.BeginChangeCheck ();
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Draw Calib GL")) {
			if (!isCalibrating) {
				pupilTracker.StartCalibration ();
			} else {
				pupilTracker.StopCalibration ();
			}
		}
		//pupilTracker.targetEye = EditorGUILayout.EnumPopup ((pupilTracker.targetEye)as Enum);
		//if (pupilTracker.OperatorCamera)
//		GUILayout.Label("Target Eye");
//		pupilTracker.targetMask = (StereoTargetEyeMask)EditorGUILayout.EnumPopup(pupilTracker.targetMask);
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("3D")) {
			pupilTracker.To3DMethod ();
		}
		if (GUILayout.Button ("2D")) {
			pupilTracker.To2DMethod ();
		}
		GUILayout.EndHorizontal ();

		GUILayout.EndHorizontal ();
		if (GUILayout.Button ("start binocular vector gaze mapper", GUILayout.MinWidth (100))) {
			pupilTracker.StartBinocularVectorGazeMapper ();
		}
		if (GUILayout.Button ("Save string to Byte, File", GUILayout.MinWidth (100))) {
			pupilTracker.WriteStringToFile ("ez az adata tomeg jeee", "dataStringFileName");
		}
		if (GUILayout.Button ("Load string from Byte, File", GUILayout.MinWidth (100))) {
			Debug.Log (pupilTracker.ReadStringFromFile ("dataStringFileName"));
		}
		pupilTracker.isOperatorMonitor = GUILayout.Toggle (pupilTracker.isOperatorMonitor, "Operator Monitor", "Button", GUILayout.MinWidth(100));
		pupilTracker.DebugVariables.packetsOnMainThread = GUILayout.Toggle (pupilTracker.DebugVariables.packetsOnMainThread, "Process Packets on Main Thread", "Button", GUILayout.MinWidth(100));
		if (EditorGUI.EndChangeCheck ()) {
			if (pupilTracker.isOperatorMonitor) {
				//if () {
				pupilTracker.OnCalibDebug -= pupilTracker.DrawCalibrationDebugView;
				pupilTracker.calibrationDebugMode = false;
				Debug.Log("instantiate operator monitor");
					OperatorMonitor.Instantiate ();
				//}
			} else {
				if (pupilTracker.OperatorMonitorProperties[0].OperatorCamera != null)
					OperatorMonitor.Instance.ExitOperatorMonitor ();
				
				//	Destroy (pupilTracker.OperatorMonitorProperties[0].OperatorCamera.gameObject);
			}
		}

		////////BUTTONS FOR DEBUGGING TO COMMENT IN PUBLIC VERSION////////

		pupilTracker.DebugVariables.printSampling = GUILayout.Toggle (pupilTracker.DebugVariables.printSampling, "Print Sampling", "Button");
		pupilTracker.DebugVariables.printMessage = GUILayout.Toggle (pupilTracker.DebugVariables.printMessage, "Print Msg", "Button");
		pupilTracker.DebugVariables.printMessageType = GUILayout.Toggle (pupilTracker.DebugVariables.printMessageType, "Print Msg Types", "Button");

		pupilTracker.DebugVariables.subscribeAll = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeAll, "Subscribe to all", "Button");

		pupilTracker.DebugVariables.subscribeFrame = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeFrame, "Subscribe to frame.", "Button");

		pupilTracker.DebugVariables.subscribeGaze = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeGaze, "Subscribe to gaze.", "Button");

		pupilTracker.DebugVariables.subscribeNotify = GUILayout.Toggle (pupilTracker.DebugVariables.subscribeNotify, "Subscribe to notifications.", "Button");


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
			GUILayout.Label ("Pupil App Path : ", pupilTracker.SettingsLabelsStyle, GUILayout.MinWidth (100));
			pupilTracker.PupilServicePath = EditorGUILayout.TextArea (pupilTracker.PupilServicePath, pupilTracker.SettingsBrowseStyle, GUILayout.MinWidth (100), GUILayout.Height (22));
			if (GUILayout.Button ("Browse")) {
				pupilTracker.PupilServicePath = EditorUtility.OpenFilePanel ("Select Pupil service application file", pupilTracker.PupilServicePath, "exe");
			}
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			if (pupilTracker.connectionMode == 0) {
				GUI.enabled = false;
				GUILayout.Label ("localhost : ", pupilTracker.SettingsLabelsStyle, GUILayout.MinWidth (100));
			} else {
				GUILayout.Label ("Server IP : ", pupilTracker.SettingsLabelsStyle, GUILayout.MinWidth (100));
			}

			pupilTracker.ServerIP = EditorGUILayout.TextArea (pupilTracker.ServerIP, pupilTracker.SettingsValuesStyle, GUILayout.MinWidth (100), GUILayout.Height (22));
			if (GUILayout.Button ("Default")) {
				pupilTracker.ServerIP = "127.0.0.1";
				Repaint ();
				GUI.FocusControl ("");
			}
			GUI.enabled = true;
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			EditorGUILayout.Separator ();//------------------------------------------------------------//

			GUILayout.BeginHorizontal ();////////////////////HORIZONTAL////////////////////
			GUILayout.Label ("Service Port : ", pupilTracker.SettingsLabelsStyle, GUILayout.MinWidth (100));
			pupilTracker.ServicePort = EditorGUILayout.IntField (pupilTracker.ServicePort, pupilTracker.SettingsValuesStyle, GUILayout.MinWidth (100), GUILayout.Height (22));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (5);//------------------------------------------------------------//

			GUI.enabled = true;

			EditorGUI.BeginChangeCheck ();
			GUI.color = new Color (.7f, .7f, .7f, 1f);
			pupilTracker.connectionMode = GUILayout.Toolbar (pupilTracker.connectionMode, new string[]{ "Local", "Remote" }, GUILayout.Height (50), GUILayout.MinWidth(30));
			GUI.color = Color.white;
			if (EditorGUI.EndChangeCheck ()) {
				if (pupilTracker.connectionMode == 0) {
					tempServerIP = pupilTracker.ServerIP;
					pupilTracker.ServerIP = "127.0.0.1";
				} else {
					pupilTracker.ServerIP = tempServerIP;
				}
			}

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
			GUILayout.Label ("Calibration Sample Count: ", pupilTracker.SettingsLabelsStyle, GUILayout.Width (172));
			pupilTracker.DefaultCalibrationCount = EditorGUILayout.IntSlider (pupilTracker.DefaultCalibrationCount, 1, 120, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();////////////////////HORIZONTAL////////////////////

			GUILayout.Space (10);//------------------------------------------------------------//

			//EditorGUI.indentLevel = 0;

			//pupilTracker.ButtonStyle.
			//////////////////////////CALIBRATION POINTS//////////////////////////
			pupilTracker.CalibrationPointsFoldout = EditorGUILayout.Foldout (pupilTracker.CalibrationPointsFoldout," Calibration Points", pupilTracker.FoldOutStyle);
			GUILayout.Space (10);//------------------------------------------------------------//
			if (pupilTracker.CalibrationPointsFoldout) {
				//GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
				GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
				GUILayout.BeginHorizontal ();
				pupilTracker.CalibrationPoints2DFoldout = EditorGUILayout.Foldout (pupilTracker.CalibrationPoints2DFoldout, "2D", pupilTracker.FoldOutStyle);
				if (GUILayout.Button ("Add 2D Calibration Point")) {
					pupilTracker.Add2DCalibrationPoint (new floatArray(){axisValues = new float[]{0f,0f,0f}});
					//CalibrationPointsEditor.DrawCalibrationPointsEditor (pupilTracker, pupilTracker._calibPoints.Get2DList().Count-1,0, new Vector3 (0, 0, 0));
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (7);//------------------------------------------------------------//

				if (pupilTracker.CalibrationPoints2DFoldout) {//////2D POINTS//////
					foreach (floatArray _point in pupilTracker._calibPoints.Get2DList()) {
						int index = pupilTracker._calibPoints.Get2DList().IndexOf (_point);

						GUILayout.BeginHorizontal (pupilTracker.CalibRowStyle);
						//++EditorGUI.indentLevel;
						Vector3 _v2 = new Vector2 (_point.axisValues [0], _point.axisValues [1]);

						GUILayout.Label ("2D Point " + (index + 1) + " : ", GUILayout.MinWidth (0f));
						_v2 = EditorGUILayout.Vector2Field ("", _v2, GUILayout.MinWidth (20f));

						_point.axisValues [0] = _v2.x;
						_point.axisValues [1] = _v2.y;

						if (GUILayout.Button ("Remove")) {
							if (pupilTracker._calibPoints.Get2DList ().IndexOf (_point) == pupilTracker._calibPoints.Get2DList ().Count - 1) {
								pupilTracker.editedCalibIndex = pupilTracker._calibPoints.Get2DList ().Count - 1;
								//CalibrationPointsEditor.DrawCalibrationPointsEditor (pupilTracker);
							}
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
						GUILayout.BeginHorizontal (pupilTracker.CalibRowStyle);

						Vector3 _v3 = new Vector3 (_point.axisValues [0], _point.axisValues [1], _point.axisValues [2]);

						//EditorGUI.InspectorTitlebar (Rect (0, 0, 200, 20), new UnityEngine.Object[]());


						//_v3 = EditorGUILayout.Vector3Field ("3D Point " + (index + 1) + " :", _v3);
						GUILayout.Label("3D Point " + (index + 1) + " :", GUILayout.MinWidth (0f));
						_v3 = EditorGUILayout.Vector3Field ("", _v3, GUILayout.MinWidth (35f));
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

		if (isConnected) {

			EditorGUI.BeginChangeCheck ();
			pupilTracker.calibrationDebugMode = GUILayout.Toggle (pupilTracker.calibrationDebugMode, "Calibration Debug Mode", "Button");
			GUI.enabled = true;
			if (EditorGUI.EndChangeCheck ()) {
				//if (Application.isPlaying) {
					Debug.Log ("Calibration Debug mode on/off");
					if (pupilTracker.calibrationDebugMode) {
					if (pupilTracker.OperatorMonitorProperties [0].OperatorCamera != null)
						OperatorMonitor.Instance.ExitOperatorMonitor ();

					pupilTracker.isOperatorMonitor = false;
					pupilTracker.OnCalibDebug -= pupilTracker.DrawCalibrationDebugView;
					pupilTracker.OnCalibDebug += pupilTracker.DrawCalibrationDebugView;
					pupilTracker.InitializeFramePublishing ();
					pupilTracker.StartFramePublishing ();
						//pupilTracker.FramePublishingVariables.StreamCameraImages = true;
					} else {
					pupilTracker.OnCalibDebug -= pupilTracker.DrawCalibrationDebugView;
					pupilTracker.OnUpdate -= pupilTracker.CalibrationDebugInteraction;
						pupilTracker.StopFramePublishing ();
						//pupilTracker.FramePublishingVariables.StreamCameraImages = false;
					}
				//}
			}

			EditorGUI.BeginChangeCheck ();
			pupilTracker.FramePublishingVariables.StreamCameraImages = GUILayout.Toggle (pupilTracker.FramePublishingVariables.StreamCameraImages, " Stream Camera Image", "Button");
			if (EditorGUI.EndChangeCheck ()) {
				Pupil.connectionParameters.update = true;
				if (!pupilTracker.FramePublishingVariables.StreamCameraImages) {
					Pupil.connectionParameters.toSubscribe.Remove ("frame.");
				} else {
					Pupil.connectionParameters.toSubscribe.Add ("frame.");
				}
			}

			if (pupilTracker.calibrationDebugMode) {
				pupilTracker.calibrationDebugCamera = (PupilGazeTracker.CalibrationDebugCamera) EditorGUILayout.EnumPopup (pupilTracker.calibrationDebugCamera);
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

		}
		if (pupilTracker.calibrationMode == 1) {


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
		if (pupilTracker.IsConnected) {
			//Debug.Log ("Connection Established");
			//if (Pupil.processStatus.eyeProcess0 && Pupil.processStatus.eyeProcess1) {
				Repaint ();
				EditorApplication.update -= CheckConnection;
				isConnected = true;
			//}
		}
	}
	public void CheckCalibration(){
		Debug.Log ("Editor Update : Check Calibration");
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


	//out
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
	void OnApplicationQuit(){
		EditorApplication.update = null;
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

