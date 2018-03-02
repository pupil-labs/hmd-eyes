using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramePublishingDemo : MonoBehaviour 
{
	FramePublishing publisher;

	void OnEnable () 
	{
		PupilTools.OnConnected += StartFramePublishing;
		PupilTools.OnDisconnecting += StopFramePublishing;
	}

	void StartFramePublishing()
	{
		if (publisher == null)
			publisher = gameObject.AddComponent<FramePublishing> ();
		else
			publisher.enabled = true;
	}

	void StopFramePublishing()
	{
		if (publisher != null)
			publisher.enabled = false;
	}

	void OnDisable()
	{
		PupilTools.OnConnected -= StartFramePublishing;
		PupilTools.OnDisconnecting -= StopFramePublishing;
	}
}
