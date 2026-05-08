using UnityEngine;

/// <summary>
/// Unity 原生 Mesh 合并器
/// 使用 Unity 内置的 Mesh.CombineMeshes 方法
/// 不处理交集，保留所有面（性能最优）
/// </summary>
public static class UnityNativeMeshCombiner
{
    /// <summary>
    /// 合并两个游戏对象的 Mesh
    /// </summary>
    /// <param name="obj1">第一个对象</param>
    /// <param name="obj2">第二个对象</param>
    /// <returns>合并后的 Mesh</returns>
    public static Mesh Combine(GameObject obj1, GameObject obj2)
    {
        MeshFilter mf1 = obj1.GetComponent<MeshFilter>();
        MeshFilter mf2 = obj2.GetComponent<MeshFilter>();

        if (mf1 == null || mf2 == null)
        {
            Debug.LogError("对象缺少 MeshFilter 组件");
            return null;
        }

        return Combine(mf1, mf2, obj1.transform, obj2.transform);
    }

    /// <summary>
    /// 合并多个游戏对象的 Mesh
    /// </summary>
    /// <param name="objects">要合并的对象数组</param>
    /// <returns>合并后的 Mesh</returns>
    public static Mesh Combine(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0)
        {
            Debug.LogError("没有提供要合并的对象");
            return null;
        }

        CombineInstance[] combine = new CombineInstance[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            MeshFilter mf = objects[i].GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"对象 {objects[i].name} 缺少 MeshFilter 或 Mesh");
                continue;
            }

            combine[i].mesh = mf.sharedMesh;
            combine[i].transform = objects[i].transform.localToWorldMatrix;
        }

        return CombineMeshes(combine);
    }

    /// <summary>
    /// 合并两个 MeshFilter
    /// </summary>
    private static Mesh Combine(MeshFilter mf1, MeshFilter mf2, Transform t1, Transform t2)
    {
        CombineInstance[] combine = new CombineInstance[2];

        combine[0].mesh = mf1.sharedMesh;
        combine[0].transform = t1.localToWorldMatrix;

        combine[1].mesh = mf2.sharedMesh;
        combine[1].transform = t2.localToWorldMatrix;

        return CombineMeshes(combine);
    }

    /// <summary>
    /// 执行 Mesh 合并
    /// </summary>
    private static Mesh CombineMeshes(CombineInstance[] combine)
    {
        Mesh result = new Mesh();
        result.CombineMeshes(combine, true, true);
        result.RecalculateNormals();
        result.RecalculateBounds();
        result.Optimize();

        return result;
    }
}
