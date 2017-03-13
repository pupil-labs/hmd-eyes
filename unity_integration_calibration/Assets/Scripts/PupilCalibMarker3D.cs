using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PupilCalibMarker3D : MonoBehaviour {

	public float animTotalLength;
	public int animSegmentAmount;

	[Range(0.01f,5.0f)]
	public float pulsateSpeed = 1f;
	[Range(0.1f,1.0f)]
	public float maxScale;
	[Range(0.1f,1.0f)]
	public float minScale;

	public Color StartColor;
	public Color EndColor;

	public Sprite sprite;

	private float animSegmentLenght;
	private SpriteRenderer haloRenderer;
	public int tmp = 0;

	void Awake(){
		animSegmentLenght = animTotalLength / animSegmentAmount;
		haloRenderer = transform.FindChild ("PupilLabsLogoHalo").gameObject.GetComponent<SpriteRenderer> ();
	}

	void Update(){
		PulsateHalo ();
		if (Input.GetKeyUp (KeyCode.A)) {
			tmp++;
			UpdateAnim(tmp);
		}
	}

	void PulsateHalo(){
		float _t = Time.time;
		Color _c = new Color (1, 1, 1, Mathf.Abs (Mathf.Sin (Time.time * (pulsateSpeed + Time.deltaTime))));

		haloRenderer.color = _c;
	}

	public void UpdateAnim(int index){

		float _p = Mathf.InverseLerp (0.9f, animSegmentAmount, index);
		//print ("Segment percentage : " + _p);
		float _s = Mathf.Lerp (minScale, maxScale, _p);
		//print ("Segment scale : " + _s);

		GameObject _go = new GameObject ();
		_go.transform.parent = this.gameObject.transform;
		_go.transform.localScale = new Vector3 (0, 0, 0);
		SpriteRenderer _sRenderer = _go.AddComponent<SpriteRenderer> ();
		_sRenderer.sprite = sprite;
		_sRenderer.color = Color.Lerp (StartColor, EndColor, _p);


		StartCoroutine (SegmentAnimation (animSegmentLenght, _s, _go));

	}

	IEnumerator SegmentAnimation(float length, float scale, GameObject _go){
		
		float currTime = 0f;
		float _minS = 0.1f;
		float _maxS = scale;
		float step = scale / length;
		while (currTime<length) {
			_go.transform.localScale += new Vector3 (step, step, step);
			currTime++;
			yield return null;
		}
			

	}

}
