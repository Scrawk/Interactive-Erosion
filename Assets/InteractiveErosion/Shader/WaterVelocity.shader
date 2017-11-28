//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/WaterVelocity" 
{
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
			
			uniform sampler2D _WaterField, _WaterFieldOld, _OutFlowField;
			uniform float L, _TexSize;
		
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
				
				float4 flowC = tex2D(_OutFlowField, IN.uv);
				float4 flowL = tex2D(_OutFlowField, IN.uv + float2(-u, 0));
				float4 flowR = tex2D(_OutFlowField, IN.uv + float2(u, 0));
				float4 flowT = tex2D(_OutFlowField, IN.uv + float2(0, u));
				float4 flowB = tex2D(_OutFlowField, IN.uv + float2(0, -u));
			
				float2 velocityField;
				velocityField.x = (flowL.y - flowC.x + flowC.y - flowR.x) * 0.5;
				velocityField.y = (flowB.z - flowC.w + flowC.z - flowT.w) * 0.5;
				
				// compute the velocity
				float waterC = tex2D(_WaterField, IN.uv).x;
				float waterCOld = tex2D(_WaterFieldOld, IN.uv).x;
				
				float velocityFactor = L * (waterC+waterCOld) * 0.5;	
				
				velocityField = (velocityFactor > 1e-5) ? velocityField/velocityFactor : float2(0,0);
				
				return float4(velocityField,0,1);
			}
			
			ENDCG

    	}
	}
}
















