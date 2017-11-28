//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/OutFlow" 
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
			
			sampler2D _MainTex; 
			uniform sampler2D _TerrainField, _Field;
			uniform float _TexSize, T, L, A, G, _Layers, _Damping;
		
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
			
			float GetTotalHeight(float4 texData) 
			{
				float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
				float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
				return dot(texData, addVec);
			}	
			
			float4 frag(v2f IN) : COLOR
			{
				float u = 1.0f/_TexSize;
				
				float ht = GetTotalHeight(tex2D(_TerrainField, IN.uv));
				float htL = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(-u, 0)));
				float htR = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(u, 0)));
				float htT = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(0, u)));
				float htB = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(0, -u)));
	
				float field = tex2D(_Field, IN.uv).x;
				float fieldL = tex2D(_Field, IN.uv + float2(-u, 0)).x;
				float fieldR = tex2D(_Field, IN.uv + float2(u, 0)).x;
				float fieldT = tex2D(_Field, IN.uv + float2(0, u)).x;
				float fieldB = tex2D(_Field, IN.uv + float2(0, -u)).x;
				
				float4 flow = tex2D(_MainTex, IN.uv) * _Damping;
				
				ht += field;
				
				//deltaHX is the height diff between this cell and neighbour X
				float deltaHL = ht - htL - fieldL;
				float deltaHR = ht - htR - fieldR;
				float deltaHT = ht - htT - fieldT;
				float deltaHB = ht - htB - fieldB;
				
				//new flux value is old value + delta time * area * ((gravity * delta ht) / length)
				//max 0, no neg values
				//left(x), right(y), top(z), bottom(w)
				
				float flowL = max(0.0, flow.x + T * A * ((G * deltaHL) / L));
				float flowR = max(0.0, flow.y + T * A * ((G * deltaHR) / L));
				float flowT = max(0.0, flow.z + T * A * ((G * deltaHT) / L));
				float flowB = max(0.0, flow.w + T * A * ((G * deltaHB) / L));
				
				//If the sum of the outflow flux exceeds the water amount of the
				//cell, flux value will be scaled down by a factor K to avoid negative
				//updated water height
				
				float K = min(1.0, (field * L*L) / ((flowL + flowR + flowT + flowB) * T) );
				
				return float4(flowL, flowR, flowT, flowB) * K;

			}
			
			ENDCG

    	}
	}
}