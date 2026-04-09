Shader "Unlit/CircleWipe"
{
    Properties
    {
        _Progress ("Progress", Range(0,1)) = 0
        _EdgeSoftness ("Edge Softness", Range(0, 0.1)) = 0.01
    }
    SubShader
    {
        Tags { "Queue"="Overlay+1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            float _Progress;
            float _EdgeSoftness;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 centered = i.uv - 0.5;
                centered.x *= aspect;

                float dist = length(centered);
                float maxDist = length(float2(0.5 * aspect, 0.5));

                // circle radius driven by _Progress
                float radius = (1.0 - _Progress) * maxDist;
                float alpha = dist > radius ? 1.0 : 0.0;

                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }
    }
}