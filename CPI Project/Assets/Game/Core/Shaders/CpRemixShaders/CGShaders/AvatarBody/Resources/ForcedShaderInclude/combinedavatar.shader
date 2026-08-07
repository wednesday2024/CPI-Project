Shader "CpRemix/Combined Avatar"
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
            Tags { "LightMode" = "ForwardBase" "Queue" = "Geometry+99" "RenderType" = "Opaque" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float2 uv      : TEXCOORD0;
                float3 color   : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 lighting : TEXCOORD1;
                float3 color    : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                o.color = v.color;

                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                float3 toLightDir = _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
                float3 lightDir   = normalize(toLightDir);

                float NdotL = max(dot(worldNormal, lightDir), 0.0);

                float3 ambient = glstate_lightmodel_ambient.xyz * 0.9;
                float3 diffuse = NdotL * _LightColor0.xyz;

                float wrap = (NdotL + 0.5) * 0.6;

                o.lighting.x = max(wrap, mad(diffuse.x, 0.75, ambient.x));
                o.lighting.y = max(wrap, mad(diffuse.y, 0.75, ambient.y));
                o.lighting.z = max(wrap, mad(diffuse.z, 0.75, ambient.z));

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);

                float  alpha    = tex.w;
                float  blend    = mad(alpha, 2.0, -1.0);
                float  mask     = (blend >= 0.0) ? 1.0 : 0.0;
                float  litBlend = blend * mask;
                float  rawBlend = mad(-blend, mask, 1.0);

                float3 litColor = tex.xyz * litBlend;
                float3 rawColor = tex.xyz * i.lighting * rawBlend;

                float4 result;
                result.x = rawColor.x + litColor.x;
                result.y = rawColor.y + litColor.y;
                result.z = rawColor.z + litColor.z;
                result.w = 1.0;

                return result;
            }

            ENDHLSL
        }
    }
}