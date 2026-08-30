Shader "Custom/2D/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness (pixels)", Range(0, 12)) = 1
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.05

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        // SpriteRenderer masking support.
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
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
        ZWrite Off
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SpriteOutline"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineThickness;
                half _AlphaCutoff;
            CBUFFER_END

            // SpriteRenderer supplies these values per renderer.
            half4 _RendererColor;
            float4 _Flip;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                input.positionOS.xy *= _Flip.xy;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            half SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                sprite *= input.color;

                float2 stepUV = _MainTex_TexelSize.xy * _OutlineThickness;

                // Eight directions produce a smooth, symmetric outline.
                half nearbyAlpha = 0.0h;
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2( stepUV.x, 0.0)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2(-stepUV.x, 0.0)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2(0.0,  stepUV.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2(0.0, -stepUV.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2( stepUV.x,  stepUV.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2(-stepUV.x,  stepUV.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2( stepUV.x, -stepUV.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(input.uv + float2(-stepUV.x, -stepUV.y)));

                half sourceAlpha = SampleAlpha(input.uv);
                half inside = smoothstep(_AlphaCutoff, _AlphaCutoff + 0.01h, sourceAlpha);
                half outlineAlpha = saturate(nearbyAlpha - inside) * _OutlineColor.a * input.color.a;

                // Premultiplied output matches Unity's sprite blending convention.
                half4 outline = half4(_OutlineColor.rgb * outlineAlpha, outlineAlpha);
                half4 spritePremultiplied = half4(sprite.rgb * sprite.a, sprite.a);
                return lerp(outline, spritePremultiplied, inside);
            }
            ENDHLSL
        }

        // Allows the material to remain visible if a camera uses the standard
        // Universal forward pass instead of a 2D Renderer.
        Pass
        {
            Name "SpriteOutlineForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half4 _OutlineColor; float _OutlineThickness; half _AlphaCutoff;
            CBUFFER_END
            half4 _RendererColor; float4 _Flip;
            Varyings Vert(Attributes i) { Varyings o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); i.positionOS.xy *= _Flip.xy; o.positionCS = TransformObjectToHClip(i.positionOS); o.uv = i.uv; o.color = i.color * _Color * _RendererColor; return o; }
            half A(float2 uv) { return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a; }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float2 d = _MainTex_TexelSize.xy * _OutlineThickness;
                half n = 0;
                n=max(n,A(i.uv+float2(d.x,0))); n=max(n,A(i.uv-float2(d.x,0)));
                n=max(n,A(i.uv+float2(0,d.y))); n=max(n,A(i.uv-float2(0,d.y)));
                n=max(n,A(i.uv+float2(d.x,d.y))); n=max(n,A(i.uv+float2(-d.x,d.y)));
                n=max(n,A(i.uv+float2(d.x,-d.y))); n=max(n,A(i.uv-float2(d.x,d.y)));
                half sourceAlpha=A(i.uv);
                half inside=smoothstep(_AlphaCutoff,_AlphaCutoff+0.01h,sourceAlpha);
                half oa=saturate(n-inside)*_OutlineColor.a*i.color.a;
                return lerp(half4(_OutlineColor.rgb*oa,oa),half4(s.rgb*s.a,s.a),inside);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
