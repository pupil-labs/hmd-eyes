Shader "Pupil/GazeHighlight"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_highlightThreshold ("Highlight Threshold", Range(0.01,0.5)) = 0.05
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			uniform float2 _viewportGazePosition;
			uniform float _highlightThreshold = 0.1;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			half4 _MainTex_ST;

			fixed4 frag (v2f i) : SV_Target
			{
				half2 uv = UnityStereoScreenSpaceUVAdjust(i.uv,_MainTex_ST);
				fixed4 col = tex2D(_MainTex, uv);

				if ( distance(uv,_viewportGazePosition) > _highlightThreshold )
					col.rgb = dot(col.rgb,float3(0.3,0.59,0.11));
				
				return col;
			}
			ENDCG
		}
	}
}
