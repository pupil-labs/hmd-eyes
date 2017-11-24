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

	Thread udpThread;
	UdpClient udpClient;
	public int listeningPort = 12345;
	public int receivingPort = 12346;
	public string receivingIP = "192.168.1.90";

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
			IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

			while (true)
			{
				// receive bytes
				byte[] data = udpClient.Receive(ref anyIP);
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
			PupilConnection.sendRequestMessage (dictionary ["subject"].ToString (), message);
			UnityEngine.Debug.Log ("sendRequestMessage; subject: " + dictionary ["subject"].ToString ());
			break;
		case 30:
			UnityEngine.Debug.Log ("Update pupil time");
			PupilConnection.updatingPupilTimestamp = data [1] == 1;
			break;
		case 40:
			float time = System.BitConverter.ToSingle (data, 1);
			UnityEngine.Debug.Log ("Set time reference " + time.ToString ("0.00000000"));
			PupilConnection.SetPupilTimestamp (time);
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
		
	IPEndPoint remoteEndPoint;
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
		if (remoteEndPoint == null)
		{
			remoteEndPoint = new IPEndPoint(IPAddress.Parse(receivingIP), receivingPort);
		}

		messageSent = false;

		udpClient.BeginSend(data, data.Length, remoteEndPoint, 
			new AsyncCallback(SendCallback), udpClient);

		while (!messageSent)
		{
			Thread.Sleep(100);
		}
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
	public void StartCalibration()
	{
		SendUDPData(new byte[] { 90, 1 });
		calibrationStarted = true;
		calibrationButtonText.text = "Stop Calibration";
	}
	public void StopCalibration()
	{
		SendUDPData(new byte[] { 90, 0 });
	}
	public void ResetCalibrationButton()
	{
		calibrationStarted = false;
		calibrationButtonText.text = "Re-/Start Calibration";
	}

	// Use this for initialization
	void Start () 
	{
		StartUDPThread ();		

		InitializeCalibrationButton ();
	}

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

		PupilConnection.UpdatePupilTimestamp ();

		if ( isConnected != PupilConnection.isConnected)
		{
			isConnected = PupilConnection.isConnected;
		}
	}

	void OnDisable()
	{
		StopUDPThread ();

		PupilConnection.CloseSockets ();
	}
}
