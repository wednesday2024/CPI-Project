Shader "CpRemix/Combined Avatar Depth"
{
    Properties
    {
        _MainTex ("Diffuse Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            sampler2D _DetailAndMatcapMaskAndEmissive;

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
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                float velX = _SurfaceVelocityX * _Time.x;
                float velZ = _SurfaceVelocityZ * _Time.x;
                o.uvSurface.x = mad(worldPos.x, _DynSurfaceTexTile, -velX);
                o.uvSurface.y = mad(worldPos.z, _DynSurfaceTexTile, -velZ);

                o.uvMain = v.uv;

                float3 toLightDir  = _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
                float3 lightDir    = normalize(toLightDir);
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                float NdotL = max(dot(worldNormal, lightDir), 0.0);

                float3 ambient = glstate_lightmodel_ambient.xyz * 0.9;
                float3 diffuse = NdotL * _LightColor0.xyz;

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
                float4 main   = tex2D(_MainTex,                      i.uvMain);
                float4 surf   = tex2D(_DetailAndMatcapMaskAndEmissive, i.uvSurface);

                float  blend     = mad(main.w, 2.0, -1.0);
                float  mask      = (blend >= 0.0) ? 1.0 : 0.0;
                float  litBlend  = blend * mask;
                float  rawBlend  = mad(-blend, mask, 1.0);

                float3 litColor  = main.xyz * litBlend;
                float3 rawColor  = main.xyz * i.lighting * rawBlend;
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