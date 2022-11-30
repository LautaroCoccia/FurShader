Shader "Custom/ShellTextureGeoLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _DisplacementTexture("Displacement texture", 2D ) = "bump" {}
        _DisplacementAmount("Displacement amount", float ) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct appdata
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
            float2 texcoord1 : TEXCOORD1;
            float2 texcoord2 : TEXCOORD2;
            float3 normal : NORMAL;
            float4 color : COLOR;
            uint id : SV_VertexID;
        };
        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        struct DrawVertex
        {
            float3 position;
            float3 normal;
            float2 uv;
            float4 color;
        };

        struct DrawTriangle
        {
            DrawVertex drawVertices[3];
        };
#ifdef SHADER_API_D3D11
        StructuredBuffer<DrawTriangle> _DrawTrianglesBuffer;
#endif

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _DisplacementAmount;
        sampler2D _DisplacementTexture;
        float4 _DisplacementTexture_ST;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata output, out Input o)
        {
#ifdef SHADER_API_D3D11
            DrawTriangle tri = _DrawTrianglesBuffer[output.id / 3];
            DrawVertex v = tri.drawVertices[output.id % 3];


            output.vertex = float4(v.position, 1);
            output.normal = v.normal;
            output.texcoord = v.uv;
            output.texcoord1 = v.uv;
            output.texcoord2 = v.uv;
            output.color = v.color;

            UNITY_INITIALIZE_OUTPUT(Input, o);

            o.color = v.color;
#endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float2 displ = tex2D(_DisplacementTexture, IN.uv_MainTex * _DisplacementTexture_ST.xy + _Time.y * _DisplacementTexture_ST.zw).rg;
            displ = (displ * 2.0 - 1.0) * _DisplacementAmount * IN.color.x;
            float tex = tex2D (_MainTex, IN.uv_MainTex + displ).x;
            clip(tex - IN.color.x);
            o.Albedo = tex * _Color.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
