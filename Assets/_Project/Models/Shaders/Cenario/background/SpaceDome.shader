Shader "Custom/SpaceDome_V2"
{
    Properties
    {
        // ---------- Stars (camada 1) ----------
        _StarDensity    ("Star Density",    Range(0,1))          = 0.45
        _StarBrightness ("Star Brightness", Range(0,3))          = 1.2
        _StarSize       ("Star Size",       Range(0.0005,0.01))  = 0.003
        _StarSoftness   ("Star Softness",   Range(0.0001,0.01))  = 0.0015
        _TwinkleAmount  ("Twinkle Amount",  Range(0,1))          = 0.30
        _TwinkleSpeed   ("Twinkle Speed",   Range(0,8))          = 1.6
        _StarTile       ("Star Tile (cells)", Range(64,2048))    = 1024

        // ---------- Stars (camada 2 opcional) ----------
        _StarDensity2    ("Star Density 2",    Range(0,1))        = 0.25
        _StarBrightness2 ("Star Brightness 2", Range(0,3))        = 0.8
        _StarSize2       ("Star Size 2",       Range(0.0005,0.01))= 0.0022
        _StarTile2       ("Star Tile 2",       Range(64,4096))    = 2048

        // ---------- Nebula ----------
        _NebulaOn        ("Nebula On", Float) = 0
        _NebulaIntensity ("Nebula Intensity", Range(0,1))  = 0.25
        _NebulaScale     ("Nebula Scale",     Range(0.2,6))= 1.2
        _NebulaContrast  ("Nebula Contrast",  Range(0.5,3))= 1.6

        // ---------- Glints ----------
        _GlintsOn       ("Glints On", Float)  = 0
        _GlintAmount    ("Glint Amount", Range(0,1)) = 0.12
        _GlintSpeed     ("Glint Speed",  Range(0,8)) = 1.5

        // ---------- Global ----------
        _StarsOn        ("Stars On",  Float) = 1
        _Tint           ("Overall Tint", Color) = (1,1,1,1)
        _Fade           ("Global Fade (0-1)", Range(0,1)) = 1

        // Render/culling (Front = normais para fora, câmera dentro)
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 1
    }

    SubShader
    {
        // Desenha como fundo, por cima do skybox sem depender de profundidade
        Tags { "Queue"="Background" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull [_Cull]
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ===== Structs =====
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ===== Material params =====
            CBUFFER_START(UnityPerMaterial)
                // stars (layer 1)
                float _StarDensity, _StarBrightness, _StarSize, _StarSoftness, _TwinkleAmount, _TwinkleSpeed, _StarTile;
                // stars (layer 2)
                float _StarDensity2, _StarBrightness2, _StarSize2, _StarTile2;
                // nebula
                float _NebulaOn, _NebulaIntensity, _NebulaScale, _NebulaContrast;
                // glints
                float _GlintsOn, _GlintAmount, _GlintSpeed;
                // global
                float _StarsOn, _Fade;
                float4 _Tint;
            CBUFFER_END

            // ===== Hash & noise =====
            inline float hash11(float x) {
                x = frac(x * 0.1031);
                x *= x + 33.33;
                x *= x + x;
                return frac(x);
            }
            inline float hash21(float2 p) {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453);
            }
            inline float2 hash22(float2 p) {
                float n = sin(dot(p, float2(41.2321, 289.133)));
                return frac(float2(262144.0 * n, 32768.0 * n));
            }
            inline float noise3(float3 p){
                float3 i = floor(p);
                float3 f = frac(p);
                float n = 0.0;
                [unroll] for (int x=0;x<2;x++)
                [unroll] for (int y=0;y<2;y++)
                [unroll] for (int z=0;z<2;z++) {
                    float3 g = float3(x,y,z);
                    float3 o = i + g;
                    float w = hash11(dot(o, float3(1,57,113)));
                    float3 u = f - g;
                    float3 k = 1.0 - abs(u);
                    n += w * (k.x*k.y*k.z);
                }
                return saturate(n);
            }
            inline float fbm(float3 p){
                float a=0.5, s=0.0;
                [unroll] for(int i=0;i<5;i++){
                    s += a * noise3(p);
                    p *= 2.02;
                    a *= 0.5;
                }
                return s;
            }

            // ===== Vertex =====
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(worldPos);
                OUT.worldPos   = worldPos;
                return OUT;
            }

            // ===== Star helpers =====
            // Um plano (UV) do triplanar
            inline float StarEvalPlane(float2 uv, float tile, float starSize, float starSoft,
                                       float density, float brightness, float t, float seed)
            {
                float2 g = uv * tile;
                float2 cell = floor(g);
                float2 f    = frac(g);

                // hash por célula
                float h   = hash21(cell + seed);
                float2 sp = hash22(cell + seed*13.37);

                // presença de estrela
                float hasStar = step(1.0 - density, h);

                // distância PERIÓDICA dentro da célula (elimina linhas nas bordas)
                float2 dxy = abs(f - sp);
                dxy = min(dxy, 1.0 - dxy);
                float r = length(dxy);

                // footprint em UV -> tamanho do pixel na célula (AA adaptativo estável)
                float2 uv_dx = ddx(uv) * tile;
                float2 uv_dy = ddy(uv) * tile;
                float px = max(length(uv_dx), length(uv_dy));
                float softAA = px * 1.25;

                float soft = max(starSoft, softAA);

                // brilho + twinkle
                float baseB  = brightness * lerp(0.8, 1.6, smoothstep(0.85, 1.0, h));
                float tw     = 1.0 + _TwinkleAmount * (sin(t*_TwinkleSpeed + h*6.2831853) * 0.5 + 0.5);

                float core = 1.0 - smoothstep(starSize - soft, starSize + soft, r);
                return hasStar * baseB * tw * core;
            }

            // Triplanar estável em direção de visão
            inline float StarLayer(float3 viewDir, float tile, float starSize, float starSoft,
                                   float density, float brightness, float t, float seed)
            {
                float3 d = normalize(viewDir);

                // pesos triplanar suavizados
                float3 aw = abs(d);
                float3 w = pow(aw, 4.0);
                w /= max(1e-4, (w.x + w.y + w.z));

                // UVs dos 3 planos
                float2 uvX = d.zy * 0.5 + 0.5;   // plano YZ
                float2 uvY = d.xz * 0.5 + 0.5;   // plano XZ
                float2 uvZ = d.xy * 0.5 + 0.5;   // plano XY

                float sx = StarEvalPlane(uvX, tile, starSize, starSoft, density, brightness, t, seed);
                float sy = StarEvalPlane(uvY, tile, starSize, starSoft, density, brightness, t, seed + 7.0);
                float sz = StarEvalPlane(uvZ, tile, starSize, starSoft, density, brightness, t, seed + 13.0);

                return sx*w.x + sy*w.y + sz*w.z;
            }

            // Nebula/Glints em direção (sem parallax)
            inline float NebulaFromDir(float3 viewDir){
                float3 d = normalize(viewDir);
                float3 p = d * (_NebulaScale * 18.0);
                float f  = fbm(p);
                f = pow(saturate(f), _NebulaContrast);
                return f * _NebulaIntensity;
            }
            inline float GlintsFromDir(float3 viewDir, float t){
                float3 d = normalize(viewDir);
                float s = fbm(d * 6.0 + float3(t*_GlintSpeed, 0, -t*_GlintSpeed));
                s = smoothstep(0.88, 0.98, s);
                return s * _GlintAmount;
            }

            // ===== Fragment =====
            half4 frag (Varyings IN) : SV_Target
            {
                // trava tudo na rotação/posição: usa direção do fragmento para a câmera
                float3 viewDir = normalize(IN.worldPos - _WorldSpaceCameraPos);
                float t = _Time.y;

                float lum = 0.0;

                if (_StarsOn > 0.5)
                {
                    // Camada base
                    lum += StarLayer(viewDir, _StarTile,  _StarSize,  _StarSoftness, _StarDensity,  _StarBrightness,  t, 17.0);
                    // Camada de variação (opcional)
                    lum += StarLayer(viewDir, _StarTile2, _StarSize2, _StarSoftness, _StarDensity2, _StarBrightness2, t, 53.0);
                }

                if (_GlintsOn  > 0.5) lum += GlintsFromDir(viewDir, t);
                if (_NebulaOn  > 0.5) lum += NebulaFromDir(viewDir);

                float3 col = saturate(_Tint.rgb * lum) * _Fade;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
