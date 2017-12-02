//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/GetValue"
{
	Properties
	{
		_Output("Output", Vector) = (0,0,0,1)
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

			uniform sampler2D _InputTexture;
			extern float4 _Output;
			uniform float2 _Coords;

			struct v2f
			{
				float4  pos : SV_POSITION;
				float2  uv : TEXCOORD0;
			};

			/*struct f2a
			{
				float col0 : COLOR0;
			};*/

			v2f vert(appdata_base v)
			{
				v2f OUT;
				OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				OUT.uv = v.texcoord.xy;
				return OUT;
			}


			float4 frag(v2f IN) : COLOR
			{
				if (IN.uv.x == _Coords.x && IN.uv.y == _Coords.y)
					_Output = tex2D(_InputTexture, IN.uv);
				else
					_Output = float4(-1, -1, -1, -1);
				return tex2D(_InputTexture, IN.uv);
			}

			ENDCG

		}
	}
}