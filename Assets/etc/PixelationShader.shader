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
                // �ؽ�ó�� ũ�� ������ ���� �ȼ� ũ�� ����
    float2 pixelSize = float2(1.0 / _PixelWidth, 1.0 / _PixelHeight);

                // �ȼ�ȭ�� UV ��ǥ ��� (�߾� ����)
    float2 uv = floor(i.uv / pixelSize) * pixelSize + (pixelSize * 0.5);

                // �ȼ�ȭ�� UV ��ǥ�� �ؽ�ó ���� ��������
    fixed4 color = tex2D(_MainTex, uv);
                
                // ���İ��� �����Ͽ� ������ ó��
    return color;
}
            ENDCG
        }
    }
}