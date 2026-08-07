Shader "CpRemix/World/WorldObject"
{
    Properties
    {
        _Diffuse ("Diffuse Texture", 2D) = "" {}
        [HideInInspector] _MainTex ("Main Tex", 2D) = "" {}
        [HideInInspector] _BlobShadowTex ("Blob Shadow Tex", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float _ShadowPlaneDim;
            float _ShadowTextureDim;
            float3 _ShadowPlaneWorldPos;

            sampler2D _Diffuse;
            sampler2D _MainTex;
            sampler2D _BlobShadowTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
                float2 uv0    : TEXCOORD0;
                float2 uv1    : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                float2 lightmapUV  : TEXCOORD1;
                float3 shadowData  : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);

                o.color = v.color;
                o.uv = v.uv0;
                o.lightmapUV = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;

                float halfDim = _ShadowPlaneDim * 0.5;
                float aspectOfs = 1.0 / _ShadowTextureDim;

                float offsetX = worldPos.x - _ShadowPlaneWorldPos.x;
                float offsetZ = worldPos.z - _ShadowPlaneWorldPos.z;

                o.shadowData.x = (aspectOfs + offsetX / halfDim + 1.0) * 0.5;
                o.shadowData.y = (aspectOfs + offsetZ / halfDim + 1.0) * 0.5;
                o.shadowData.z = worldPos.y;

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 shadowSample = tex2D(_BlobShadowTex, i.shadowData.xy);

                float shadowDepth = shadowSample.y;
                float shadowIntensity = shadowSample.x;

                float isAbove = (i.shadowData.z >= shadowDepth) ? 2.0 : 1.0;
                float depthDiff = shadowDepth - i.shadowData.z;

                float shadowFactor = mad(abs(depthDiff), isAbove, isAbove) - 0.5;
                float shadowMult = min(shadowIntensity * max(shadowFactor, 1.0), 1.0);

                float4 lm = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUV);
                float3 lightColor = lm.rgb * (lm.a * unity_Lightmap_HDR.x);

                float4 diffSample = tex2D(_Diffuse, i.uv);
                float4 mainSample = tex2D(_MainTex, i.uv);
                float4 diff = diffSample.a > 0.0 ? diffSample : mainSample;

                float3 col = diff.rgb * lightColor * i.color.rgb * shadowMult;
                float4 final = float4(col, 1.0);

                UNITY_APPLY_FOG(i.fogCoord, final);

                return final;
            }

            ENDCG
        }
    }
}