Shader "UI/SelectiveGlow_URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 0
        _Threshold ("Glow Threshold", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
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

            float4 _GlowColor;
            float _GlowIntensity;
            float _Threshold;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Calculate brightness
                float brightness = dot(tex.rgb, float3(0.299, 0.587, 0.114));

                // Only glow if brightness is above threshold
                float glowMask = step(_Threshold, brightness);

                float3 glow = _GlowColor.rgb * _GlowIntensity * glowMask;

                return half4((tex.rgb * _GlowColor.rgb) + glow, tex.a);
            }

            ENDHLSL
        }
    }
}