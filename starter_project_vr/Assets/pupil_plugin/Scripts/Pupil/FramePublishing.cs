using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramePublishing : MonoBehaviour 
{
	int targetFPS = 20;
	public Texture2D[] eyeTexture = new Texture2D[2];
	byte[][] eyeImageRaw = new byte[2][];
	MeshRenderer[] eyeRenderer = new MeshRenderer[2];
	bool[] eyePublishingInitialized = new bool[2];

//	public bool visualizeSphereProjection = false;

	void OnEnable () 
	{
		// Main
		if (GetComponentInChildren<Camera> () == null)
		{
			Debug.Log ("Frame Publisher could not find a Camera gameobject");
			return;
		}
		eyePublishingInitialized = new bool[] { false, false };

		PupilTools.SubscribeTo ("frame.");

		PupilTools.Send (new Dictionary<string,object> { { "subject","start_plugin" }, { "name","Frame_Publisher" } });

//		// Sphere Projection
//		if (visualizeSphereProjection)
//		{
//			if (PupilTools.CalibrationMode == Calibration.Mode._3D)
//			{
//				PupilTools.SubscribeTo ("pupil.");
//			} 
//			else
//				Debug.Log ("Sphere projections are only available for 3D calibration");
//		}

		PupilTools.OnReceiveData += CustomReceiveData;
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
		Transform parent = GetComponentInChildren<Camera> ().transform;
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

//		if (visualizeSphereProjection)
//		{
//			var circleGO = GameObject.Instantiate (go, go.transform);
//			circleGO.transform.localPosition = Vector3.zero;
//			circleGO.transform.localEulerAngles = Vector3.zero;
//			circleGO.transform.localScale = Vector3.one;
//			sphereProjectionMaterial [eyeIndex] = new Material (Resources.Load<Material> ("Materials/Circle"));
//			sphereProjectionMaterial [eyeIndex].SetColor ("_TintColor", Color.green);
//			circleGO.GetComponent<MeshRenderer> ().material = sphereProjectionMaterial [eyeIndex];
//		}

		return go.GetComponent<MeshRenderer> ();
	}

	Material[] sphereProjectionMaterial = new Material[2];
	void UpdateSphereProjection(int eyeIndex, Dictionary<string,object> dictionary)
	{
		var innerDictionary = PupilTools.DictionaryFromDictionary (dictionary, "projected_sphere");
		if (innerDictionary != null)
		{
			foreach (var item in innerDictionary)
			{
				switch (item.Key.ToString ())
				{
				case "angle": // Currently always 90
					break;
				case "axes":
					var axes = PixelPosition (item.Value);
					axes.x = eyeTexture[eyeIndex].width / axes.x;
					axes.y = eyeTexture[eyeIndex].height / axes.y;
					sphereProjectionMaterial [eyeIndex].mainTextureScale = axes;
					break;
				case "center":
					var centerPosition = PixelPosition (item.Value);
					centerPosition.x /= eyeTexture[eyeIndex].width;// / sphereProjectionMaterial [eyeIndex].mainTextureScale.x;
					centerPosition.y /= eyeTexture[eyeIndex].height;// / sphereProjectionMaterial [eyeIndex].mainTextureScale.y;
					sphereProjectionMaterial [eyeIndex].mainTextureOffset = centerPosition;
					break;
				default :
					break;
				}
			}
		}
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
		UnityEngine.Debug.Log ("Disabling Frame Publisher");

		PupilTools.Send (new Dictionary<string,object> { { "subject","stop_plugin" }, { "name", "Frame_Publisher" } });

		PupilTools.UnSubscribeFrom ("frame.");

		for (int i = eyeRenderer.Length - 1; i >= 0; i--)
			if (eyeRenderer [i] != null && eyeRenderer [i].gameObject != null)
				Destroy (eyeRenderer [i].gameObject);

		PupilTools.OnReceiveData -= CustomReceiveData;
	}
}
