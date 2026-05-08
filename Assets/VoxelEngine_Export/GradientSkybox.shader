Shader "Custom/GradientSkybox"
{
    Properties
    {
        _TopColor ("Цвет верха", Color) = (0.1, 0.3, 0.8, 1)
        _BottomColor ("Цвет низа", Color) = (0.5, 0.8, 1, 1)
        _Offset ("Смещение горизонта", Range(-1, 1)) = 0.0
        _Smoothness ("Плавность перехода", Range(0.1, 3)) = 1.0
        
        _SunColor ("Цвет Солнца", Color) = (1, 0.9, 0.5, 1)
        _SunSize ("Размер Солнца", Range(0.001, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 texcoord : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; };

            float4 _TopColor; float4 _BottomColor;
            float _Offset; float _Smoothness;
            float4 _SunColor; float _SunSize;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos);
                
                // 1. Рисуем градиент неба
                float t = (dir.y + _Offset) * _Smoothness;
                t = saturate(t * 0.5 + 0.5);
                float4 skyColor = lerp(_BottomColor, _TopColor, t);

                // 2. РИСУЕМ КВАДРАТНОЕ СОЛНЦЕ
                // Получаем направление глобального источника света (Солнца)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                
                // Математика для создания квадрата, который всегда смотрит на камеру
                float3 right = normalize(cross(lightDir, float3(0, 1, 0) + float3(0.001, 0, 0)));
                float3 up = cross(right, lightDir);
                
                float x = dot(dir, right);
                float y = dot(dir, up);
                
                // Если мы смотрим прямо на солнце - выводим его размер (квадрат)
                float isSun = step(max(abs(x), abs(y)), _SunSize);
                
                // Чтобы солнце не рисовалось под землей ночью
                isSun *= step(0.0, dot(dir, lightDir));

                // Смешиваем небо и солнце
                return lerp(skyColor, _SunColor, isSun);
            }
            ENDCG
        }
    }
}