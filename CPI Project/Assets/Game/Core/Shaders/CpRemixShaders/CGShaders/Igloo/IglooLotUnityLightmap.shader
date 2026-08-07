Shader "CpRemix/Igloo/IglooLotUnityLightmap"
{
	Properties
	{
		_Color ("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Texture (RGB)", 2D) = "white" {}
		_Highlight ("Additional Highlight", Range(0, 1)) = 0
		[HideInInspector] _BlobShadowTex ("Blob Shadow Tex", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"LIGHTMODE" = "FORWARDBASE"
			"QUEUE" = "Geometry"
			"RenderType" = "Opaque"
		}

		Pass
		{
			Tags
			{
				"LIGHTMODE" = "FORWARDBASE"
				"QUEUE" = "Geometry"
				"RenderType" = "Opaque"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _BlobShadowTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Highlight;
			float4 _LightColor0;
			float _ShadowPlaneDim;
			float _ShadowTextureDim;
			float3 _ShadowPlaneWorldPos;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR0;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float3 shadowData : TEXCOORD3;
				UNITY_FOG_COORDS(2)
			};

			inline float3 DecodeDirectionalVertexLighting(float3 worldNormal)
			{
				float4 n = float4(worldNormal, 1.0);

				float3 shLinear;
				shLinear.x = dot(unity_SHAr, n);
				shLinear.y = dot(unity_SHAg, n);
				shLinear.z = dot(unity_SHAb, n);

				float4 shCross = worldNormal.xyzz * worldNormal.yzzx;

				float3 shQuadratic;
				shQuadratic.x = dot(unity_SHBr, shCross);
				shQuadratic.y = dot(unity_SHBg, shCross);
				shQuadratic.z = dot(unity_SHBb, shCross);
				shQuadratic += unity_SHC.xyz * (worldNormal.x * worldNormal.x - worldNormal.y * worldNormal.y);

				float3 ambient = max(shLinear + shQuadratic, 0.0);

				ambient = max((1.055 * pow(ambient, 0.4166667)) - 0.055, 0.0);

				float ndl = saturate((dot(worldNormal, _WorldSpaceLightPos0.xyz) + 1.0) * 0.25);
				float3 direct = _LightColor0.rgb * ndl;

				return direct + ambient;
			}

			v2f vert(appdata v)
			{
				v2f o;

				o.position = UnityObjectToClipPos(v.vertex);

				float3 worldNormal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
				float3 lighting = DecodeDirectionalVertexLighting(worldNormal);

				o.color = float4(lighting, 2.0) * _Color + _Highlight.xxxx;
				o.texcoord = v.texcoord;
				o.texcoord1 = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float halfDim = _ShadowPlaneDim * 0.5;
				float aspectOfs = 1.0 / _ShadowTextureDim;
				float offsetX = worldPos.x - _ShadowPlaneWorldPos.x;
				float offsetZ = worldPos.z - _ShadowPlaneWorldPos.z;
				o.shadowData.x = (aspectOfs + offsetX / halfDim + 1.0) * 0.5;
				o.shadowData.y = (aspectOfs + offsetZ / halfDim + 1.0) * 0.5;
				o.shadowData.z = worldPos.y;

				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 lightmapSample = UNITY_SAMPLE_TEX2D_SAMPLER(unity_Lightmap, unity_Lightmap, i.texcoord1);
				lightmapSample.rgb *= lightmapSample.a * unity_Lightmap_HDR.x;
				lightmapSample.a = 1.0;

				fixed4 albedo = tex2D(_MainTex, i.texcoord);
				fixed4 col = lightmapSample * i.color * albedo * 0.9;

				float4 shadowSample = tex2D(_BlobShadowTex, i.shadowData.xy);
				float shadowDepth = shadowSample.y;
				float shadowIntensity = shadowSample.x;
				float isAbove = (i.shadowData.z >= shadowDepth) ? 2.0 : 1.0;
				float depthDiff = shadowDepth - i.shadowData.z;
				float shadowFactor = mad(abs(depthDiff), isAbove, isAbove) - 0.5;
				float shadowMult = min(shadowIntensity * max(shadowFactor, 1.0), 1.0);
				col.rgb *= shadowMult;

				UNITY_APPLY_FOG(i.fogCoord, col);
				UNITY_OPAQUE_ALPHA(col.a);
				return col;
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}