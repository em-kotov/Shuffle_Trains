Shader "ShuffleCats/CatRunningSmoke"
{
    Properties
    {
        _Color ("Line tint", Color) = (1, 0.97, 0.94, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            float _Opacity;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float across = i.uv.y * 2.0 - 1.0;
                float lines = 1.0 - smoothstep(0.028 / 0.055, 1.0, abs(across));
                float endFade = 1.0 - smoothstep(0.0, 1.0, i.uv.x);
                return fixed4(_Color.rgb, lines * endFade * _Opacity);
            }
            ENDCG
        }
    }
}
