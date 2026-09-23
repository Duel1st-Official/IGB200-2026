Shader "GhostBatSanctuary/HeatwaveBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Background Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Heatwave Intensity", Range(0,1)) = 0.6
        _Distortion ("Distortion Amount", Range(0,0.02)) = 0.003
        _Speed ("Rising Speed", Range(0,4)) = 0.65
        _Frequency ("Ripple Frequency", Range(1,100)) = 35
        _BottomBias ("Stronger Near Ground", Range(0,1)) = 0.65
        _Warmth ("Warm Tint", Range(0,1)) = 0.12
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Intensity, _Distortion, _Speed, _Frequency, _BottomBias, _Warmth;
            v2f vert(appdata v)
            {
                v2f o;
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color; o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _Speed;
                float heightWeight = lerp(1.0, (1.0 - uv.y) * (1.0 - uv.y), _BottomBias);
                // Stop distortion at the texture border to prevent edge streaking.
                float edge = smoothstep(0.0, 0.04, uv.x) * smoothstep(0.0, 0.04, 1.0 - uv.x)
                    * smoothstep(0.0, 0.04, uv.y) * smoothstep(0.0, 0.04, 1.0 - uv.y);
                float phase = uv.y * _Frequency - time * 3.0;
                float ripple = sin(phase + sin(uv.x * 13.0 + time) * 1.4)
                    + 0.35 * sin(phase * 1.73 - uv.x * 9.0 + time * 0.7);
                float amount = _Distortion * _Intensity * heightWeight * edge;
                float2 offset = float2(ripple, 0.18 * sin(uv.x * 19.0 + phase * 0.6)) * amount;
                float2 halfTexel = abs(_MainTex_TexelSize.xy) * 0.5;
                float2 sampleUV = clamp(uv + offset, halfTexel, 1.0 - halfTexel);
                fixed4 col = tex2D(_MainTex, sampleUV) + _TextureSampleAdd;
                col.rgb *= lerp(float3(1,1,1), float3(1.08,0.96,0.82), _Warmth * _Intensity);
                col *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
