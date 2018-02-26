
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

			tempServerIP = PupilTools.Connection.IP;
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
		if (PupilTools.IsConnected) {
			GUI.color = Color.green;
			if (PupilTools.Connection.isLocal) {
				GUILayout.Label ("localHost ( Connected )", pupilSettings.GUIStyles[1]);
			} else {
				GUILayout.Label ("remote " + PupilTools.Connection.IP + " ( Connected )" , pupilSettings.GUIStyles[1]);
			}

		} else {
			if (PupilTools.Connection.isLocal) {
				GUILayout.Label ("localHost ( Not Connected )", pupilSettings.GUIStyles[1]);
			} else {
				GUILayout.Label ("remote " + PupilTools.Connection.IP + " ( Not Connected )" , pupilSettings.GUIStyles[1]);
			}
		}
		GUI.color = Color.white;

		Texture2D eyeIcon = Resources.Load("eye") as Texture2D;
		if ( PupilTools.eyeProcess0) {
			GUI.color = Color.green;
		} else {
			GUI.color = Color.gray;
		}
		GUILayout.Label (eyeIcon as Texture2D, pupilSettings.GUIStyles[2], GUILayout.Width (20), GUILayout.Height (20));
		GUILayout.Space (5);
		if (PupilTools.eyeProcess1) {
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
		if (PupilTools.IsConnected) 
		{
			if (!PupilTools.IsCalibrating) 
			{
				if (GUILayout.Button ("Start Calibration", GUILayout.Height (50))) 
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
			GUILayout.Button ("Start Calibration (Requires Connection to Pupil)", GUILayout.Height (50));
		}
		GUI.enabled = true;
		////////////////////////////CALIBRATE BUTTON////////////////////////////

		GUILayout.Space (5);

		////////////////////////////RECORDING BUTTON////////////////////////////
		if (PupilTools.IsConnected)
		{
			EditorGUI.BeginChangeCheck ();

			Recorder.isRecording = GUILayout.Toggle (Recorder.isRecording, !Recorder.isRecording ? "Start Recording" : "Stop Recording", "Button", GUILayout.Height (50));

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
			GUILayout.Button ("Start Recording (Requires Connection to Pupil)", GUILayout.Height (50));
		}
		GUI.enabled = true;
		////////////////////////////RECORDING BUTTON////////////////////////////

		GUILayout.Space (20);

		GUI.skin = default(GUISkin);
		GUI.depth = 0;
		GUI.color = Color.white;
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line
		GUI.depth = 1;
		GUI.color = Color.white;

		if (pupilSettings.customGUIVariables.bools.isAdvanced) 
		{
			if (GUILayout.Button ("IsConnected")) {
				PupilTools.IsConnected = true;
			}

			GUI.backgroundColor = Color.white;
			GUILayout.Space (10);

			pupilSettings.debug.printSampling = GUILayout.Toggle (pupilSettings.debug.printSampling, "Print Sampling", "Button");

			pupilSettings.debug.printMessage = GUILayout.Toggle (pupilSettings.debug.printMessage, "Print Msg", "Button");

			pupilSettings.debug.printMessageType = GUILayout.Toggle (pupilSettings.debug.printMessageType, "Print Msg Types", "Button");
		}


	}
	private void DrawSettings()
	{
		PupilSettings pupilSettings = PupilSettings.Instance;

		////////////////////////////CONNECTION MODE////////////////////////////

		if (PupilTools.IsCalibrating)
		{
			GUI.enabled = false;
		}
		GUILayout.Space (4);
		GUILayout.Label ("Connection" , pupilSettings.GUIStyles[3]);
		GUILayout.Space (4);
		EditorGUI.BeginChangeCheck ();
		PupilTools.Connection.isLocal = Convert.ToBoolean (GUILayout.Toolbar (Convert.ToInt32 (PupilTools.Connection.isLocal), new string[] 
		{
			"Remote",
			"Local"
		}, GUILayout.Height (30), GUILayout.MinWidth (25)));

		GUI.color = Color.white;
		if (EditorGUI.EndChangeCheck ())
		{
			if (PupilTools.Connection.isLocal) 
			{
				tempServerIP = PupilTools.Connection.IP;
				PupilTools.Connection.IP = "127.0.0.1";
			} 
			else
				PupilTools.Connection.IP = tempServerIP;
		}
		//ADVANCED SETTING
		if (pupilSettings.customGUIVariables.bools.isAdvanced)
		{
			////////////////////////////SERVICE PORT////////////////////////////
			/// 
			GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
			//
			GUILayout.Label ("Service Port : ", pupilSettings.GUIStyles[3], GUILayout.MinWidth (50));
			PupilTools.Connection.PORT = EditorGUILayout.IntField (PupilTools.Connection.PORT, pupilSettings.GUIStyles[4], GUILayout.MinWidth (100), GUILayout.Height (22));
			//
			GUILayout.EndHorizontal ();//---------HORIZONTAL GROUP\---------//
			///
			////////////////////////////SERVICE PORT\////////////////////////////
			base.OnInspectorGUI ();
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));//Separator Line

		}
		//ADVANCED SETTING\

		//---------REMOTE CONNECTION MODE---------//
		if (!PupilTools.Connection.isLocal) 
		{
			GUILayout.Space (5);

			////////////////////////////SERVER IP ADDRESS////////////////////////////
			/// 
			GUILayout.BeginHorizontal ();//---------HORIZONTAL GROUP---------//
			//
			GUILayout.Label ("IP : ", pupilSettings.GUIStyles[5], GUILayout.MinWidth (50));
			//
			PupilTools.Connection.IP = EditorGUILayout.TextArea (PupilTools.Connection.IP, pupilSettings.GUIStyles[4], GUILayout.MinWidth (50), GUILayout.Height (22));
			if (GUILayout.Button ("Default")) 
			{
				PupilTools.Connection.IP = "127.0.0.1";
				Repaint ();
				GUI.FocusControl ("");
			}
			//
			GUILayout.EndHorizontal ();//---------HORIZONTAL GROUP\---------//
			///
			////////////////////////////SERVER IP ADDRESS\////////////////////////////
		}
		//---------REMOTE CONNECTION MODE\---------//

		GUI.enabled = true;

		GUILayout.Space (4);
		GUILayout.Label ("Recording" , pupilSettings.GUIStyles[3]);
		GUILayout.Space (4);

		GUILayout.BeginHorizontal ();
		PupilSettings.Instance.recorder.resolution = (FFmpegOut.FFmpegPipe.Resolution)EditorGUILayout.EnumPopup (PupilSettings.Instance.recorder.resolution);
		PupilSettings.Instance.recorder.codec = (FFmpegOut.FFmpegPipe.Codec)EditorGUILayout.EnumPopup (PupilSettings.Instance.recorder.codec);//  GUILayout.Toolbar (pupilTracker.Codec, new string[] {
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal ();
		EditorGUI.BeginChangeCheck ();
		PupilSettings.Instance.recorder.isCustomPath = GUILayout.Toggle (PupilSettings.Instance.recorder.isCustomPath, "CustomPath", "Button", GUILayout.Width (90));
		if (EditorGUI.EndChangeCheck ()) 
			if (PupilSettings.Instance.recorder.isCustomPath)
				PupilSettings.Instance.recorder.filePath = EditorUtility.OpenFolderPanel ("Select the output folder", PupilSettings.Instance.recorder.filePath, "");

		if (PupilSettings.Instance.recorder.isCustomPath) 
		{
			GUIStyle centeredStyle = new GUIStyle (GUI.skin.textField);
			centeredStyle.alignment = TextAnchor.MiddleCenter;
			centeredStyle.margin = new RectOffset (0, 0, 3, 0);
			centeredStyle.fixedHeight = 20;
			PupilSettings.Instance.recorder.filePath = GUILayout.TextField (PupilSettings.Instance.recorder.filePath, centeredStyle);
			if (GUILayout.Button ("Browse", GUILayout.Width(60))) {
				PupilSettings.Instance.recorder.filePath = EditorUtility.OpenFolderPanel ("Select the output folder", PupilSettings.Instance.recorder.filePath, "");
			}
		}
		GUILayout.EndHorizontal ();

		GUILayout.Space (10);

	}

	public void CheckRecording()
	{
		if (!Recorder.isRecording) 
		{
			EditorApplication.update -= CheckRecording;
			Repaint ();
		}
	}
		
	void OnApplicationQuit()
	{
		EditorApplication.update = null;
	}

#endif
}

