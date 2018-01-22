using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeData
{
	class MovingAverage
	{
		public float Value = 0;

		private List<float> samples = new List<float> ();
		private int length = 5;

		public MovingAverage (int len)
		{
			length = len;
		}

		public void AddSample (float v)
		{
			samples.Add (v);
			while (samples.Count > length)
			{
				samples.RemoveAt (0);
			}

			Value = 0;
			for (int i = 0; i < samples.Count; ++i)
				Value += samples [i];

			Value /= (float)samples.Count;
		}

		public static MovingAverage[] InitializeArray (int numberOfSamples, int length)
		{
			MovingAverage[] array = new MovingAverage[length];
			for (int i = 0; i < length; i++)
				array[i] = new MovingAverage (numberOfSamples);
			return array;
		}
	}

	private MovingAverage[] data;
	private float[] raw;

	public EyeData (int numberOfSamples, int dimensions)
	{
		data = MovingAverage.InitializeArray (numberOfSamples, dimensions);
	}

	public void AddGaze(float[] position, bool sample)
	{
		if (position.Length != data.Length)
		{
			Debug.Log ("Array length not supported");
			return;
		}
		if (sample)
		{
			for (int i = 0; i < data.Length; i++)
			{
				data [i].AddSample (position [i]);
			}
		} else
		{
			raw = position;
		}
	}
	private Vector2 _average2D = Vector2.zero;
	public Vector2 Average2D
	{
		get
		{
			if (data == null || data.Length != 2)
				return Vector2.zero;
			_average2D.x = data [0].Value;
			_average2D.y = data [1].Value;
			return _average2D;
		}
	}
	private Vector3 _average3D = Vector3.zero;
	public Vector3 Average3D
	{
		get
		{
			if (data == null || data.Length != 3)
				return Vector3.zero;
			_average3D.x = data [0].Value;
			_average3D.y = -data [1].Value;
			_average3D.z = data [2].Value;
			return _average3D;	// Pupil data is currently in mm
		}
	}
	private Vector2 _raw2D = Vector2.zero;
	public Vector2 Raw2D
	{
		get
		{
			if (raw == null || raw.Length != 2)
				return Vector2.zero;
			_raw2D.x = raw [0];
			_raw2D.y = raw [1];
			return _raw2D;
		}
	}
	private Vector3 _raw3D = Vector3.zero;
	public Vector3 Raw3D
	{
		get
		{
			if (raw == null || raw.Length != 3)
				return Vector3.zero;
			_raw3D.x = raw [0];
			_raw3D.y = -raw [1];
			_raw3D.z = raw [2];
			return _raw3D;
		}
	}
}
