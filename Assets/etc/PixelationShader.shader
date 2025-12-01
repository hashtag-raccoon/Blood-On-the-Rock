Shader"Custom/PixelationSbader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelWidth ("Pixel Width", Float) = 50
        _PixelHeight ("Pixel Height", Float) = 50
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

sampler2D _MainTex;
SamplerState point_clamp_sampler; // Point 필터링 강제
float4 _MainTex_TexelSize;
float _PixelWidth;
float _PixelHeight;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR; // Vertex Color 추가
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float4 color : COLOR; // Color 전달
};

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.color = v.color; // Vertex Color를 Fragment Shader로 전달
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
                // 텍스처의 크기 기반으로 픽셀 크기 계산
    float2 pixelSize = float2(1.0 / _PixelWidth, 1.0 / _PixelHeight);

                // 픽셀화된 UV 좌표 계산 (중앙 정렬)
    float2 uv = floor(i.uv / pixelSize) * pixelSize + (pixelSize * 0.5);

                // 픽셀화된 UV 좌표로 텍스처 색상 샘플링
    fixed4 color = tex2D(_MainTex, uv);
                
                // Vertex Color와 곱하여 SpriteRenderer.color 반영
    return color * i.color;
}
            ENDCG
        }
    }
}