using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramePublishingDemoManager : MonoBehaviour 
{
	public int targetFPS = 20;
	public MeshRenderer rightEye;
	public MeshRenderer leftEye;
	public Texture2D rightEyeImage;
	public Texture2D leftEyeImage;

	byte[] raw0;
	byte[] raw1;
	bool streamCameraImages = false;

	void Start () 
	{
		PupilTools.OnConnected += StartBlinkSubscription;
		PupilTools.OnDisconnecting += StopBlinkSubscription;

		PupilTools.OnReceiveData += CustomReceiveData;
	}

	void StartBlinkSubscription()
	{
		streamCameraImages = true;
		InitializeFramePublishing ();

		PupilTools.SubscribeTo ("frame.");

		PupilTools.Send (new Dictionary<string,object> { { "subject","start_plugin" }, { "name","Frame_Publisher" } });
	}

	void StopBlinkSubscription()
	{
		UnityEngine.Debug.Log ("Disconnected");

		PupilTools.Send (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });

		PupilTools.UnSubscribeFrom ("frame.");

		streamCameraImages = false;
	}

	void CustomReceiveData(string topic, Dictionary<string,object> dictionary, byte[] thirdFrame = null)
	{
		if (thirdFrame == null)
			return;

		if ( topic == "frame.eye.0")
			raw0 = thirdFrame;
		if ( topic == "frame.eye.1")
			raw1 = thirdFrame;
	}

	float lastUpdate;
	public void InitializeFramePublishing ()
	{
		rightEyeImage = new Texture2D (100, 100);
		leftEyeImage = new Texture2D (100, 100);

		Shader shader = Shader.Find ("Unlit/Texture");

		rightEye.material = new Material (shader);
		rightEye.material.mainTexture = rightEyeImage;

		leftEye.material = new Material (shader);
		leftEye.material.mainTexture = leftEyeImage;
		leftEye.material.mainTextureScale = new Vector2 (-1, -1);

		lastUpdate = Time.time;
	}

	float elapsedTime = 0;
	void Update()
	{
		if (streamCameraImages)
		{
			//Put this in a function and delegate it to the OnUpdate delegate
			elapsedTime = Time.time - lastUpdate;
			if (elapsedTime >= (1f / targetFPS))
			{
				//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				rightEyeImage.LoadImage (raw0);
				leftEyeImage.LoadImage (raw1);
				lastUpdate = Time.time;
			}
		}
	}

	void OnDisable()
	{
		PupilTools.OnConnected -= StartBlinkSubscription;
		PupilTools.OnDisconnecting -= StopBlinkSubscription;

		PupilTools.OnReceiveData -= CustomReceiveData;
	}
}
