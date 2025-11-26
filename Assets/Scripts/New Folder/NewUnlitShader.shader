Shader "Custom/JoseonStyle_ToonCharacter"
{
    Properties
    {
        _Color ("Main Color (Base Fabric)", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (Base Fabric)", 2D) = "white" {}
        _RampTex ("Shading Ramp (Gradient 1D)", 2D) = "gray" {} // 그림자 단계 조절용 1D 텍스처
        _OutlineColor ("Outline Color (Ink Line)", Color) = (0.05, 0.05, 0.05, 1) // 먹물색
        _OutlineThickness ("Outline Thickness", Range(0, 0.05)) = 0.005 

        _Glossiness ("Smoothness (Silk Sheen)", Range(0, 1)) = 0.2
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        // 1. Toon Shading Pass (그림자 단계 표현)
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _RampTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        // Custom Toon Lighting Model
        void Toon (SurfaceOutputStandard s, fixed3 lightDir, fixed3 viewDir, fixed atten, inout fixed4 color)
        {
            half NdotL = dot(s.Normal, lightDir);
            
            // 램프 텍스처를 사용하여 그림자 단계 결정
            half shadowStep = tex2D(_RampTex, float2(NdotL * 0.5 + 0.5, 0.5)).r;
            
            // 최종 조명 계산
            fixed3 finalLighting = shadowStep * _LightColor0.rgb * atten;

            // Albedo와 광택 적용
            fixed3 diff = s.Albedo * finalLighting;
            fixed3 spec = s.Specular * pow(max(0, dot(s.Normal, reflect(-lightDir, s.Normal))), s.Smoothness * 128);

            color.rgb = diff + spec;
            color.a = s.Alpha;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG

        // 2. Outline Pass (외곽선 표현)
        // 캐릭터의 외곽선(먹선 효과)을 그리기 위한 별도의 패스
        Pass
        {
            Name "OUTLINE"
            Cull Front // 앞면을 컬링하여 뒷면만 그림
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineThickness;

            struct appdata 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f 
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v) 
            {
                v2f o;
                // 노멀 방향으로 버텍스를 밀어내어 외곽선 두께를 만듭니다.
                float4 pos = v.vertex;
                pos.xyz += v.normal * _OutlineThickness; 
                
                o.pos = UnityObjectToClipPos(pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}