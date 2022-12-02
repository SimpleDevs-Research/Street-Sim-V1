Shader "Custom/Replacer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature UV_VIS
            #pragma shader_feature LOCAL_POS_VIS
            #pragma shader_feature WORLD_POS_VIS
            #pragma shader_feature DEPTH_VIS
            #pragma shader_feature LOCAL_NORMAL_VIS
            #pragma shader_feature WORLD_NORMAL_VIS
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
                float  depth : TEXCOORD3;
                float3 normal : TEXCOORD4;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
   
            v2f vert (appdata v)
            {
                v2f o;
                o.localPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = normalize(o.worldPos);
                o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z * _ProjectionParams.w;
                o.uv = v.uv;
                o.normal = v.normal;
                #if WORLD_NORMAL_VIS
                o.normal = mul(unity_ObjectToWorld, o.normal);
                #endif
                return o;
            }
   
            fixed4 frag (v2f i) : SV_Target
            {
                #if UV_VIS
                return fixed4(i.uv, 0, 1);
                #endif
                #if LOCAL_POS_VIS
                return fixed4(i.localPos, 1.0);
                #endif
                #if WORLD_POS_VIS
                return fixed4(i.worldPos, 1.0);
                #endif
                #if DEPTH_VIS
                return i.depth;
                #endif
                #if LOCAL_NORMAL_VIS || WORLD_NORMAL_VIS
                return fixed4(normalize(i.normal) * 0.5 + 0.5,1.0);
                #endif
                return tex2D(_MainTex, i.uv); //If no shader feature is enabled
            }
            ENDCG
        }
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "MyTag"="OnlyColors" } //Subshader with same tag "MyTag"

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature UV_VIS
            #pragma shader_feature LOCAL_POS_VIS
            #pragma shader_feature WORLD_POS_VIS
            #pragma shader_feature DEPTH_VIS
            #pragma shader_feature LOCAL_NORMAL_VIS
            #pragma shader_feature WORLD_NORMAL_VIS
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag () : SV_Target
            {
                #if UV_VIS
                return fixed4(1,0,0,1);
                #endif
                #if LOCAL_POS_VIS
                return fixed4(0,1,0,1);
                #endif
                #if WORLD_POS_VIS
                return fixed4(0,0,1,1);
                #endif
                #if DEPTH_VIS
                return fixed4(1,1,0,1);
                #endif
                #if LOCAL_NORMAL_VIS
                return fixed4(1,0,1,1);
                #endif
                #if WORLD_NORMAL_VIS
                return fixed4(0,1,1,1);
                #endif
                return fixed4(1,1,1,1);
            }
            ENDCG
        }
    }
}