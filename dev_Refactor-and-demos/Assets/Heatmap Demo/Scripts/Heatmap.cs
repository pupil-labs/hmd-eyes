// Alan Zucconi
// www.alanzucconi.com
using UnityEngine;
using System.Collections;

public class Heatmap : MonoBehaviour
{

	public Mesh mesh;

	[HideInInspector]
	public Vector3[] vertexArray;

	[HideInInspector]
	public Vector4[] positions;

	[HideInInspector]
	public Vector4[] properties;

	[HideInInspector]
	public float[] trueHeatData;

	public Material material;

	private bool _isHeatMapView;
	public bool isHeatmapView{
	
		get{ return _isHeatMapView; }
		set{ 
		
			_isHeatMapView = value;

			if (_isHeatMapView) {
				GetComponent<MeshRenderer> ().enabled = true;
			} else {
				GetComponent<MeshRenderer> ().enabled = false;
			}
		}

	}

	int count;

	Camera cam;

	RaycastHit hit;

	public GameObject indicatorSphere;

	void Start ()
	{

		cam = Camera.main;

		vertexArray = mesh.vertices;

		count = mesh.vertexCount;

		isHeatmapView = false;

		positions = new Vector4[count];
		properties = new Vector4[count];
		trueHeatData = new float[count];

		Vector3[] vertices = mesh.vertices;

		for( int j = 0; j<mesh.vertices.Length;j++){

			positions [j] = new Vector4 (mesh.vertices [j].x, mesh.vertices [j].y, mesh.vertices [j].z, 0);
			properties[j] = new Vector4(baseRadius, baseHeat, 0, 0);
			trueHeatData [j] = baseHeat;

		}

		mesh.vertices = vertices;

		material.SetInt("_Points_Length", count);
		material.SetVectorArray("_Points", positions);
		material.SetVectorArray("_Properties", properties);

	}

	public float multiply;
	public float baseRadius;
	public float maxRadius;
	public float baseHeat;
	public float fallOff;

	public void RecalculateHeat(){
	
		float highestHeat = 0;

		//get highest heat
		foreach (float heat in trueHeatData) {
			if (heat > highestHeat)
				highestHeat = heat;
		}

		for (int i = 0; i < trueHeatData.Length; i++) {

			float heatScale = Mathf.InverseLerp (0, highestHeat, trueHeatData [i]);

			properties [i].y = heatScale;

		}

		material.SetVectorArray("_Properties", properties);

	}

//	public static int GetClosestVertex(RaycastHit aHit, int[] aTriangles)
//	{
//		var b = aHit.barycentricCoordinate;
//		int index = aHit.triangleIndex * 3;
//		if (aTriangles == null || index < 0 || index + 2 >= aTriangles.Length)
//			return -1;
//		if (b.x > b.y)
//		{
//			if (b.x > b.z)
//				return aTriangles[index]; // x
//			else
//				return aTriangles[index + 2]; // z
//		}
//		else if (b.y > b.z)
//			return aTriangles[index + 1]; // y
//		else
//			return aTriangles[index + 2]; // z
//	}

	void Update()
	{


		if (Input.GetKeyUp (KeyCode.R)) {
			RecalculateHeat ();
		}

		if (Input.GetKeyUp (KeyCode.Space)) {
		
			if (isHeatmapView) {
				isHeatmapView = false;
			} else {
				isHeatmapView = true;
			}

		}

		if (!isHeatmapView) {

			if (!Physics.Raycast (origin: cam.transform.position, direction: cam.transform.forward, hitInfo: out hit))
				return;


//
			int[] triangles = mesh.triangles;
			int index = hit.triangleIndex * 3;

			float dist = 0;

			for (int i = 0; i < 3; i++) {

				dist = Vector3.Distance (hit.point, positions [triangles [index + i]]);

				if (properties [triangles [index + i]].x < maxRadius) {

					properties [triangles [index + i]].x += multiply * Mathf.Pow (fallOff, dist);//dist * multiply;

				}	
				
				trueHeatData [triangles [index + i]] += multiply * Mathf.Pow (fallOff, dist);

			}

			RecalculateHeat ();

		}

	}

}