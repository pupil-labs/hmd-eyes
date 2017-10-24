using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public static class PupilConversions
{
	public struct _double
	{
		public double[] value;

		public Vector2 v2{ get { return new Vector2 ((float)value [0], (float)value [1]); } }

		public Vector3 v3{ get { return new Vector3 ((float)value [0], (float)value [1], (float)value [2]); } }

		public Vector3 convertedV3{ get { return new Vector3 (-(float)value [0], (float)value [1], (float)value [2]); } }
	}

	public static Vector3[] Vector3ArrayFromString (string v3StringArray)
	{
		List<Vector3> _v3List = new List<Vector3> ();
		List<float> _v3TempList = new List<float> ();
		string[] _v3StringArray = v3StringArray.Split ("],[".ToCharArray ());
		foreach (string s in _v3StringArray)
		{
			if (s != "")
			{
				_v3TempList.Add (float.Parse (s));
			}
			if (_v3TempList.Count == 3)
			{
				_v3List.Add (new Vector3 (_v3TempList [0], _v3TempList [1], _v3TempList [2]));
				_v3TempList.Clear ();
			}

		}
		return _v3List.ToArray ();
	}

	private static float[] vector2ToFloatArray = new float[2];
	public static float[] Vector2ToFloatArray(Vector2 vector)
	{
		vector2ToFloatArray [0] = vector.x;
		vector2ToFloatArray [1] = vector.y;
		return vector2ToFloatArray;
	}

	public static Matrix4x4 Matrix4x4FromString (string matrixString, bool column = true, float scaler = 1f)
	{
		Matrix4x4 _m = new Matrix4x4 ();
		List<Vector4> _v4List = new List<Vector4> ();
		List<float> _v4TempList = new List<float> ();
		string[] _matrixStringArray = matrixString.Split ("],[".ToCharArray ());
		int ind = 0;
		foreach (string s in _matrixStringArray)
		{
			if (s != "")
				_v4TempList.Add (float.Parse (s));
			if (_v4TempList.Count == 4)
			{
				_v4List.Add (new Vector4 (_v4TempList [0], _v4TempList [1], _v4TempList [2], _v4TempList [3]));
				_v4TempList.Clear ();
				if (column)
				{
					_m.SetColumn (ind, _v4List.LastOrDefault ());
				} else
				{
					_m.SetRow (ind, _v4List.LastOrDefault ());
				}
				ind++;
			}
		}
		return _m;
	}

	public static Dictionary<string, object> DictionaryFromJSON (string json)
	{
		List<string> keys = new List<string> ();
		List<string> values = new List<string> ();
		Dictionary<string,object> dict = new Dictionary<string, object> ();

		string[] a = json.Split ("\"".ToCharArray (), 50);

		int ind = 0;
		foreach (string s in a)
		{
			if (s.Contains (":") && !s.Contains ("{"))
			{
				//print (s);
				keys.Add (a [ind - 1]);
				a [ind] = a [ind].Replace (":", "");
				a [ind] = a [ind].Substring (0, a [ind].Length - 2);
				a [ind] = a [ind].Replace (" ", "");
				a [ind] = a [ind].Replace ("}", "");
				values.Add (a [ind]);
			}
			ind++;
		}


		for (int i = 0; i < keys.Count; i++)
		{
			dict.Add (keys [i], values [i]);
		}

		return dict;
	}

	public static void ReadCalibrationData (string from, ref Calibration.data to)
	{
		if (!from.Contains ("null"))
		{
			Dictionary<string, object> camera_matricies_dict = DictionaryFromJSON (from);
			//print (camera_matricies_dict.Count);

			to = JsonUtility.FromJson<Calibration.data> (from);
			object o;
			camera_matricies_dict.TryGetValue ("cal_gaze_points0_3d", out o);
			to.cal_gaze_points0_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_gaze_points1_3d", out o);
			to.cal_gaze_points1_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_ref_points_3d", out o);
			to.cal_ref_points_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("cal_points_3d", out o);
			to.cal_points_3d = Vector3ArrayFromString (o.ToString ());
			camera_matricies_dict.TryGetValue ("eye_camera_to_world_matrix0", out o);
			to.eye_camera_to_world_matrix0 = Matrix4x4FromString (o.ToString (), false) * Matrix4x4.Scale (new Vector3 (1, -1, 1));
			camera_matricies_dict.TryGetValue ("eye_camera_to_world_matrix1", out o);
			to.eye_camera_to_world_matrix1 = Matrix4x4FromString (o.ToString (), false) * Matrix4x4.TRS (Vector3.zero, Quaternion.Euler (0, 0, 0), new Vector3 (1, -1, 1));

		}
	}

	public static T ByteArrayToObject<T> (byte[] arrayOfBytes)
	{
		if (arrayOfBytes == null || arrayOfBytes.Length < 1)
			return default(T);

		System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter ();

		T obj = (T)binaryFormatter.Deserialize (new MemoryStream (arrayOfBytes));

		return obj;
	}

	public static byte[] doubleArrayToByteArray (double[] doubleArray)
	{
		byte[] _bytes_blockcopy;

		_bytes_blockcopy = new byte[doubleArray.Length * 8];

		System.Buffer.BlockCopy (doubleArray, 0, _bytes_blockcopy, 0, doubleArray.Length * 8);

		return _bytes_blockcopy;

	}

	public static void WriteStringToFile (string dataString, string fileName = "defaultFilename")
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes (dataString);
		File.WriteAllBytes (Application.dataPath + "/" + fileName, bytes);
	}

	public static string ReadStringFromFile (string fileName = "defaultFilename")
	{
		if (File.Exists (Application.dataPath + "/" + fileName))
		{
			string _str = File.ReadAllText (Application.dataPath + "/" + fileName);
			return _str;
		} else
		{
			return "file Doesnt exist - null";
		}
	}
}

public class JsonHelper
{
	/// <summary>
	///Usage:
	///YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
	/// </summary>
	public static T[] getJsonArray<T> (string json)
	{

		string newJson = "{ \"array\": " + json + "}";
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (newJson);
		return wrapper.array;
	}

	//Usage:
	//string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
	public static string arrayToJson<T> (T[] array)
	{
		Wrapper<T> wrapper = new Wrapper<T> ();
		wrapper.array = array;
		return JsonUtility.ToJson (wrapper);
	}

	private class Wrapper<T>
	{
		public T[] array;
	}

}
