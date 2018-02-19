using System;
using UnityEngine;

[Serializable]
public class FramePublishing
{
	public int targetFPS = 20;
	public Texture2D eye0Image;
	public Texture2D eye1Image;
	[HideInInspector]
	public byte[] raw0;
	[HideInInspector]
	public byte[] raw1;
	[HideInInspector]
	public bool StreamCameraImages = false;
	public Material eye0ImageMaterial;
	public Material eye1ImageMaterial;

	public void InitializeFramePublishing ()
	{
		if (!eye0ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			eye0ImageMaterial = new Material (shader);
			eye0ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		if (!eye1ImageMaterial)
		{
			Shader shader = Shader.Find ("Unlit/Texture");
			eye1ImageMaterial = new Material (shader);
			eye1ImageMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		eye0Image = new Texture2D (100, 100);
		eye1Image = new Texture2D (100, 100);

		lastUpdate = Time.time;
	}

	float lastUpdate;
	float elapsedTime = 0;
	public void UpdateEyeTextures()
	{
		if (StreamCameraImages)
		{
			//Put this in a function and delegate it to the OnUpdate delegate
			elapsedTime = Time.time - lastUpdate;
			if (elapsedTime >= (1f / targetFPS))
			{
				//Limiting the MainThread calls to framePublishFramePerSecondLimit to avoid issues. 20-30 ideal.
				eye0Image.LoadImage (raw0);
				eye0ImageMaterial.mainTexture = eye0Image;
				eye1Image.LoadImage (raw1);
				eye1ImageMaterial.mainTexture = eye1Image;
				eye1ImageMaterial.mainTextureScale = new Vector2 (-1, -1);
				lastUpdate = Time.time;
			}
		}
	}
}
