// Pupil Gaze Tracker service
// Written by MHD Yamen Saraiji
// https://github.com/mrayy

using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PupilGazeTracker:MonoBehaviour
{
	public PupilSettings Settings;

	static PupilGazeTracker _Instance;
	public static PupilGazeTracker Instance
	{
		get
		{
			if (_Instance == null)
			{
				_Instance = new GameObject ("PupilGazeTracker").AddComponent<PupilGazeTracker> ();
			}
			return _Instance;
		}
	}

	public string ProjectName;

	#region delegates

	public delegate void OnUpdateDeleg ();
	public delegate void DrawMenuDeleg ();

	public DrawMenuDeleg DrawMenu;
	public OnUpdateDeleg OnUpdate;

	#endregion

	public PupilGazeTracker ()
	{
		_Instance = this;
	}

#region Start

	void Start ()
	{
		Settings = PupilSettings.Instance;

		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;

		#if !UNITY_WSA
		PupilData.calculateMovingAverage = false;
		#endif

		PupilGazeTracker.Instance.ProjectName = Application.productName;

		PupilTools.IsConnected = false;
		PupilTools.IsIdle = true;

		var relativeRightEyePosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.RightEye) - UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye);
		PupilTools.Calibration.rightEyeTranslation = new float[] { relativeRightEyePosition.z*PupilSettings.PupilUnitScalingFactor, 0, 0 };
		var relativeLeftEyePosition = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.LeftEye) - UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.CenterEye);
		PupilTools.Calibration.leftEyeTranslation = new float[] { relativeLeftEyePosition.z*PupilSettings.PupilUnitScalingFactor, 0, 0 };

		#if !UNITY_WSA
		RunConnect ();
		#endif
	}

	public void RunConnect()
	{
		StartCoroutine (PupilTools.Connect (retry: true, retryDelay: 5f));
	}

#endregion

#region Update

	void Update ()
	{
		if (PupilTools.IsCalibrating)
		{
			PupilTools.Calibration.UpdateCalibration ();
		} 

		PupilTools.Connection.UpdateSubscriptionSockets ();

		if (PupilTools.IsConnected && Input.GetKeyUp (KeyCode.C))
		{
			if (PupilTools.IsCalibrating)
			{
				PupilTools.StopCalibration ();
			} else
			{
				PupilTools.StartCalibration ();
			}
		}
#if !UNITY_WSA
		if (Input.GetKeyUp (KeyCode.R))
		{
			if (PupilTools.IsConnected)
			{
				if (!Recorder.isRecording)
				{
					Recorder.isRecording = true;
					Recorder.Start ();
				} else
				{
					Recorder.isRecording = false;
					Recorder.Stop ();
				}
			} else
				print ("Can not start recording without connection to pupil service");
		}
#endif

		if (Instance.OnUpdate != null)
			Instance.OnUpdate ();
	}

#endregion

	void OnEnable ()
	{
		if (PupilGazeTracker._Instance == null)
			PupilGazeTracker._Instance = this;
	}

	void OnDisable ()
	{
		CloseShop ();

		PupilGazeTracker._Instance = null;
		var pupilSettings = PupilSettings.Instance;
		SavePupilSettings (ref pupilSettings);
	}

	public void CloseShop ()
	{
		#if !UNITY_WSA
		if (Recorder.isRecording)
		{
			Recorder.Stop ();
		}
		#endif
		PupilTools.Disconnect ();

		StopAllCoroutines ();

		PupilTools.RepaintGUI ();
	}

	public static void SavePupilSettings (ref PupilSettings pupilSettings)
	{
#if UNITY_EDITOR
		AssetDatabase.Refresh ();
		EditorUtility.SetDirty (pupilSettings);
		AssetDatabase.SaveAssets ();
#endif
	}

#region Gaze Visualization

	PupilMarker _markerLeftEye;
	PupilMarker _markerRightEye;
	PupilMarker _markerGazeCenter;
	PupilMarker _gaze3D;

	public void StartVisualizingGaze ()
	{
		Instance.OnUpdate += VisualizeGaze;

        PupilSettings.Instance.currentCamera = Camera.main;

        if ( !PupilMarker.TryToReset(_markerLeftEye) )
			_markerLeftEye= new PupilMarker("LeftEye_2D",PupilSettings.leftEyeColor);
		if ( !PupilMarker.TryToReset(_markerRightEye) )
			_markerRightEye = new PupilMarker("RightEye_2D",PupilSettings.rightEyeColor);
		if ( !PupilMarker.TryToReset(_markerGazeCenter) )
			_markerGazeCenter = new PupilMarker("Gaze_2D",Color.red);
		if ( !PupilMarker.TryToReset(_gaze3D) )
			_gaze3D = new PupilMarker("Gaze_3D", Color.yellow);

		PupilTools.IsGazing = true;
		PupilTools.SubscribeTo("gaze");
	}

	public void StopVisualizingGaze ()
	{
		Instance.OnUpdate -= VisualizeGaze;

		_markerLeftEye.SetActive (false);
		_markerRightEye.SetActive (false);
		_markerGazeCenter.SetActive (false);
		_gaze3D.SetActive (false);

//		PupilTools.UnSubscribeFrom("gaze");
	}

	void VisualizeGaze ()
	{
		if (PupilTools.IsGazing)
		{
			if (PupilTools.CalibrationMode == Calibration.Mode._2D)
			{
				_markerLeftEye.UpdatePosition(PupilData._2D.LeftEyePosition);
				_markerRightEye.UpdatePosition (PupilData._2D.RightEyePosition);
				_markerGazeCenter.UpdatePosition (PupilData._2D.GazePosition);
			}
			else if (PupilTools.CalibrationMode == Calibration.Mode._3D)
			{
				_gaze3D.UpdatePosition(PupilData._3D.GazePosition);
			}
		} 
	}

#endregion
}
