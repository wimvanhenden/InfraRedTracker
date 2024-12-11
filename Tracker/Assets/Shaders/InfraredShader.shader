Shader "Custom/InfraredShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MinValue ("Min Value", Float) = 0
        _MaxValue ("Max Value", Float) = 65535
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float _MinValue;
            float _MaxValue;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = v.uv;
                o.uv = float2(1-v.uv.x, 1 - v.uv.y);  // Flip the UV vertically and horizontally
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float infraredValue = tex2D(_MainTex, i.uv).r * 65535;
                float normalizedValue = (infraredValue - _MinValue) / (_MaxValue - _MinValue);
                return fixed4(normalizedValue, normalizedValue, normalizedValue, 1);
            }
            ENDCG
        }
    }
}