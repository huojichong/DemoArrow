using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 简化版 CSG 实现 - 适用于立方体的布尔合并
/// 基于 BSP 树的思想，但简化为只处理轴对齐的立方体
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

        // 有交集，进行简化的 CSG 合并
        return UnionWithIntersection(mf1, mf2, obj1.transform, obj2.transform, bounds1, bounds2);
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

    private static Mesh UnionWithIntersection(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2, Bounds b1, Bounds b2)
    {
        // 简化版：移除内部面
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // 获取交集区域
        Bounds intersection = GetIntersection(b1, b2);

        // 处理第一个 mesh
        AddMeshWithCulling(mf1.sharedMesh, t1, intersection, vertices, triangles, normals, true);

        // 处理第二个 mesh
        AddMeshWithCulling(mf2.sharedMesh, t2, intersection, vertices, triangles, normals, false);

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

    private static void AddMeshWithCulling(Mesh mesh, Transform transform, Bounds cullBounds,
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

            // 如果三角形在交集区域内，跳过（剔除内部面）
            if (cullBounds.Contains(triCenter))
            {
                // 简化判断：如果三角形中心在交集内，检查法线方向
                Vector3 normal = transform.TransformDirection(meshNormals[i0]);

                // 如果法线指向内部，跳过这个三角形
                if (IsInternalFace(triCenter, normal, cullBounds, isFirst))
                {
                    continue;
                }
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

    private static bool IsInternalFace(Vector3 faceCenter, Vector3 normal, Bounds intersection, bool isFirst)
    {
        // 简化判断：如果法线指向交集中心，则认为是内部面
        Vector3 toCenter = intersection.center - faceCenter;
        float dot = Vector3.Dot(normal, toCenter.normalized);

        // 如果法线指向交集内部，则是内部面
        return dot > 0.1f;
    }
}
