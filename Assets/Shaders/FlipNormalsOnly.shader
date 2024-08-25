Shader "AiWorldGeneration/Flip Normals Only" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {

        Tags { "RenderType" = "Opaque" }

        Cull Off

        CGPROGRAM

        #pragma surface surf Lambert vertex:flip_vertex
        sampler2D _MainTex;

        struct Input {
            float2 uv_MainTex;
            float4 color: COLOR;
        };

        void flip_vertex(inout appdata_full vert) {
            vert.normal.xyz = vert.normal * -1;
        }

        void surf(Input input, inout SurfaceOutput output) {
             fixed3 result = tex2D(_MainTex, input.uv_MainTex);
             output.Albedo = result.rgb;
             output.Alpha = 1;
        }

        ENDCG

    }

      Fallback "Diffuse"
}
