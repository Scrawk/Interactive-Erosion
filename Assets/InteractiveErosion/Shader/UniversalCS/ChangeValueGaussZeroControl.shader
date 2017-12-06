//UNITY_SHADER_NO_UPGRADE
Shader "Erosion/ChangeValueGaussZeroControl"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader
	{
		Pass
	{
		ZTest Always Cull Off ZWrite Off
		Fog{ Mode off }

		CGPROGRAM
#include "UnityCG.cginc"
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag

		sampler2D _MainTex;
	uniform float2 _Point;
	uniform float _Radius, _Amount;
	uniform float4 _LayerMask;

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

	float GetGaussFactor(float2 diff, float rad2)
	{
		return exp(-(diff.x*diff.x + diff.y*diff.y) / rad2);
	}

	float4 frag(v2f IN) : COLOR
	{

		float gauss = GetGaussFactor(_Point - IN.uv, _Radius*_Radius);

	float value = gauss * _Amount;

	float4 res = tex2D(_MainTex, IN.uv) + _LayerMask * value;
	if (_LayerMask.x == 1)
	res.x = max(res.x, 0);
	else if (_LayerMask.y == 1)
	res.y = max(res.y, 0);
	else if (_LayerMask.z == 1)
	res.z = max(res.z, 0);
	else if (_LayerMask.w == 1)
	res.w = max(res.w, 0);
	return res; // float4(waterAmount, 0, 0, 0);
	}

		ENDCG

	}
	}
}