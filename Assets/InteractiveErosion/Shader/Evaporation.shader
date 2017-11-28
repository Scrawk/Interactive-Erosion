
Shader "Erosion/Evaporation" 
{
	//UNITY_SHADER_NO_UPGRADE
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
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
			
			sampler2D _MainTex; //m_waterField
			float _EvaporationConstant;
		
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
				float waterField = tex2D(_MainTex, IN.uv).x;
			
				waterField -= _EvaporationConstant;
				
				waterField = max(waterField,0.0);
				
				return float4(waterField,0,0,0);
			}
			
			ENDCG

    	}
	}
}