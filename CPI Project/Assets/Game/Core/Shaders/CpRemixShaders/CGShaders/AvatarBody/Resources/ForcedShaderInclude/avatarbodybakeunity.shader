Shader "CpRemix/Avatar Body Bake"
{
    Properties
    {
        _Diffuse ("Diffuse", 2D) = "black" {}
        _BodyColorsMaskTex ("Body Color Mask", 2D) = "black" {}
        _BodyRedChannelColor ("Body Red Channel Color", Color) = (1,0,0,1)
        _BodyGreenChannelColor ("Body Green Channel Color", Color) = (1,1,0,1)
        _BodyBlueChannelColor ("Body Blue Channel Color", Color) = (1,0,1,1)
        _DetailAndMatcapMaskAndEmissive ("r=detail g=MatCapMask b=emissive", 2D) = "black" {}
        _AtlasOffsetU ("AtlasOffset U", Float) = 0
        _AtlasOffsetV ("AtlasOffset V", Float) = 0
        _AtlasOffsetScaleU ("AtlasOffset U Scale", Float) = 1
        _AtlasOffsetScaleV ("AtlasOffset V Scale", Float) = 1
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend One One, One One

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            sampler2D _Diffuse;
            sampler2D _BodyColorsMaskTex;
            sampler2D _DetailAndMatcapMaskAndEmissive;

            float3 _BodyRedChannelColor;
            float3 _BodyGreenChannelColor;
            float3 _BodyBlueChannelColor;

            float _AtlasOffsetU;
            float _AtlasOffsetV;
            float _AtlasOffsetScaleU;
            float _AtlasOffsetScaleV;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.x = (v.uv.x - _AtlasOffsetU) / _AtlasOffsetScaleU;
                o.uv.y = (v.uv.y - _AtlasOffsetV) / _AtlasOffsetScaleV;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float4 mask   = tex2D(_BodyColorsMaskTex, uv);
                float4 detail = tex2D(_DetailAndMatcapMaskAndEmissive, uv);
                float4 diff   = tex2D(_Diffuse, uv);

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

                float detailMask  = detail.x;
                float matcapMask  = detail.y;

                float3 composite;
                composite.x = mad(diff.x, invMax, tinted.x) * detailMask;
                composite.y = mad(diff.y, invMax, tinted.y) * detailMask;
                composite.z = mad(diff.z, invMax, tinted.z) * detailMask;

                float2 centered = uv - 0.5;
                float2 doubled  = centered * 2.0;
                float  inBounds = (1.0 >= max(abs(doubled.y), abs(doubled.x))) ? 1.0 : 0.0;

                float4 result;
                result.x = composite.x * inBounds;
                result.y = composite.y * inBounds;
                result.z = composite.z * inBounds;
                result.w = mad(-matcapMask, 0.5, 0.5) * inBounds;

                return result;
            }

            ENDHLSL
        }
    }
}