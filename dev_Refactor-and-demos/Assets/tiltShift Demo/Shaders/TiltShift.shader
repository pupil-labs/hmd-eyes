Shader "Hidden/PostFX/TiltShift"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE

		#pragma vertex vert_img
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma glsl
		#pragma multi_compile __ USE_DISTORTION
		#pragma target 3.0
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		half3 _Gradient;
		half4 _GoldenRot;
		half4 _Distortion;
		half4 _Params;

		#define Offset _Gradient.x
		#define Area _Gradient.y
		#define Spread _Gradient.z
		#define Samples _Params.x
		#define Radius _Params.y
		#define PixelSize _Params.zw
		#define CubicDistortion _Distortion.x
		#define DistortionScale _Distortion.y

		inline half gradient(half2 uv)
		{
			#if USE_DISTORTION
			// Cubic distortion
			half2 h = uv.xy - half2(0.5, 0.5);
			half r2 = dot(h, h);
			uv = (1.0 + r2 * (CubicDistortion * sqrt(r2))) * DistortionScale * h + 0.5;
			#endif

			// Gradient
			half2 coord = uv * 2.0 - 1.0 + Offset;
			return pow(abs(coord.y * Area), Spread);
		}

		half4 frag_preview(v2f_img i) : SV_Target
		{
			return gradient(i.uv);
		}
		
		half4 frag(v2f_img i) : SV_Target
		{
			// Based on the method described in https://www.shadertoy.com/view/4d2Xzw
			half2x2 rot = half2x2(_GoldenRot);
			half4 accumulator = 0.0;
			half4 divisor = 0.0;

			half r = 1.0;
			half2 angle = half2(0.0, Radius * saturate(gradient(i.uv)));

			for (int j = 0; j < Samples; j++)
			{
				r += 1.0 / r;
				angle = mul(rot, angle);
				half4 bokeh = tex2Dlod(_MainTex, half4(i.uv + PixelSize * (r - 1.0) * angle, 0.0, 0.0));
				accumulator += bokeh * bokeh;
				divisor += bokeh;
			}

			return accumulator / divisor;
		}

	ENDCG

	SubShader
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		// (0) Preview
		Pass
		{
			CGPROGRAM
				#pragma fragment frag_preview
			ENDCG
		}

		// (1) Tilt shift
		Pass
		{
			CGPROGRAM
				#pragma fragment frag
			ENDCG
		}
	}

	FallBack off
}
