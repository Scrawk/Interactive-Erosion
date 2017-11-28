
Shader "Erosion/InitShader" 
{
	Properties 
	{
    	_MainTex("MainTex", 2D) = "black" { }
	}
	SubShader 
	{
    	Pass 
    	{
			ZTest Always Cull Off ZWrite Off
	  		Fog { Mode off }
			
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			uniform float _Height, _Amp, _UseAbs;
			uniform float4 _Mask;
			uniform sampler2D _NoiseTex;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 frag(v2f IN) : COLOR
			{
				//Get the noise value for this layer
				float4 n = tex2D(_NoiseTex, IN.uv).xxxx * _Mask * _Amp;
				//if this noise value should be abs 
				if(_UseAbs) n = abs(n);
				//Layers 1,2 or 3 MUST be positive
				//If this layer is not to use the abs value the noise must be clamped at 0
				//Layer 0 can be negative
				n.yzw = max(n.yzw, 0.0);
				//Add to the terrain field (MainTex) and scale by the terrain height
				return tex2D(_MainTex, IN.uv) + n * _Height;
			}
			
		ENDCG

    	}
	}
}