// Based on Unlit shader, but culls the front faces instead of the back
Shader "Pupil/SphericalVideoExternal" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
        _UTex("UTexture", 2D) = "white" {}
        _VTex("VTexture", 2D) = "white" {}
	}
	
	SubShader 
	{
	Tags { "RenderType"="Opaque" }
	Cull front    // ADDED BY BERNIE, TO FLIP THE SURFACES
	LOD 100
	
	Pass 
	{  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t 
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f 
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;		
            sampler2D _UTex;
            sampler2D _VTex;
			
			v2f vert (appdata_t v)
			{
				v2f o;	
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(1 - v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float4 y = tex2D(_MainTex, i.uv);
                float4 u = tex2D(_UTex, i.uv);
                float4 v = tex2D(_VTex, i.uv);

                 // apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);            
                                
                // based on https://en.wikipedia.org/wiki/YUV#Y.E2.80.B2UV420sp_.28NV21.29_to_RGB_conversion_.28Android.29
                float r = saturate( y.a + (1.370705 * (v.a - 0.5)) );
                float g = saturate( y.a - (0.698001 * (v.a - 0.5)) - (0.337633 * (u.a - 0.5)) );
                float b = saturate( y.a + (1.732446 * (u.a - 0.5)) );

                return float4(r, g, b, 1.0);    
			}
		ENDCG
	}
	}
}