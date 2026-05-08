using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 高级 CSG 合并器 - 完全去除内部顶点和面
/// 专门处理立方体的布尔合并
/// </summary>
public static class AdvancedCSG
{
    private const float EPSILON = 0.0001f;

    /// <summary>
    /// 合并两个立方体，完全去除内部部分
    /// </summary>
    public static Mesh Union(GameObject obj1, GameObject obj2)
    {
        MeshFilter mf1 = obj1.GetComponent<MeshFilter>();
        MeshFilter mf2 = obj2.GetComponent<MeshFilter>();

        if (mf1 == null || mf2 == null)
        {
            Debug.LogError("对象缺少 MeshFilter 组件");
            return null;
        }

        // 获取世界空间的包围盒
        Bounds bounds1 = GetWorldBounds(obj1);
        Bounds bounds2 = GetWorldBounds(obj2);

        // 检查是否有交集
        if (!bounds1.Intersects(bounds2))
        {
            Debug.Log("没有交集，使用简单合并");
            return SimpleCombine(mf1, mf2, obj1.transform, obj2.transform);
        }

        Debug.Log("检测到交集，使用高级 CSG 合并");
        return UnionWithCSG(mf1, mf2, obj1.transform, obj2.transform, bounds1, bounds2);
    }

    private static Bounds GetWorldBounds(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;

        Vector3 center = obj.transform.TransformPoint(bounds.center);
        Vector3 size = Vector3.Scale(bounds.size, obj.transform.lossyScale);

        return new Bounds(center, size);
    }

    private static Mesh SimpleCombine(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2)
    {
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = mf1.sharedMesh;
        combine[0].transform = t1.localToWorldMatrix;
        combine[1].mesh = mf2.sharedMesh;
        combine[1].transform = t2.localToWorldMatrix;

        Mesh result = new Mesh();
        result.CombineMeshes(combine, true, true);
        result.RecalculateNormals();
        result.RecalculateBounds();

        return result;
    }

    private static Mesh UnionWithCSG(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2, Bounds b1, Bounds b2)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // 获取交集区域
        Bounds intersection = GetIntersection(b1, b2);

        Debug.Log($"交集区域: Center={intersection.center}, Size={intersection.size}");

        // 处理第一个 mesh
        int count1 = ProcessMesh(mf1.sharedMesh, t1, b2, intersection, vertices, triangles, normals);
        Debug.Log($"Mesh1: 保留 {count1} 个三角形");

        // 处理第二个 mesh
        int count2 = ProcessMesh(mf2.sharedMesh, t2, b1, intersection, vertices, triangles, normals);
        Debug.Log($"Mesh2: 保留 {count2} 个三角形");

        // 去除重复顶点
        int originalCount = vertices.Count;
        WeldVertices(ref vertices, ref triangles, ref normals);
        Debug.Log($"顶点优化: {vertices.Count} 个唯一顶点 (原始: {originalCount})");

        Mesh result = new Mesh();
        result.vertices = vertices.ToArray();
        result.triangles = triangles.ToArray();
        result.normals = normals.ToArray();
        result.RecalculateBounds();

        return result;
    }

    private static Bounds GetIntersection(Bounds b1, Bounds b2)
    {
        Vector3 min = Vector3.Max(b1.min, b2.min);
        Vector3 max = Vector3.Min(b1.max, b2.max);
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;

        return new Bounds(center, size);
    }

    /// <summary>
    /// 处理单个 mesh，剔除内部三角形
    /// </summary>
    private static int ProcessMesh(Mesh mesh, Transform transform, Bounds otherBounds, Bounds intersection,
        List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;
        Vector3[] meshNormals = mesh.normals;

        int vertexOffset = vertices.Count;
        int triangleCount = 0;

        for (int i = 0; i < meshTris.Length; i += 3)
        {
            int i0 = meshTris[i];
            int i1 = meshTris[i + 1];
            int i2 = meshTris[i + 2];

            Vector3 v0 = transform.TransformPoint(meshVerts[i0]);
            Vector3 v1 = transform.TransformPoint(meshVerts[i1]);
            Vector3 v2 = transform.TransformPoint(meshVerts[i2]);
            Vector3 n0 = transform.TransformDirection(meshNormals[i0]);

            // 判断是否应该保留这个三角形
            if (ShouldKeepTriangle(v0, v1, v2, n0, otherBounds, intersection))
            {
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                triangles.Add(vertexOffset);
                triangles.Add(vertexOffset + 1);
                triangles.Add(vertexOffset + 2);

                normals.Add(n0);
                normals.Add(transform.TransformDirection(meshNormals[i1]));
                normals.Add(transform.TransformDirection(meshNormals[i2]));

                vertexOffset += 3;
                triangleCount++;
            }
        }

        return triangleCount;
    }

    /// <summary>
    /// 判断三角形是否应该保留
    /// </summary>
    private static bool ShouldKeepTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal,
        Bounds otherBounds, Bounds intersection)
    {
        Vector3 triCenter = (v0 + v1 + v2) / 3f;

        // 1. 如果三角形完全在另一个对象内部，剔除
        if (IsPointInsideBounds(v0, otherBounds, EPSILON) &&
            IsPointInsideBounds(v1, otherBounds, EPSILON) &&
            IsPointInsideBounds(v2, otherBounds, EPSILON))
        {
            return false;
        }

        // 2. 如果三角形中心在交集内部，检查法线方向
        if (IsPointInsideBounds(triCenter, intersection, EPSILON))
        {
            // 计算从三角形中心到交集中心的向量
            Vector3 toIntersectionCenter = intersection.center - triCenter;

            // 如果法线指向交集内部（与 toIntersectionCenter 同向），则是内部面，剔除
            float dot = Vector3.Dot(normal.normalized, toIntersectionCenter.normalized);
            if (dot > 0.3f) // 使用更宽松的阈值
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断点是否在包围盒内部（带容差）
    /// </summary>
    private static bool IsPointInsideBounds(Vector3 point, Bounds bounds, float tolerance)
    {
        Vector3 min = bounds.min - Vector3.one * tolerance;
        Vector3 max = bounds.max + Vector3.one * tolerance;

        return point.x > min.x && point.x < max.x &&
               point.y > min.y && point.y < max.y &&
               point.z > min.z && point.z < max.z;
    }

    /// <summary>
    /// 焊接重复顶点
    /// </summary>
    private static void WeldVertices(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals)
    {
        Dictionary<Vector3Int, int> vertexMap = new Dictionary<Vector3Int, int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        int[] indexMap = new int[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3Int key = QuantizeVertex(vertices[i]);

            if (!vertexMap.ContainsKey(key))
            {
                vertexMap[key] = newVertices.Count;
                newVertices.Add(vertices[i]);
                newNormals.Add(normals[i]);
            }

            indexMap[i] = vertexMap[key];
        }

        // 重建三角形索引
        for (int i = 0; i < triangles.Count; i++)
        {
            triangles[i] = indexMap[triangles[i]];
        }

        vertices.Clear();
        vertices.AddRange(newVertices);
        normals.Clear();
        normals.AddRange(newNormals);
    }

    /// <summary>
    /// 量化顶点坐标用于去重
    /// </summary>
    private static Vector3Int QuantizeVertex(Vector3 v)
    {
        float scale = 1f / EPSILON;
        return new Vector3Int(
            Mathf.RoundToInt(v.x * scale),
            Mathf.RoundToInt(v.y * scale),
            Mathf.RoundToInt(v.z * scale)
        );
    }
}
