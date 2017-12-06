
Shader "Erosion/ChangeValueZeroControl"
{
	//UNITY_SHADER_NO_UPGRADE
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
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
			float _Value;

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
				float value = tex2D(_MainTex, IN.uv).x;
				value += _Value;
				value = max(value,0.0);
				return float4(value,0,0,0);
			}

			ENDCG

		}
	}
}