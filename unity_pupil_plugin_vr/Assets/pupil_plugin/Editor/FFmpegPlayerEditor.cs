using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FFmpegPlayer))]
[CanEditMultipleObjects]
public class FFmpegPlayerEditor : Editor
{
	SerializedProperty FilePath;

	void OnEnable()
	{
		FilePath = serializedObject.FindProperty("FilePath");
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.LabelField ("Current video path: " + FilePath.stringValue);
		GUILayout.BeginHorizontal ();
		EditorGUI.BeginChangeCheck ();
		if (GUILayout.Button ("Select new video..", GUILayout.Width (120)))
		{
			var newPath = EditorUtility.OpenFilePanel ("Select spherical video", FilePath.stringValue, "mp4");
			if (newPath != "")
				FilePath.stringValue = newPath;
		}
		GUILayout.EndHorizontal ();
		serializedObject.ApplyModifiedProperties();
	}
}
