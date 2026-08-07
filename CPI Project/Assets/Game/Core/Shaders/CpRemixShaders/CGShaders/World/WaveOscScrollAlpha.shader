Shader "CpRemix/World/Wave Osc Scroll with Alpha"
{
    Properties
    {
        _TintColor ("Tint Colour", Color) = (0,0,0,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _OscDir ("World Osc  Dir", Vector) = (1,0,0,1)
        _OscAxis ("World Osc Axs (w = wave freq)", Vector) = (0,1,0,1)
        _OscSpeed ("Osc Speed", Float) = 1
        _XScrollSpeed ("X Scroll Speed", Float) = 1
        _YScrollSpeed ("Y Scroll Speed", Float) = 1
    }
    SubShader
    {
        Tags { "QUEUE" = "Transparent" "DisableBatching" = "True" }
        Pass
        {
            Tags { "QUEUE" = "Transparent" }
            Blend One One, One One
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _TintColor;
            float3    _OscDir;
            float4    _OscAxis;
            float     _OscSpeed;
            float     _XScrollSpeed;
            float     _YScrollSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1;
            };

            struct v2f
            {
                float4 color  : TEXCOORD0;
                float2 uv     : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 localOscAxis = mul((float3x3)unity_WorldToObject, _OscAxis.xyz);
                float3 localOscDir  = mul((float3x3)unity_WorldToObject, _OscDir);

                float amplitude    = 1.0 - v.color.w;
                float phase        = dot(v.vertex.xyz, localOscAxis) * _OscAxis.w
                                   + _OscSpeed * _Time.y;
                float displacement = amplitude * sin(phase);

                v.vertex.xyz += displacement * localOscDir;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color  = v.color;
                o.uv     = v.uv + float2(_XScrollSpeed, _YScrollSpeed) * _Time.x;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color.w + _TintColor;
            }

            ENDCG
        }
    }
}