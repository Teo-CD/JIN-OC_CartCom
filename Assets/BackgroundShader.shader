Shader "Custom/BackgroundShader"
{
    Properties
    {
        _Color ("Color", Color) = (0.2,0.5,0.4,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Size ("Taille", Range(0,100)) = 75.0
        _Speed ("Vitesse", Range(0,100)) = 20.0
    }
    SubShader
    {
        Pass
        {
            Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
            Cull Off
            ZWrite Off

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
            float4 _MainTex_ST;
            float4 _Color;
            float _Size;
            float _Speed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // Offset enough so that the 0 crossing won't appear on large screens.
                float time_step = (_Time[1]*_Speed) + 10000;
                float4 fragColor;
                float x = abs((-i.vertex.x + time_step) % _Size);
                float y = abs((-i.vertex.y - time_step) % _Size);
                if (( x <= _Size/2 && y <= _Size/2)
                 || ( x > _Size/2 && y > _Size/2)) {
                    fragColor = _Color;
                } else {
                    fragColor = _Color*0.8;
                }
                
                return fragColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
