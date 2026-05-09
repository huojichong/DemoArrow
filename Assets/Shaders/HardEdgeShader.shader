Shader "Custom/HardEdgeShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _EdgeColor;
            float _EdgeWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float edgeMask : TEXCOORD2;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;

                // 检测三条边是否为硬边
                float3 n0 = normalize(input[0].normal);
                float3 n1 = normalize(input[1].normal);
                float3 n2 = normalize(input[2].normal);

                // 计算每条边的法线差异
                float edge0 = 1.0 - abs(dot(n0, n1)); // 边 0-1
                float edge1 = 1.0 - abs(dot(n1, n2)); // 边 1-2
                float edge2 = 1.0 - abs(dot(n2, n0)); // 边 2-0

                // 阈值：如果法线差异大于0.1，认为是硬边
                float threshold = 0.1;
                float3 edgeMask = float3(
                    edge0 > threshold ? 1.0 : 0.0,
                    edge1 > threshold ? 1.0 : 0.0,
                    edge2 > threshold ? 1.0 : 0.0
                );

                // 顶点0：对面是边1-2
                o.pos = UnityObjectToClipPos(input[0].vertex);
                o.barycentric = float3(1, 0, 0);
                o.worldNormal = UnityObjectToWorldNormal(input[0].normal);
                o.edgeMask = edgeMask.x;
                triStream.Append(o);

                // 顶点1：对面是边2-0
                o.pos = UnityObjectToClipPos(input[1].vertex);
                o.barycentric = float3(0, 1, 0);
                o.worldNormal = UnityObjectToWorldNormal(input[1].normal);
                o.edgeMask = edgeMask.y;
                triStream.Append(o);

                // 顶点2：对面是边0-1
                o.pos = UnityObjectToClipPos(input[2].vertex);
                o.barycentric = float3(0, 0, 1);
                o.worldNormal = UnityObjectToWorldNormal(input[2].normal);
                o.edgeMask = edgeMask.z;
                triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // 简单光照
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float ndotl = max(0, dot(normalize(i.worldNormal), lightDir));
                float3 lighting = ndotl * 0.8 + 0.2;

                float4 baseColor = _Color * float4(lighting, 1);

                // 计算到边缘的距离
                float3 d = fwidth(i.barycentric);
                float3 a3 = smoothstep(float3(0.0, 0.0, 0.0), d * _EdgeWidth * 10, i.barycentric);
                float edgeFactor = min(min(a3.x, a3.y), a3.z);

                // 只在硬边处显示边缘
                if (i.edgeMask > 0.5)
                {
                    return lerp(_EdgeColor, baseColor, edgeFactor);
                }

                return baseColor;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
