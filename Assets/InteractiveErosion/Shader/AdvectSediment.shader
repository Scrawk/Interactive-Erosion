// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Erosion/AdvectSediment" 
{
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
			
			sampler2D _MainTex;
			uniform float T, _VelocityFactor, _TexSize;
			uniform sampler _VelocityField;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 frag(v2f IN) : COLOR
			{
			
				float2 velocity = _VelocityFactor * tex2D(_VelocityField, IN.uv).xy / _TexSize;
				float2 targetPos = IN.uv - (T * velocity);
				float targetData = tex2D(_MainTex, targetPos).r;	
				
				return float4(targetData,0,0,0);	
			}
			
			ENDCG

    	}
	}
}