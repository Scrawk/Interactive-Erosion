// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Erosion/MaxSlippageHeight" 
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
			
			uniform sampler _TerrainField;
			uniform float _MaxHeightDif, _Layers, _TexSize, _Height;
		
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
				
				//smoothing of hard edges
				float maxLocalDif = _MaxHeightDif * 0.01;
				float avgDif = (htL + htR + htB + htT) * 0.25 - ht;
				avgDif = 10.0 * max(abs(avgDif) - maxLocalDif, 0);

				return float4(max(_MaxHeightDif - avgDif, 0), 0,0,0);
			}
			
			ENDCG

    	}
	}
}