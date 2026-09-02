Shader "CpRemix/World/Wave Osc Depth (Vertex Alpha)" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_OscDir ("World Osc  Dir", Vector) = (1,0,0,1)
		_OscAxis ("World Osc Axs (w = wave freq)", Vector) = (0,1,0,1)
		_OscSpeed ("Osc Speed", Float) = 1
		_DepthMultiply ("DepthMultiply", Range(0, 1)) = 1
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Pass {
			Tags { "RenderType" = "Opaque" }
			GpuProgramID 65288
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct v2f {
				float4 position    : SV_POSITION0;
				float3 color       : COLOR0;
				float2 texcoord    : TEXCOORD0;
				float2 texcoord1   : TEXCOORD1;
				float3 texcoord2   : TEXCOORD2;
				float3 texcoord3   : TEXCOORD3;
				float2 texcoord4   : TEXCOORD4;
				UNITY_FOG_COORDS(5)
			};

			struct fout {
				float4 sv_target : SV_Target0;
			};

			float4 _MainTex_ST;
			float3 _OscDir;
			float4 _OscAxis;
			float  _OscSpeed;
			float  _DepthMultiply;
			float  _SurfaceYCoord;
			float  _DeepestYCoord;
			float3 _DepthColor;
			float3 _SurfaceReflectionColor;
			float  _DynSurfaceTexTile;
			float  _DynSurfaceMultiplier;
			float  _SurfaceVelocityX;
			float  _SurfaceVelocityZ;

			sampler2D _MainTex;
			sampler2D _SurfaceReflectionsRGB;

			v2f vert(appdata_full v) {
				v2f o;

				float3 oscAxisOS = float3(
					dot(unity_WorldToObject._m00_m10_m20, _OscAxis.xxx) + dot(unity_WorldToObject._m01_m11_m21, _OscAxis.yyy) + dot(unity_WorldToObject._m02_m12_m22, _OscAxis.zzz)
				, 0, 0);
				oscAxisOS.x = oscAxisOS.x;
				oscAxisOS = float3(
					unity_WorldToObject._m00 * _OscAxis.x + unity_WorldToObject._m01 * _OscAxis.y + unity_WorldToObject._m02 * _OscAxis.z,
					unity_WorldToObject._m10 * _OscAxis.x + unity_WorldToObject._m11 * _OscAxis.y + unity_WorldToObject._m12 * _OscAxis.z,
					unity_WorldToObject._m20 * _OscAxis.x + unity_WorldToObject._m21 * _OscAxis.y + unity_WorldToObject._m22 * _OscAxis.z
				);

				float wave = dot(v.vertex.xyz, oscAxisOS) * _OscAxis.w;
				wave = sin(_Time.y * _OscSpeed + wave);
				wave *= (1.0 - v.color.w);

				float3 oscDirOS = float3(
					unity_WorldToObject._m00 * _OscDir.x + unity_WorldToObject._m01 * _OscDir.y + unity_WorldToObject._m02 * _OscDir.z,
					unity_WorldToObject._m10 * _OscDir.x + unity_WorldToObject._m11 * _OscDir.y + unity_WorldToObject._m12 * _OscDir.z,
					unity_WorldToObject._m20 * _OscDir.x + unity_WorldToObject._m21 * _OscDir.y + unity_WorldToObject._m22 * _OscDir.z
				);

				float3 displacedOS = v.vertex.xyz + wave * oscDirOS;

				float4 worldPos = mul(unity_ObjectToWorld, float4(displacedOS, 1.0));
				o.position = mul(unity_MatrixVP, worldPos);
				o.color = v.color.xyz;

				float3 rawWorldPos = float3(
					unity_ObjectToWorld._m00 * v.vertex.x + unity_ObjectToWorld._m01 * v.vertex.y + unity_ObjectToWorld._m02 * v.vertex.z + unity_ObjectToWorld._m03,
					unity_ObjectToWorld._m10 * v.vertex.x + unity_ObjectToWorld._m11 * v.vertex.y + unity_ObjectToWorld._m12 * v.vertex.z + unity_ObjectToWorld._m13,
					unity_ObjectToWorld._m20 * v.vertex.x + unity_ObjectToWorld._m21 * v.vertex.y + unity_ObjectToWorld._m22 * v.vertex.z + unity_ObjectToWorld._m23
				);

				float2 surfaceScroll = _DynSurfaceTexTile * float2(_SurfaceVelocityX, _SurfaceVelocityZ) * _Time.x;
				o.texcoord4 = rawWorldPos.xz * _DynSurfaceTexTile - surfaceScroll;
				o.texcoord  = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				o.texcoord1 = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

				float depthRange  = _SurfaceYCoord - _DeepestYCoord;
				float depthFactor = saturate((rawWorldPos.y - _DeepestYCoord) / max(depthRange, 0.0001));
				depthFactor = 1.0 - depthFactor;
				float depthBlend  = depthFactor * _DepthMultiply;
				float invBlend    = 1.0 - depthBlend;
				o.texcoord2 = _DepthColor * depthBlend + invBlend;

				float aboveSurface = rawWorldPos.y < _SurfaceYCoord ? (_SurfaceYCoord - rawWorldPos.y) : 0.0;
				aboveSurface = min(aboveSurface, 1.0);

				float3 normalWS = float3(
					unity_WorldToObject._m00 * v.normal.x + unity_WorldToObject._m10 * v.normal.y + unity_WorldToObject._m20 * v.normal.z,
					unity_WorldToObject._m01 * v.normal.x + unity_WorldToObject._m11 * v.normal.y + unity_WorldToObject._m21 * v.normal.z,
					unity_WorldToObject._m02 * v.normal.x + unity_WorldToObject._m12 * v.normal.y + unity_WorldToObject._m22 * v.normal.z
				);
				float normLen   = rsqrt(dot(normalWS, normalWS));
				float normY     = normLen * normalWS.y;
				float normYSq   = normY > 0.0 ? normY * normY : 0.0;
				float surfRefl  = invBlend * aboveSurface * normYSq * _DynSurfaceMultiplier * 0.5;
				o.texcoord3 = surfRefl * _SurfaceReflectionColor;

				UNITY_TRANSFER_FOG(o, o.position);
				return o;
			}

			fout frag(v2f inp) {
				fout o;

				float4 lm   = UNITY_SAMPLE_TEX2D_SAMPLER(unity_Lightmap, unity_Lightmap, inp.texcoord1);
				float3 lmRGB = lm.xyz * (lm.w * unity_Lightmap_HDR.x);

				float4 albedo = tex2D(_MainTex, inp.texcoord);
				float3 lit    = lmRGB * (albedo.xyz * inp.color);

				float4 refl   = tex2D(_SurfaceReflectionsRGB, inp.texcoord4);
				float3 reflRGB = refl.x * inp.texcoord3;

				o.sv_target = float4(lit * inp.texcoord2 + reflRGB, 1.0);
				UNITY_APPLY_FOG(inp.fogCoord, o.sv_target);
				UNITY_OPAQUE_ALPHA(o.sv_target.w);
				return o;
			}
			ENDCG
		}
	}
}