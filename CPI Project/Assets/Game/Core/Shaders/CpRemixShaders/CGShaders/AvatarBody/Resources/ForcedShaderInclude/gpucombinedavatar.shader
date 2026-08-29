Shader "CpRemix/GPU Combined Avatar"
{
    Properties
    {
        _MainTex ("Diffuse Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Geometry+99" "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 bonepos[48];
            float4 bonequat[48];

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float3 color : COLOR;
                float4 boneData : TANGENT;

                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 lighting : TEXCOORD1;
                float3 color : COLOR;

                #ifdef UNITY_INSTANCING_ENABLED
                UNITY_VERTEX_INPUT_INSTANCE_ID
                #endif
            };

            float3 RotateVectorByQuaternion(float3 v, float4 q)
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

                int4 boneIndicesSigned = (int4)v.boneData;
                uint4 indices = (uint4)boneIndicesSigned;
                float4 weights = frac(v.boneData) * 2.0;

                float3 skinnedPos =
                    weights.x * (RotateVectorByQuaternion(v.vertex.xyz, bonequat[indices.x]) + bonepos[indices.x].xyz) +
                    weights.y * (RotateVectorByQuaternion(v.vertex.xyz, bonequat[indices.y]) + bonepos[indices.y].xyz) +
                    weights.z * (RotateVectorByQuaternion(v.vertex.xyz, bonequat[indices.z]) + bonepos[indices.z].xyz) +
                    weights.w * (RotateVectorByQuaternion(v.vertex.xyz, bonequat[indices.w]) + bonepos[indices.w].xyz);

                o.pos = mul(UNITY_MATRIX_VP, float4(skinnedPos, 1.0));
                o.uv = v.uv;

                float3 worldNormal = normalize(RotateVectorByQuaternion(v.normal, bonequat[indices.x]));
                float3 lightVec = _WorldSpaceLightPos0.xyz - skinnedPos * _WorldSpaceLightPos0.w;
                float3 lightDir = normalize(lightVec);

                float ndotl = max(dot(worldNormal, lightDir), 0.0);
                float3 diffuse = ndotl * _LightColor0.rgb;
                float3 litFloor = (ndotl + 0.5) * 0.6;
                float3 ambient = glstate_lightmodel_ambient.rgb * 0.9;

                o.lighting = max(litFloor, diffuse * 0.75 + ambient);
                o.color = v.color;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = _MainTex.Sample(sampler_MainTex, i.uv);
                float alphaSigned = tex.a * 2.0 - 1.0;
                float selfLit = saturate(alphaSigned);
                float3 color = tex.rgb * (i.lighting * (1.0 - selfLit) + selfLit);
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}