Shader "Pupil/Heatmap (Unlit)" 
{
	Properties 
	{
    	_Cubemap ("Reflection Probe", CUBE) = "white" {}
	}

SubShader {
    Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    LOD 100

    //ZWrite Off
    //Blend SrcAlpha OneMinusSrcAlpha

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
//            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
    			float3 normal : NORMAL;
    			float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            	float3 normal: TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            samplerCUBE _Cubemap;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 cubemap = texCUBE(_Cubemap, i.normal);

                return cubemap;
            }
        ENDCG
    }
}

}
