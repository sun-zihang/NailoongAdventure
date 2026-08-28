Shader "Nailoong/VertexLit"
{
    // 卡通渲染：顶点色 + 半兰伯特阶梯 + 边缘光 + 受击闪白
    Properties
    {
        _RimColor ("边缘光颜色", Color) = (1, 0.92, 0.6, 1)
        _RimPower ("边缘光强度", Range(0.2, 6)) = 2.2
        _RimStrength ("边缘光亮度", Range(0, 2)) = 0.55
        _ShadowTint ("暗部色调", Color) = (0.62, 0.58, 0.72, 1)
        _Steps ("明暗阶梯", Range(1, 6)) = 3
        _Glossiness ("高光强度", Range(0, 1)) = 0.25
        _Specular ("高光颜色", Color) = (1, 1, 1, 1)
        _HitFlash ("受击闪白", Range(0, 1)) = 0
        _HitColor ("闪白颜色", Color) = (1, 0.35, 0.35, 1)
        _Emissive ("自发光", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf NailoongToon fullforwardshadows addshadow
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
            float3 viewDir;
        };

        float4 _RimColor;
        float _RimPower;
        float _RimStrength;
        float4 _ShadowTint;
        float _Steps;
        float _Glossiness;
        float4 _Specular;
        float _HitFlash;
        float4 _HitColor;
        float _Emissive;

        half4 LightingNailoongToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half ndl = dot(normalize(s.Normal), normalize(lightDir)) * 0.5 + 0.5;
            // 阶梯化半兰伯特，制造卡通分层
            half stepped = floor(ndl * _Steps) / max(_Steps - 0.001, 1.0);
            stepped = saturate(lerp(ndl, stepped, 0.65));

            half3 shadow = s.Albedo * _ShadowTint.rgb;
            half3 lit = lerp(shadow, s.Albedo, stepped);

            half3 h = normalize(lightDir + viewDir);
            half spec = pow(saturate(dot(s.Normal, h)), 32.0) * _Glossiness;

            half4 c;
            c.rgb = lit * atten + _Specular.rgb * spec * atten + s.Albedo * _Emissive;
            c.a = s.Alpha;
            return c;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            half3 base = IN.color.rgb;
            base = lerp(base, _HitColor.rgb, _HitFlash);
            o.Albedo = base;
            o.Alpha = 1;

            half rim = 1.0 - saturate(dot(normalize(o.Normal), normalize(IN.viewDir)));
            o.Emission = _RimColor.rgb * pow(rim, _RimPower) * _RimStrength + base * _Emissive * 0.35;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
