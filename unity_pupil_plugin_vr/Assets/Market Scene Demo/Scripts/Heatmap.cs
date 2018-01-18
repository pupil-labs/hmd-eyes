using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Pupil;
using FFmpegOut;

public class Heatmap : MonoBehaviour 
{
	public Color highlightColor;
	public bool displayOnHeadset = false;
	public float removeHighlightPixelsAfterTimeInterval = 10;

	LayerMask collisionLayer;
	EStatus previousEStatus;

	Camera cam;
	// Use this for initialization
	void OnEnable () 
	{
		if (PupilTools.IsConnected)
		{
			if (PupilTools.DataProcessState != EStatus.ProcessingGaze)
			{
				previousEStatus = PupilTools.DataProcessState;
				PupilTools.DataProcessState = EStatus.ProcessingGaze;
				PupilTools.SubscribeTo ("gaze");
			}
		}

		cam = GetComponentInParent<Camera> ();
		transform.localPosition = Vector3.zero;

		InitializeHighlightTexture ();

		int heatmapLayer = LayerMask.NameToLayer ("Heatmap");
		collisionLayer = (1 << heatmapLayer);

		GetComponent<MeshRenderer> ().enabled = displayOnHeadset;

//		if (displayOnHeadset)
//			cam.cullingMask = cam.cullingMask | (1 << heatmapLayer);
//		else
//			cam.cullingMask &= ~(1 << heatmapLayer);

		highlightPixelsToBeRemoved = new Dictionary<Vector2, float> ();

		InitializeSpheres ();
	}

	void OnDisable()
	{
		if (previousEStatus != EStatus.ProcessingGaze)
		{
			PupilTools.DataProcessState = previousEStatus;
			PupilTools.UnSubscribeFrom ("gaze");
		}

		if ( _pipe != null)
			ClosePipe ();
	}

	[Range(0.125f,1f)]
	public float highlightSize = 1;
	int highlightTextureHeight = 128;
	Texture2D highlightTexture;
	void InitializeHighlightTexture()
	{
		highlightTextureHeight = (int)(128f / highlightSize);

		highlightTexture = new Texture2D (2*highlightTextureHeight, highlightTextureHeight,TextureFormat.ARGB32,false);
		Color[] cleared = new Color[highlightTexture.width * highlightTexture.height];
		for (int i = 0; i < cleared.Length; i++)
			cleared [i] = Color.clear;

		highlightTexture.SetPixels (cleared);
		highlightTexture.Apply ();

		heatmapMaterial = GetComponent<MeshRenderer> ().material;
		heatmapMaterial.SetTexture ("_MainTex", highlightTexture);

		if (highlightColor.a != 1)
			highlightColor.a = 1;
	}

	private RenderTexture _cubemap;
	public RenderTexture Cubemap
	{
		get
		{
			if (_cubemap == null)
			{
				_cubemap = new RenderTexture (2048, 2048, 0);
				_cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
				_cubemap.enableRandomWrite = true;
				_cubemap.Create ();
			}
			return _cubemap;
		}
	}

	public TextMesh infoText;
	public MeshFilter RenderingMeshFilter;
	public Camera RenderingCamera;
	Material heatmapMaterial;
	Material renderingMaterial;
	RenderTexture renderingTexture;
	void InitializeSpheres()
	{
		var sphereMesh = GetComponent<MeshFilter> ().mesh;
		if (sphereMesh.triangles [0] == 0)
		{
			sphereMesh.triangles = sphereMesh.triangles.Reverse ().ToArray ();
		}
		gameObject.AddComponent<MeshCollider> ();

		if (RenderingMeshFilter != null)
		{
			var mesh = RenderingMeshFilter.mesh;

			var normals = mesh.normals;
			Color[] colors = new Color[normals.Length];
			for (int i = 0; i < colors.Length; i++)
			{
				var normal = normals [i].normalized;
				colors [i] = new Color (normal.x, normal.y, normal.z);
			}
			mesh.colors = colors;

			var newCoordsFromUV = mesh.uv;
			Vector3[] newCoords = new Vector3[newCoordsFromUV.Length];
			Vector3[] newNormals = new Vector3[newCoordsFromUV.Length];
			for (int i = 0; i < newCoordsFromUV.Length; i++)
			{
				newCoords [i] = newCoordsFromUV [i] - Vector2.one * 0.5f;
				newCoords [i].x *= 2;
				newNormals [i] = Vector3.forward;
			}
			mesh.vertices = newCoords;
			mesh.RecalculateNormals ();

			renderingMaterial = RenderingMeshFilter.gameObject.GetComponent<MeshRenderer> ().material;
			renderingMaterial.SetTexture ("_MainTex", highlightTexture);
			renderingMaterial.SetTexture ("_Cubemap", Cubemap);
			renderingMaterial.SetColor ("_highlightColor", highlightColor);

			if (RenderingCamera != null)
			{
				RenderingCamera.aspect = 2;
				renderingTexture = new RenderTexture (2048, 1024, 0);
				RenderingCamera.targetTexture = renderingTexture;
			}

			RenderingMeshFilter.gameObject.transform.parent = null;
		}
	}

	Dictionary<Vector2,float> highlightPixelsToBeRemoved;
	bool updateHighlightTexture = false;
	Texture2D temporaryTexture;
	void Update () 
	{
		transform.rotation = Quaternion.identity;

		if (PupilTools.IsConnected && PupilTools.DataProcessState == EStatus.ProcessingGaze)
		{
			Vector2 gazePosition = PupilData._2D.GetEyeGaze (GazeSource.BothEyes);

			RaycastHit hit;
//			if (Input.GetMouseButton(0) && Physics.Raycast(cam.ScreenPointToRay (Input.mousePosition), out hit, 1f, (int) collisionLayer))
			if (Physics.Raycast(cam.ViewportPointToRay (gazePosition), out hit, 1f, (int)collisionLayer))
			{
				if ( hit.collider.gameObject != gameObject )
					return;
			
				Vector2 pixelUV = hit.textureCoord;
				pixelUV.x = (int) (pixelUV.x*highlightTexture.width);
				pixelUV.y = (int) (pixelUV.y*highlightTexture.height);

				highlightTexture.SetPixel ((int)pixelUV.x, (int)pixelUV.y, highlightColor);
				updateHighlightTexture = true;

				if (removeHighlightPixelsAfterTimeInterval > 0)
				{
					if (highlightPixelsToBeRemoved.ContainsKey (pixelUV))
						highlightPixelsToBeRemoved [pixelUV] = Time.time + removeHighlightPixelsAfterTimeInterval;
					else
						highlightPixelsToBeRemoved.Add (pixelUV, Time.time + removeHighlightPixelsAfterTimeInterval);
				}
			}
		}
		var removablePixels = highlightPixelsToBeRemoved.Where(p => p.Value < Time.time);
		for (int i = 0; i < removablePixels.Count() ; i++)
		{
			var pixel = removablePixels.ElementAt (i);
			highlightTexture.SetPixel ((int)pixel.Key.x, (int)pixel.Key.y, Color.clear);
			updateHighlightTexture = true;
			highlightPixelsToBeRemoved.Remove (pixel.Key);
		}

		if (updateHighlightTexture)
		{
			highlightTexture.Apply ();
			updateHighlightTexture = false;
		}

		if (Input.GetKeyUp (KeyCode.H))
			recording = !recording;

		if (recording)
		{
			if ( renderingMaterial != null)
				cam.RenderToCubemap (Cubemap);
			
			if (infoText.gameObject.activeInHierarchy)
				infoText.gameObject.SetActive (false);
			
			if (_pipe == null)
				OpenPipe ();
			else
			{
				previouslyActiveRenderTexture = RenderTexture.active;

				RenderTexture.active = renderingTexture;
				if (temporaryTexture == null)
				{
					temporaryTexture = new Texture2D (renderingTexture.width, renderingTexture.height, TextureFormat.RGB24, false);
				}
				temporaryTexture.ReadPixels (new Rect (0, 0, renderingTexture.width, renderingTexture.height), 0, 0, false);
				temporaryTexture.Apply ();

				// With the winter 2017 release of this plugin, Pupil timestamp is set to Unity time when connecting
				timeStampList.Add (Time.time);
				_pipe.Write (temporaryTexture.GetRawTextureData ());

				RenderTexture.active = previouslyActiveRenderTexture;
			}
		} else
		{
			if (_pipe != null)
				ClosePipe ();
		}
	}

	bool recording = false;
	RenderTexture previouslyActiveRenderTexture;

	FFmpegPipe _pipe;
	List<double> timeStampList = new List<double>();
	int _frameRate = 30;

	void OpenPipe()
	{
		timeStampList = new List<double> ();

		// Open an output stream.
		_pipe = new FFmpegPipe("Heatmap", renderingTexture.width, renderingTexture.height, _frameRate, PupilSettings.Instance.recorder.codec);

		Debug.Log("Capture started (" + _pipe.Filename + ")");
	}

	void ClosePipe()
	{
		// Close the output stream.
		Debug.Log ("Capture ended (" + _pipe.Filename + ").");

		// Write pupil timestamps to a file
		string timeStampFileName = "Heatmap_Timestamps";
		byte[] timeStampByteArray = PupilConversions.doubleArrayToByteArray (timeStampList.ToArray ());
		File.WriteAllBytes(_pipe.FilePath + "/" + timeStampFileName + ".time", timeStampByteArray);

		_pipe.Close();

		if (!string.IsNullOrEmpty(_pipe.Error))
		{
			Debug.LogWarning(
				"ffmpeg returned with a warning or an error message. " +
				"See the following lines for details:\n" + _pipe.Error
			);
		}

		_pipe = null;

		if (!infoText.gameObject.activeInHierarchy)
			infoText.gameObject.SetActive (true);
	}
}
