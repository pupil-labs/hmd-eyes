using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateTextSheet : MonoBehaviour {

	Material mat;

	public int targetFrame;
	public float delayInBetweenFrames;

	private int _currentFrame;
	public int currentFrame{
		get{
			return _currentFrame;
		}
		set{
			_currentFrame = value;
			float x = (currentFrame * 0.2f);
			float y = (currentFrame / 5)*0.2f;
			mat.mainTextureOffset = new Vector2 (x, -y);
		}
	}

	IEnumerator AnimateTextureSheet(int targetFrame, float delayInBetweenFrames) {
	
		while (currentFrame != targetFrame) {
		
			if (currentFrame > targetFrame) {
				
				currentFrame--;

			} else {
				
				currentFrame++;

			}

			yield return new WaitForSeconds (delayInBetweenFrames);

		}

	}

	public void AnimateToFrame(int targetFrame, float delayInBetweenFrames){
	
		this.StopAllCoroutines ();
		StartCoroutine (AnimateTextureSheet (targetFrame, delayInBetweenFrames));

	}

	void OnTriggerEnter(Collider c){
	
		print ("trigger enter : " + c.name);	
		AnimateToFrame (5, 0.05f);

	}

	void OnTriggerExit(){

//		print ("trigger exit : " + gameObject.name);	
		AnimateToFrame (29, 0.05f);

	}
		
	void Start () {

		mat = GetComponent<MeshRenderer> ().material;

	}

}
