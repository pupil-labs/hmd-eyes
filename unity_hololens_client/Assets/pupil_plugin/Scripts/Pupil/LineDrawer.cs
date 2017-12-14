using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LineDrawer : MonoBehaviour {

	private Mesh mesh;
	private MeshRenderer mRenderer;
	private Material lineMaterial;

	private int lineID = -1;

	private List<Vector3> verticies = new List<Vector3>();
	private List<Color> colors = new List<Color>();
	private List<int> indicies = new List<int>();

//	PupilGazeTracker pupilTracker;

	static LineDrawer _Instance;
	public static LineDrawer Instance
	{
		get{
			return _Instance;
		}
	}

	public class param{
		public Vector3[] points;
		public Color color;
	}


	void Start () {
//		pupilTracker = PupilGazeTracker.Instance;

		mRenderer = GetComponent<MeshRenderer> ();
		mRenderer.material = Resources.Load ("Material/Pupil", typeof(Material)) as Material;

		Invoke ("InitializeLineDrawer", .2f);

		_Instance = this;
	}

	public void InitializeLineDrawer(){
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;
		mesh.name = "LineDrawerMesh";
	}

	public void Draw(){
		
		addLinesToMesh ();

	}

	public void Clear(){
		
		verticies.Clear ();
		colors.Clear ();
		indicies.Clear ();
		lineID = -1;

	}

	public void AddLineToMesh(param _params) {
		verticies.Add(_params.points[0]);
		verticies.Add(_params.points[1]);
		colors.Add (_params.color);
		colors.Add (_params.color);
		lineID++;
		indicies.Add (lineID);
		lineID++;
		indicies.Add (lineID);
	}

	private void addLinesToMesh() {
		mesh.vertices = verticies.ToArray();
		mesh.colors = colors.ToArray ();
		mesh.SetIndices (indicies.ToArray (), MeshTopology.Lines, 0);
	}

}