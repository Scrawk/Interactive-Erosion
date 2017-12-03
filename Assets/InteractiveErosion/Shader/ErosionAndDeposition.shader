
Shader "Erosion/ErosionAndDeposition"
{
	//UNITY_SHADER_NO_UPGRADE
	Properties
	{
		_DissolveLimit("_DissolveLimit", float) = 0.001
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

			uniform float _MinTiltAngle, _SedimentCapacity, _DepositionConstant, _Layers, _DissolveLimit;
			uniform float4 _DissolvingConstant;
			uniform sampler2D _TiltAngle, _TerrainField, _VelocityField, _SedimentField, _WaterField;

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
				float4 addVec = abs(float4(_Layers - 1, _Layers - 2, _Layers - 3, _Layers - 4));
				return float4(1,1,1,1) - min(float4(1,1,1,1),addVec);
			}

			f2a frag(v2f IN)
			{
				float tiltAngle = tex2D(_TiltAngle, IN.uv).x;
				tiltAngle = (tiltAngle < _MinTiltAngle) ? _MinTiltAngle : tiltAngle;

				float4 terrain = tex2D(_TerrainField, IN.uv);
				float sediment = tex2D(_SedimentField, IN.uv).x;

				float velocity = length(tex2D(_VelocityField, IN.uv).xy);

				//float sedimentCapacityFactor = _SedimentCapacity * tiltAngle * velocity;
				float sedimentCapacityFactor  =  _SedimentCapacity * velocity;

				float4 finalMaxSediment;
				finalMaxSediment.x = sedimentCapacityFactor;
				finalMaxSediment.y = sedimentCapacityFactor;
				finalMaxSediment.z = sedimentCapacityFactor;
				finalMaxSediment.w = sedimentCapacityFactor;

				float4 terrainDif = float4(0.0, 0.0, 0.0, 0.0);
				float finalSedimentDif = 0.0;

				if (sediment > sedimentCapacityFactor)
				{
					//deposit it
					{
						float sedimentDif = _DepositionConstant * (sediment - sedimentCapacityFactor);
						finalSedimentDif -= sedimentDif;
						terrainDif = GetTopmostLayerVec() * sedimentDif;
					}
				}
				else
				{
					//dissolve it										
					float waterLevel = tex2D(_WaterField, IN.uv).x;
					if (waterLevel > _DissolveLimit)
					{

					float layersHeight = 0.0;
					float4 layerMask = float4(0.0, 0.0, 0.0, 1.0);

					for (int k = 3; k >= 0; k--)
					{
						if (k < _Layers)
						{
							float maxSedimentInLayer = finalMaxSediment[k] - layersHeight;

							if (maxSedimentInLayer < sediment) break;

							layersHeight += terrain[k];
							float sedimentDif = _DissolvingConstant[k] * (maxSedimentInLayer - sediment);

							//limit the dissolution to the actual layer thickness
							if (k > 0) sedimentDif = min(terrain[k], sedimentDif);

							finalSedimentDif += sedimentDif;
							terrainDif = terrainDif - sedimentDif * layerMask;
						}

						layerMask.xyzw = layerMask.yzwx;
					}
				}
				}

				f2a OUT;

				OUT.col0 = terrain + terrainDif;
				OUT.col1 = float4(sediment + finalSedimentDif, 0.0, 0.0 ,0.0);

				return OUT;
			}

			ENDCG

		}
	}
}