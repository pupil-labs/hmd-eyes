using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Video;
using FFmpegUtils;
using FFmpegOut;

// Based on this GitHub repository: https://github.com/ousttrue/FFMPEG_Texture

public class SphericalVideoPlayer : MonoBehaviour 
{
	public string FilePath;

	void Start () 
	{
		var player = gameObject.AddComponent<VideoPlayer> ();
		player.playOnAwake = false;
		player.isLooping = true;
		player.renderMode = VideoRenderMode.RenderTexture;
		player.url = FilePath;
		player.targetTexture = Resources.Load<RenderTexture> ("SphericalVideo");
		player.Play ();

		RenderSettings.skybox = Resources.Load<Material> ("Materials/SphericalVideo");
	}
}
