Shader "Unlit/BlurShader"
{
    Properties
    {
        _Radius("Radius", Range(0, 10)) = 3.0
        _Downsample("Downsample", Range(1, 4)) = 2
        _blurColor("Blur Color", Color) = (1,1,1,1)
        _Darkness("Darkness", Range(0, 1)) = 0.3
    }
 
    Category
    {
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
     
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
                float4 _blurColor;
                float _Darkness;
 
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
                    
                    // 더 많은 샘플링으로 블러 강화
                    half4 color = half4(0, 0, 0, 0);
                    
                    // 중앙
                    color += tex2D(_BackgroundTexture, i.uv) * 0.2;
                    
                    // 대각선 4방향
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset, offset) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset, offset) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset, -offset) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset, -offset) * texelSize) * 0.1;
                    
                    // 상하좌우 4방향 추가
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset, 0) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset, 0) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(0, offset) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(0, -offset) * texelSize) * 0.1;
                    
                    // 추가 대각선 샘플 (더 멀리)
                    float offset2 = _Radius * 1.5;
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset2, offset2) * texelSize) * 0.05;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset2, offset2) * texelSize) * 0.05;
                    color += tex2D(_BackgroundTexture, i.uv + float2(offset2, -offset2) * texelSize) * 0.05;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-offset2, -offset2) * texelSize) * 0.05;
                    
                    // 어둡게 처리 (검은색 오버레이)
                    color.rgb *= (1.0 - _Darkness);
                    
                    return color;
                }
                ENDCG
            }
        }
        
        SubShader
        {
            GrabPass { "_BackgroundTexture" }
            
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
                float4 _blurColor;
                float _Darkness;
 
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
                    
                    // 더 많은 샘플링
                    half4 color = tex2D(_BackgroundTexture, i.uv) * 0.3;
                    
                    color += tex2D(_BackgroundTexture, i.uv + float2(1, 1) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-1, -1) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(1, -1) * texelSize) * 0.1;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-1, 1) * texelSize) * 0.1;
                    
                    color += tex2D(_BackgroundTexture, i.uv + float2(1, 0) * texelSize) * 0.075;
                    color += tex2D(_BackgroundTexture, i.uv + float2(-1, 0) * texelSize) * 0.075;
                    color += tex2D(_BackgroundTexture, i.uv + float2(0, 1) * texelSize) * 0.075;
                    color += tex2D(_BackgroundTexture, i.uv + float2(0, -1) * texelSize) * 0.075;
                    
                    // 어둡게 처리
                    color.rgb *= (1.0 - _Darkness);
                    
                    return color * _blurColor;
                }
                ENDCG
            }
        }
    }
}