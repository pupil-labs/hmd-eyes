using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_WSA

[Serializable]
public class Recorder
{
	public static GameObject RecorderGO;
	public static bool isRecording;
	public static bool isProcessing;
#if !UNITY_WSA
	public FFmpegOut.FFmpegPipe.Codec codec;
	public FFmpegOut.FFmpegPipe.Resolution resolution;
#endif
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
		RecorderGO.transform.parent = PupilSettings.Instance.currentCamera.transform;
		RecorderGO.transform.localPosition = Vector3.zero;
		RecorderGO.transform.localEulerAngles = Vector3.zero;

		RecorderGO.AddComponent<FFmpegOut.CameraCapture> ();
		Camera c = RecorderGO.GetComponent<Camera> ();
		c.clearFlags = CameraClearFlags.Color;
		c.targetDisplay = 1;
		c.stereoTargetEye = StereoTargetEyeMask.None;
		#if UNITY_5_6_OR_NEWER
		c.allowHDR = false;
		c.allowMSAA = false;
		#endif
		c.fieldOfView = PupilSettings.Instance.currentCamera.fieldOfView;
		PupilTools.RepaintGUI ();
	}

	public static void Stop ()
	{
		RecorderGO.GetComponent<FFmpegOut.CameraCapture> ().Stop ();
		GameObject.Destroy (RecorderGO);
		PupilTools.RepaintGUI ();
	}
}

#endif


