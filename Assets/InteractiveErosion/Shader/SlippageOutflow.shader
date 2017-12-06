//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/SlippageOutflow" 
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
			
			uniform sampler2D _MaxSlippageHeights, _TerrainField;
			uniform float T, _Layers, _TexSize;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};
			
			float GetTotalHeight(float4 texData) 
			{
				float4 maskVec = float4(_Layers, _Layers-1, _Layers-2, _Layers-3);
				float4 addVec = min(float4(1,1,1,1),max(float4(0,0,0,0), maskVec));	
				return dot(texData, addVec);
			}	

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 frag(v2f IN) : COLOR
			{
				// has negative texture coordinates glitch
				// actually it doesn't glitch since it based on water outflow which is fixed
				float u = 1.0f/_TexSize;
				
				float4 hts = tex2D(_TerrainField, IN.uv);
				float ht = GetTotalHeight(hts);
				float maxHt = tex2D(_MaxSlippageHeights, IN.uv).x;

				float htL = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(-u, 0)));
				float maxHtL = tex2D(_MaxSlippageHeights, IN.uv + float2(-u, 0)).x;

				float htR = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(u, 0)));
				float maxHtR = tex2D(_MaxSlippageHeights, IN.uv + float2(u, 0)).x;

				float htT = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(0, u)));
				float maxHtT = tex2D(_MaxSlippageHeights, IN.uv + float2(0, u)).x;

				float htB = GetTotalHeight(tex2D(_TerrainField, IN.uv + float2(0, -u)));
				float maxHtB = tex2D(_MaxSlippageHeights, IN.uv + float2(0, -u)).x;
				
				float4 dif;
				dif.x = ht - htL - (maxHtL + maxHt) * 0.5;
				dif.y = ht - htR - (maxHtR + maxHt) * 0.5;
				dif.z = ht - htT - (maxHtT + maxHt) * 0.5;
				dif.w = ht - htB - (maxHtB + maxHt) * 0.5;
				
				dif = max(float4(0,0,0,0), dif);
				
				float4 newFlow = dif*0.2;
				
				float layerData = 0.0;
				
				for(int i = 0; i < 4; i++) 	
					if(i ==_Layers-1) layerData = hts[i];

				if(_Layers > 1.0) 
				{
					float outFactor = ((newFlow.x + newFlow.y + newFlow.z + newFlow.w) * T);
					
					if(outFactor > 1e-5) 
					{
						outFactor = layerData / outFactor;
						if(outFactor > 1.0) outFactor = 1;					
						newFlow = newFlow * outFactor;			
						
					} 
					else 
						newFlow = float4(0,0,0,0);	
				}
				
				return newFlow;
			}
			
			ENDCG

    	}
	}
}