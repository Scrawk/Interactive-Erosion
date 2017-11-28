
Shader "Erosion/ProcessMacCormack" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
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
			
			sampler _MainTex;
			uniform sampler2D _VelocityField, _InterField1, _InterField2;
			uniform float T, _TexSize;
		
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
				float2 velocity = tex2D(_VelocityField, IN.uv).xy;
				float2 targetPos = IN.uv * _TexSize - T * velocity;	
				
				//find clamping values
				float4 st;
				st.xy = floor(targetPos - 0.5) + 0.5;
				st.zw = st.xy + 1.0;
					
			    float nodeVal[4];
				nodeVal[0] = tex2D(_MainTex, st.xy/_TexSize).x;
				nodeVal[1] = tex2D(_MainTex, st.zy/_TexSize).x;
				nodeVal[2] = tex2D(_MainTex, st.xw/_TexSize).x;
				nodeVal[3] = tex2D(_MainTex, st.zw/_TexSize).x;		
				
				float clampMin = min(min(min(nodeVal[0],nodeVal[1]),nodeVal[2]),nodeVal[3]);
				float clampMax = max(max(max(nodeVal[0],nodeVal[1]),nodeVal[2]),nodeVal[3]);
				
				float sediment = tex2D(_MainTex, IN.uv).x;
			
			    float res = tex2D(_InterField1, IN.uv).x + 0.5 * (sediment - tex2D(_InterField2, IN.uv).x);
			    
			    sediment = max(min(res, clampMax), clampMin);
			    
			    return float4(sediment,0,0,0);
			}
			
			ENDCG

    	}
	}
}