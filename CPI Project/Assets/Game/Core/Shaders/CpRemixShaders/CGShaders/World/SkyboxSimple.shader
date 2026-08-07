Shader "CpRemix/Skybox/Simple Cubemap Shader"
{
    Properties
    {
        _cubemap("Environment Map", Cube) = "white" {}
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "QUEUE" = "Background"
        }
        Pass
        {
            Tags
            {
                "QUEUE" = "Background"
            }
            ZWrite Off
            Cull Off
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            samplerCUBE _cubemap;
            sampler2D _MainTex;
            fixed4 _Color;

            struct v2f
            {
                float4 uv : TEXCOORD0;
            };

            struct FragOutput
            {
                fixed4 color : SV_Target;
            };

            v2f vert(
                float4 vertex : POSITION,
                float4 uv : TEXCOORD0,
                out float4 outpos : SV_POSITION
            )
            {
                v2f o;
                outpos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            FragOutput frag(v2f i)
            {
                FragOutput o;

                float4 texColor = tex2D(_MainTex, i.uv.xy);
                fixed4 cubemapColor = texCUBE(_cubemap, i.uv);

                o.color = texColor * cubemapColor * _Color;

                return o;
            }

            ENDCG
        }
    }
    FallBack Off
}
