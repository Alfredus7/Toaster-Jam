Shader "UI/RetroGlitchedScreen"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _HoloColor ("Screen Tint", Color) = (0.2, 1, 0.3, 1)
        _ScanSpeed ("Scan Speed", Range(0,10)) = 2 
        _ScanDensity ("Scan Density", Range(10,1000)) = 300
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 2
        _GlitchLevel ("Glitch Level (0-5)", Range(0,5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RetroGlitch"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _HoloColor;
            float _GlitchLevel;
            float _ScanSpeed;
            float _ScanDensity;
            float _GlowIntensity;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float fast_rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _HoloColor;
                return o;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y * 3.0;

                // --- Escala de glitch por nivel ---
                float glitchStrength = saturate(_GlitchLevel / 5.0);
                float blockIntensity = pow(glitchStrength, 1.3);
                float noiseIntensity = glitchStrength * 0.05;
                float flickerAmp     = glitchStrength * 0.05;
                float waveAmp        = glitchStrength * 0.004;

                // --- Curva CRT leve ---
                float2 centered = uv - 0.5;
                uv += centered * dot(centered, centered) * 0.15 * glitchStrength;

                // --- Distorsión horizontal (ondas CRT) ---
                uv.x += sin(uv.y * 100.0 + _Time.y * 5.0) * waveAmp;

                // --- Glitch tipo bloque ---
                float blockSize = lerp(0.25, 0.05, glitchStrength);
                float2 blockUV = floor(uv / blockSize) * blockSize;
                float blockNoise = fast_rand(blockUV + t);
                uv.x += (blockNoise - 0.5) * 0.1 * blockIntensity * step(0.7, blockNoise);

                // --- Ruido estático fino ---
                float staticNoise = fast_rand(uv * 500.0 + _Time.yy);
                uv.xy += (staticNoise - 0.5) * noiseIntensity;

                // --- Muestra textura ---
                fixed4 col = tex2D(_MainTex, uv);
                col = (col + _TextureSampleAdd) * IN.color;

                // --- Líneas de escaneo adaptadas a pantalla y landscape ---
                float2 screenUV = IN.vertex.xy / _ScreenParams.xy;
                float scanCoord = screenUV.y; // vertical scanlines
                float scan = frac(scanCoord * _ScanDensity + _Time.y * _ScanSpeed);
                scan = 1.0 - abs(scan * 2.0 - 1.0);
                scan *= scan;
                float scanMul = 0.7 + scan * 0.3;

                // --- Flicker global ---
                float flicker = 1.0 + sin(_Time.y * (3.0 + glitchStrength * 10.0)) * flickerAmp;

                // --- Aplicar intensidades ---
                col.rgb *= scanMul * _GlowIntensity * flicker;
                col.g += glitchStrength * 0.05; // brillo fósforo

                // --- Líneas de glitch ---
                float lineY = frac(t * (1.5 + glitchStrength * 3.0));
                float glitchLine = smoothstep(0.0, 0.02, abs(uv.y - lineY));
                col.rgb *= 1.0 - glitchLine * 0.4 * glitchStrength;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
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
