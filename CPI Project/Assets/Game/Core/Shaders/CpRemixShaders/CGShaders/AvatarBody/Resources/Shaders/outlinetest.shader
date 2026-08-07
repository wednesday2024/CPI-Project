Shader "Cg shader for toon shading"
{
    Properties
    {
        _Color ("Diffuse Color", Color) = (1,1,1,1)
        _UnlitColor ("Unlit Diffuse Color", Color) = (0.5,0.5,0.5,1)
        _DiffuseThreshold ("Threshold for Diffuse Colors", Range(0,1)) = 0.1
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _LitOutlineThickness ("Lit Outline Thickness", Range(0,1)) = 0.1
        _UnlitOutlineThickness ("Unlit Outline Thickness", Range(0,1)) = 0.4
        _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _Shininess ("Shininess", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            HLSLPROGRAM

            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4 _Color;
            float4 _UnlitColor;
            float _DiffuseThreshold;
            float4 _OutlineColor;
            float _LitOutlineThickness;
            float _UnlitOutlineThickness;
            float _Shininess;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);

                float3 L;
                float attenuation;

                if (_WorldSpaceLightPos0.w == 0.0)
                {
                    L = normalize(_WorldSpaceLightPos0.xyz);
                    attenuation = 1.0;
                }
                else
                {
                    float3 lightVec = _WorldSpaceLightPos0.xyz - i.worldPos;
                    float dist = max(length(lightVec), 0.00001);
                    L = lightVec / dist;
                    attenuation = 1.0 / dist;
                }

                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                float ndotl = max(dot(N, L), 0.0) * attenuation;

                float3 baseColor =
                    (ndotl >= _DiffuseThreshold)
                    ? (_Color.rgb * _LightColor0.rgb)
                    : _UnlitColor.rgb;

                float outlineThreshold =
                    _UnlitOutlineThickness +
                    ((_LitOutlineThickness - _UnlitOutlineThickness) * ndotl);

                if (dot(V, N) < outlineThreshold)
                {
                    baseColor = _OutlineColor.rgb;
                }

                float3 R = reflect(-L, N);

                float spec =
                    pow(
                        max(dot(R, V), 0.0),
                        max(_Shininess, 0.0001)
                    ) * attenuation;

                if (spec > 0.5 && ndotl > 0.0)
                {
                    baseColor =
                        lerp(
                            baseColor,
                            _SpecColor.rgb,
                            _SpecColor.a
                        );
                }

                return float4(baseColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="ForwardAdd" }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float _Shininess;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                float3 L;

                if (_WorldSpaceLightPos0.w == 0.0)
                {
                    L = normalize(_WorldSpaceLightPos0.xyz);
                }
                else
                {
                    L = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                }

                float ndotl = max(dot(N, L), 0.0);

                float3 R = reflect(-L, N);

                float spec =
                    pow(
                        max(dot(R, V), 0.0),
                        max(_Shininess, 0.0001)
                    );

                if (spec > 0.5 && ndotl > 0.0)
                {
                    return float4(
                        _LightColor0.rgb * _SpecColor.rgb,
                        _SpecColor.a
                    );
                }

                return float4(0,0,0,0);
            }

            ENDHLSL
        }
    }

    FallBack "Specular"
}