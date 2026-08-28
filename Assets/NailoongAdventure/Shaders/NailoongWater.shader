Shader "Nailoong/StylizedWater"
{
    // 卡通水面：双层波动 + 泡沫边 + 深浅渐变
    Properties
    {
        _ShallowColor ("浅水颜色", Color) = (0.45, 0.85, 0.92, 0.75)
        _DeepColor ("深水颜色", Color) = (0.12, 0.42, 0.68, 0.9)
        _FoamColor ("泡沫颜色", Color) = (1, 1, 1, 0.9)
        _WaveSpeed ("波动速度", Range(0, 3)) = 0.8
        _WaveScale ("波动密度", Range(0.1, 8)) = 2.2
        _WaveHeight ("波动高度", Range(0, 0.6)) = 0.08
        _FoamWidth ("泡沫宽度", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha vertex:vert
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float wave;
        };

        float4 _ShallowColor, _DeepColor, _FoamColor;
        float _WaveSpeed, _WaveScale, _WaveHeight, _FoamWidth;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float t = _Time.y * _WaveSpeed;
            float2 p = v.vertex.xz * _WaveScale;
            float w = sin(p.x + t) * 0.5 + sin(p.y * 1.3 - t * 0.8) * 0.5;
            v.vertex.y += w * _WaveHeight;
            o.wave = w;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            float depthFade = saturate(IN.wave * 0.5 + 0.5);
            float3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthFade);
            float foam = smoothstep(1.0 - _FoamWidth, 1.0, IN.wave);
            col = lerp(col, _FoamColor.rgb, foam * 0.85);
            o.Albedo = col;
            o.Alpha = lerp(_DeepColor.a, _ShallowColor.a, depthFade);
            o.Emission = col * 0.15;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
