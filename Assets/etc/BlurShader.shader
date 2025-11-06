Shader "Unlit/BlurShader"
{
    Properties
    {
        _Radius("Radius", Range(0, 5)) = 1.5
        _Downsample("Downsample", Range(1, 4)) = 2
    }

    Category
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }

        SubShader
        {
            GrabPass
            {
                "_BackgroundTexture"
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _BackgroundTexture;
                float4 _BackgroundTexture_TexelSize;
                float _Radius;
                float _Downsample;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = ComputeGrabScreenPos(o.vertex);
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    float2 texelSize = _BackgroundTexture_TexelSize.xy * _Downsample;
                    float offset = _Radius;

                    half4 color = tex2D(_BackgroundTexture, i.uv) * 0.5;

                    color += tex2D(_BackgroundTexture, i.uv + float2(offset, offset) * texelSize) * 0.125;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset, offset) * texelSize) * 0.125;
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset, -offset) * texelSize) * 0.125;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset, -offset) * texelSize) * 0.125;

                    return color;
                }
                ENDCG
            }
        }

        SubShader
        {
            GrabPass
            {
                "_BackgroundTexture"
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _BackgroundTexture;
                float4 _BackgroundTexture_TexelSize;
                float _Radius;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = ComputeGrabScreenPos(o.vertex);
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    float2 texelSize = _BackgroundTexture_TexelSize.xy * _Radius * 2;

                    half4 color = tex2D(_BackgroundTexture, i.uv) * 0.6;
                    color += tex2D(_BackgroundTexture, i.uv + float2(1, 1) * texelSize) * 0.2;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-1, -1) * texelSize) * 0.2;

                    return color;
                }
                ENDCG
            }
        }
    }
}
