
Shader "Erosion/DepositsOverlay" 
{
	//UNITY_SHADER_NO_UPGRADE
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LayerColor0("LayerColor0", Color) = (1,1,1,1)
		_LayerColor1("LayerColor1", Color) = (1,1,1,1)
		_LayerColor2("LayerColor2", Color) = (1,1,1,1)
		_LayerColor3("LayerColor3", Color) = (1,1,1,1)
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma exclude_renderers gles
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0
		#pragma glsl

		uniform sampler2D _MainTex;
		uniform sampler2D _SedimentDepositionField;
		uniform float3 _LayerColor0, _LayerColor1, _LayerColor2, _LayerColor3;
		uniform float _ScaleY, _Layers, _TexSize;
		
		struct Input 
		{
			float2 uv_MainTex;
		};
		
		float GetTotalHeight(float4 texData) 
		{
			float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
			float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
			return dot(texData, addVec);
		}
		
		void vert(inout appdata_full v) 
		{
			v.tangent = float4(1,0,0,1);
		
			v.vertex.y += GetTotalHeight(tex2Dlod(_MainTex, float4(v.texcoord.xy, 0.0, 0.0))) * _ScaleY;
		}
		
		float3 FindNormal(float2 uv, float u)
        {

        	float ht0 = GetTotalHeight(tex2D(_MainTex, uv + float2(-u, 0)));
            float ht1 = GetTotalHeight(tex2D(_MainTex, uv + float2(u, 0)));
            float ht2 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, -u)));
            float ht3 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, u)));
            
            float2 _step = float2(1.0, 0.0);

            float3 va = normalize(float3(_step.xy, ht1-ht0));
            float3 vb = normalize(float3(_step.yx, ht2-ht3));

           return cross(va,vb);
        }

		void surf(Input IN, inout SurfaceOutput o) 
		{
			float3 n = FindNormal(IN.uv_MainTex, 1.0/_TexSize);
			
			float4 hts = tex2D(_MainTex, IN.uv_MainTex);
			 
			o.Albedo = lerp(_LayerColor0, _LayerColor1, clamp(hts.y * 2.0, 0.0, 1.0));
			o.Albedo = lerp(o.Albedo, _LayerColor2, clamp(hts.z * 2.0, 0.0, 1.0));
			o.Albedo = lerp(o.Albedo, _LayerColor3, clamp(hts.w * 2.0, 0.0, 1.0));
			
			float sediment = tex2D(_SedimentDepositionField, IN.uv_MainTex).x;
			o.Albedo.r += sediment*300;			

			o.Alpha = 1.0;
			o.Normal = n;
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
