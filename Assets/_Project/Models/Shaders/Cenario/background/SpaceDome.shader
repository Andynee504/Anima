Shader "Custom/SpaceDome"
{
    Properties
    {
        // ----- Stars -----
        _StarDensity   ("Star Density", Range(0,1)) = 0.55
        _StarBrightness("Star Brightness", Range(0,2)) = 1.5
        _TwinkleAmount ("Twinkle Amount", Range(0,1)) = 0.30
        _TwinkleSpeed  ("Twinkle Speed",  Range(0,5)) = 1.2
        _StarTile      ("Star Tile (cells)", Range(128,2048)) = 1024
        _StarSize      ("Star Size", Range(0.0005, 0.01)) = 0.0025
        _StarSoftness  ("Star Softness", Range(0.0001, 0.01)) = 0.0012

        // ----- Nebula -----
        _NebulaIntensity("Nebula Intensity", Range(0,1)) = 0.20
        _NebulaScale    ("Nebula Scale",     Range(0.1,4)) = 1.2
        _NebulaContrast ("Nebula Contrast",  Range(0.5,3)) = 1.6

        // ----- Glints -----
        _GlintAmount   ("Glint Amount", Range(0,1)) = 0.12
        _GlintSpeed    ("Glint Speed",  Range(0,5)) = 1.6

        // ----- Global -----
        _Tint ("Overall Tint", Color) = (1,1,1,1)
        _StarsOn  ("Stars On",  Float) = 1
        _NebulaOn ("Nebula On", Float) = 0
        _GlintsOn ("Glints On", Float) = 0
        _Fade     ("Global Fade (0-1)", Range(0,1)) = 1

        // Culling (debug no inspector)
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 1
    }

    SubShader
    {
        Tags{ "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull [_Cull]
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                // stars
                float _StarDensity, _StarBrightness, _TwinkleAmount, _TwinkleSpeed;
                float _StarTile, _StarSize, _StarSoftness;
                // nebula
                float _NebulaIntensity, _NebulaScale, _NebulaContrast;
                // glints
                float _GlintAmount, _GlintSpeed;
                // global
                float4 _Tint;
                float _StarsOn, _NebulaOn, _GlintsOn, _Fade;
            CBUFFER_END

            // --- Helpers / noise ---
            float hash21(float2 p) {
                p = frac(p*float2(123.34, 345.45));
                p += dot(p, p+34.345);
                return frac(p.x*p.y);
            }
            float2 hash22(float2 p){
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(float2(p.x*p.y + p.x, p.x + p.y));
            }
            float hash31(float3 p) {
                p = frac(p*float3(127.1, 311.7, 74.7));
                p += dot(p, p.yzx+19.19);
                return frac(p.x*p.y*p.z);
            }
            float noise3(float3 p){
                float3 i = floor(p);
                float3 f = frac(p);
                float n = 0.0;
                [unroll] for(int x=0;x<2;x++)
                [unroll] for(int y=0;y<2;y++)
                [unroll] for(int z=0;z<2;z++){
                    float3 g = float3(x,y,z);
                    float3 o = i+g;
                    float w = hash31(o);
                    float3 u = f - g;
                    float3 k = 1.0 - abs(u);
                    n += w * (k.x*k.y*k.z);
                }
                return saturate(n);
            }
            float fbm3(float3 p){
                float a = 0.5, s = 0.0;
                [unroll] for(int i=0;i<5;i++){
                    s += a*noise3(p);
                    p *= 2.02; a *= 0.5;
                }
                return s;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            // --- Stars: pontuais com posicao randomica por celula ---
            float starField(float3 n, float t){
                // UV equiretangulares estaveis
                float u = atan2(n.z, n.x) / 6.2831853;  // [-0.5,0.5]
                u = frac(u + 1.0);                      // [0,1)
                float v = acos(clamp(n.y, -1.0, 1.0)) / 3.1415926; // [0,1]
                float2 uv = float2(u, v);

                float2 g = uv * _StarTile;
                float2 cell = floor(g);
                float2 f = frac(g);

                float h = hash21(cell);
                float hasStar = step(1.0 - _StarDensity, h);

                float2 spos = hash22(cell + 17.3); // posicao aleatoria dentro da celula
                float r = length(f - spos);
                float core = 1.0 - smoothstep(_StarSize - _StarSoftness, _StarSize, r);

                float big = smoothstep(0.98, 1.0, h);
                float baseB = lerp(0.6, 1.6, big) * _StarBrightness;

                float twSeed = hash21(cell + 37.42);
                float tw = 1.0 + _TwinkleAmount * (sin(t*_TwinkleSpeed + twSeed*6.28318)*0.5 + 0.5);

                return hasStar * core * baseB * tw;
            }

            // --- Glints: pontos esparsos que acendem/apagam ---
            float glints(float3 n, float t){
                float s = fbm3(n*6.0 + float3(t*_GlintSpeed, 0, -t*_GlintSpeed));
                s = smoothstep(0.88, 0.98, s);
                return s * _GlintAmount;
            }

            // --- Nebula: fbm em world space com fator base pequeno ---
            float nebulas(float3 posW){
                float3 p = posW * _NebulaScale * 0.0005; // ajuste grosso via _NebulaScale
                float f = fbm3(p);
                f = pow(saturate(f), _NebulaContrast);
                return f * _NebulaIntensity;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // usa a direcao do fragmento para a camera como “normal” virtual
                float3 viewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);
                float t = _Time.y;

                float lum = 0.0;
                if(_StarsOn  > 0.5)  lum += starField(viewDir, t);
                if(_GlintsOn > 0.5)  lum += glints(viewDir, t);
                if(_NebulaOn > 0.5)  lum += nebulas(IN.worldPos); // manter em world para parallax sutil - APENAS NEBULOSA
                // // descomentar se for nebula sem parallax
                // if(_NebulaOn > 0.5)  lum += glints(viewDir*0.0, t) + nebulas(viewDir * 100.0);

                float3 col = saturate(_Tint.rgb * lum) * _Fade;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
