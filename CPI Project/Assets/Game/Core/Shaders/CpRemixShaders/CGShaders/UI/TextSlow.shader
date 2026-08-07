Shader "CpRemix/UI/TextSlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [KeywordEnum(Off,Sine)] _OscMode ("Oscillation Mode", Float) = 0
        _Oscillation ("X Speed, X Amp, Y Speed, Y Amp", Vector) = (1,0.01,1,0.01)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "DEFAULT"

            HLSLPROGRAM

            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _OSCMODE_OFF _OSCMODE_SINE

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float4 _TextureSampleAdd;
            float4 _Oscillation;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 clipPos = UnityObjectToClipPos(v.vertex);

                #if defined(_OSCMODE_SINE)
                clipPos.x += sin(_Time.y * _Oscillation.x) * _Oscillation.y;
                clipPos.y += sin(_Time.y * _Oscillation.z) * _Oscillation.w;
                #endif

                o.vertex = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texcol = tex2D(_MainTex, i.uv);
                texcol += _TextureSampleAdd;

                return texcol * i.color;
            }

            ENDHLSL
        }
    }

    FallBack "UI/Default"
}