Shader "UI/MenuBackgroundMotionBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurStrength ("Vertical Blur", Range(0,0.03)) = 0
        _HeatIntensity ("Heat Intensity", Range(0,1)) = 0
        _HeatDistortion ("Heat Distortion", Range(0,0.02)) = 0.003
        _HeatSpeed ("Heat Speed", Range(0,4)) = 0.65
        _HeatWarmth ("Heat Warmth", Range(0,1)) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _BlurStrength;
            float _HeatIntensity, _HeatDistortion, _HeatSpeed, _HeatWarmth;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _HeatSpeed;
                float edge = smoothstep(0.0, 0.04, uv.x) * smoothstep(0.0, 0.04, 1.0 - uv.x)
                    * smoothstep(0.0, 0.04, uv.y) * smoothstep(0.0, 0.04, 1.0 - uv.y);
                float phase = uv.y * 35.0 - time * 3.0;
                float ripple = sin(phase + sin(uv.x * 13.0 + time) * 1.4)
                    + 0.35 * sin(phase * 1.73 - uv.x * 9.0 + time * 0.7);
                float amount = _HeatDistortion * _HeatIntensity * edge * lerp(1.0, (1.0-uv.y)*(1.0-uv.y), 0.65);
                uv += float2(ripple, 0.18 * sin(uv.x * 19.0 + phase * 0.6)) * amount;
                // The steady background needs only one sample; blur is transition-only.
                if (_BlurStrength < 0.00001)
                {
                    fixed4 sharp = tex2D(_MainTex, saturate(uv));
                    sharp.rgb *= lerp(float3(1,1,1), float3(1.08,0.96,0.82), _HeatWarmth * _HeatIntensity);
                    return sharp * i.color;
                }
                fixed4 result = fixed4(0,0,0,0);
                [unroll] for (int tap = -4; tap <= 4; tap++)
                {
                    float weight = 5.0 - abs(tap);
                    result += tex2D(_MainTex, saturate(uv + float2(0, tap * _BlurStrength / 4.0))) * weight;
                }
                result /= 25.0;
                result.rgb *= lerp(float3(1,1,1), float3(1.08,0.96,0.82), _HeatWarmth * _HeatIntensity);
                return result * i.color;
            }
            ENDCG
        }
    }
}
