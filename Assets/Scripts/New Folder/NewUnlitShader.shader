Shader "Custom/JoseonStyle_ToonCharacter"
{
    Properties
    {
        _Color ("Main Color (Base Fabric)", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (Base Fabric)", 2D) = "white" {}
        _RampTex ("Shading Ramp (Gradient 1D)", 2D) = "gray" {} 
        _OutlineColor ("Outline Color (Ink Line)", Color) = (0.05, 0.05, 0.05, 1)
        _OutlineThickness ("Outline Thickness", Range(0, 0.05)) = 0.005 

        _Glossiness ("Glossiness (Shininess)", Range(0, 1)) = 0.2 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        

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


        fixed4 LightingToon (SurfaceOutput s, fixed3 lightDir, fixed3 viewDir, fixed atten)
        {
            half NdotL = dot(s.Normal, lightDir) * 0.5 + 0.5; 
            
            half3 ramp = tex2D(_RampTex, float2(NdotL, 0.5)).rgb;
            
            half3 h = normalize(lightDir + viewDir);
            float NdotH = max(0, dot(s.Normal, h));
            float spec = pow(NdotH, s.Specular * 128.0) * s.Gloss;

            fixed4 c;
            c.rgb = (s.Albedo * _LightColor0.rgb * ramp) + (_LightColor0.rgb * spec * atten);
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            
            o.Specular = _Glossiness; 
            o.Gloss = 1.0;            
        }
        ENDCG

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            
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
                float3 norm = normalize(v.normal);
                float3 pos = v.vertex.xyz + norm * _OutlineThickness; 
                
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
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