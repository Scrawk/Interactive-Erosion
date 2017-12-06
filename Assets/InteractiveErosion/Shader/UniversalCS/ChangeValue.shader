
Shader "Erosion/ChangeValue"
{
	//UNITY_SHADER_NO_UPGRADE
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

			sampler2D _MainTex;
			uniform float4 _Value;

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
				float4 oldValue = tex2D(_MainTex, IN.uv);
				float4 newValue = oldValue + _Value;
				//newValue = max(newValue, 0.0);
				return float4(newValue.x, newValue.y, newValue.z, newValue.w);
			}
			ENDCG
		}
	}
}