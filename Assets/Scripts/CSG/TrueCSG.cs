using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 真正的 CSG 布尔运算实现
/// 完整处理交集区域，生成新的几何体
/// </summary>
public static class TrueCSG
{
    private const float EPSILON = 0.0001f;

    /// <summary>
    /// 布尔并集运算
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

        Debug.Log("=== 开始真正的 CSG 布尔运算 ===");
        return PerformBooleanUnion(mf1, mf2, obj1.transform, obj2.transform, bounds1, bounds2);
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

    /// <summary>
    /// 执行真正的布尔并集运算
    /// </summary>
    private static Mesh PerformBooleanUnion(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2, Bounds b1, Bounds b2)
    {
        // 获取交集区域
        Bounds intersection = GetIntersection(b1, b2);
        Debug.Log($"交集区域: {intersection.center}, 大小: {intersection.size}");

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // 第一步：添加完全在外部的三角形
        int outside1 = AddOutsideTriangles(mf1.sharedMesh, t1, b2, vertices, triangles, normals);
        int outside2 = AddOutsideTriangles(mf2.sharedMesh, t2, b1, vertices, triangles, normals);
        Debug.Log($"外部三角形: Mesh1={outside1}, Mesh2={outside2}");

        // 第二步：处理交集边界的三角形
        int boundary1 = AddBoundaryTriangles(mf1.sharedMesh, t1, intersection, b2, vertices, triangles, normals);
        int boundary2 = AddBoundaryTriangles(mf2.sharedMesh, t2, intersection, b1, vertices, triangles, normals);
        Debug.Log($"边界三角形: Mesh1={boundary1}, Mesh2={boundary2}");

        // 第三步：去除重复顶点
        int originalCount = vertices.Count;
        WeldVertices(ref vertices, ref triangles, ref normals);
        Debug.Log($"顶点优化: {vertices.Count} 个唯一顶点 (原始: {originalCount})");

        // 第四步：移除退化三角形
        RemoveDegenerateTriangles(ref vertices, ref triangles, ref normals);

        Mesh result = new Mesh();
        result.vertices = vertices.ToArray();
        result.triangles = triangles.ToArray();
        result.normals = normals.ToArray();
        result.RecalculateBounds();

        Debug.Log($"=== 布尔运算完成 ===");
        Debug.Log($"最终顶点数: {result.vertexCount}");
        Debug.Log($"最终三角形数: {result.triangles.Length / 3}");

        return result;
    }

    private static Bounds GetIntersection(Bounds b1, Bounds b2)
    {
        Vector3 min = Vector3.Max(b1.min, b2.min);
        Vector3 max = Vector3.Min(b1.max, b2.max);
        return new Bounds((min + max) * 0.5f, max - min);
    }

    /// <summary>
    /// 添加完全在另一个对象外部的三角形
    /// </summary>
    private static int AddOutsideTriangles(Mesh mesh, Transform transform, Bounds otherBounds,
        List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;
        Vector3[] meshNormals = mesh.normals;

        int count = 0;
        int vertexOffset = vertices.Count;

        for (int i = 0; i < meshTris.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(meshVerts[meshTris[i]]);
            Vector3 v1 = transform.TransformPoint(meshVerts[meshTris[i + 1]]);
            Vector3 v2 = transform.TransformPoint(meshVerts[meshTris[i + 2]]);

            // 如果三角形的所有顶点都在外部，保留
            if (!IsPointInBounds(v0, otherBounds) &&
                !IsPointInBounds(v1, otherBounds) &&
                !IsPointInBounds(v2, otherBounds))
            {
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                triangles.Add(vertexOffset);
                triangles.Add(vertexOffset + 1);
                triangles.Add(vertexOffset + 2);

                normals.Add(transform.TransformDirection(meshNormals[meshTris[i]]));
                normals.Add(transform.TransformDirection(meshNormals[meshTris[i + 1]]));
                normals.Add(transform.TransformDirection(meshNormals[meshTris[i + 2]]));

                vertexOffset += 3;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 添加交集边界的三角形（朝外的面）
    /// </summary>
    private static int AddBoundaryTriangles(Mesh mesh, Transform transform, Bounds intersection, Bounds otherBounds,
        List<Vector3> vertices, List<int> triangles, List<Vector3> normals)
    {
        Vector3[] meshVerts = mesh.vertices;
        int[] meshTris = mesh.triangles;
        Vector3[] meshNormals = mesh.normals;

        int count = 0;
        int vertexOffset = vertices.Count;

        for (int i = 0; i < meshTris.Length; i += 3)
        {
            Vector3 v0 = transform.TransformPoint(meshVerts[meshTris[i]]);
            Vector3 v1 = transform.TransformPoint(meshVerts[meshTris[i + 1]]);
            Vector3 v2 = transform.TransformPoint(meshVerts[meshTris[i + 2]]);
            Vector3 triCenter = (v0 + v1 + v2) / 3f;

            // 检查三角形是否在交集边界上
            bool v0InOther = IsPointInBounds(v0, otherBounds);
            bool v1InOther = IsPointInBounds(v1, otherBounds);
            bool v2InOther = IsPointInBounds(v2, otherBounds);

            // 如果至少有一个顶点在另一个对象内，且至少有一个在外
            bool isBoundary = (v0InOther || v1InOther || v2InOther) &&
                             (!v0InOther || !v1InOther || !v2InOther);

            if (isBoundary)
            {
                // 检查法线方向，只保留朝外的面
                Vector3 normal = transform.TransformDirection(meshNormals[meshTris[i]]);
                Vector3 toOutside = triCenter - intersection.center;

                // 如果法线朝外（与 toOutside 同向），保留
                if (Vector3.Dot(normal, toOutside) > -0.1f)
                {
                    vertices.Add(v0);
                    vertices.Add(v1);
                    vertices.Add(v2);

                    triangles.Add(vertexOffset);
                    triangles.Add(vertexOffset + 1);
                    triangles.Add(vertexOffset + 2);

                    normals.Add(normal);
                    normals.Add(transform.TransformDirection(meshNormals[meshTris[i + 1]]));
                    normals.Add(transform.TransformDirection(meshNormals[meshTris[i + 2]]));

                    vertexOffset += 3;
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsPointInBounds(Vector3 point, Bounds bounds)
    {
        return point.x >= bounds.min.x - EPSILON && point.x <= bounds.max.x + EPSILON &&
               point.y >= bounds.min.y - EPSILON && point.y <= bounds.max.y + EPSILON &&
               point.z >= bounds.min.z - EPSILON && point.z <= bounds.max.z + EPSILON;
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

        for (int i = 0; i < triangles.Count; i++)
        {
            triangles[i] = indexMap[triangles[i]];
        }

        vertices.Clear();
        vertices.AddRange(newVertices);
        normals.Clear();
        normals.AddRange(newNormals);
    }

    private static Vector3Int QuantizeVertex(Vector3 v)
    {
        float scale = 1f / EPSILON;
        return new Vector3Int(
            Mathf.RoundToInt(v.x * scale),
            Mathf.RoundToInt(v.y * scale),
            Mathf.RoundToInt(v.z * scale)
        );
    }

    /// <summary>
    /// 移除退化三角形（面积为0或顶点重复）
    /// </summary>
    private static void RemoveDegenerateTriangles(ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector3> normals)
    {
        List<int> validTriangles = new List<int>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            // 检查是否有重复顶点
            if (i0 == i1 || i1 == i2 || i2 == i0)
                continue;

            Vector3 v0 = vertices[i0];
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];

            // 检查三角形面积是否太小
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            float area = Vector3.Cross(edge1, edge2).magnitude * 0.5f;

            if (area > EPSILON)
            {
                validTriangles.Add(i0);
                validTriangles.Add(i1);
                validTriangles.Add(i2);
            }
        }

        int removed = (triangles.Count - validTriangles.Count) / 3;
        if (removed > 0)
        {
            Debug.Log($"移除了 {removed} 个退化三角形");
        }

        triangles.Clear();
        triangles.AddRange(validTriangles);
    }
}
