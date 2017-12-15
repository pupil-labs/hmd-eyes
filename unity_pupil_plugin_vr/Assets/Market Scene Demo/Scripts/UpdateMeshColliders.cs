using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateMeshColliders : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	[ContextMenu("Add MeshCollider to MeshRenderers")]
	void AddMeshColliderToMeshRenderers()
	{
		var meshrenderers = gameObject.GetComponentsInChildren<MeshRenderer> (true);

		print (meshrenderers.Length);
		foreach (var item in meshrenderers)
		{
			print (item.name);
			item.gameObject.AddComponent<MeshCollider> ();
		}
	}

	[ContextMenu("Add CapsuleCollider to SkinnedMeshRenderers")]
	void AddCapsuleColliderToSkinnedMeshRenderers()
	{
		var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer> (true);

		print (skinnedMeshRenderers.Length);
		foreach (var item in skinnedMeshRenderers)
		{
			print (item.name);
			item.gameObject.AddComponent<CapsuleCollider> ();
		}
	}

	[ContextMenu("Delete Existing Colliders")]
	void DeleteExistingColliders()
	{
		var colliders = gameObject.GetComponentsInChildren<Collider> (true);

		print (colliders.Length);
		foreach (var item in colliders)
		{
			print (item.name);
			DestroyImmediate (item);
		}
	}
}
