Shader "CpRemix/Combined Avatar Depth"
{
    Properties
    {
        _MainTex ("Diffuse Texture", 2D) = "white" {}
        _Diffuse ("Diffuse", 2D) = "black" {}
        _BodyColorsMaskTex ("Body Color Mask", 2D) = "black" {}
        _BodyRedChannelColor ("Body Red Channel Color", Color) = (1,0,0,1)
        _BodyGreenChannelColor ("Body Green Channel Color", Color) = (1,1,0,1)
        _BodyBlueChannelColor ("Body Blue Channel Color", Color) = (1,0,1,1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+99" "RenderType" = "Opaque" }

        Pass
        {
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Diffuse);
            SAMPLER(sampler_Diffuse);
            TEXTURE2D(_BodyColorsMaskTex);
            SAMPLER(sampler_BodyColorsMaskTex);
            TEXTURE2D(_DetailAndMatcapMaskAndEmissive);
            SAMPLER(sampler_DetailAndMatcapMaskAndEmissive);

            float3 _BodyRedChannelColor;
            float3 _BodyGreenChannelColor;
            float3 _BodyBlueChannelColor;

            float  _SurfaceYCoord;
            float  _DeepestYCoord;
            float3 _DepthColor;
            float3 _SurfaceReflectionColor;
            float  _DynSurfaceTexTile;
            float  _DynSurfaceMultiplier;
            float  _SurfaceVelocityX;
            float  _SurfaceVelocityZ;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                float3 color  : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uvMain      : TEXCOORD0;
                float2 uvSurface   : TEXCOORD5;
                float3 lighting    : TEXCOORD1;
                float3 color       : COLOR;
                float3 depthColor  : TEXCOORD3;
                float3 reflection  : TEXCOORD4;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.pos = TransformWorldToHClip(worldPos);
                o.color = v.color;

                float velX = _SurfaceVelocityX * _Time.x;
                float velZ = _SurfaceVelocityZ * _Time.x;
                o.uvSurface.x = mad(worldPos.x, _DynSurfaceTexTile, -velX);
                o.uvSurface.y = mad(worldPos.z, _DynSurfaceTexTile, -velZ);

                o.uvMain = v.uv;

                float3 worldNormal = normalize(TransformObjectToWorldNormal(v.normal));
                Light mainLight = GetMainLight();
                float NdotL = max(dot(worldNormal, mainLight.direction), 0.0);
                float3 ambient = unity_AmbientSky.rgb * 0.45;
                float3 diffuse = NdotL * mainLight.color;

                float wrap = (NdotL + 0.5) * 0.6;

                o.lighting.x = max(wrap, mad(diffuse.x, 0.75, ambient.x));
                o.lighting.y = max(wrap, mad(diffuse.y, 0.75, ambient.y));
                o.lighting.z = max(wrap, mad(diffuse.z, 0.75, ambient.z));

                float depthRange  = _SurfaceYCoord - _DeepestYCoord;
                float depthT      = clamp((worldPos.y - _DeepestYCoord * 2.0 + _SurfaceYCoord) / (_SurfaceYCoord - _DeepestYCoord * 2.0 + _SurfaceYCoord), 0.0, 1.0);
                float invDepthT   = 1.0 - depthT;
                float invInvDepth = 1.0 - invDepthT;

                o.depthColor.x = mad(_DepthColor.x, invDepthT, invInvDepth);
                o.depthColor.y = mad(_DepthColor.y, invDepthT, invInvDepth);
                o.depthColor.z = mad(_DepthColor.z, invDepthT, invInvDepth);

                float aboveSurface = (0.0 < (worldPos.y - _SurfaceYCoord)) ? (worldPos.y - _SurfaceYCoord) : 0.0;
                float normalUpSq   = worldNormal.y * worldNormal.y;
                float normalUpMask = (0.0 < worldNormal.y) ? normalUpSq : 0.0;
                float reflStrength = min(aboveSurface, 1.0) * normalUpMask * invInvDepth * _DynSurfaceMultiplier * 0.5;

                o.reflection.x = reflStrength * _SurfaceReflectionColor.x;
                o.reflection.y = reflStrength * _SurfaceReflectionColor.y;
                o.reflection.z = reflStrength * _SurfaceReflectionColor.z;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uvMain);
                float4 surf = SAMPLE_TEXTURE2D(_DetailAndMatcapMaskAndEmissive, sampler_DetailAndMatcapMaskAndEmissive, i.uvSurface);
                float4 bodyDetail = SAMPLE_TEXTURE2D(_DetailAndMatcapMaskAndEmissive, sampler_DetailAndMatcapMaskAndEmissive, i.uvMain);
                float4 bodyMask = SAMPLE_TEXTURE2D(_BodyColorsMaskTex, sampler_BodyColorsMaskTex, i.uvMain);
                float4 bodyDiff = SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, i.uvMain);

                float3 bodyColorFromMask;
                bodyColorFromMask.x = mad(bodyMask.z, _BodyBlueChannelColor.x, mad(bodyMask.x, _BodyRedChannelColor.x, bodyMask.y * _BodyGreenChannelColor.x));
                bodyColorFromMask.y = mad(bodyMask.z, _BodyBlueChannelColor.y, mad(bodyMask.x, _BodyRedChannelColor.y, bodyMask.y * _BodyGreenChannelColor.y));
                bodyColorFromMask.z = mad(bodyMask.z, _BodyBlueChannelColor.z, mad(bodyMask.x, _BodyRedChannelColor.z, bodyMask.y * _BodyGreenChannelColor.z));
                float bodyMaskMax = max(bodyMask.z, max(bodyMask.y, bodyMask.x));
                float3 bodyColor = bodyDiff.rgb * (1.0 - bodyMaskMax) + bodyMaskMax * bodyColorFromMask;
                float3 sourceColor = (bodyMaskMax > 0.0) ? bodyColor : main.rgb;
                float sourceAlpha = (bodyMaskMax > 0.0) ? bodyDiff.a : main.a;

                if (bodyMaskMax > 0.0)
                {
                    float3 previewColor = bodyColor * bodyDetail.x * i.lighting * 2;
                    float3 depthTint = lerp(1.0.xxx, i.depthColor, 0.1);
                    previewColor *= depthTint;
                    return float4(previewColor, 1.0);
                }

                float  blend     = mad(sourceAlpha, 2.0, -1.0);
                float  mask      = (blend >= 0.0) ? 1.0 : 0.0;
                float  litBlend  = blend * mask;
                float  rawBlend  = mad(-blend, mask, 1.0);

                float3 litColor  = sourceColor * litBlend;
                float3 rawColor  = sourceColor * i.lighting * rawBlend;
                float3 combined  = rawColor + litColor;

                float  surfRed   = surf.x;

                float4 result;
                result.x = mad(combined.x, i.depthColor.x, surfRed * i.reflection.x);
                result.y = mad(combined.y, i.depthColor.y, surfRed * i.reflection.y);
                result.z = mad(combined.z, i.depthColor.z, surfRed * i.reflection.z);
                result.w = 1.0;

                return result;
            }

            ENDHLSL
        }
    }
}
