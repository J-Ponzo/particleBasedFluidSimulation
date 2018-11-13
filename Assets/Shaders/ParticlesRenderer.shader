Shader "Custom/ParticlesRenderer"
{
    Properties
    {
        _ParticleTexture ("ParticleTexture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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

            sampler2D _ParticleTexture;

			v2g vert (appdata v)
            {
				v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			int _NbParts = 0;
			float4 _Particles[1024];

			
			[maxvertexcount(4096)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> tristream) {
				float offset = 1;
				float2 particle;
				v2g test = (v2g)0;
				for (int i = 0; i < _NbParts; i++) {
					particle = _Particles[0];

					//Down Right face
					test.vertex.x = particle.x - offset;
					test.vertex.y = particle.y - offset;
					test.uv = float2(0, 0);
					tristream.Append(test);

					test.vertex.x = particle.x + offset;
					test.vertex.y = particle.y - offset;
					test.uv = float2(1, 0);
					tristream.Append(test);

					test.vertex.x = particle.x + offset;
					test.vertex.y = particle.y + offset;
					test.uv = float2(1, 1);
					tristream.Append(test);
					tristream.RestartStrip();
				}

				//v2g test = (v2g)0;
				/*for (int i = 0; i < 3; i++)
				{
					test.vertex = input[i].vertex;
					test.uv = input[i].uv;
					tristream.Append(test);
				}*/
			}

            fixed4 frag (g2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_ParticleTexture, i.uv);
                return float4(1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
