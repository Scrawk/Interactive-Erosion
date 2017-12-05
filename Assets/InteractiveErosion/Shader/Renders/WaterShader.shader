//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/WaterShader" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_SkyColor("SkyColor", Color) = (0,0,1,1)
		_WaterAbsorption("WaterAbsorption", Vector) = (0.259, 0.086, 0.113, 2000.0)
		_FresnelFactor("FresnelFactor", Float) = 4.0
		_MinWaterHt("MinWaterHt", Float) = 1.0
		_SunSpecStr("SunSpecStr", Float) = 0.4
	    _SedimentColor("SedimentColor", Vector) = (0.192, 0.596, 1.0 , 1.0)
	}
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		
		GrabPass { "_GrabTex" }
		
		CGPROGRAM
		#pragma exclude_renderers gles
		#pragma surface surf Lambert vertex:vert noforwardadd nolightmap
		#pragma target 3.0
		#pragma glsl

		sampler2D _MainTex, _GrabTex, _CameraDepthTexture;
		float4 _SkyColor, _WaterAbsorption;
		float _FresnelFactor, _MinWaterHt, _SunSpecStr;
		
		uniform sampler2D _WaterField, _SedimentField, _VelocityField;
		uniform float _ScaleY, _TexSize, _Layers;
		uniform float3 _SunDir, _SedimentColor;
		
		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
			float4 grabUV;
			float4 projPos;
			float depth;
		};
		
		float GetTotalHeight(float4 texData) 
		{
			float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
			float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
			return dot(texData, addVec);
		}
		
		void vert(inout appdata_tan v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o)
			
			v.tangent = float4(1,0,0,1);
		
			v.vertex.y += GetTotalHeight(tex2Dlod(_MainTex, float4(v.texcoord.xy, 0.0, 0.0))) * _ScaleY;
			v.vertex.y += tex2Dlod(_WaterField, float4(v.texcoord.xy, 0.0, 0.0)).x * _ScaleY;
			
			float4 pos = mul (UNITY_MATRIX_MVP, v.vertex);
			o.grabUV = ComputeGrabScreenPos(pos);
			o.projPos = ComputeScreenPos(pos);
			o.depth = pos.z / pos.w;
			
		}
		
		float3 FindNormal(float2 uv, float u)
        {
        	float ht0 = GetTotalHeight(tex2D(_MainTex, uv + float2(-u, 0)));
            float ht1 = GetTotalHeight(tex2D(_MainTex, uv + float2(u, 0)));
            float ht2 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, -u)));
            float ht3 = GetTotalHeight(tex2D(_MainTex, uv + float2(0, u)));
      
            ht0 += tex2D(_WaterField, uv + float2(-u, 0)).x;
            ht1 += tex2D(_WaterField, uv + float2(u, 0)).x;
            ht2 += tex2D(_WaterField, uv + float2(0, -u)).x;
            ht3 += tex2D(_WaterField, uv + float2(0, u)).x;
            
            float2 _step = float2(1.0, 0.0);

            float3 va = normalize(float3(_step.xy, ht1-ht0));
            float3 vb = normalize(float3(_step.yx, ht2-ht3));

           return cross(va,vb);
        }
        
        float3 Sun(float3 V, float3 N)
		{
			float3 H = normalize(V+_SunDir.xzy);
			return pow(abs(dot(H,N)), 512).xxx;
		}

		void surf(Input IN, inout SurfaceOutput o) 
		{
		
			float ht = tex2D(_WaterField, IN.uv_MainTex).x;
	
			if(ht < _MinWaterHt) discard;
		
			float3 N = FindNormal(IN.uv_MainTex, 1.0/_TexSize);
			
			float3 V = normalize(_WorldSpaceCameraPos-IN.worldPos).xzy;
			
			float fresnel = exp(-max(dot(V,N),0.0) * _FresnelFactor);
			
			float3 grab = tex2Dproj(_GrabTex, UNITY_PROJ_COORD(IN.grabUV)).rgb;
			
			float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(IN.projPos)).r);
			float fragmentsDepth = Linear01Depth(IN.depth);
			
			float waterDepth = clamp(depth - fragmentsDepth, 0.0, 1.0);

			float sediment = tex2D(_SedimentField, IN.uv_MainTex);

			

			float3 AbsorptonCof =( _WaterAbsorption.rgb + _SedimentColor.rgb*sediment ) * waterDepth * _WaterAbsorption.a;
						

			float3 col = grab * exp(-AbsorptonCof*AbsorptonCof);
			
			
			o.Albedo = lerp(col, _SkyColor.rgb, fresnel*0.4) + Sun(V,N) * _SunSpecStr;
			
			o.Alpha = 1.0;
			o.Normal = N;
			
			//o.Albedo += length(tex2D(_VelocityField, IN.uv_MainTex).xy).xxx*0.1;
			o.Albedo += length(tex2D(_VelocityField, IN.uv_MainTex).xy).xxx*0.03;			
			
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
