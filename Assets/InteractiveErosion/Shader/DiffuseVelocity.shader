
Shader "Erosion/DiffuseVelocity" 
{
	Properties 
	{
    	_MainTex("MainTex", 2D) = "black" { }
	}
	SubShader 
	{
    	Pass 
    	{
			ZTest Always

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			uniform float _Alpha, _TexSize;
		
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
				float u = 1.0f/_TexSize;
				
				float4 left = tex2D(_MainTex, IN.uv + float2(-u, 0));
				float4 right = tex2D(_MainTex, IN.uv + float2(u, 0));
				float4 top = tex2D(_MainTex, IN.uv + float2(0, u));
				float4 bottom = tex2D(_MainTex, IN.uv + float2(0, -u));
				
				float4 center = tex2D(_MainTex, IN.uv);
				
				return (left+right+top+bottom + _Alpha*center)/(4.0 + _Alpha);
			}
			
			ENDCG

    	}
	}
}