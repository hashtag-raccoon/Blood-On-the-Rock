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
        Blend
        SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

sampler2D _MainTex;
float _PixelWidth;
float _PixelHeight;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
                // 텍스처의 크기 비율에 따른 픽셀 크기 설정
    float2 pixelSize = float2(1.0 / _PixelWidth, 1.0 / _PixelHeight);

                // 픽셀화된 UV 좌표 계산 (중앙 보정)
    float2 uv = floor(i.uv / pixelSize) * pixelSize + (pixelSize * 0.5);

                // 픽셀화된 UV 좌표로 텍스처 색상 가져오기
    fixed4 color = tex2D(_MainTex, uv);
                
                // 알파값을 유지하여 투명도 처리
    return color;
}
            ENDCG
        }
    }
}