// Lifts a single cloud out of the licensed Kenney day skybox panorama for a world-space quad:
// _MainTex_ST windows onto one cloud, the red channel keys the pale-blue sky out (the clouds are
// the only near-white pixels; thresholds are in sRGB, as the texture was authored), and a radial falloff on the quad's own UVs hides the window edges.
// _Opacity keeps the broad masses faint.
Shader "Game/CloudCutout"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "white" {}
        _KeyLow ("Key low (red)", Range(0, 1)) = 0.66
        _KeyHigh ("Key high (red)", Range(0, 1)) = 0.82
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.1
        _Opacity ("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _KeyLow;
                half _KeyHigh;
                half _Brightness;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 quadUV : TEXCOORD0;
                float2 texUV : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.quadUV = input.uv;
                output.texUV = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texUV);
                half keyed = pow(saturate(tex.r), 1.0 / 2.2); // back to sRGB: the texture is linearized on sampling.
                half key = saturate((keyed - _KeyLow) / max(_KeyHigh - _KeyLow, 0.001));
                float edge = length((input.quadUV - 0.5) * 2.0);
                half falloff = saturate((1.0 - edge) / 0.3);
                return half4(saturate(tex.rgb * _Brightness), key * falloff * _Opacity);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
