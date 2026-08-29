Shader "CpRemix/Avatar Body Preview"
{
    Properties
    {
        _Diffuse ("Diffuse", 2D) = "black" {}
        _BodyColorsMaskTex ("Body Color Mask", 2D) = "black" {}
        _BodyRedChannelColor ("Body Red Channel Color", Color) = (1,0,0,1)
        _BodyGreenChannelColor ("Body Green Channel Color", Color) = (1,1,0,1)
        _BodyBlueChannelColor ("Body Blue Channel Color", Color) = (1,0,1,1)
        _DetailAndMatcapMaskAndEmissive ("r=detail g=MatCapMask b=emissive", 2D) = "black" {}
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "Always" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _Diffuse;
            sampler2D _BodyColorsMaskTex;

            float3 _BodyRedChannelColor;
            float3 _BodyGreenChannelColor;
            float3 _BodyBlueChannelColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 mask = tex2D(_BodyColorsMaskTex, i.uv);
                float4 diff = tex2D(_Diffuse,           i.uv);

                float maskR = mask.x;
                float maskG = mask.y;
                float maskB = mask.z;

                float3 colorFromMask;
                colorFromMask.x = mad(maskB, _BodyBlueChannelColor.x,  mad(maskR, _BodyRedChannelColor.x,  maskG * _BodyGreenChannelColor.x));
                colorFromMask.y = mad(maskB, _BodyBlueChannelColor.y,  mad(maskR, _BodyRedChannelColor.y,  maskG * _BodyGreenChannelColor.y));
                colorFromMask.z = mad(maskB, _BodyBlueChannelColor.z,  mad(maskR, _BodyRedChannelColor.z,  maskG * _BodyGreenChannelColor.z));

                float maxChannel = max(maskB, max(maskG, maskR));
                float3 tinted    = maxChannel * colorFromMask;
                float  invMax    = 1.0 - maxChannel;

                float4 result;
                result.x = mad(diff.x, invMax, tinted.x);
                result.y = mad(diff.y, invMax, tinted.y);
                result.z = mad(diff.z, invMax, tinted.z);
                result.w = 1.0;

                return result;
            }

            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend DstColor SrcColor, DstColor SrcColor

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _DetailAndMatcapMaskAndEmissive;

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
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 lighting : TEXCOORD1;
                float3 color    : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color;

                float3 worldNormal = normalize(mul(transpose((float3x3)unity_WorldToObject), v.normal));

                float3 toLightDir = _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
                float3 lightDir   = normalize(toLightDir);

                float NdotL = max(dot(worldNormal, lightDir), 0.0);

                float3 ambient = glstate_lightmodel_ambient.xyz * 0.9;
                float3 diffuse = NdotL * _LightColor0.xyz;

                o.lighting.x = mad(diffuse.x, 0.75, ambient.x);
                o.lighting.y = mad(diffuse.y, 0.75, ambient.y);
                o.lighting.z = mad(diffuse.z, 0.75, ambient.z);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 detail = tex2D(_DetailAndMatcapMaskAndEmissive, i.uv);
                float  red    = detail.x;

                float4 result;
                result.x = red * i.lighting.x * 0.47;
                result.y = red * i.lighting.y * 0.47;
                result.z = red * i.lighting.z * 0.47;
                result.w = 1.0;

                return result;
            }

            ENDHLSL
        }
    }
}