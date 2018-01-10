using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InvertMesh : MonoBehaviour 
{
	[ContextMenu ("Invert Mesh")]
	private void Invert()
	{
		Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
		mesh.triangles = mesh.triangles.Reverse().ToArray();
	}
}
