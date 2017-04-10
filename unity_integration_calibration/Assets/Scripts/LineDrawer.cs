using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LineDrawer : MonoBehaviour {

	private Mesh mesh;
	private MeshRenderer mRenderer;
	private Material lineMaterial;

	PupilGazeTracker pupilTracker;

	static LineDrawer _Instance;
	public static LineDrawer Instance
	{
		get{
			return _Instance;
		}
	}

	void Start () {
		mesh = new Mesh();
		pupilTracker = PupilGazeTracker.Instance;

		mRenderer = GetComponent<MeshRenderer> ();
		mRenderer.material = Resources.Load ("Material/Pupil", typeof(Material)) as Material;

		Invoke ("InitializeLineDrawer", 1f);

		_Instance = this;
	}

	public void InitializeLineDrawer(){
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		mesh.name = "LineDrawerMesh";
	}

	public void AddLineToMesh(Vector3[] points, Color color) {
		int[] oldIndices = mesh.GetIndices (0);
		int[] indecies = new int[points.Length];
		Color[] colors = new Color[points.Length];

		for(int i=0;i<points.Length;++i) {
			indecies [i] = i + oldIndices.Length;
			colors[i] = color;
		}

		Vector3[] newPoints = mesh.vertices.Concat (points).ToArray ();
		Color[] newColors = mesh.colors.Concat(colors).ToArray();
		mesh.vertices = newPoints;
		mesh.colors = newColors;
		mesh.SetIndices(oldIndices.Concat(indecies).ToArray(), MeshTopology.Lines,0);

	}
//	void Update(){
//		if (Input.GetKeyUp (KeyCode.L)) {
//			AddLineToMesh (new Vector3[] {
//				new Vector3 (Random.Range (-100, 100), Random.Range (-100, 100), Random.Range (-100, 100)),
//				new Vector3 (Random.Range (-100, 100), Random.Range (-100, 100), Random.Range (-100, 100))
//			}, new Color (Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f), Random.Range (0f, 1f)));
//		}
//	}
}