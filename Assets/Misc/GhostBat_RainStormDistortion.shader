Shader "GhostBat/StormRainDistortionOverlay"
{
    Properties
    {
        [Header(Overall)]
        _EffectStrength ("Effect Strength", Range(0, 1)) = 1
        _OverlayColor ("Atmosphere Colour", Color) = (0.20, 0.25, 0.30, 1)
        _OverlayOpacity ("Atmosphere Opacity", Range(0, 0.35)) = 0.08

        [Header(Rain Distortion)]
        _DistortionStrength ("Distortion Strength", Range(0, 0.08)) = 0.012
        _DistortionScale ("Distortion Scale", Range(1, 80)) = 28
        _DistortionSpeed ("Distortion Speed", Range(0, 8)) = 1.8
        _DistortionDirection ("Distortion Direction", Vector) = (0.15, -1, 0, 0)

        [Header(Rain Streaks)]
        _RainAmount ("Rain Amount", Range(0, 1)) = 0.35
        _RainScale ("Rain Scale", Range(10, 200)) = 90
        _RainSpeed ("Rain Speed", Range(0, 15)) = 6
        _RainOpacity ("Rain Opacity", Range(0, 0.35)) = 0.08
        _RainColor ("Rain Colour", Color) = (0.75, 0.85, 0.95, 1)

        [Header(Vignette)]
        _VignetteStrength ("Vignette Strength", Range(0, 0.5)) = 0.10
        _VignetteSoftness ("Vignette Softness", Range(0.05, 1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+500"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RainStormOverlay"

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

            CBUFFER_START(UnityPerMaterial)
                float _EffectStrength;
                float4 _OverlayColor;
                float _OverlayOpacity;
                float _DistortionStrength;
                float _DistortionScale;
                float _DistortionSpeed;
                float4 _DistortionDirection;
                float _RainAmount;
                float _RainScale;
                float _RainSpeed;
                float _RainOpacity;
                float4 _RainColor;
                float _VignetteStrength;
                float _VignetteSoftness;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float strength = saturate(_EffectStrength);
                float time = _Time.y;

                // Animated refraction-style wobble.
                float2 noiseUV =
                    input.uv * _DistortionScale +
                    _DistortionDirection.xy * time * _DistortionSpeed;

                float n1 = Noise(noiseUV);
                float n2 = Noise(noiseUV * 1.73 + float2(7.2, 3.1));

                float2 distortion =
                    (float2(n1, n2) - 0.5) *
                    _DistortionStrength *
                    strength;

                // We cannot sample the camera colour in a plain 2D overlay pass,
                // so use the distortion to warp the atmospheric/rain pattern itself.
                float2 warpedUV = input.uv + distortion;

                // Thin diagonal rain streak pattern.
                float2 rainUV = warpedUV;
                rainUV.x += rainUV.y * 0.18;
                rainUV.y += time * _RainSpeed * 0.08;

                float2 rainCells = rainUV * float2(_RainScale * 0.45, _RainScale);
                float2 cell = floor(rainCells);
                float2 local = frac(rainCells);

                float random = Hash21(cell);
                float streak =
                    step(1.0 - _RainAmount * 0.18, random) *
                    smoothstep(0.10, 0.0, abs(local.x - 0.5)) *
                    smoothstep(1.0, 0.25, local.y);

                float vignetteDistance =
                    length((input.uv - 0.5) * float2(1.25, 1.0));

                float vignette =
                    smoothstep(
                        0.5,
                        0.5 + _VignetteSoftness,
                        vignetteDistance
                    );

                float baseAlpha =
                    _OverlayOpacity * strength;

                float rainAlpha =
                    streak *
                    _RainOpacity *
                    strength;

                float vignetteAlpha =
                    vignette *
                    _VignetteStrength *
                    strength;

                float totalAlpha =
                    saturate(baseAlpha + rainAlpha + vignetteAlpha);

                float3 colour =
                    lerp(
                        _OverlayColor.rgb,
                        _RainColor.rgb,
                        saturate(streak * 0.65)
                    );

                return half4(colour, totalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
