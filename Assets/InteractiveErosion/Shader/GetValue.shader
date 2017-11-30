
Shader "GetValue" 
{
	//UNITY_SHADER_NO_UPGRADE
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
			
			//uniform float _MinTiltAngle, _SedimentCapacity, _DepositionConstant, _Layers, _ErosionLimit;			
			//uniform float4 _DissolvingConstant;
			uniform sampler2D _InputTexture;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};
			
			struct f2a
			{
				float col0 : COLOR0;				
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}			
			
			
			f2a frag(v2f IN)
			{	
				float velocity = length(tex2D(_VelocityField, IN.uv).xy);
			
				f2a OUT;
				
				OUT.col0 = velocity;				
				
				return OUT;
			}
			
			ENDCG

    	}
	}
}