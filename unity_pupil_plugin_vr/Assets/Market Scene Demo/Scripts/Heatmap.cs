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
	public float removeParticleAfterTimeInterval = 10;
	public TextMesh infoText;

	int heatmapLayer;
	LayerMask collisionLayer;
	EStatus previousEStatus;

	Camera cam;
	Camera RenderingCamera;
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
		RenderingCamera = GetComponentInChildren<Camera> ();

		transform.localPosition = Vector3.zero;

		heatmapLayer = LayerMask.NameToLayer ("Heatmap");
		collisionLayer = (1 << heatmapLayer);

		InitializeMeshes ();
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
	public float particleSize = 1;

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

	MeshFilter RenderingMeshFilter;
	Material renderingMaterial;
	RenderTexture renderingTexture;
	void InitializeMeshes()
	{
		var sphereMesh = GetComponent<MeshFilter> ().mesh;
		if (sphereMesh.triangles [0] == 0)
		{
			sphereMesh.triangles = sphereMesh.triangles.Reverse ().ToArray ();
		}
		gameObject.AddComponent<MeshCollider> ();
		visualization = GetComponentInChildren<ParticleSystem> ();

		if (RenderingCamera != null)
		{
			RenderingCamera.aspect = 2;
			renderingTexture = new RenderTexture (2048, 1024, 0);
			RenderingCamera.targetTexture = renderingTexture;

			RenderingMeshFilter = RenderingCamera.GetComponentInChildren<MeshFilter> ();
			RenderingMeshFilter.mesh = GeneratePlaneWithSphereNormals ();
			renderingMaterial = RenderingCamera.GetComponentInChildren<MeshRenderer> ().material;
			renderingMaterial.SetTexture ("_Cubemap", Cubemap);

			RenderingCamera.transform.parent = null;
		}
	}

	int sphereMeshHeight = 32;
	int sphereMeshWidth = 32;
	Vector2 sphereMeshCenterOffset = Vector2.one * 0.5f;
	Mesh GeneratePlaneWithSphereNormals()
	{
		Mesh result = new Mesh ();

		var vertices = new Vector3[sphereMeshHeight * sphereMeshWidth];
		var normals = new Vector3[sphereMeshHeight * sphereMeshWidth];
		var uvs = new Vector2[sphereMeshHeight * sphereMeshWidth];

		List<int> triangles = new List<int> ();

		for (int i = 0; i < sphereMeshHeight; i++)
		{
			for (int j = 0; j < sphereMeshWidth; j++)
			{
				Vector2 uv = new Vector2 ((float)j / (float)(sphereMeshWidth - 1), (float)i / (float)(sphereMeshHeight - 1));
				uvs [j + i * sphereMeshWidth] = new Vector2(1f - uv.x, 1f - uv.y);
				normals [j + i * sphereMeshWidth] = NormalForUV (uv);
				vertices [j + i * sphereMeshWidth] = PositionForUV(uv);

				if (i > 0 && j > 0)
				{
					triangles.Add (j + i * sphereMeshWidth);
					triangles.Add ((j - 1) + (i - 1) * sphereMeshWidth);
					triangles.Add ((j - 1) + i * sphereMeshWidth);
					triangles.Add (j + (i - 1) * sphereMeshWidth);
					triangles.Add ((j - 1) + (i - 1) * sphereMeshWidth);
					triangles.Add (j + i * sphereMeshWidth);
				}
			}
		}
		result.vertices = vertices;
		result.normals = normals;
		result.triangles = triangles.ToArray ();
		result.uv = uvs;

		return result;
	}

	Vector3 PositionForUV (Vector2 uv)
	{
		Vector2 position = uv;
		position -= sphereMeshCenterOffset;
		position.x *= RenderingCamera.aspect;
		position.y *= RenderingCamera.orthographicSize * 2f;
		return position;
	}

	Vector3 NormalForUV (Vector2 uv)
	{
		var normal = Vector3.zero;
		if (uv.x <= 0.25f)
			normal = Vector3.Slerp (Vector3.back, Vector3.left, uv.x * 4f);
		else if (uv.x <= 0.5f)
			normal = Vector3.Slerp (Vector3.left, Vector3.forward, (uv.x - 0.25f) * 4f);
		else if (uv.x <= 0.75f)
			normal = Vector3.Slerp (Vector3.forward, Vector3.right, (uv.x - 0.5f) * 4f);
		else //if (uv.x <= 1f)
			normal = Vector3.Slerp (Vector3.right, Vector3.back, (uv.x - 0.75f) * 4f);

		if (uv.y <= 0.5f)
			normal = Vector3.Slerp (Vector3.up, normal, uv.y * 2f);
		else //if (uv.y <= 1f)
			normal = Vector3.Slerp (normal, Vector3.down, (uv.y - 0.5f) * 2f);

		return normal;
	}

	ParticleSystem visualization;
	ParticleSystem.EmitParams particleSystemParameters = new ParticleSystem.EmitParams ();

	Texture2D temporaryTexture;
	void Update () 
	{
		// Keep heatmap collider rotation constant. '-90' derives from the sphere mesh normals we construct above
		transform.eulerAngles = Vector3.up * -90f;

//		if (PupilTools.IsConnected && PupilTools.DataProcessState == EStatus.ProcessingGaze)
//		{
//			Vector2 gazePosition = PupilData._2D.GetEyeGaze (GazeSource.BothEyes);

			RaycastHit hit;
			if (Input.GetMouseButton(0) && Physics.Raycast(cam.ScreenPointToRay (Input.mousePosition), out hit, 1f, (int) collisionLayer))
//			if (Physics.Raycast(cam.ViewportPointToRay (gazePosition), out hit, 1f, (int)collisionLayer))
			{
				if ( hit.collider.gameObject != gameObject )
					return;

				particleSystemParameters.startLifetime = removeParticleAfterTimeInterval;
				particleSystemParameters.startColor = highlightColor;
				if (displayOnHeadset)
				{
					visualization.gameObject.layer = 0;
					particleSystemParameters.startSize = particleSize * 0.05f;
					particleSystemParameters.position = hit.point;
				}
				else
				{
					visualization.gameObject.layer = heatmapLayer;
					particleSystemParameters.startSize = particleSize * 0.033f;
					particleSystemParameters.position = RenderingMeshFilter.transform.localToWorldMatrix.MultiplyPoint3x4 (PositionForUV (Vector2.one - hit.textureCoord) - Vector3.forward * 0.001f);
				}
				visualization.Emit (particleSystemParameters, 1);
			}
//		}

		if (Input.GetKeyUp (KeyCode.H))
			recording = !recording;

		if ( renderingMaterial != null)
			cam.RenderToCubemap (Cubemap);
		
		if (recording)
		{
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
