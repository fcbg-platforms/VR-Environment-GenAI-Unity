Shader "AiWorldGeneration/Flip Normals" { 
    Properties { 
         _MainTex ("Texture", 2D) = "white"{} 
     } 

    SubShader { 
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}

        Cull Off

        Pass { 
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            uniform sampler2D _MainTex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = -v.normal;
                return o;
            }

            fixed4 frag(appdata i) : COLOR {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG 
        }

    }
}
