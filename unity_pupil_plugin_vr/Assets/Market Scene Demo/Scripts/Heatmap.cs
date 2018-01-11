using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pupil;

public class Heatmap : MonoBehaviour 
{
	public bool displayHeatmapOnHeadset = false;
	public float removeHeatmapPixelsAfterTimeInterval = 10;

	LayerMask collisionLayer;
	EStatus previousEStatus;
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

		PupilSettings.Instance.currentCamera = GetComponentInParent<Camera> ();
		transform.localPosition = Vector3.zero;

		InitializeHeatmapTexture ();

		int heatmapLayer = LayerMask.NameToLayer ("Heatmap");

		collisionLayer = (1 << heatmapLayer);

		if (displayHeatmapOnHeadset)
			PupilSettings.Instance.currentCamera.cullingMask = PupilSettings.Instance.currentCamera.cullingMask | (1 << heatmapLayer);
		else
			PupilSettings.Instance.currentCamera.cullingMask &= ~(1 << heatmapLayer);

		heatmapPixelsToBeRemoved = new Dictionary<Vector2, float> ();

		InitializeHeatmapSphere ();
	}

	void OnDisable()
	{
		if (previousEStatus != EStatus.ProcessingGaze)
		{
			PupilTools.DataProcessState = previousEStatus;
			PupilTools.UnSubscribeFrom ("gaze");
		}
	}

	Texture2D heatmapTexture;
	void InitializeHeatmapTexture()
	{
		if (heatmapTexture == null)
			heatmapTexture = Resources.Load<Texture2D> ("HeatmapTexture");
		
		Color[] cleared = new Color[heatmapTexture.width * heatmapTexture.height];
		for (int i = 0; i < cleared.Length; i++)
			cleared [i] = Color.clear;

		heatmapTexture.SetPixels (cleared);
		heatmapTexture.Apply ();
	}

	void InitializeHeatmapSphere()
	{
		var sphereMesh = GetComponent<MeshFilter> ().sharedMesh;
		if (sphereMesh.triangles [0] == 0)
		{
			Debug.Log ("Sphere-mesh needed to be inverted");
			sphereMesh.triangles = sphereMesh.triangles.Reverse ().ToArray ();
		}
	}

	Dictionary<Vector2,float> heatmapPixelsToBeRemoved;
	bool updateHeatmapTexture = false;
	void Update () 
	{
		transform.rotation = Quaternion.identity;

		if (PupilTools.IsConnected && PupilTools.DataProcessState == EStatus.ProcessingGaze)
		{
			Vector2 gazePosition = PupilData._2D.GetEyeGaze (GazeSource.BothEyes);

			RaycastHit hit;
			//			if (Input.GetMouseButton(0) && Physics.Raycast(PupilSettings.Instance.currentCamera.ScreenPointToRay (Input.mousePosition), out hit, 2000f, (int)collisionLayer))
			if (Physics.Raycast(PupilSettings.Instance.currentCamera.ViewportPointToRay (gazePosition), out hit, 2000f, (int)collisionLayer))
			{
				if ( hit.collider.gameObject != gameObject )
					return;
				Vector2 pixelUV = hit.textureCoord;
				pixelUV.x = (int) (pixelUV.x*heatmapTexture.width);
				pixelUV.y = (int) (pixelUV.y*heatmapTexture.height);

				heatmapTexture.SetPixel ((int)pixelUV.x, (int)pixelUV.y, Color.red);
				updateHeatmapTexture = true;

				if (removeHeatmapPixelsAfterTimeInterval > 0)
				{
					if (heatmapPixelsToBeRemoved.ContainsKey (pixelUV))
						heatmapPixelsToBeRemoved [pixelUV] = Time.time + removeHeatmapPixelsAfterTimeInterval;
					else
						heatmapPixelsToBeRemoved.Add (pixelUV, Time.time + removeHeatmapPixelsAfterTimeInterval);
				}
			}
		}
		var removablePixels = heatmapPixelsToBeRemoved.Where(p => p.Value < Time.time);
		for (int i = 0; i < removablePixels.Count() ; i++)
		{
			var pixel = removablePixels.ElementAt (i);
			heatmapTexture.SetPixel ((int)pixel.Key.x, (int)pixel.Key.y, Color.clear);
			updateHeatmapTexture = true;
			heatmapPixelsToBeRemoved.Remove (pixel.Key);
		}

		if (updateHeatmapTexture)
		{
			heatmapTexture.Apply ();
			updateHeatmapTexture = false;
		}
	}
}
