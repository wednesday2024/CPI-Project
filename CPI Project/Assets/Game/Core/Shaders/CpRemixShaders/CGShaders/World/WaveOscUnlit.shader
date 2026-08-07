Shader "CpRemix/World/Wave Osc Unlit (Vertex Alpha)"
{
    Properties
    {
        _MainTex  ("Base (RGB)", 2D) = "white" {}
        _OscDir   ("World Osc  Dir", Vector) = (1,0,0,1)
        _OscAxis  ("World Osc Axs (w = wave freq)", Vector) = (0,1,0,1)
        _OscSpeed ("Osc Speed", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "DisableBatching" = "True" }

        Pass
        {
            Tags { "RenderType" = "Opaque" }

            HLSLPROGRAM

            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   4.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float3    _OscDir;
            float4    _OscAxis;
            float     _OscSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                float2 uv2    : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float3 color : COLOR;
                float2 uv    : TEXCOORD0;
                float2 uv2   : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3x3 w2o3     = (float3x3)unity_WorldToObject;
                float3 oscAxisObj = mul(w2o3, _OscAxis.xyz);
                float3 oscDirObj  = mul(w2o3, _OscDir);

                float phase     = dot(v.vertex.xyz, oscAxisObj) * _OscAxis.w;
                float amplitude = (1.0 - v.color.a) * sin(_Time.y * _OscSpeed + phase);

                float4 displaced = v.vertex;
                displaced.xyz += amplitude * oscDirObj;

                o.pos   = UnityObjectToClipPos(displaced);
                o.color = v.color.rgb;
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2   = v.uv2 * unity_LightmapST.xy + unity_LightmapST.zw;

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 lm       = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2);
                float3 lightmap = lm.rgb * (lm.a * unity_Lightmap_HDR.x);

                float4 mainTex = tex2D(_MainTex, i.uv);
                float3 col     = lightmap * mainTex.rgb * i.color;

                float4 result = float4(col, 1.0);

                UNITY_APPLY_FOG(i.fogCoord, result);

                return result;
            }

            ENDHLSL
        }
    }
}