Shader "CpRemix/Igloo/IglooFurniture"
{
	Properties
	{
		_Color("Tint Color", Color) = (1,1,1,1)
		_MainTex("Texture (RGB)", 2D) = "white" {}
		_Highlight("Additional Highlight", Range(0, 1)) = 0
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
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Highlight;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 color : COLOR0;
				float2 uv : TEXCOORD0;
				float4 color2 : COLOR1;
				float4 pos : SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			inline float3 ComputeVertexLighting(float3 objectNormal)
			{
				float3x3 worldToObject3x3;
				worldToObject3x3[0] = unity_WorldToObject[0].xyz;
				worldToObject3x3[1] = unity_WorldToObject[1].xyz;
				worldToObject3x3[2] = unity_WorldToObject[2].xyz;

				float3 normalWorldSpace = normalize(mul(objectNormal, worldToObject3x3));
				float4 normalWS4 = float4(normalWorldSpace, 1.0);

				float3 sh0;
				sh0.x = dot(unity_SHAr, normalWS4);
				sh0.y = dot(unity_SHAg, normalWS4);
				sh0.z = dot(unity_SHAb, normalWS4);

				float4 shCross = normalWorldSpace.xyzz * normalWorldSpace.yzzx;

				float3 sh1;
				sh1.x = dot(unity_SHBr, shCross);
				sh1.y = dot(unity_SHBg, shCross);
				sh1.z = dot(unity_SHBb, shCross);

				float3 ambient = sh0 + sh1 + unity_SHC.xyz *
					((normalWorldSpace.x * normalWorldSpace.x) -
					(normalWorldSpace.y * normalWorldSpace.y));

				ambient = max((1.055 * pow(max(ambient, 0.0), 0.4166667)) - 0.055, 0.0);

				float ndl = clamp((dot(float4(normalWorldSpace, 0.0), _WorldSpaceLightPos0) + 1.0) / 4.0, 0.0, 1.0);
				float3 direct = (_LightColor0.rgb * ndl);

				return ambient + direct;
			}

			v2f vert(appdata v)
			{
				UNITY_SETUP_INSTANCE_ID(v);

				v2f o;
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 lighting = ComputeVertexLighting(v.normal);
				float4 litColor = float4(lighting, 1.0);

				litColor *= _Color;
				litColor += _Highlight;

				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = litColor;
				o.uv = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				o.color2 = v.color;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.uv);
				return (i.color * fixed4(0.9, 0.9, 0.9, 0.9)) * tex * i.color2;
			}

			ENDCG
		}
	}

	FallBack "VertexLit"
}