//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/SlippageUpdate" 
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
			uniform sampler2D _SlippageOutflow;
			uniform float T, _TexSize, _Layers;
		
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
				float u = 1.0 / _TexSize;
			
				float4 flow = tex2D(_SlippageOutflow, IN.uv);
				float4 flowL = tex2D(_SlippageOutflow, IN.uv + float2(-u, 0));
				float4 flowR = tex2D(_SlippageOutflow, IN.uv + float2(u, 0));
				float4 flowT = tex2D(_SlippageOutflow, IN.uv + float2(0, u));
				float4 flowB = tex2D(_SlippageOutflow, IN.uv + float2(0, -u));
				
				float layerDif = T * ((flowL.y + flowR.x + flowT.w + flowB.z) - (flow.x + flow.y + flow.z + flow.w));	
				
				float4 layerData = float4(0,0,0,0);
					
				if (0 ==_Layers-1) layerData[0] = layerDif;
				if (1 == _Layers - 1) layerData[1] = layerDif;
				if (2 == _Layers - 1) layerData[2] = layerDif;
				if (3 == _Layers - 1) layerData[3] = layerDif;
				
				return tex2D(_MainTex, IN.uv) + layerData;

			}
			
			ENDCG

    	}
	}
}