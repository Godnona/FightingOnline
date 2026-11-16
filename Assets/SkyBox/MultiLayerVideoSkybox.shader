Shader "Custom/MultiLayerVideoSkybox"
{
    Properties
    {
        _LayerCount ("Number of Layers", Range(1,8)) = 1

        // Video textures
        _VideoLayer1 ("Video Layer 1", 2D) = "white" {}
        _VideoLayer2 ("Video Layer 2", 2D) = "white" {}
        _VideoLayer3 ("Video Layer 3", 2D) = "white" {}
        _VideoLayer4 ("Video Layer 4", 2D) = "white" {}
        _VideoLayer5 ("Video Layer 5", 2D) = "white" {}
        _VideoLayer6 ("Video Layer 6", 2D) = "white" {}
        _VideoLayer7 ("Video Layer 7", 2D) = "white" {}
        _VideoLayer8 ("Video Layer 8", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _VideoLayer1;
            sampler2D _VideoLayer2;
            sampler2D _VideoLayer3;
            sampler2D _VideoLayer4;
            sampler2D _VideoLayer5;
            sampler2D _VideoLayer6;
            sampler2D _VideoLayer7;
            sampler2D _VideoLayer8;
            float _LayerCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_VideoLayer1, i.uv); // first layer

                if (_LayerCount >= 2)
                    color = lerp(color, tex2D(_VideoLayer2, i.uv), tex2D(_VideoLayer2, i.uv).a);

                if (_LayerCount >= 3)
                    color = lerp(color, tex2D(_VideoLayer3, i.uv), tex2D(_VideoLayer3, i.uv).a);

                if (_LayerCount >= 4)
                    color = lerp(color, tex2D(_VideoLayer4, i.uv), tex2D(_VideoLayer4, i.uv).a);

                if (_LayerCount >= 5)
                    color = lerp(color, tex2D(_VideoLayer5, i.uv), tex2D(_VideoLayer5, i.uv).a);

                if (_LayerCount >= 6)
                    color = lerp(color, tex2D(_VideoLayer6, i.uv), tex2D(_VideoLayer6, i.uv).a);

                if (_LayerCount >= 7)
                    color = lerp(color, tex2D(_VideoLayer7, i.uv), tex2D(_VideoLayer7, i.uv).a);

                if (_LayerCount >= 8)
                    color = lerp(color, tex2D(_VideoLayer8, i.uv), tex2D(_VideoLayer8, i.uv).a);

                return color;
            }
            ENDCG
        }
    }
}