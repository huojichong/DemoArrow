using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 简化版 CSG 实现 - 适用于立方体的布尔合并
/// 基于 BSP 树的思想，但简化为只处理轴对齐的立方体
/// v2.0 - 增强版：去除内部顶点和面
/// </summary>
public class SimpleCSG
{
    /// <summary>
    /// 合并两个立方体（Union 操作）
    /// 注意：这是简化版，仅适用于轴对齐的立方体
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
            // 没有交集，直接合并
            return SimpleCombine(mf1, mf2, obj1.transform, obj2.transform);
        }

        // 有交集，进行增强的 CSG 合并
        return UnionWithIntersectionEnhanced(mf1, mf2, obj1.transform, obj2.transform, bounds1, bounds2);
    }

    private static Bounds GetWorldBounds(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;

        // 转换到世界空间
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

    /// <summary>
    /// 增强版合并：去除内部顶点和面
    /// </summary>
    private static Mesh UnionWithIntersectionEnhanced(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2, Bounds b1, Bounds b2)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // 获取交集区域
        Bounds intersection = GetIntersection(b1, b2);

        // 处理第一个 mesh
        AddMeshWithCulling(mf1.sharedMesh, t1, intersection, b2, vertices, triangles, normals, true);

        // 处理第二个 mesh
        AddMeshWithCulling(mf2.sharedMesh, t2, intersection, b1, vertices, triangles, normals, false);

        // 去除重复顶点
        RemoveDuplicateVertices(ref vertices, ref triangles, ref normals);

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

    private static void AddMeshWithCulling(Mesh mesh, Transform transform, Bounds cullBounds, Bounds otherBounds,
        List<Vector3> vertices, List<int> triangles, List<Vector3> normals, bool isFirst)
    {
        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;
        Vector3[] meshNormals = mesh.normals;

        int vertexOffset = vertices.Count;

        // 遍历所有三角形
        for (int i = 0; i < meshTris.Length; i += 3)
        {
            int i0 = meshTris[i];
            int i1 = meshTris[i + 1];
            int i2 = meshTris[i + 2];

            Vector3 v0 = transform.TransformPoint(meshVerts[i0]);
            Vector3 v1 = transform.TransformPoint(meshVerts[i1]);
            Vector3 v2 = transform.TransformPoint(meshVerts[i2]);

            // 计算三角形中心
            Vector3 triCenter = (v0 + v1 + v2) / 3f;

            // 检查三角形是否应该被剔除
            if (ShouldCullTriangle(triCenter, v0, v1, v2, transform, meshNormals[i0], cullBounds, otherBounds, isFirst))
            {
                continue;
            }

            // 添加顶点和三角形
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            triangles.Add(vertexOffset);
            triangles.Add(vertexOffset + 1);
            triangles.Add(vertexOffset + 2);

            normals.Add(transform.TransformDirection(meshNormals[i0]));
            normals.Add(transform.TransformDirection(meshNormals[i1]));
            normals.Add(transform.TransformDirection(meshNormals[i2]));

            vertexOffset += 3;
        }
    }

    /// <summary>
    /// 判断三角形是否应该被剔除
    /// </summary>
    private static bool ShouldCullTriangle(Vector3 triCenter, Vector3 v0, Vector3 v1, Vector3 v2,
        Transform transform, Vector3 localNormal, Bounds intersection, Bounds otherBounds, bool isFirst)
    {
        // 如果三角形中心在交集区域内
        if (intersection.Contains(triCenter))
        {
            Vector3 normal = transform.TransformDirection(localNormal);

            // 检查法线是否指向交集内部
            Vector3 toCenter = intersection.center - triCenter;
            float dot = Vector3.Dot(normal, toCenter.normalized);

            // 如果法线指向交集内部，剔除这个三角形
            if (dot > 0.1f)
            {
                return true;
            }
        }

        // 检查三角形的所有顶点是否都在另一个对象内部
        if (otherBounds.Contains(v0) && otherBounds.Contains(v1) && otherBounds.Contains(v2))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 去除重复的顶点
    /// </summary>
    private static void RemoveDuplicateVertices(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals)
    {
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>(new Vector3EqualityComparer());
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Count; i++)
        {
            int oldIndex = triangles[i];
            Vector3 vertex = vertices[oldIndex];
            Vector3 normal = normals[oldIndex];

            int newIndex;
            if (!vertexMap.TryGetValue(vertex, out newIndex))
            {
                newIndex = newVertices.Count;
                vertexMap[vertex] = newIndex;
                newVertices.Add(vertex);
                newNormals.Add(normal);
            }

            newTriangles.Add(newIndex);
        }

        vertices = newVertices;
        normals = newNormals;
        triangles = newTriangles;

        Debug.Log($"顶点优化：{vertexMap.Count} 个唯一顶点（原始：{vertices.Count}）");
    }

    /// <summary>
    /// Vector3 相等比较器（用于去重）
    /// </summary>
    private class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private const float Epsilon = 0.0001f;

        public bool Equals(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < Epsilon;
        }

        public int GetHashCode(Vector3 v)
        {
            // 将坐标量化到网格上以提高哈希效率
            int x = Mathf.RoundToInt(v.x / Epsilon);
            int y = Mathf.RoundToInt(v.y / Epsilon);
            int z = Mathf.RoundToInt(v.z / Epsilon);
            return x ^ (y << 10) ^ (z << 20);
        }
    }
}
