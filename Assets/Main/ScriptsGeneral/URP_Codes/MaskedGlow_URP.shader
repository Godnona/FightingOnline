Shader "UI/MaskedGlow_URP_Scatter"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _GlowMask ("Glow Mask", 2D) = "black" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,10)) = 0
        _Scatter ("Glow Scatter", Range(0,10)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

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

            TEXTURE2D(_GlowMask);
            SAMPLER(sampler_GlowMask);

            float4 _GlowColor;
            float _GlowIntensity;
            float _Scatter;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float SampleBlurredMask(float2 uv)
            {
                float spread = _Scatter * 0.002;

                float2 offsetX = float2(spread, 0);
                float2 offsetY = float2(0, spread);

                float sum = 0;

                sum += SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, uv).r;
                sum += SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, uv + offsetX).r;
                sum += SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, uv - offsetX).r;
                sum += SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, uv + offsetY).r;
                sum += SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, uv - offsetY).r;

                return sum / 5.0;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float mask = SampleBlurredMask(i.uv);

                float3 glow = _GlowColor.rgb * mask * _GlowIntensity;

                return half4(baseTex.rgb + glow, baseTex.a);
            }

            ENDHLSL
        }
    }
}