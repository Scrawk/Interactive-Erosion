
Shader "Erosion/MoveByLiquid"
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

		uniform sampler2D _MainTex;
	uniform float T;
	uniform sampler2D _OutFlow, _LuquidLevel;

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


		// moves sediment according to liquid outflow
		float4 liquidFlow = tex2D(_OutFlow, IN.uv);
		float liquidLevel = tex2D(_OutFlow, IN.uv).x;
		float totalLiquid = liquidFlow.x + liquidFlow.y + liquidFlow.z + liquidFlow.w + liquidLevel;
		float sedimentWas = tex2D(_MainTex, IN.uv).x;

		//= sedimentWas* liquidLevel / totalLiquid;
		float flowL = sedimentWas* liquidFlow.x / totalLiquid
		float flowR = sedimentWas* liquidFlow.y / totalLiquid
		float flowT = sedimentWas* liquidFlow.z / totalLiquid
		float flowB = sedimentWas* liquidFlow.w / totalLiquid
			//write all five cells



			//If the sum of the outflow flux exceeds the water amount of the
			//cell, flux value will be scaled down by a factor K to avoid negative
			//updated water height

			//float K = min(1.0, (sedimentWas * L*L) / ((flowL + flowR + flowT + flowB) * T));
			float K = min(1.0, (sedimentWas) / ((flowL + flowR + flowT + flowB) * T));

		return float4(flowL, flowR, flowT, flowB) * K;
		}

			ENDCG

		}
	}
}
//Shader "Erosion/MoveByLiquid"
//{
//	//UNITY_SHADER_NO_UPGRADE
//	Properties
//	{
//		_MainTex("Base (RGB)", 2D) = "white" {}
//	}
//		SubShader
//	{
//
//		Pass
//		{
//			ZTest Always
//
//			CGPROGRAM
//			#include "UnityCG.cginc"
//			#pragma target 3.0
//			#pragma vertex vert
//			#pragma fragment frag
//
//			uniform sampler2D _MainTex;
//			uniform float T, _TexSize;
//			uniform sampler2D _VelocityField;
//
//			struct v2f
//			{
//				float4  pos : SV_POSITION;
//				float2  uv : TEXCOORD0;
//			};
//
//			v2f vert(appdata_base v)
//			{
//				v2f OUT;
//				OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//				OUT.uv = v.texcoord.xy;
//				return OUT;
//			}
//
//			float4 frag(v2f IN) : COLOR
//			{
//				// moves sediment according to water velocity and size of texture(?)
//				float2 velocity = T * tex2D(_VelocityField, IN.uv).xy;// / _TexSize
//				float2 targetPos = IN.uv + velocity;
//				//float targetData = tex2D(_MainTex, IN.uv).r+ tex2D(_MainTex, targetPos).r;
//				float targetData =  tex2D(_MainTex, targetPos).r;
//
//				return float4(targetData,0,0,0);
//			}
//
//			ENDCG
//
//		}
//	}
//}