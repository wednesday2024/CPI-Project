Shader "CpRemix/GPU Combined Avatar Alpha"
{
    Properties
    {
        _MainTex ("Diffuse Texture", 2D) = "white" {}
        _Alpha ("Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float _Alpha;

            float4 bonepos[48];
            float4 bonequat[48];

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                float3 color  : COLOR;
                float4 tangent: TANGENT;

                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };

            struct v2f
            {
                float2 uv          : TEXCOORD0;
                float3 ambientLit  : TEXCOORD1;
                float3 color       : COLOR;
                float4 vertex      : SV_POSITION;

                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };

            float3 RotateByQuat(float3 v, float4 q)
            {
                float3 t = 2.0 * cross(q.xyz, v);
                return v + q.w * t + cross(q.xyz, t);
            }

            v2f vert(appdata v)
            {
                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_SETUP_INSTANCE_ID(v);
                #endif
                v2f o;
                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                #endif

                float4 bw = frac(v.tangent);
                uint4 bi = uint4(int4(v.tangent));

                float3 worldPos = float3(0,0,0);
                float3 worldNormal = float3(0,0,0);

                {
                    float3 p = RotateByQuat(v.vertex.xyz, bonequat[bi.x]) + bonepos[bi.x].xyz;
                    worldPos += p * bw.x;
                    float3 n = RotateByQuat(v.normal, bonequat[bi.x]);
                    worldNormal += n * bw.x;
                }
                {
                    float3 p = RotateByQuat(v.vertex.xyz, bonequat[bi.y]) + bonepos[bi.y].xyz;
                    worldPos += p * bw.y;
                }
                {
                    float3 p = RotateByQuat(v.vertex.xyz, bonequat[bi.z]) + bonepos[bi.z].xyz;
                    worldPos += p * bw.z;
                }
                {
                    float3 p = RotateByQuat(v.vertex.xyz, bonequat[bi.w]) + bonepos[bi.w].xyz;
                    worldPos += p * bw.w;
                }

                o.vertex = mul(unity_MatrixVP, float4(worldPos, 1.0));
                o.uv = v.uv;

                float3 normalWS = normalize(worldNormal);
                float nDotL = max(dot(normalWS, _WorldSpaceLightPos0.xyz), 0.0);
                float3 diffuse = nDotL * _LightColor0.rgb;

                float rim = nDotL + 0.5;
                float rimClamped = rim * 0.6;

                float3 ambient = glstate_lightmodel_ambient.rgb * 0.9;
                float3 lit = mad(diffuse, 0.75, ambient);

                o.ambientLit = max(float3(rimClamped, rimClamped, rimClamped), lit);

                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                float alphaSigned = tex.a * 2.0 - 1.0;
                float posPart = max(alphaSigned, 0.0);
                float negPart = 1.0 - posPart;

                fixed3 result = tex.rgb * i.ambientLit * negPart + tex.rgb * posPart;

                return fixed4(result, _Alpha);
            }
            ENDCG
        }
    }
}