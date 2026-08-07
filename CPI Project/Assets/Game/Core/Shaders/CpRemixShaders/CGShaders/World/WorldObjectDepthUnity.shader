Shader "CpRemix/World/WorldObject Depth"
{
	Properties
	{
	  _Diffuse("Diffuse Texture", 2D) = "" {}
	  [HideInInspector] _BlobShadowTex ("Blob Shadow Tex", 2D) = "white" {}
	}
	SubShader
	{
	  Tags
	  {
	  }
	  Pass // ind: 1, name: 
	  {
		Tags
		{
		  "LIGHTMODE" = "FORWARDBASE"
		}
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog

		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"

		//float4 _Time;
		// float4x4 unity_ObjectToWorld;
		//float4x4 unity_WorldToObject;
		//float4x4 unity_MatrixVP;
		//float4 unity_LightmapST;
		float _SurfaceYCoord;
		float _DeepestYCoord;
		float3 _DepthColor;
		float3 _SurfaceReflectionColor;
		float _SurfaceTexTile;
		float _SurfaceMultiplier;
		float _SurfaceVelocityX;
		float _SurfaceVelocityZ;
		float _ShadowPlaneDim;
		float _ShadowTextureDim;
		float3 _ShadowPlaneWorldPos;
		//sampler2D unity_Lightmap;
		//float4 unity_Lightmap_HDR;
		sampler2D _Diffuse;
		sampler2D _SurfaceReflectionsRGB;
		sampler2D _BlobShadowTex;

		struct v2f
		{
			float3 xlv_COLOR : COLOR;
			float2 xlv_TEXCOORD0 : TEXCOORD0;
			float2 xlv_TEXCOORD1 : TEXCOORD1;
			float3 xlv_TEXCOORD3 : TEXCOORD3;
			float3 xlv_TEXCOORD4 : TEXCOORD4;
			float2 xlv_TEXCOORD5 : TEXCOORD5;
			float3 shadowData : TEXCOORD6;
			UNITY_FOG_COORDS(2)
		};

		struct FragOutput
		{
			float4 FragData : SV_Target;
		};

		v2f vert(
			float4 _glesVertex : POSITION,
			float4 _glesColor : COLOR,
			float3 _glesNormal : NORMAL,
			float4 _glesMultiTexCoord0 : TEXCOORD0,
			float4 _glesMultiTexCoord1 : TEXCOORD1,
			out float4 Position : SV_POSITION
		)
		{
			v2f o;
			float isBelowSurface_2;
			float depthDeltaNormalized_3;
			float3 worldSpaceNormalNormalized_4;
			float4 v_9;
			v_9.x = unity_WorldToObject[0].x;
			v_9.y = unity_WorldToObject[1].x;
			v_9.z = unity_WorldToObject[2].x;
			v_9.w = unity_WorldToObject[3].x;
			float4 v_10;
			v_10.x = unity_WorldToObject[0].y;
			v_10.y = unity_WorldToObject[1].y;
			v_10.z = unity_WorldToObject[2].y;
			v_10.w = unity_WorldToObject[3].y;
			float4 v_11;
			v_11.x = unity_WorldToObject[0].z;
			v_11.y = unity_WorldToObject[1].z;
			v_11.z = unity_WorldToObject[2].z;
			v_11.w = unity_WorldToObject[3].z;
			worldSpaceNormalNormalized_4 = normalize(((
				(v_9.xyz * _glesNormal.x)
			   +
				(v_10.xyz * _glesNormal.y)
			  ) + (v_11.xyz * _glesNormal.z)));
			float3 tmpvar_13 = mul(unity_ObjectToWorld, _glesVertex).xyz;
			depthDeltaNormalized_3 = (1.0 - clamp((
				(tmpvar_13.y - _DeepestYCoord)
			   /
				(_SurfaceYCoord - _DeepestYCoord)
			  ), 0.0, 1.0));
			isBelowSurface_2 = (_SurfaceYCoord - tmpvar_13.y);
			isBelowSurface_2 = (isBelowSurface_2 * float((isBelowSurface_2 > 0.0)));
			isBelowSurface_2 = min(1.0, isBelowSurface_2);
			Position = UnityObjectToClipPos(float4(_glesVertex.xyz, 1.0));
			o.xlv_COLOR = _glesColor.xyz;
			o.xlv_TEXCOORD0 = _glesMultiTexCoord0.xy;
			o.xlv_TEXCOORD1 = ((_glesMultiTexCoord1.xy * unity_LightmapST.xy) + unity_LightmapST.zw);
			o.xlv_TEXCOORD3 = ((_DepthColor * depthDeltaNormalized_3) + float((1.0 - depthDeltaNormalized_3)));
			o.xlv_TEXCOORD4 = (_SurfaceReflectionColor * ((
				((((worldSpaceNormalNormalized_4.y * worldSpaceNormalNormalized_4.y) * float(
				  (worldSpaceNormalNormalized_4.y > 0.0)
				))* isBelowSurface_2)* (1.0 - depthDeltaNormalized_3))
			   * 0.5)* _SurfaceMultiplier));
			o.xlv_TEXCOORD5 = ((tmpvar_13.xz * _SurfaceTexTile) - (_Time.xx * float2(_SurfaceVelocityX * _SurfaceTexTile, _SurfaceVelocityZ * _SurfaceTexTile)));

			float halfDim = _ShadowPlaneDim * 0.5;
			float aspectOfs = 1.0 / _ShadowTextureDim;
			float offsetX = tmpvar_13.x - _ShadowPlaneWorldPos.x;
			float offsetZ = tmpvar_13.z - _ShadowPlaneWorldPos.z;
			o.shadowData.x = (aspectOfs + offsetX / halfDim + 1.0) * 0.5;
			o.shadowData.y = (aspectOfs + offsetZ / halfDim + 1.0) * 0.5;
			o.shadowData.z = tmpvar_13.y;

			UNITY_TRANSFER_FOG(o,Position);
			return o;
		}


		FragOutput frag(v2f i)
		{
			FragOutput o;
			float3 diffuseSample_2 = tex2D(_Diffuse, i.xlv_TEXCOORD0).xyz;
			
			float3 lightmapColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.xlv_TEXCOORD1));
			
			float4 shadowSample = tex2D(_BlobShadowTex, i.shadowData.xy);
			float shadowDepth = shadowSample.y;
			float shadowIntensity = shadowSample.x;
			float isAbove = (i.shadowData.z >= shadowDepth) ? 2.0 : 1.0;
			float depthDiff = shadowDepth - i.shadowData.z;
			float shadowFactor = mad(abs(depthDiff), isAbove, isAbove) - 0.5;
			float shadowMult = min(shadowIntensity * max(shadowFactor, 1.0), 1.0);

			float3 outputColor_1 = diffuseSample_2 * lightmapColor * i.xlv_COLOR * i.xlv_TEXCOORD3 * shadowMult;
			outputColor_1 = (outputColor_1 + (tex2D(_SurfaceReflectionsRGB, i.xlv_TEXCOORD5).x * i.xlv_TEXCOORD4));
			
			float4 tmpvar_7;
			tmpvar_7.w = 1.0;
			tmpvar_7.xyz = outputColor_1;
			UNITY_APPLY_FOG(i.fogCoord, tmpvar_7);
			UNITY_OPAQUE_ALPHA(tmpvar_7.w);
			o.FragData = tmpvar_7;
			return o;
		}

		ENDCG
	  }
	}
	FallBack Off
}