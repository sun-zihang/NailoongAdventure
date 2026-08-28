Shader "Nailoong/GradientSkybox"
{
    // 三段渐变程序化天空盒：顶部 / 地平线 / 底部 + 太阳光晕
    Properties
    {
        _TopColor ("天顶颜色", Color) = (0.35, 0.62, 0.95, 1)
        _HorizonColor ("地平线颜色", Color) = (0.92, 0.86, 0.78, 1)
        _BottomColor ("地面颜色", Color) = (0.55, 0.48, 0.45, 1)
        _SunColor ("太阳颜色", Color) = (1, 0.95, 0.7, 1)
        _SunSize ("太阳大小", Range(0.001, 0.6)) = 0.05
        _SunPower ("太阳锐度", Range(1, 64)) = 12
        _Exposure ("曝光", Range(0.2, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
                return o;
            }

            float4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            float _SunSize, _SunPower, _Exposure;

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 col;
                if (h >= 0)
                    col = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.65));
                else
                    col = lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-h), 0.5));

                float3 sunDir = normalize(_WorldSpaceLightPos0.xyz);
                float sd = saturate(dot(d, sunDir));
                col += _SunColor.rgb * pow(sd, _SunPower * 40) * 1.6;
                col += _SunColor.rgb * smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.2, sd) * 0.55;
                return float4(col * _Exposure, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
