Shader "Custom/EdgeHighlight"
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
            #pragma fragment frag
            #pragma geometry geom
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
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                float3 normal : NORMAL;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.pos = v.vertex;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;

                // 为三角形的三个顶点分配重心坐标
                o.pos = UnityObjectToClipPos(IN[0].pos);
                o.barycentric = float3(1, 0, 0);
                o.normal = IN[0].normal;
                triStream.Append(o);

                o.pos = UnityObjectToClipPos(IN[1].pos);
                o.barycentric = float3(0, 1, 0);
                o.normal = IN[1].normal;
                triStream.Append(o);

                o.pos = UnityObjectToClipPos(IN[2].pos);
                o.barycentric = float3(0, 0, 1);
                o.normal = IN[2].normal;
                triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // 计算到最近边的距离
                float3 d = fwidth(i.barycentric);
                float3 a3 = smoothstep(float3(0.0, 0.0, 0.0), d * _EdgeWidth * 10, i.barycentric);
                float edgeFactor = min(min(a3.x, a3.y), a3.z);

                // 简单的光照
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float ndotl = max(0, dot(normalize(i.normal), lightDir));
                float3 lighting = ndotl * 0.8 + 0.2;

                // 混合边缘颜色和主颜色
                float4 baseColor = _Color * float4(lighting, 1);
                return lerp(_EdgeColor, baseColor, edgeFactor);
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
