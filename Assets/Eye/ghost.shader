Shader "FX/Ghost" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_NormalTex ("Normal (RGB)", 2D) = "white" {}
		_EmissionTex ("Emission (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Blightness ("Metallic", Range(0,3)) = 0.0
	}

	SubShader {

		// 1パス目
		Pass
		{
			Zwrite On
			ColorMask 0
			Lighting OFF
		}

		// 2パス目

		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		Zwrite Off
		ZTest LEqual
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _NormalTex;
		sampler2D _EmissionTex;
		half _Blightness;

		struct Input {
			float2 uv_MainTex;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb ;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic * _Color.a;
			o.Smoothness = _Glossiness * _Color.a;
			o.Normal = tex2D (_NormalTex, IN.uv_MainTex) * _Color.a;
			o.Emission = (tex2D (_EmissionTex, IN.uv_MainTex) * _Color.a) * _Blightness;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}