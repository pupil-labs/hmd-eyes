// Code based on Alan Zucconi's Tutorial!
// www.alanzucconi.com
Shader "Pupil Demos/Heatmap" {
		Properties{

			_HeatTex("Texture", 2D) = "white" {}

			_HitPosition("_HitPosition", Vector) = (1,1,1,1)

			_SphereSize("_SphereSize", Float) = 1

			_HeatEffectRadius("HeatEffectRadius", Float) = 1

			_CoolDownFactor("CoolDownFactor", Float) = 1

			_FallOff("FallOff", Float) = 1

			_Power("Power", Float) = 1

			_Color("Color", Color) = (1,1,1,1)


		}
			SubShader{
			Tags{ "Queue" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha // Alpha blend
			Cull Back
			Pass{
		
			

			CGPROGRAM
#pragma vertex vert             
#pragma fragment frag

		struct vertInput {
			float4 pos : POSITION;
		};

		struct vertOutput {
			float4 pos : POSITION;
			fixed3 worldPos : TEXCOORD1;
		};

		vertOutput vert(vertInput input) {
			vertOutput o;
			o.pos = UnityObjectToClipPos(input.pos);
			o.worldPos = mul(unity_ObjectToWorld, input.pos).xyz;
			return o;
		}

		uniform int _Points_Length = 1;
		uniform float4 _Points[766];		// (x, y, z) = position
		uniform float4 _Properties[766];	// x = radius, y = intensity
		uniform float3 _HitPosition;
		uniform float _SphereSize;
//		uniform float _BrushSize;
		uniform fixed4 _Color;
		half h = 0;
		sampler2D _HeatTex;
		float _Radius;
		float _HeatEffectRadius;
		float _CoolDownFactor;
		float _FallOff;
		float _Power;

		half4 frag(vertOutput output) : COLOR{

		for (int i = 0; i < _Points_Length; i++)
		{

			// Calculates the contribution of each point
			half di = distance(output.worldPos*_SphereSize, _Points[i].xyz*_SphereSize);

			half dist = distance(_HitPosition, output.worldPos );

			half ri=0;
			half hi=0;


						ri = _Properties[i].x;

//			hi = (1 - saturate((di) / ri));

//			hi = saturate(pow(_FallOff,di)/(ri*_Power));

			hi =  saturate(pow(_FallOff,di));



			h += hi * _Properties[i].y;
//////////////////////////////////////////////////////////////

		}

//		if (_Points_Length < 10){
//		h = 100;
//		}

		// Converts (0-1) according to the heat texture
//		h = saturate(h);


		if (h>.9f){
		h=.9f;
		}
		half4 color = tex2D(_HeatTex, fixed2(h, 0.5));
		return color;
		}
			ENDCG

		}
		}
			
			Fallback "Diffuse"
	}