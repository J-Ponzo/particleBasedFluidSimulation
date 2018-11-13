Shader "Custom/fluidRenderingPostEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_ParticleTex("ParticleTexture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            struct v2g
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2g vert (appdata v)
            {
				v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			int _nbParts = 0;
			float _Particles[4096];

			[maxvertexcount(12)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> tristream) {
				for (int i = 0; i < _nbParts; i++) {

				}
			}

            sampler2D _MainTex;
			sampler2D _ParticleTex;

            fixed4 frag (g2f i) : SV_Target
			{
                fixed4 col = tex2D(_MainTex, i.uv);
				
                return float4(i.uv.x, i.uv.y, 0, 1);
            }
            ENDCG
        }
    }
}
