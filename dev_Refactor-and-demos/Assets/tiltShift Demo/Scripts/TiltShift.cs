namespace PostFX
{
	using UnityEngine;

	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode, AddComponentMenu("Image Effects/Tilt Shift")]
	public class TiltShift : MonoBehaviour
	{
		public bool Preview = false;

		public Shader _shader;

		[Range(-1f, 1f)]
		public float Offset = 0f;

		[Range(0f, 20f)]
		public float Area = 1f;

		[Range(0f, 20f)]
		public float Spread = 1f;

		[Range(8, 64)]
		public int Samples = 32;

		[Range(0f, 2f)]
		public float Radius = 1f;

		public bool UseDistortion = true;

		[Range(0f, 20f)]
		public float CubicDistortion = 5f;

		[Range(0.01f, 2f)]
		public float DistortionScale = 1f;

		public Shader Shader;

		protected Material m_Material;
		public Material Material
		{
			get
			{
				if (m_Material == null)
				{
					m_Material = new Material(_shader);
					m_Material.hideFlags = HideFlags.HideAndDontSave;
				}

				return m_Material;
			}
		}

		protected Vector4 m_GoldenRot = new Vector4();

		void Start()
		{
			// Disable if we don't support image effects
			if (!SystemInfo.supportsImageEffects)
			{
				Debug.LogWarning("Image effects aren't supported on this device");
				enabled = false;
				return;
			}

			// Disable the image effect if the shader can't run on the users graphics card
			if (!Shader || !Shader.isSupported)
			{
				Debug.LogWarning("The shader is null or unsupported on this device");
				enabled = false;
			}

			// Precompute rotations
			float c = Mathf.Cos(2.39996323f);
			float s = Mathf.Sin(2.39996323f);
			m_GoldenRot.Set(c, s, -s, c);
		}

		void OnDisable()
		{
			if (m_Material)
				DestroyImmediate(m_Material);
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (UseDistortion)
				Material.EnableKeyword("USE_DISTORTION");
			else
				Material.DisableKeyword("USE_DISTORTION");

			Material.SetVector("_GoldenRot", m_GoldenRot);
			Material.SetVector("_Gradient", new Vector3(Offset, Area, Spread));
			Material.SetVector("_Distortion", new Vector2(CubicDistortion, DistortionScale));
			Material.SetVector("_Params", new Vector4(Samples, Radius, 1f / source.width, 1f / source.height));
			Graphics.Blit(source, destination, Material, Preview ? 0 : 1);
		}
	}
}
