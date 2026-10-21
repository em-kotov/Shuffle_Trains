Shader "ShuffleCats/CatHintGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cat sprite", 2D) = "white" {}
        _SpriteUVBounds ("Sprite UV bounds", Vector) = (0, 0, 1, 1)
        _HintAge ("Animation", Float) = 0
        _GlowScale ("UI scale", Float) = 1
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
                float2 shape : TEXCOORD1;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
                float2 position : TEXCOORD2;
            };
            sampler2D _MainTex;
            float4 _SpriteUVBounds, _ClipRect;
            float _HintAge, _GlowScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex.xy;
                o.color = v.color;
                o.uv = v.uv;
                o.shape = v.shape;
                return o;
            }

            float Alpha(float2 uv)
            {
                float2 inside = step(_SpriteUVBounds.xy, uv) * step(uv, _SpriteUVBounds.zw);
                return tex2D(_MainTex, clamp(uv, _SpriteUVBounds.xy, _SpriteUVBounds.zw)).a * inside.x * inside.y;
            }

            float Surround(float2 uv, float2 dx, float2 dy, float radius)
            {
                dx *= radius;
                dy *= radius;
                float a = max(max(Alpha(uv + dx), Alpha(uv - dx)), max(Alpha(uv + dy), Alpha(uv - dy)));
                float2 d1 = (dx + dy) * 0.707107, d2 = (dx - dy) * 0.707107;
                return max(a, max(max(Alpha(uv + d1), Alpha(uv - d1)), max(Alpha(uv + d2), Alpha(uv - d2))));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Match the HUD scale instead of vanishing at high resolution.
                float2 dx = ddx(i.uv) * _GlowScale, dy = ddy(i.uv) * _GlowScale;
                float center = Alpha(i.uv);
                float width = 2.4 + 0.35 * sin(_HintAge * 4.71239);
                float edge = Surround(i.uv, dx, dy, width);
                float middle = Surround(i.uv, dx, dy, width + 1.2);
                float outer = Surround(i.uv, dx, dy, width + 2.8);
                float outside = 1.0 - smoothstep(0.03, 0.6, center);
                float opacity = saturate(edge + middle * 0.40 + outer * 0.16) * outside;
                float angle = atan2(i.shape.y - 0.5, i.shape.x - 0.5);
                float shimmer = pow(saturate(0.5 + 0.5 * cos(angle - _HintAge * 2.2)), 10.0);
                fixed3 tint = lerp(fixed3(1.0, 0.62, 0.08), fixed3(1.0, 0.96, 0.71), shimmer * edge);
                opacity *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                opacity *= UnityGet2DClipping(i.position, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(opacity - 0.001);
                #endif
                return fixed4(tint * i.color.rgb, opacity);
            }
            ENDCG
        }
    }
}
