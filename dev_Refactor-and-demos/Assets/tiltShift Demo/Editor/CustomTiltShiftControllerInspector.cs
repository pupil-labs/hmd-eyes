using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TiltShiftController))]
public class CustomTiltShiftControllerInspector : Editor {

	TiltShiftController tiltShiftController;

	void OnEnable(){
	
		tiltShiftController = (TiltShiftController)target;

	}

	public override void OnInspectorGUI ()
	{
	
//		string[] propertyTypes = Enum.GetNames(typeof(TiltShiftController.TiltShiftProperty));

		EditorGUI.BeginChangeCheck ();
		tiltShiftController.tiltShiftPropertyTypes = (TiltShiftController.TiltShiftProperty)EditorGUILayout.EnumMaskField (tiltShiftController.tiltShiftPropertyTypes);

		if (EditorGUI.EndChangeCheck ()) {

			tiltShiftController.flaggedTiltShiftProperties = tiltShiftController.tiltShiftPropertyTypes.GetFlags ();
		
		}

		base.OnInspectorGUI ();
	
	}

}
