using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SphericalVideoPlayer))]
[CanEditMultipleObjects]
public class SphericalVideoPlayerEditor : Editor
{
	SerializedProperty FilePath;

	void OnEnable()
	{
		FilePath = serializedObject.FindProperty("FilePath");
	}

//	int videoMode = 0;
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		GUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Current video path: " + FilePath.stringValue);
//		UseExternalVideo.boolValue = videoMode == 0 ? false : true;
		GUILayout.EndHorizontal ();

		GUILayout.BeginHorizontal ();
		EditorGUI.BeginChangeCheck ();
		if (GUILayout.Button ("Set path..", GUILayout.Width (128)))
		{
			var newPath = EditorUtility.OpenFilePanel ("Select spherical video", FilePath.stringValue, "mp4");
			if (newPath != "")
				FilePath.stringValue = newPath;
		}
		GUILayout.EndHorizontal ();
		serializedObject.ApplyModifiedProperties();
	}
}
