using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Recorder
{
	public static GameObject RecorderGO;
	public static bool isRecording;
	public static bool isProcessing;

	public FFmpegOut.FFmpegPipe.Codec codec;
	public FFmpegOut.FFmpegPipe.Resolution resolution;
	public List<int[]> resolutions = new List<int[]> () {
		new int[]{ 1920, 1080 },
		new int[]{ 1280, 720 },
		new int[] {
			640,
			480
		}
	};
	public string filePath;
	public bool isFixedRecordingLength;
	public float recordingLength = 10f;
	public bool isCustomPath;

	public static void Start ()
	{
		RecorderGO = new GameObject ("RecorderCamera");
		RecorderGO.transform.parent = Camera.main.gameObject.transform;

		RecorderGO.AddComponent<FFmpegOut.CameraCapture> ();
		Camera c = RecorderGO.GetComponent<Camera> ();
		c.targetDisplay = 1;
		c.stereoTargetEye = StereoTargetEyeMask.None;
		#if UNITY_5_6_OR_NEWER
		c.allowHDR = false;
		c.allowMSAA = false;
		#endif
		c.fieldOfView = 111;
		PupilTools.RepaintGUI ();
	}

	public static void Stop ()
	{
		RecorderGO.GetComponent<FFmpegOut.CameraCapture> ().Stop ();
		PupilTools.RepaintGUI ();
	}
}


