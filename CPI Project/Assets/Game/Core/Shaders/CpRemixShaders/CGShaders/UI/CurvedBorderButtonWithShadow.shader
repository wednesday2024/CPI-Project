Shader "CpRemix/UI/CurvedBorderButtonWithShadow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "black" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Centre("Centre Color", Color) = (0,0.372,0.792,1)
        _Border("Border Color", Color) = (1,1,1,1)
        _BorderSize("Border Size ", float) = 0.15
        _AAliasSize("Anti-Aliasing Size", float) = 0.03
        _Roundness("Roundness", float) = 1
        _ShadowVec("Shadow Vector", Vector) = (-0.05,0.15,0,0)
        _ScaleBox("Scale For Shadow", float) = 1.2
        _StencilComp("Stencil Comparison", float) = 8
        _Stencil("Stencil ID", float) = 0
        _StencilOp("Stencil Operation", float) = 0
        _StencilWriteMask("Stencil Write Mask", float) = 255
        _StencilReadMask("Stencil Read Mask", float) = 255
        _ColorMask("Color Mask", float) = 15
    }
    SubShader
    {
        Tags
        {
            "PreviewType" = "Plane"
            "QUEUE" = "Transparent"
        }
        Pass
        {
            Tags
            {
                "PreviewType" = "Plane"
                "QUEUE" = "Transparent"
            }
            ZWrite Off
            Cull Off
            Stencil
            {
                Ref[_Stencil]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
                Pass[_StencilOp]
                Comp[_StencilComp]
                Fail Keep
                ZFail Keep
                PassFront Keep
                FailFront Keep
                ZFailFront Keep
                PassBack Keep
                FailBack Keep
                ZFailBack Keep
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask[_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Color;
            float4 _Centre;
            float4 _Border;
            float _AAliasSize;
            float _BorderSize;
            float _Roundness;
            float2 _ShadowVec;
            float _ScaleBox;
            sampler2D _MainTex;

            struct v2f
            {
                float4 xlv_COLOR : COLOR;
                float2 xlv_TEXCOORD0 : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            struct fragOutput
            {
                float4 gl_FragData : SV_Target;
            };

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);  // Avoid extra matrix multiplications
                o.xlv_COLOR = v.color * _Color;          // Simplified color calculation
                o.xlv_TEXCOORD0 = v.texcoord.xy;
                return o;
            }

            fragOutput frag(v2f i)
            {
                fragOutput o;
                float2 texCoord = i.xlv_TEXCOORD0 * 2.0 - 1.0;
                texCoord *= _ScaleBox;
                float2 powTexCoord = pow(abs(texCoord), _Roundness);
                float2 powShadowTexCoord = pow(abs(texCoord + _ShadowVec), _Roundness);

                float borderSizeRounded = pow(1.0 - _BorderSize, _Roundness);
                float aliasSizeInv = 1.0 / max(_AAliasSize, 0.0001);
                float aliasSize = 1.0 - _AAliasSize;

                float distToCenter = sqrt(dot(powTexCoord, powTexCoord));
                float distToShadowCenter = sqrt(dot(powShadowTexCoord, powShadowTexCoord));

                float centerFactor = 1.0 - ((clamp(distToCenter, aliasSize, 1.0) - aliasSize) * aliasSizeInv);
                float borderFactor = 1.0 - ((clamp(distToCenter, borderSizeRounded - _AAliasSize, borderSizeRounded) - (borderSizeRounded - _AAliasSize)) * aliasSizeInv);
                float shadowBorderFactor = 1.0 - ((clamp(distToShadowCenter, borderSizeRounded - _AAliasSize, borderSizeRounded) - (borderSizeRounded - _AAliasSize)) * aliasSizeInv);
                float shadowCenterFactor = 1.0 - ((clamp(distToShadowCenter, aliasSize, 1.0) - aliasSize) * aliasSizeInv);

                float borderBlend = min(centerFactor * 1000.0, 1.0);
                float shadowBlend = (shadowBorderFactor < 0.9) ? (0.8 + (shadowBorderFactor * 0.2)) : 1.0;
                float shadowAlpha = (shadowCenterFactor > 0.5) ? (shadowCenterFactor * 0.2) : 0.0;

                float4 texColor = tex2D(_MainTex, i.xlv_TEXCOORD0);
                float4 borderColor = _Border * (1.0 - borderFactor);
                float4 centerColor = _Centre * borderFactor * (1.0 - texColor.w) + texColor * texColor.w * borderFactor;

                float4 finalColor = (centerColor * shadowBlend + borderColor * borderBlend) * i.xlv_COLOR;
                finalColor.w = max(centerFactor, shadowAlpha);

                o.gl_FragData = finalColor;
                return o;
            }

            ENDCG
        }
    }
    FallBack Off
}