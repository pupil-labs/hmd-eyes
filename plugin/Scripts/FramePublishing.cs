using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PupilLabs
{
	
	public class FramePublishing : MonoBehaviour 
	{
		public RequestController requestController;
		public SubscriptionsController subscriptionsController;

		public int targetFPS = 20;
		public Transform cameraAsParent; //TODO pose not 100% clear yet

		[SerializeField]
		Texture2D[] eyeTexture = new Texture2D[2];
		byte[][] eyeImageRaw = new byte[2][];
		MeshRenderer[] eyeRenderer = new MeshRenderer[2];
		bool[] eyePublishingInitialized = new bool[2];

		void OnEnable () 
		{
			if (cameraAsParent == null)
			{
				Debug.LogWarning("Frame Publisher needs the camera transform");
				enabled = false;
				return;
			}

			if (requestController == null || subscriptionsController == null)
			{
				Debug.LogWarning("Frame Publisher needs access to Subscriptions and Request Controller");
				enabled = false;
				return;
			}

			eyePublishingInitialized = new bool[] { false, false };

			subscriptionsController.SubscribeTo ("frame.",CustomReceiveData);

			requestController.StartPlugin("Frame_Publisher");
		}

		private static object[] position_o;
		Vector2 PixelPosition (object position)
		{
			position_o = position as object[];
			Vector2 result = Vector2.zero;
			if (position_o.Length != 2)
				UnityEngine.Debug.Log ("Array length not supported");
			else
			{
				result.x = float.Parse(position_o [1].ToString());
				result.y = float.Parse(position_o [0].ToString());
			}
			return result;
		}
		float FloatForKeyInDictionary (Dictionary<string,object> dictionary, string key)
		{
			if (dictionary.ContainsKey (key))
				return float.Parse (dictionary [key].ToString ());
			return 0;
		}
		void CustomReceiveData(string topic, Dictionary<string,object> dictionary, byte[] thirdFrame = null)
		{
	//		if (topic == "pupil.0" || topic == "pupil.1")
	//			UpdateSphereProjection (eyeIndex: topic == "pupil.0" ? 0 : 1, dictionary: dictionary);

			if (thirdFrame == null)
				return;

			if (topic == "frame.eye.0")
			{
				if (!eyePublishingInitialized [0])
					InitializeFramePublishing (0);
				eyeImageRaw [0] = thirdFrame;
			} 
			else if (topic == "frame.eye.1")
			{
				if (!eyePublishingInitialized [1])
					InitializeFramePublishing (1);
				eyeImageRaw [1] = thirdFrame;
			}
		}

		public void InitializeFramePublishing (int eyeIndex)
		{
			Transform parent = cameraAsParent;
			Shader shader = Shader.Find ("Unlit/Texture");

			eyeTexture[eyeIndex] = new Texture2D (100, 100);
			eyeRenderer[eyeIndex] = InitializeEyeObject (eyeIndex, parent);
			eyeRenderer[eyeIndex].material = new Material (shader);
			eyeRenderer[eyeIndex].material.mainTexture = eyeTexture[eyeIndex];
			if (eyeIndex==1)
				eyeRenderer[eyeIndex].material.mainTextureScale = new Vector2 (-1, -1);
			
			lastUpdate = Time.time;

			eyePublishingInitialized [eyeIndex] = true;
		}

		MeshRenderer InitializeEyeObject(int eyeIndex, Transform parent)
		{
			GameObject go = GameObject.CreatePrimitive (PrimitiveType.Plane);
			go.name = "Eye " + eyeIndex.ToString();
			go.transform.parent = parent;
			go.transform.localEulerAngles = Vector3.left * 90;
			go.transform.localScale = Vector3.one * 0.05f;
			go.transform.localPosition = new Vector3 ((eyeIndex == 0 ? -0.3f : 0.3f), -0.5f, 1.9999f);

			Destroy (go.GetComponent<Collider> ());

			return go.GetComponent<MeshRenderer> ();
		}

		float lastUpdate;
		void Update()
		{
			//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
			if ((Time.time - lastUpdate) >= (1f / targetFPS))
			{
				for (int i = 0; i < 2; i++)
					if (eyePublishingInitialized [i])
						eyeTexture [i].LoadImage (eyeImageRaw [i]);
				lastUpdate = Time.time;
			}
		}

		void OnDisable()
		{
			Debug.Log ("Disabling Frame Publisher");

			if (requestController != null)
			{
				requestController.StopPlugin("Frame_Publisher");
			}

			if (subscriptionsController != null)
			{
				subscriptionsController.UnsubscribeFrom("frame",CustomReceiveData);
			}

			for (int i = eyeRenderer.Length - 1; i >= 0; i--)
				if (eyeRenderer [i] != null && eyeRenderer [i].gameObject != null)
					Destroy (eyeRenderer [i].gameObject);
		}
	}
}


