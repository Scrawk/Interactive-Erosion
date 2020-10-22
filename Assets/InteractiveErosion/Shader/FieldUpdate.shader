// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Erosion/FieldUpdate" 
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
			
			sampler _MainTex;
			uniform sampler2D _OutFlowField;
			uniform float _TexSize, T, L;
		
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
				float u = 1.0f/_TexSize;
				
				float field = tex2D(_MainTex, IN.uv).x;
				
				float4 flow = tex2D(_OutFlowField, IN.uv);
				float4 flowL = tex2D(_OutFlowField, IN.uv + float2(-u, 0));
				float4 flowR = tex2D(_OutFlowField, IN.uv + float2(u, 0));
				float4 flowT = tex2D(_OutFlowField, IN.uv + float2(0, u));
				float4 flowB = tex2D(_OutFlowField, IN.uv + float2(0, -u));
								
				//Flux in is inlow from neighour cells. Note for the cell on the left you need thats cells flow to the right (ie it flows into this cell)
				float flowIN = flowL.y + flowR.x + flowT.w + flowB.z;
				
				//Flux out is all out flows from this cell
				//left(x), right(y), top(z), bottom(w)
				float flowOUT = flow.x + flow.y + flow.z + flow.w;
				
				//V is net volume change for the water over time
				float V = T * (flowIN - flowOUT);
				
				//The water ht is the previously calculated ht plus the net volume change divided by lenght squared
				field = max(0, field + V / (L*L));
				
				return float4(field,0,0,0);

			}
			
			ENDCG

    	}
	}
}