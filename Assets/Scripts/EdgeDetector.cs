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
        int[] triangles = mesh.triangles;

        // 构建边-三角形映射
        Dictionary<Edge, List<int>> edgeToTriangles = new Dictionary<Edge, List<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int triIndex = i / 3;
            AddEdge(edgeToTriangles, triangles[i], triangles[i + 1], triIndex);
            AddEdge(edgeToTriangles, triangles[i + 1], triangles[i + 2], triIndex);
            AddEdge(edgeToTriangles, triangles[i + 2], triangles[i], triIndex);
        }

        // 检测硬边
        List<Vector3> edgePoints = new List<Vector3>();

        foreach (var kvp in edgeToTriangles)
        {
            if (kvp.Value.Count == 2) // 共享边
            {
                Vector3 normal1 = GetTriangleNormal(mesh, kvp.Value[0]);
                Vector3 normal2 = GetTriangleNormal(mesh, kvp.Value[1]);
                float angle = Vector3.Angle(normal1, normal2);

                if (angle > angleThreshold)
                {
                    Edge edge = kvp.Key;
                    edgePoints.Add(transform.TransformPoint(vertices[edge.v1]));
                    edgePoints.Add(transform.TransformPoint(vertices[edge.v2]));
                }
            }
            else if (kvp.Value.Count == 1) // 边界边（只属于一个三角形）
            {
                Edge edge = kvp.Key;
                edgePoints.Add(transform.TransformPoint(vertices[edge.v1]));
                edgePoints.Add(transform.TransformPoint(vertices[edge.v2]));
            }
        }

        // 使用LineRenderer绘制边缘
        if (edgePoints.Count > 0)
        {
            DrawEdges(edgePoints);
        }
    }

    void AddEdge(Dictionary<Edge, List<int>> dict, int v1, int v2, int triIndex)
    {
        Edge edge = new Edge(v1, v2);
        if (!dict.ContainsKey(edge))
            dict[edge] = new List<int>();
        dict[edge].Add(triIndex);
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
}
