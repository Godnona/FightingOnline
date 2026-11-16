Shader "TMP/URP/HoverGlow"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 0
        _GlowSize ("Glow Size", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _FaceColor;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float sdf = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                // Sharp text edge
                float edge = smoothstep(0.5 - 0.01, 0.5 + 0.01, sdf);

                float3 face = _FaceColor.rgb * edge;

                // Outer glow area
                float glow = smoothstep(0.5 - _GlowSize, 0.5, sdf);
                float3 emission = _GlowColor.rgb * glow * _GlowIntensity;

                float alpha = edge;

                return half4(face + emission, alpha);
            }

            ENDHLSL
        }
    }
}