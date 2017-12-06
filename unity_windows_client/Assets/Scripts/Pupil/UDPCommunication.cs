using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UDPCommunication : MonoBehaviour 
{
	private Connection _pupilConnection;
	public Connection PupilConnection
	{
		get
		{
			if (_pupilConnection == null)
				_pupilConnection = new Connection (this);
			return _pupilConnection;
		}
	}
	private PupilTools _pupilTools;
	public PupilTools pupilTools
	{
		get
		{
			if (_pupilTools == null)
				_pupilTools = new PupilTools ();
			return _pupilTools;
		}
	}

	Thread udpThread;
	UdpClient udpClient;
	public int listeningPort = 12345;
	public int hololensClientPort = 12346;
	public string hololensClientIP = "192.168.1.90";

	void StartUDPThread()
	{
		StopUDPThread ();

		// create thread for reading UDP messages
		udpThread = new Thread(new ThreadStart(Listen));
		udpThread.IsBackground = true;
		udpThread.Start();
	}

	// Stop reading UDP messages
	private void StopUDPThread()
	{
		if (udpThread != null && udpThread.IsAlive)
		{
			udpThread.Abort();
		}
		if (udpClient != null)
			udpClient.Close();
	}

	private void Listen()
	{
		try
		{
			udpClient = new UdpClient(listeningPort);
			print ("Started UDP client on port: " + listeningPort);

			while (true)
			{
				// receive bytes
				byte[] data = udpClient.Receive(ref _remoteEndPoint);
				InterpreteByteData(data);
			}
		}
		catch (Exception err)
		{
			print(err.ToString());
		}
	}

	void InterpreteByteData(byte[] data)
	{
		byte id = data [0];

		byte[] message = new byte[data.Length - 1];
		for (int i = 1; i < data.Length; i++)
		{
			message [i - 1] = data [i];
		}

		switch (id)
		{
		// InitializeSubscriptionSocket
		case 1:
			UnityEngine.Debug.Log ("InitializeSubscriptionSocket");
			string initializeTopic = Encoding.ASCII.GetString (message);
			PupilConnection.InitializeSubscriptionSocket (initializeTopic);
			break;
			// CloseSubscriptionSocket
		case 2:
			UnityEngine.Debug.Log ("CloseSubscriptionSocket");
			string closeTopic = Encoding.ASCII.GetString (message);
			PupilConnection.CloseSubscriptionSocket (closeTopic);
			break;
		// sendRequestMessage
		case 10:
			var dictionary = MessagePack.MessagePackSerializer.Deserialize<Dictionary<string,object>> (message);
			var subject = dictionary ["subject"].ToString ();
			if (waitingForCalibrationStart && subject == "start_plugin")
				waitingForCalibrationStart = false;
			if (waitingForConnectionToBeCommunicated && subject.StartsWith ("eye_process.should_start"))
				waitingForConnectionToBeCommunicated = false;
			PupilConnection.sendRequestMessage (subject, message);
			UnityEngine.Debug.Log ("sendRequestMessage; subject: " + subject);
			break;
		case 40:
			float time = System.BitConverter.ToSingle (data, 1);
			UnityEngine.Debug.Log ("Set time reference " + time.ToString ("0.00000000"));
			PupilConnection.SetPupilTimestamp (time);
			break;
		case 50:
			UnityEngine.Debug.Log ("Setting PupilData UDP mode to " + data [1].ToString ());
			PupilData.mode = (PupilData.udpMode)(int)data [1];
			break;
		case 59:
			UnityEngine.Debug.Log ("Calculate moving average " + (data [1] == 1).ToString ());
			PupilData.calculateMovingAverage = data [1] == 1;
			break;
		// Calling functions
		default:
			string functionName = Encoding.ASCII.GetString (message);
			UnityEngine.Debug.Log (functionName);
			switch (functionName)
			{
			case "InitializeRequestSocket":
				PupilConnection.InitializeRequestSocket ();
				break;
			case "CloseSockets":
				PupilConnection.CloseSockets ();
				break;
			case "TerminateContext":
				PupilConnection.TerminateContext ();
				break;	
			default:
				print (functionName);
				break;
			}
			break;
		}
	}

	IPEndPoint _remoteEndPoint;
	IPEndPoint remoteEndPoint
	{
		get 
		{
			if ( _remoteEndPoint == null )
				_remoteEndPoint = new IPEndPoint(IPAddress.Parse(hololensClientIP), hololensClientPort);
			return _remoteEndPoint;
		}
	}
	public bool messageSent = true;
	public void SendCallback(IAsyncResult ar)
	{
		UdpClient u = (UdpClient)ar.AsyncState;
		u.EndSend (ar);
//		Console.WriteLine("number of bytes sent: {0}", u.EndSend(ar));
		messageSent = true;
	}


	public void SendUDPData(byte[] data)
	{
//		messageSent = false;
		try
		{
			udpClient.Send (data, data.Length, remoteEndPoint);
		}
		catch (Exception e)
		{
			UnityEngine.Debug.Log (e.ToString ());
		}
//		udpClient.BeginSend(data, data.Length, remoteEndPoint, 
//			new AsyncCallback(SendCallback), udpClient);
//
//		while (!messageSent)
//		{
//			Thread.Sleep(1);
//		}
	}

	bool calibrationStarted = false;
	Button calibrationButton;
	Text calibrationButtonText;
	void InitializeCalibrationButton()
	{
		calibrationButton = GameObject.Find ("CalibrationButton").GetComponent<Button> ();
		calibrationButtonText = calibrationButton.gameObject.GetComponentInChildren<Text> ();
	}
	public void CalibrationButtonClicked()
	{
		if (calibrationStarted)
			StopCalibration ();
		else
			StartCalibration ();
	}

	bool waitingForCalibrationStart = true;
	public void StartCalibration()
	{
		PupilData.mode = PupilData.udpMode.DontSendData;
		UnityEngine.Debug.Log ("UDP: Starting calibration");
		SendUDPData(new byte[] { 90, 1 });
		waitingForCalibrationStart = true;
	}
	public void CalibrationStarted()
	{
		calibrationStarted = true;
		calibrationButtonText.text = "Stop Calibration";
	}
	public void StopCalibration()
	{
		UnityEngine.Debug.Log ("UDP: Stopping calibration");
		SendUDPData(new byte[] { 90, 0 });
	}
	public void ResetCalibrationButton()
	{
		calibrationStarted = false;
		waitingForCalibrationStart = true;
		calibrationButtonText.text = "Re-/Start Calibration";
	}

	// Use this for initialization
	void Start () 
	{
		StartUDPThread ();		

		InitializeCalibrationButton ();
	}

	bool waitingForConnectionToBeCommunicated = true;
	bool _isConnected = false;
	bool isConnected
	{
		get { return _isConnected; }
		set
		{
			_isConnected = value;
			if (_isConnected)
			{
				calibrationButton.interactable = true;
				calibrationButtonText.text = "Start Calibration";
			}
		}
	}

	void Update () 
	{
		PupilConnection.UpdateSubscriptionSockets ();

		UpdatePupilData ();

		if (!waitingForConnectionToBeCommunicated && !isConnected)
			isConnected = true;

		if (!waitingForCalibrationStart && !calibrationStarted)
			CalibrationStarted ();

//		try 
//		{
//		if (dataToSend.Count > 0)
//		{
//			var current = dataToSend[0];
//			udpClient.Send (current, current.Length, remoteEndPoint);
//			dataToSend.RemoveAt(0);
//		}
//		}
//		catch (Exception e)
//		{
//			UnityEngine.Debug.Log (e.ToString ());
//		}
	}

	void UpdatePupilData()
	{
		switch (PupilData.mode)
		{
		case PupilData.udpMode.LeftEyeOnly:
			byte[] leftEyedata = Vector2ToByteArray (PupilData._2D.GetEyeGaze (PupilData.GazeSource.LeftEye));
			leftEyedata [0] = 52;
			SendUDPData (leftEyedata);
			break;
		case PupilData.udpMode.RightEyeOnly:
			byte[] rightEyeData = Vector2ToByteArray (PupilData._2D.GetEyeGaze (PupilData.GazeSource.RightEye));
			rightEyeData [0] = 53;
			SendUDPData (rightEyeData);
			break;
		case PupilData.udpMode.LeftAndRight:
			byte[] dataLeft = Vector2ToByteArray (PupilData._2D.GetEyeGaze (PupilData.GazeSource.LeftEye));
			dataLeft [0] = 52;
			SendUDPData (dataLeft);
			byte[] dataRight = Vector2ToByteArray (PupilData._2D.GetEyeGaze (PupilData.GazeSource.RightEye));
			dataRight [0] = 53;
			SendUDPData (dataRight);
			break;
		case PupilData.udpMode.Gaze2D:
			byte[] gaze2D = Vector2ToByteArray (PupilData._2D.GetEyeGaze (PupilData.GazeSource.BothEyes));
			gaze2D [0] = 54;
			SendUDPData (gaze2D);
			break;
		case PupilData.udpMode.Gaze3D:
			byte[] gaze3D = Vector3ToByteArray (PupilData._3D.GazePosition);
			gaze3D [0] = 55;
			SendUDPData (gaze3D);
			break;
		default:
			break;
		}
	}

	byte[] Vector2ToByteArray (Vector2 position)
	{
		byte[] data = new byte[1 + 2 * sizeof(float)];

		for (int i = 0; i < 2; i++)
		{
			byte[] xy = BitConverter.GetBytes (position.x);
			if ( i==1 )
				xy = BitConverter.GetBytes (position.y);
			for (int j = 0; j < xy.Length; j++) 
			{
				data [1 + i * sizeof(float) + j] = xy [j];
			}
		}
		return data;
	}
	byte[] Vector3ToByteArray (Vector3 position)
	{
		byte[] data = new byte[1 + 3 * sizeof(float)];

		for (int i = 0; i < 3; i++)
		{
			byte[] xy = BitConverter.GetBytes (position.x);
			if ( i==1 )
				xy = BitConverter.GetBytes (position.y);
			else if ( i== 2 )
				xy = BitConverter.GetBytes (position.z);
			for (int j = 0; j < xy.Length; j++) 
			{
				data [1 + i * sizeof(float) + j] = xy [j];
			}
		}
		return data;
	}

	void OnDisable()
	{
		PupilConnection.CloseSockets ();

		StopUDPThread ();
	}
}
