Shader "BouncingPlanet/MusouRingFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0,1)) = 1
        _StartAngle ("Start Angle (deg)", Float) = 80
        _SpanAngle ("Span Angle (deg)", Float) = 344
        _Clockwise ("Clockwise", Float) = 1
        _CenterX ("Center X", Range(0,1)) = 0.5
        _CenterY ("Center Y", Range(0,1)) = 0.5
        _Aspect ("Aspect", Float) = 0.9248
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Progress;
            float _StartAngle;
            float _SpanAngle;
            float _Clockwise;
            float _CenterX;
            float _CenterY;
            float _Aspect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // 以图片中心为圆心，从顶部开始按角度裁剪；
                // 进度层和底图使用同一张贴图，因此填充像素与底图完全重合。
                float2 d = float2(
                    (i.texcoord.x - _CenterX) * _Aspect,
                    i.texcoord.y - _CenterY);
                float angle = atan2(d.y, d.x);
                float start = radians(_StartAngle);
                float delta = _Clockwise > 0.5
                    ? start - angle
                    : angle - start;

                // 先把角度差归一到 [0, 2π)，再按“美术环的实际跨度”缩放。
                // 顶部有缺口，填充从 start 开始，走到 span 结束。
                float twoPi = 6.28318530718;
                delta -= floor(delta / twoPi) * twoPi;
                float span = max(0.0174533, radians(_SpanAngle));
                float t = delta / span;

                clip(_Progress - t);
                return col;
            }
            ENDCG
        }
    }
}
