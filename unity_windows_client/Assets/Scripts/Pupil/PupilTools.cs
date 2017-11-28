using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;

public class PupilTools : MonoBehaviour
{
	public static float PupilUnitScalingFactor = 1000;	// Pupil is currently operating in mm

	public static Dictionary<string, object> pupil0Dictionary;
	public static Dictionary<string, object> pupil1Dictionary;
	private static Dictionary<string, object> _gazeDictionary;
	public static Dictionary<string, object> gazeDictionary
	{
		get
		{
			return _gazeDictionary;
		}
		set
		{
			_gazeDictionary = value;
			UpdateGaze ();
			UpdateEyeID ();
		}
	}

	private static string[] gazeKeys = { "gaze_point_3d", "norm_pos", "eye_centers_3d" , "gaze_normals_3d" };
	private static string eyeDataKey;
	private static void UpdateGaze()
	{
		foreach (var key in gazeKeys)
		{
			if (gazeDictionary.ContainsKey (key))
			{
				switch (key)
				{
				case "norm_pos": // 2D case
					eyeDataKey = key + "_" + stringForEyeID (); // we add the identifier to the key
					var position = Position (gazeDictionary [key], false);
					PupilData.AddGazeToEyeData(eyeDataKey,position);
					break;
				case "eye_centers_3d":
				case "gaze_normals_3d":
					// in case of eye_centers_3d and gaze_normals_3d, we get an dictionary with one positional object for each eye id (the key)
					if (gazeDictionary [key] is Dictionary<object,object>)
						foreach (var item in (gazeDictionary[key] as Dictionary<object,object>))
						{
							eyeDataKey = key + "_" + item.Key.ToString ();
							PupilData.AddGazeToEyeData (eyeDataKey, Position (item.Value,true));
						}
					break;
				default:
					PupilData.AddGazeToEyeData(key,Position(gazeDictionary[key],true));
					break;
				}
			}
		}
	}

	private static object IDo;
	private static void UpdateEyeID ()
	{
		string id = "";

		if (gazeDictionary != null)
			if (gazeDictionary.TryGetValue ("id", out IDo))
				id = IDo.ToString ();
			
		PupilData.UpdateCurrentEyeID(id);
	}

	public static string stringForEyeID ()
	{
		object IDo;
		if (gazeDictionary == null)
			return null;

		bool isID = gazeDictionary.TryGetValue ("id", out IDo);

		if (isID)
		{
			return IDo.ToString ();

		}
		else
		{
			return null;
		}
	}

	private static object[] position_o;
	private static float[] Position (object position, bool applyScaling)
	{
		position_o = position as object[];
		float[] position_f = new float[position_o.Length];
		for (int i = 0; i < position_o.Length; i++)
		{
			position_f [i] = (float)(double)position_o [i];
		}
		if (applyScaling)
			for (int i = 0; i < position_f.Length; i++)
				position_f [i] /= PupilUnitScalingFactor;
		return position_f;
	}

	public static float ConfidenceForDictionary(Dictionary<string,object> dictionary)
	{
		object conf0;
		dictionary.TryGetValue ("confidence", out conf0);
		return (float)(double)conf0;
	}

	public static float Confidence (int eyeID)
	{
		if (eyeID == PupilData.rightEyeID)
			return ConfidenceForDictionary (pupil0Dictionary);
		else if (eyeID == PupilData.leftEyeID)
			return ConfidenceForDictionary (pupil1Dictionary); 
		else
			return 0;
	}

	public static Dictionary<object,object> BaseData ()
	{
		object o;
		gazeDictionary.TryGetValue ("base_data", out o);
		return o as Dictionary<object,object>;
	}
}
