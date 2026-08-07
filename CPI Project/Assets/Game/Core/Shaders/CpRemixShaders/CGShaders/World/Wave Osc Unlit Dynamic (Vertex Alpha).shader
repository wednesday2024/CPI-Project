Shader "CpRemix/World/Wave Osc Unlit Dynamic (Vertex Alpha)"
{
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_OscDir ("World Osc  Dir", Vector) = (1,0,0,1)
		_OscAxis ("World Osc Axs (w = wave freq)", Vector) = (0,1,0,1)
		_OscSpeed ("Osc Speed", Float) = 1
	}

	SubShader {
		Tags { "RenderType" = "Opaque" "DisableBatching" = "True" }

		Pass {
			Tags { "RenderType" = "Opaque" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 position : SV_POSITION;
				float3 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct fout
			{
				float4 sv_target : SV_Target;
			};

			float4 _MainTex_ST;
			float3 _OscDir;
			float4 _OscAxis;
			float _OscSpeed;
			sampler2D _MainTex;
			
			v2f vert(appdata_full v)
			{
				v2f o;

				float axisOffset = dot(v.vertex.xyz, mul((float3x3)unity_WorldToObject, _OscAxis.xyz)) * _OscAxis.w;
				float vertexColorApplication = 1.0 - v.color.w;

				float3 oscDirWorld = mul((float3x3)unity_WorldToObject, _OscDir);
				float oscillatedVertex = sin((_Time.y * _OscSpeed) + axisOffset) * vertexColorApplication;
				float3 newPosition = v.vertex.xyz + oscillatedVertex * oscDirWorld;

				o.position = UnityObjectToClipPos(float4(newPosition, 1.0));

				o.texcoord = v.color.xyz;
				o.texcoord1.xy = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;

				return o;
			}

			fout frag(v2f inp)
			{
				fout o;

				float4 texColor = tex2D(_MainTex, inp.texcoord1.xy);
				o.sv_target.xyz = texColor.xyz * inp.texcoord.xyz;
				o.sv_target.w = 1.0;

				UNITY_OPAQUE_ALPHA(o.sv_target.w);

				return o;
			}
			ENDCG
		}
	}
}