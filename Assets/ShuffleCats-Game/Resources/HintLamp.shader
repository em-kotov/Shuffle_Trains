Shader "ShuffleCats/HintLamp"
{
    Properties
    {
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 lampUV : TEXCOORD0;
                float2 position : TEXCOORD1;
            };
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex.xy;
                o.lampUV = v.uv * 60.0;
                o.color = v.color;
                return o;
            }

            float Disc(float2 p, float2 center, float radius)
            {
                return length(p - center) - radius;
            }

            float Line(float2 p, float2 a, float2 b, float width)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / dot(ab, ab));
                return length(p - a - t * ab) - width * 0.5;
            }

            void Paint(inout float4 artwork, float distance, float4 tint)
            {
                // These are the same authored sRGB colors as the vector mesh.
                // Shader constants need the conversion normally applied to UI colors.
                #ifndef UNITY_COLORSPACE_GAMMA
                tint.rgb = GammaToLinearSpace(tint.rgb);
                #endif
                float aa = max(fwidth(distance), 0.001);
                float alpha = tint.a * (1.0 - smoothstep(-aa * 0.5, aa * 0.5, distance));
                // Composite the opaque artwork first, retaining premultiplied
                // color until all shapes have been painted over one another.
                artwork.rgb = tint.rgb * alpha + artwork.rgb * (1.0 - alpha);
                artwork.a = alpha + artwork.a * (1.0 - alpha);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.lampUV;
                float4 artwork = 0;
                float4 ink = float4(148.0, 103.0, 64.0, 255.0) / 255.0;
                Paint(artwork, Disc(p, float2(30, 27), 28), float4(92.0, 69.0, 55.0, 65.0) / 255.0);
                Paint(artwork, Disc(p, float2(30, 30), 28), float4(171.0, 138.0, 111.0, 255.0) / 255.0);
                Paint(artwork, Disc(p, float2(30, 31), 25.5), float4(255.0, 244.0, 216.0, 255.0) / 255.0);
                Paint(artwork, Disc(p, float2(30, 35), 12), ink);
                Paint(artwork, Disc(p, float2(30, 35.5), 9.5), float4(255.0, 209.0, 91.0, 255.0) / 255.0);

                // Union the rounded strokes so shared endpoints do not overlap.
                float strokes = Line(p, float2(25, 24), float2(35, 24), 5);
                strokes = min(strokes, Line(p, float2(26, 20), float2(34, 20), 3));
                strokes = min(strokes, Line(p, float2(28, 16), float2(32, 16), 3));
                strokes = min(strokes, Line(p, float2(30, 25), float2(30, 32), 2));
                strokes = min(strokes, Line(p, float2(26, 34), float2(30, 31), 2));
                strokes = min(strokes, Line(p, float2(30, 31), float2(34, 34), 2));
                [unroll]
                for (int ray = 0; ray < 5; ray++)
                {
                    float angle = ray * UNITY_PI / 4.0;
                    float2 direction = float2(cos(angle), sin(angle));
                    strokes = min(strokes, Line(p, float2(30, 35) + direction * 16,
                        float2(30, 35) + direction * 19, 2));
                }
                Paint(artwork, strokes, ink);
                Paint(artwork, Line(p, float2(25, 38), float2(27, 41), 2.2), float4(1, 1, 1, 1));

                // Only the finished lamp receives the UI tint/opacity. The
                // disabled state therefore fades as one image, including its rim.
                artwork.rgb /= max(artwork.a, 0.0001);
                artwork *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                artwork.a *= UnityGet2DClipping(i.position, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(artwork.a - 0.001);
                #endif
                return artwork;
            }
            ENDCG
        }
    }
}
