using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PupilDataReceiver))]
public class CustomPupilDataReceiverInspector : Editor {

	PupilSettings pupilSettings;

	bool _pupilTrackerExists;

	void OnEnable(){
		
		pupilSettings = PupilTools.GetPupilSettings ();

		_pupilTrackerExists = PupilTools.PupilGazeTrackerExists ();

	}

	void OnDisable(){

		PupilTools.SavePupilSettings (ref pupilSettings);
	
	}

	public override void OnInspectorGUI(){

		if (!_pupilTrackerExists)
			CustomPupilGazeTrackerInspector.AutoRunLayout ();

		if (GUILayout.Button ("Action")) {
			Debug.Log (pupilSettings.connection.isLocal);
		}

		base.DrawDefaultInspector ();

	}

}
