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
	public bool UseExternalVideo;

	Process _subprocess;
	YUVReader _yuvReader;

	Stream _stdout;
	Stream _stderror;

	Texture2D Texture;
	Texture2D UTexture;
	Texture2D VTexture;

	void Start () 
	{
		if (UseExternalVideo)
		{
			InitializeExternalPlayer ();
			GetComponent<MeshRenderer> ().material = Resources.Load<Material> ("SphericalVideoExternal");
			GetComponent<VideoPlayer> ().enabled = false;
		}
	}

	void InitializeExternalPlayer()
	{
		if (!File.Exists (FilePath))
		{
			UnityEngine.Debug.Log ("No file at given path");
			return;
		}

		var opt = String.Format("-i \"{0}\"", FilePath);
		opt += " -f yuv4mpegpipe";
		opt += " -pix_fmt yuv444p";
		//		opt += " -filter:v \"setpts=0.25*PTS\"";
		opt += " -";

		var info = new ProcessStartInfo(FFmpegConfig.BinaryPath, opt);
		info.UseShellExecute = false;
		info.CreateNoWindow = true;
		info.RedirectStandardOutput = true;
		info.RedirectStandardError = true;
		info.StandardErrorEncoding = Encoding.UTF8;

		_subprocess = Process.Start(info);
		_yuvReader = new YUVReader ();

		_stdout = _subprocess.StandardOutput.BaseStream;
		_stdout.BeginRead(new Byte[8192], (b, c) => _yuvReader.Push(new ArraySegment<byte>(b, 0, c)));

		_stderror = _subprocess.StandardError.BaseStream;
		_stderror.BeginRead(new Byte[1024], (b, c) => ErrorHandling.OnRead(b, c));

		lastFrameNumber = -1;
	}

	private int lastFrameNumber;
	void UpdateExternalPlayer()
	{
		var error = ErrorHandling.Dequeue();
		if (error.Any())
		{
			var text = Encoding.UTF8.GetString(error, 0, error.Length);
			UnityEngine.Debug.Log(text);
		}

		if (Texture == null)
		{
			if (_yuvReader != null && _yuvReader.Header != null)
			{
				Texture = new Texture2D(_yuvReader.Header.Width, _yuvReader.Header.Height, TextureFormat.Alpha8, false);
				GetComponent<MeshRenderer> ().material.mainTexture = Texture;
				UTexture = new Texture2D(_yuvReader.Header.Width, _yuvReader.Header.Height, TextureFormat.Alpha8, false);
				GetComponent<MeshRenderer> ().material.SetTexture("_UTex", UTexture);
				VTexture = new Texture2D(_yuvReader.Header.Width, _yuvReader.Header.Height, TextureFormat.Alpha8, false);
				GetComponent<MeshRenderer> ().material.SetTexture("_VTex", VTexture);
			}
		}
		else
		{ 
			var frame = _yuvReader.GetFrame();
			if (frame.FrameNumber != lastFrameNumber)
			{
				Texture.LoadRawTextureData(frame.YBytes);
				Texture.Apply();
				UTexture.LoadRawTextureData(frame.UBytes);
				UTexture.Apply();
				VTexture.LoadRawTextureData(frame.VBytes);
				VTexture.Apply();

				lastFrameNumber = frame.FrameNumber;
			}
		}
	}

	void Update () 
	{
		if (UseExternalVideo)
			UpdateExternalPlayer ();
	}

	void OnDisable()
	{
		if (_subprocess == null) return;

		if (_stdout != null)
			_stdout.Close ();
		if (_stderror != null)
			_stderror.Close ();

		_subprocess.WaitForExit ();
		_subprocess.Close();
		_subprocess.Dispose();

		_subprocess = null;
		_stdout = null;
		_stderror = null;

	}
}
