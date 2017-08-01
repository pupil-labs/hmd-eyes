// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

// Upgrade NOTE: commented out 'float3 _WorldSpaceCameraPos', a built-in variable

Shader "Custom/VoxelTerrain" {
	Properties {
		//    _PeakColor ("PeakColor", Color) = (0.8,0.9,0.9,1)   
		_PeakLevel ("PeakLevel", Float) = 30
		_PeakTex ("Peak (RGB)", 2D) = "white" {}
	
		//    _Level3Color ("Level3Color", Color) = (0.75,0.53,0,1)
		_Level3 ("Level3", Float) = 20
		_Level3Tex ("Level3 (RGB)", 2D) = "white" {}
	
		//    _Level2Color ("Level2Color", Color) = (0.69,0.63,0.31,1)
		_Level2 ("Level2", Float) = 5
		_Level2Tex ("Level2 (RGB)", 2D) = "white" {}
	
		//    _Level1Color ("Level1Color", Color) = (0.65,0.86,0.63,1)
		_Level1 ("Level1", Float) = 5
		_Level1Tex ("Level1 (RGB)", 2D) = "white" {}
	
		_WaterLevel ("WaterLevel", Float) = 0
		//    _WaterColor ("WaterColor", Color) = (0.37,0.78,0.92,1)
		_WaterTex ("Water (RGB)", 2D) = "white" {}


		_Slope ("Slope Fader", Range (0,1)) = 0.5 // this doesnt really work in new version?

		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _WaterTex;
		sampler2D _Level1Tex;
		sampler2D _Level2Tex;
		sampler2D _Level3Tex;
		sampler2D _PeakTex;

		struct Input {
			float2 uv_WaterTex;
			float3 customColor;
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		float _PeakLevel;
	//    float4 _PeakColor;
		float _Level3;
	//    float4 _Level3Color;
		float _Level2;
	//    float4 _Level2Color;
		float _Level1;
		//float4 _Level1Color;
		float _Slope;
		float _WaterLevel;

		 // float3 _WorldSpaceCameraPos;
	//    float4 _WaterColor;
		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);
			o.customColor = abs(v.normal.y);
		}

//				void surf (Input IN, inout SurfaceOutputStandard o) {
//        if (length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz) > _PeakLevel)
//
////            o.Albedo = _PeakColor;
//            o.Albedo = tex2D(_PeakTex, IN.uv_WaterTex).rgb;
//        if (length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz) <= _PeakLevel)
////            o.Albedo = lerp(_Level3Color, _PeakColor, (IN.worldPos.y - _Level3)/(_PeakLevel - _Level3));
//            o.Albedo = lerp(tex2D(_Level3Tex, IN.uv_WaterTex).rgb, tex2D(_PeakTex, IN.uv_WaterTex).rgb, (IN.worldPos.y - _Level3)/(_PeakLevel - _Level3));
////            o.Albedo = tex2D(_Level3Tex, IN.uv_WaterTex).rgb;
//        if (length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz) <= _Level3)
////            o.Albedo = lerp(_Level2Color, _Level3Color, (IN.worldPos.y - _Level2)/(_Level3 - _Level2));
//            o.Albedo = lerp(tex2D(_Level2Tex, IN.uv_WaterTex).rgb, tex2D(_Level3Tex, IN.uv_WaterTex).rgb, (IN.worldPos.y - _Level2)/(_Level3 - _Level2));
////            o.Albedo = tex2D(_Level2Tex, IN.uv_WaterTex).rgb;
//        if (length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz) <= _Level2)
////            o.Albedo = lerp(_Level1Color, _Level2Color, (IN.worldPos.y - _WaterLevel)/(_Level2 - _WaterLevel));
//            o.Albedo = lerp(tex2D(_Level1Tex, IN.uv_WaterTex).rgb, tex2D(_Level2Tex, IN.uv_WaterTex).rgb, (IN.worldPos.y - _WaterLevel)/(_Level2 - _WaterLevel));
////            o.Albedo = tex2D(_Level1Tex, IN.uv_WaterTex).rgb;
//        
//		if (length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz) <= _WaterLevel)
//            o.Albedo = tex2D(_WaterTex, IN.uv_WaterTex).rgb;
//			
//			
//		// doesnt work
//	    o.Albedo *= saturate(IN.customColor + _Slope);
//
//
//		// Metallic and smoothness come from slider variables
//		o.Metallic = _Metallic;
//		o.Smoothness = _Glossiness;
//		o.Alpha = 1;
//		}

		void surf (Input IN, inout SurfaceOutputStandard o) {

		float dist = length(IN.worldPos.xyz - _WorldSpaceCameraPos.xyz);

        if (dist >= _PeakLevel)

//            o.Albedo = _PeakColor;
            o.Albedo = tex2D(_PeakTex, IN.uv_WaterTex).rgb;
        if (dist <= _PeakLevel)
//            o.Albedo = lerp(_Level3Color, _PeakColor, (IN.worldPos.y - _Level3)/(_PeakLevel - _Level3));
            o.Albedo = lerp(tex2D(_Level3Tex, IN.uv_WaterTex).rgb, tex2D(_PeakTex, IN.uv_WaterTex).rgb, (dist - _Level3)/(_PeakLevel - _Level3));
//            o.Albedo = tex2D(_Level3Tex, IN.uv_WaterTex).rgb;
        if (dist <= _Level3)
//            o.Albedo = lerp(_Level2Color, _Level3Color, (IN.worldPos.y - _Level2)/(_Level3 - _Level2));
            o.Albedo = lerp(tex2D(_Level2Tex, IN.uv_WaterTex).rgb, tex2D(_Level3Tex, IN.uv_WaterTex).rgb, (dist - _Level2)/(_Level3 - _Level2));
//            o.Albedo = tex2D(_Level2Tex, IN.uv_WaterTex).rgb;
        if (dist <= _Level2)
//            o.Albedo = lerp(_Level1Color, _Level2Color, (IN.worldPos.y - _WaterLevel)/(_Level2 - _WaterLevel));
            o.Albedo = lerp(tex2D(_Level1Tex, IN.uv_WaterTex).rgb, tex2D(_Level2Tex, IN.uv_WaterTex).rgb, (dist - _WaterLevel)/(_Level2 - _WaterLevel));
//            o.Albedo = tex2D(_Level1Tex, IN.uv_WaterTex).rgb;
        
		if (dist <= _WaterLevel)
//		o.Albedo = lerp(tex2D(_Level1Tex, IN.uv_WaterTex).rgb, tex2D(_Level2Tex, IN.uv_WaterTex).rgb, (dist - _WaterLevel)/(_Level2 - _WaterLevel));
            o.Albedo = lerp(tex2D(_WaterTex, IN.uv_WaterTex).rgb,tex2D(_Level1Tex, IN.uv_WaterTex).rgb, (dist - _WaterLevel)/(_Level1 - _WaterLevel)) ;
			
			
		// doesnt work
	    o.Albedo *= saturate(IN.customColor + _Slope);


		// Metallic and smoothness come from slider variables
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
