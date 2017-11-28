
Shader "Erosion/DisintegrateAndDeposition" 
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
			
			uniform sampler2D _TerrainField, _WaterField, _RegolithField; 
			uniform float _Layers, _MaxRegolith;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};
			
			struct f2a
			{
				float4 col0 : COLOR0;
				float4 col1 : COLOR1;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 GetTopmostLayerVec() 
			{
				float4 addVec = abs(float4(_Layers-1, _Layers-2, _Layers-3, _Layers-4));
				return float4(1,1,1,1) - min(float4(1,1,1,1),addVec);
			}
			
			f2a frag(v2f IN)
			{
				float4 terrain = tex2D(_TerrainField, IN.uv);		
				float regolith = tex2D(_RegolithField, IN.uv).x;	
				float water = tex2D(_WaterField, IN.uv).x;	
				
				float finalMaxRegolith = min(_MaxRegolith, water);
				float4 terrainDif = float4( 0.0, 0.0, 0.0, 0.0 );	
				float totalRegolithDif = 0.0;
				
				if(regolith > finalMaxRegolith) 
				{
					float regolithDif = (regolith - finalMaxRegolith);		
					totalRegolithDif -= regolithDif;	
					terrainDif = GetTopmostLayerVec() * regolithDif;
				} 
				else 
				{
					float layersHeight = 0.0;
					float4 layerMask = float4(0.0, 0.0, 0.0, 1.0);
					
					for(int k = 3; k >= 0; k--) 
					{	
						if(k < _Layers) 
						{
							float maxR = finalMaxRegolith - layersHeight;
							
							if(maxR < regolith) break;
							
							float actLayerHeight = terrain[k];
							layersHeight += actLayerHeight;
							float regolithDif = (maxR - regolith);
							
							//limit the dissolution to the actual layer thickness
							if(k > 0) regolithDif = min(actLayerHeight, regolithDif);
							totalRegolithDif += regolithDif;
							
							terrainDif += layerMask * -regolithDif;			
						}
						
						layerMask.xyzw = layerMask.yzwx;
					}
				}
				
				f2a OUT;
				
				OUT.col0 = terrain + terrainDif;
				OUT.col1 = float4(regolith + totalRegolithDif, 0.0, 0.0 ,0.0);	
				
				return OUT;
	
			}
			
			ENDCG

    	}
	}
}