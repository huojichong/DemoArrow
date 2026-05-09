using UnityEngine;
using System.Collections.Generic;

public class EdgeDetector : MonoBehaviour
{
    [Range(0, 180)]
    public float angleThreshold = 30f; // 法线夹角阈值
    public Color edgeColor = Color.black;
    public float edgeWidth = 0.02f;

    private GameObject edgeObject;

    void Start()
    {
        DetectAndDrawEdges();
    }

    void OnValidate()
    {
        // 参数改变时重新绘制
        if (Application.isPlaying)
        {
            DetectAndDrawEdges();
        }
    }

    public void DetectAndDrawEdges()
    {
        // 清除旧的边缘对象
        if (edgeObject != null)
        {
            DestroyImmediate(edgeObject);
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("EdgeDetector: 没有找到MeshFilter或Mesh");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        // 使用位置边作为key，存储所有使用该边的顶点索引和法线
        Dictionary<PositionEdge, List<EdgeInfo>> positionEdgeMap = new Dictionary<PositionEdge, List<EdgeInfo>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int triIndex = i / 3;
            Vector3 triNormal = GetTriangleNormal(mesh, triIndex);

            AddPositionEdge(positionEdgeMap, vertices, normals, triangles[i], triangles[i + 1], triNormal);
            AddPositionEdge(positionEdgeMap, vertices, normals, triangles[i + 1], triangles[i + 2], triNormal);
            AddPositionEdge(positionEdgeMap, vertices, normals, triangles[i + 2], triangles[i], triNormal);
        }

        // 检测硬边
        HashSet<PositionEdge> drawnEdges = new HashSet<PositionEdge>();
        List<Vector3> edgePoints = new List<Vector3>();

        foreach (var kvp in positionEdgeMap)
        {
            if (drawnEdges.Contains(kvp.Key))
                continue;

            var edgeInfos = kvp.Value;

            // 检查是否有法线差异
            bool hasHardEdge = false;

            if (edgeInfos.Count == 1)
            {
                // 边界边
                hasHardEdge = true;
            }
            else
            {
                // 检查所有法线对
                for (int i = 0; i < edgeInfos.Count; i++)
                {
                    for (int j = i + 1; j < edgeInfos.Count; j++)
                    {
                        float angle = Vector3.Angle(edgeInfos[i].normal, edgeInfos[j].normal);
                        if (angle > angleThreshold)
                        {
                            hasHardEdge = true;
                            break;
                        }
                    }
                    if (hasHardEdge) break;
                }
            }

            if (hasHardEdge)
            {
                PositionEdge edge = kvp.Key;
                edgePoints.Add(transform.TransformPoint(edge.p1));
                edgePoints.Add(transform.TransformPoint(edge.p2));
                drawnEdges.Add(edge);
            }
        }

        // 使用LineRenderer绘制边缘
        if (edgePoints.Count > 0)
        {
            DrawEdges(edgePoints);
        }
    }

    void AddPositionEdge(Dictionary<PositionEdge, List<EdgeInfo>> dict, Vector3[] vertices, Vector3[] normals, int v1, int v2, Vector3 triNormal)
    {
        PositionEdge edge = new PositionEdge(vertices[v1], vertices[v2]);
        if (!dict.ContainsKey(edge))
            dict[edge] = new List<EdgeInfo>();

        dict[edge].Add(new EdgeInfo
        {
            v1 = v1,
            v2 = v2,
            normal = triNormal
        });
    }

    struct EdgeInfo
    {
        public int v1, v2;
        public Vector3 normal;
    }

    Vector3 GetTriangleNormal(Mesh mesh, int triIndex)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        int i = triIndex * 3;
        Vector3 v1 = vertices[triangles[i]];
        Vector3 v2 = vertices[triangles[i + 1]];
        Vector3 v3 = vertices[triangles[i + 2]];

        return Vector3.Cross(v2 - v1, v3 - v1).normalized;
    }

    void DrawEdges(List<Vector3> points)
    {
        edgeObject = new GameObject("Edges");
        edgeObject.transform.parent = transform;
        edgeObject.transform.localPosition = Vector3.zero;
        edgeObject.transform.localRotation = Quaternion.identity;
        edgeObject.transform.localScale = Vector3.one;

        LineRenderer lineRenderer = edgeObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = edgeColor;
        lineRenderer.endColor = edgeColor;
        lineRenderer.startWidth = edgeWidth;
        lineRenderer.endWidth = edgeWidth;
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        lineRenderer.useWorldSpace = true;
    }

    struct Edge
    {
        public int v1, v2;

        public Edge(int v1, int v2)
        {
            // 确保v1 < v2，这样(1,2)和(2,1)会被视为同一条边
            this.v1 = Mathf.Min(v1, v2);
            this.v2 = Mathf.Max(v1, v2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge)) return false;
            Edge other = (Edge)obj;
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ v2.GetHashCode();
        }
    }

    struct PositionEdge
    {
        public Vector3 p1, p2;
        private const float EPSILON = 0.0001f;

        public PositionEdge(Vector3 p1, Vector3 p2)
        {
            // 确保p1在p2之前（按坐标排序）
            if (p1.x < p2.x || (Mathf.Approximately(p1.x, p2.x) && p1.y < p2.y) ||
                (Mathf.Approximately(p1.x, p2.x) && Mathf.Approximately(p1.y, p2.y) && p1.z < p2.z))
            {
                this.p1 = p1;
                this.p2 = p2;
            }
            else
            {
                this.p1 = p2;
                this.p2 = p1;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PositionEdge)) return false;
            PositionEdge other = (PositionEdge)obj;
            return Vector3.Distance(p1, other.p1) < EPSILON && Vector3.Distance(p2, other.p2) < EPSILON;
        }

        public override int GetHashCode()
        {
            // 使用量化的坐标作为hash
            int hash1 = Mathf.RoundToInt(p1.x * 1000) ^ Mathf.RoundToInt(p1.y * 1000) ^ Mathf.RoundToInt(p1.z * 1000);
            int hash2 = Mathf.RoundToInt(p2.x * 1000) ^ Mathf.RoundToInt(p2.y * 1000) ^ Mathf.RoundToInt(p2.z * 1000);
            return hash1 ^ hash2;
        }
    }
}
