using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using MessagePack;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PupilTools : MonoBehaviour {

	public static PupilSettings pupilSettings = null;

	public delegate void GUIRepaintAction();//InspectorGUI repaint
	public delegate void OnCalibrationStartDeleg();
	public delegate void OnCalibrationEndDeleg();

	public static event GUIRepaintAction WantRepaint;

	public static event OnCalibrationStartDeleg OnCalibrationStarted;
	public static event OnCalibrationEndDeleg OnCalibrationEnded;


	#region Recording

	public static void StartPupilServiceRecording (string path){

		var _p = path.Substring (2);

		PupilTools._sendRequestMessage (new Dictionary<string,object> {{"subject","recording.should_start"},{"session_name",_p}});

	}

	public static void StopPupilServiceRecording (){

		PupilTools._sendRequestMessage (new Dictionary<string,object> {{"subject","recording.should_stop"}});

	}

	#endregion


	#region Calibration

	public static void RepaintGUI(){
		if (WantRepaint != null)
			WantRepaint ();
	}

	public static SubscriberSocket ClearAndInitiateSubscribe(){

		if (PupilSettings.Instance.connection.subscribeSocket != null) {
			
			PupilSettings.Instance.connection.subscribeSocket.Close ();

		}

		SubscriberSocket _subscriberSocket = new SubscriberSocket (PupilSettings.Instance.connection.IPHeader + PupilSettings.Instance.connection.subport);

		//André: Is this necessary??
		_subscriberSocket.Options.SendHighWatermark = PupilSettings.Instance.numberOfMessages;// 6;

		PupilSettings.Instance.connection.topicList.ForEach(p=>_subscriberSocket.Subscribe(p));

		return _subscriberSocket;

	}

	public static void SubscribeTo(string topic){

		if (!PupilSettings.Instance.connection.topicList.Contains (topic)) {
			
			PupilSettings.Instance.connection.topicList.Add (topic);

		}

		PupilSettings.Instance.connection.subscribeSocket = ClearAndInitiateSubscribe ();

	}

	public static void UnSubscribeFrom(string topic){

		if (PupilSettings.Instance.connection.topicList.Contains (topic)) {
			PupilSettings.Instance.connection.topicList.Remove (topic);
		}

		PupilSettings.Instance.connection.subscribeSocket = ClearAndInitiateSubscribe ();

	}

	public static void StartCalibration(){

		PupilGazeTracker.Instance.StartCoroutine ("InitializeCalibration");

		_sendRequestMessage ( new Dictionary<string,object> {{"subject","start_plugin"},{"name",pupilSettings.calibration.currentCalibrationType.pluginName}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_start"},{"hmd_video_frame_size",new float[]{1000,1000}},{"outlier_threshold",35}});

		if (OnCalibrationStarted != null)
			OnCalibrationStarted ();
		else
		{
			print ("No 'calibration started' delegate set");
		}

	}

	public static void StopCalibration()
	{
		
		pupilSettings.calibration.initialized = false;
		pupilSettings.dataProcess.state = PupilSettings.EStatus.ProcessingGaze;
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","calibration.should_stop"}});

		if (pupilSettings.visualizeGaze) {
			CalibrationGL.InitializeVisuals (PupilSettings.EStatus.ProcessingGaze);
		} else {
			CalibrationGL.InitializeVisuals (PupilSettings.EStatus.Idle);
		}

		if ( OnCalibrationEnded != null )
			OnCalibrationEnded ();
		else
		{
			print ("No 'calibration ended' delegate set");
		}

	}

	#endregion


	public static NetMQMessage _sendRequestMessage(Dictionary<string,object> data)
	{
		NetMQMessage m = new NetMQMessage ();

		m.Append ("notify." + data ["subject"]);
		m.Append (MessagePackSerializer.Serialize<Dictionary<string,object>> (data));

		PupilDataReceiver.Instance._requestSocket.SendMultipartMessage (m);

		NetMQMessage recievedMsg;
		recievedMsg = PupilDataReceiver.Instance._requestSocket.ReceiveMultipartMessage ();

		return recievedMsg;
	}

	public static float GetPupilTimestamp()
	{
		PupilDataReceiver.Instance._requestSocket.SendFrame ("t");
		NetMQMessage recievedMsg = PupilDataReceiver.Instance._requestSocket.ReceiveMultipartMessage ();
		return float.Parse (recievedMsg [0].ConvertToString ());
	}

	public static void StartEyeProcesses()
	{
		_sendRequestMessage (new Dictionary<string,object> { { "subject","eye_process.should_start.0" }, { "eye_id",PupilSettings.leftEyeID } });
		_sendRequestMessage (new Dictionary<string,object> { { "subject","eye_process.should_start.1" }, { "eye_id",PupilSettings.rightEyeID } });
	}

	public static void StopEyeProcesses()
	{
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",PupilSettings.leftEyeID}});
		_sendRequestMessage ( new Dictionary<string,object> {{"subject","eye_process.should_stop"},{"eye_id",PupilSettings.rightEyeID}});
	}

	public static void SavePupilSettings(ref PupilSettings pupilSettings){
	
		#if UNITY_EDITOR
		AssetDatabase.Refresh ();
		EditorUtility.SetDirty (pupilSettings);
		AssetDatabase.SaveAssets ();
		#endif

	}

	public static PupilSettings GetPupilSettings(){

		if (pupilSettings == null) {
			pupilSettings = Resources.LoadAll<PupilSettings> ("") [0];
//			print (pupilSettings);	
		}
		
		return pupilSettings;
	
	}

	public static bool PupilGazeTrackerExists(){//this could/should be done with .Instance of the singleton type, but for Unity Editor update a FindObjectOfType seems more effective.
	
		if (FindObjectOfType<PupilGazeTracker> () == null) {
			return false;
		} else {
			return true;
		}

	}

	public static void Connect(){
	
		if (PupilSettings.Instance.connection.isLocal)
			PupilTools.RunServiceAtPath ();

		PupilDataReceiver.Instance.RunConnect ();

	}

	public static void RunServiceAtPath(bool runEyeProcess = false){

		string servicePath = PupilSettings.Instance.pupilServiceApp.servicePath;

		if (File.Exists (servicePath)) {
		
			if (Process.GetProcessesByName ("pupil_capture").Length > 0) {
			
				UnityEngine.Debug.LogWarning (" Pupil Capture is already running ! ");
			
			} else {
			
				Process serviceProcess = new Process ();
				serviceProcess.StartInfo.Arguments = servicePath;
				serviceProcess.StartInfo.FileName = servicePath;
//				serviceProcess.StartInfo.CreateNoWindow = true;
//				serviceProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
//				serviceProcess.StartInfo.UseShellExecute = false;
//				serviceProcess.StartInfo.RedirectStandardOutput = true;     

				if (File.Exists (servicePath)) {
				
					serviceProcess.Start ();
				
				} else {
				
					UnityEngine.Debug.LogWarning ("Pupil Service could not start! There is a problem with the file path. The file does not exist at given path");
			
				}
			}

		} else{

			if (servicePath == "") {
			
				UnityEngine.Debug.LogWarning ("Pupil Service filename is not specified ! Please configure it under the Pupil plugin settings");
			
			}

		}

	}
}
