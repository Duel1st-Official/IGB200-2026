Shader "GhostBat/UI/QuarterArcFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0,1)) = 1
        _Saturation ("Saturation", Range(0,1)) = 1
        _StartAngle ("Start Angle", Range(0,360)) = 0
        _ArcAngle ("Arc Angle", Range(1,360)) = 90
        _Clockwise ("Clockwise", Float) = 1

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "QuarterArcFill"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Unity 6 URP 2D does not provide ShaderLibrary/UnityUI.hlsl.
            // Local equivalent of UnityGet2DClipping keeps Canvas RectMask compatibility.
            float UnityGet2DClippingLocal(float2 position, float4 clipRect)
            {
                float2 inside =
                    step(clipRect.xy, position) *
                    step(position, clipRect.zw);

                return inside.x * inside.y;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _ClipRect;
                float _FillAmount;
            float _Saturation;
                float _StartAngle;
                float _ArcAngle;
                float _Clockwise;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.worldPosition = v.positionOS;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                // UV centre is the centre of the complete circular UI sprite.
                float2 p = i.uv - float2(0.5, 0.5);
                float angle = degrees(atan2(p.y, p.x));
                if (angle < 0.0)
                    angle += 360.0;

                float start = fmod(_StartAngle + 360.0, 360.0);
                float travelled;

                if (_Clockwise >= 0.5)
                    travelled = fmod(start - angle + 360.0, 360.0);
                else
                    travelled = fmod(angle - start + 360.0, 360.0);

                float visibleAngle = saturate(_FillAmount) * max(1.0, _ArcAngle);

                // Only reveal pixels encountered while travelling along this quarter.
                clip(visibleAngle - travelled);

                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClippingLocal(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif

                return c;
            }
            ENDHLSL
        }
    }
}
