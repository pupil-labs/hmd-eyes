using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeData
{
	class MovingAverage
	{
		public Vector3 Value = Vector3.zero;

		private List<Vector3> samples = new List<Vector3> ();
		private int length = 5;

		public MovingAverage (int len)
		{
			length = len;
		}

		public void AddSample (Vector3 v)
		{
			samples.Add (v);
			while (samples.Count > length)
				samples.RemoveAt (0);

			Value = Vector3.zero;
			foreach (var position in samples)
				Value += position;
			
			Value /= (float)samples.Count;
		}
	}

	private MovingAverage average;
	private Vector3 raw;

	public EyeData (int numberOfSamples)
	{
		average = new MovingAverage (numberOfSamples);
	}

	public void AddGaze(Vector3 position, bool sample)
	{
		if (sample)
			average.AddSample (position);
		else
			raw = position;
	}

	public Vector3 Average
	{
		get
		{
			return average.Value;	// Pupil data is currently in mm
		}
	}
	public Vector3 Raw
	{
		get
		{
			return raw;
		}
	}
}
