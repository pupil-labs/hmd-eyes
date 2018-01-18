using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SphericalVideoPlayer))]
[CanEditMultipleObjects]
public class SphericalVideoPlayerEditor : Editor
{
	SerializedProperty FilePath;
	SerializedProperty UseExternalVideo;

	void OnEnable()
	{
		FilePath = serializedObject.FindProperty("FilePath");
		UseExternalVideo = serializedObject.FindProperty ("UseExternalVideo");
	}

//	int videoMode = 0;
	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		GUILayout.BeginHorizontal ();
		EditorGUI.BeginChangeCheck ();
		UseExternalVideo.boolValue = GUILayout.Toolbar ( UseExternalVideo.boolValue ? 1 : 0, new string[] { "Use internal video", "Load video from path" }, GUILayout.Height(30)) == 1;
//		UseExternalVideo.boolValue = videoMode == 0 ? false : true;
		GUILayout.EndHorizontal ();
		////////INPUT FIELDS////////
		if (UseExternalVideo.boolValue)
		{
			EditorGUILayout.LabelField ("Current video path: " + FilePath.stringValue);
			GUILayout.BeginHorizontal ();
			EditorGUI.BeginChangeCheck ();
			if (GUILayout.Button ("Set path..", GUILayout.Width (128)))
			{
				var newPath = EditorUtility.OpenFilePanel ("Select spherical video", FilePath.stringValue, "mp4");
				if (newPath != "")
					FilePath.stringValue = newPath;
			}
			GUILayout.EndHorizontal ();
		}
		else
		{
			GUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Will load a spherical video of the market scene.");
			GUILayout.EndHorizontal ();
		}
		serializedObject.ApplyModifiedProperties();
	}
}
