

Shader "Noise/ImprovedPerlinNoise2D" 
{
	Properties 
	{
    	_MainTex("MainTex", 2D) = "black" { }
	}

	CGINCLUDE

	#include "UnityCG.cginc"
		
	sampler2D _MainTex;
	uniform sampler2D _PermTable1D, _Gradient2D;
	uniform float _Frequency, _Amp, _Offset, _Pass;
	
	struct v2f 
	{
	    float4 pos : SV_POSITION;
	    float2 uv : TEXCOORD;
	};
	
	v2f vert (appdata_base v)
	{
	    v2f o;
	    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	    o.uv = v.texcoord.xy;
	    return o;
	}
	
	float2 fade(float2 t)
	{
		return t * t * t * (t * (t * 6 - 15) + 10);
	}
	
	float perm(float x)
	{
		return tex2D(_PermTable1D, float2(x,0)).a;
	}
	
	float grad(float x, float2 p)
	{
		float2 g = tex2D(_Gradient2D, float2(x*8.0, 0) ).rg *2.0 - 1.0;
		return dot(g, p);
	}
				
	float inoise(float2 p)
	{
		float2 P = fmod(floor(p), 256.0);	// FIND UNIT SQUARE THAT CONTAINS POINT
	  	p -= floor(p);                      // FIND RELATIVE X,Y OF POINT IN SQUARE.
		float2 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y.
	
		P = P / 256.0;
		const float one = 1.0 / 256.0;
		
	    // HASH COORDINATES OF THE 4 SQUARE CORNERS
	  	float A = perm(P.x) + P.y;
	  	float B = perm(P.x + one) + P.y;
	 
		// AND ADD BLENDED RESULTS FROM 4 CORNERS OF SQUARE
	  	return lerp( lerp( grad(perm(A    	), p ),  
	                       grad(perm(B    	), p + float2(-1, 0) ), f.x),
	                 lerp( grad(perm(A+one	), p + float2(0, -1) ),
	                       grad(perm(B+one	), p + float2(-1, -1)), f.x), f.y);
	                           
	}
	
	float4 Fractal(v2f IN) : COLOR
	{
		float n = inoise((IN.uv+_Offset) * _Frequency) * _Amp;
	    return tex2D(_MainTex, IN.uv) + n.xxxx;
	}
	
	float4 Turbulence(v2f IN) : COLOR
	{
		float n = abs(inoise((IN.uv+_Offset) * _Frequency) * _Amp);
	    return tex2D(_MainTex, IN.uv) + n.xxxx;
	}
	
	float Ridge(float h, float _offset)
	{
		h = abs(h);
		h = _offset - h;
		h = h * h;
		return h;
	}
	
	float4 Ridgedmf(v2f IN) : COLOR
	{
		float4 texel = tex2D(_MainTex, IN.uv);
		float prev = texel.x;
		
		if(_Pass == 0.0) prev = 1.0;

		float n = Ridge(inoise(IN.uv*_Frequency), 1.0) * _Amp * prev;
		
		return texel + n.xxxx;
	}
	
	float4 Warped(v2f IN) : COLOR
	{
		float4 texel = tex2D(_MainTex, IN.uv);
		float2 warp = texel.xx + float2(0,1);
		
		if(_Pass == 0.0) warp = float2(1,1);
		
		float n = inoise((IN.uv + warp * 0.3 +_Offset) * _Frequency) * _Amp;
	    return texel + n.xxxx;
	}
	
	ENDCG
			
	SubShader 
	{
	    Pass 
		{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment Fractal
			#pragma target 3.0
			#include "UnityCG.cginc"
			ENDCG
		}
		
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment Turbulence
			#pragma target 3.0
			#include "UnityCG.cginc"
			ENDCG
		}
		
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment Ridgedmf
			#pragma target 3.0
			#include "UnityCG.cginc"
			ENDCG
		}
		
		Pass 
		{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment Warped
			#pragma target 3.0
			#include "UnityCG.cginc"
			ENDCG
		}
		
	}
	
}

