// Screen-space vertical sky gradient in three stops (top, middle, bottom): the flat, static
// backdrop kept behind the tower at every height. Screen-space rather than view-direction based
// so the gradient sits at the same place on every portrait aspect.
Shader "Game/GradientSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.067, 0.235, 0.741, 1)
        _MidColor ("Middle", Color) = (0.157, 0.471, 0.722, 1)
        _BottomColor ("Bottom", Color) = (0.114, 0.447, 0.729, 1)
        _MidHeight ("Middle height (0 = bottom, 1 = top)", Range(0.05, 0.95)) = 0.42
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MidColor;
                half4 _BottomColor;
                half _MidHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float y = GetNormalizedScreenSpaceUV(input.positionCS).y;
                half3 lower = lerp(_BottomColor.rgb, _MidColor.rgb, smoothstep(0.0, _MidHeight, y));
                half3 color = lerp(lower, _TopColor.rgb, smoothstep(_MidHeight, 1.0, y));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
