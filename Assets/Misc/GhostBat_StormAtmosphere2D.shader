Shader "GhostBat/StormAtmosphere2D"
{
    Properties
    {
        [MainTexture] _MainTex ("Screen Texture", 2D) = "white" {}

        [Header(Storm Tint)]
        _TintColor ("Storm Tint", Color) = (0.52, 0.60, 0.70, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.28
        _Darkness ("Darkness", Range(0, 0.8)) = 0.18
        _Saturation ("Saturation", Range(0, 1.5)) = 0.72

        [Header(Moving Cloud Shadow)]
        _ShadowStrength ("Shadow Strength", Range(0, 0.5)) = 0.10
        _ShadowScale ("Shadow Scale", Range(0.1, 8)) = 1.4
        _ShadowSpeedX ("Shadow Speed X", Range(-2, 2)) = 0.035
        _ShadowSpeedY ("Shadow Speed Y", Range(-2, 2)) = -0.012

        [Header(Vignette)]
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.20
        _VignetteSoftness ("Vignette Softness", Range(0.01, 1)) = 0.55

        [Header(Lightning)]
        _LightningFlash ("Lightning Flash", Range(0, 1)) = 0
        _LightningColor ("Lightning Color", Color) = (0.82, 0.90, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+100"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "StormAtmosphere"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _TintStrength;
                float _Darkness;
                float _Saturation;

                float _ShadowStrength;
                float _ShadowScale;
                float _ShadowSpeedX;
                float _ShadowSpeedY;

                float _VignetteStrength;
                float _VignetteSoftness;

                float _LightningFlash;
                float4 _LightningColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    TRANSFORM_TEX(input.uv, _MainTex);

                output.color =
                    input.color;

                return output;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f =
                    f * f *
                    (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            float CloudNoise(float2 uv)
            {
                float value = 0;
                float amplitude = 0.5;

                value +=
                    ValueNoise(uv) *
                    amplitude;

                uv *= 2.03;
                amplitude *= 0.5;

                value +=
                    ValueNoise(uv) *
                    amplitude;

                uv *= 2.01;
                amplitude *= 0.5;

                value +=
                    ValueNoise(uv) *
                    amplitude;

                return saturate(value);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 source =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                float3 colour =
                    source.rgb;

                // ---------------------------------------------
                // DESATURATE
                // ---------------------------------------------

                float luminance =
                    dot(
                        colour,
                        float3(
                            0.2126,
                            0.7152,
                            0.0722
                        )
                    );

                colour =
                    lerp(
                        luminance.xxx,
                        colour,
                        _Saturation
                    );

                // ---------------------------------------------
                // COOL STORM TINT
                // ---------------------------------------------

                colour =
                    lerp(
                        colour,
                        colour *
                        _TintColor.rgb,
                        _TintStrength
                    );

                colour *=
                    1.0 -
                    _Darkness;

                // ---------------------------------------------
                // SLOW MOVING STORM SHADOWS
                // ---------------------------------------------

                float2 shadowUV =
                    input.uv *
                    (_ShadowScale * 4.0);

                shadowUV +=
                    float2(
                        _Time.y *
                        _ShadowSpeedX,
                        _Time.y *
                        _ShadowSpeedY
                    );

                float shadowNoise =
                    CloudNoise(
                        shadowUV
                    );

                float shadow =
                    smoothstep(
                        0.35,
                        0.78,
                        shadowNoise
                    );

                colour *=
                    1.0 -
                    shadow *
                    _ShadowStrength;

                // ---------------------------------------------
                // SOFT VIGNETTE
                // ---------------------------------------------

                float2 centredUV =
                    input.uv *
                    2.0 -
                    1.0;

                float edgeDistance =
                    length(
                        centredUV
                    );

                float vignette =
                    smoothstep(
                        1.0 -
                        _VignetteSoftness,
                        1.15,
                        edgeDistance
                    );

                colour *=
                    1.0 -
                    vignette *
                    _VignetteStrength;

                // ---------------------------------------------
                // LIGHTNING FLASH
                //
                // Animate _LightningFlash from script or Animator.
                // 0 = normal
                // 1 = full flash
                // ---------------------------------------------

                colour =
                    lerp(
                        colour,
                        _LightningColor.rgb,
                        saturate(
                            _LightningFlash
                        )
                    );

                return half4(
                    saturate(colour),
                    source.a * input.color.a
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
