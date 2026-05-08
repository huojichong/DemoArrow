using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
#endif

/// <summary>
/// 使用 ProBuilder 进行 CSG 布尔运算
/// 需要安装 ProBuilder 包：Window > Package Manager > ProBuilder
/// </summary>
public static class ProBuilderCSG
{
    /// <summary>
    /// 布尔并集运算
    /// </summary>
    public static Mesh Union(GameObject obj1, GameObject obj2)
    {
        return null;
// #if UNITY_EDITOR
//         try
//         {
//             Debug.Log("=== 使用 ProBuilder 进行布尔运算 ===");
//
//             // 转换为 ProBuilderMesh
//             ProBuilderMesh pb1 = ConvertToProBuilderMesh(obj1);
//             ProBuilderMesh pb2 = ConvertToProBuilderMesh(obj2);
//
//             if (pb1 == null || pb2 == null)
//             {
//                 Debug.LogError("转换为 ProBuilderMesh 失败");
//                 return null;
//             }
//
//             Debug.Log($"PB1: {pb1.vertexCount} 顶点, {pb1.faceCount} 面");
//             Debug.Log($"PB2: {pb2.vertexCount} 顶点, {pb2.faceCount} 面");
//
//             // 使用 ProBuilder 的 CombineMeshes 进行合并
//             var combined = CombineMeshes.Combine(new ProBuilderMesh[] { pb1, pb2 }, pb1);
//
//             if (combined == null || combined.Count == 0)
//             {
//                 Debug.LogError("ProBuilder 合并失败");
//                 return null;
//             }
//
//             ProBuilderMesh result = combined[0];
//             result.ToMesh();
//             result.Refresh();
//
//             Debug.Log($"结果: {result.vertexCount} 顶点, {result.faceCount} 面");
//
//             // 转换回普通 Mesh
//             Mesh mesh = ConvertToUnityMesh(result);
//
//             // 清理临时对象
//             Object.DestroyImmediate(pb1.gameObject);
//             Object.DestroyImmediate(pb2.gameObject);
//
//             Debug.Log("=== ProBuilder 布尔运算完成 ===");
//
//             return mesh;
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"ProBuilder 运算失败: {e.Message}\n{e.StackTrace}");
//             return null;
//         }
// #else
//         Debug.LogWarning("ProBuilder 功能仅在编辑器模式下可用");
//         return null;
// #endif
    }

// #if UNITY_EDITOR
//     /// <summary>
//     /// 将普通 GameObject 转换为 ProBuilderMesh
//     /// </summary>
//     private static ProBuilderMesh ConvertToProBuilderMesh(GameObject obj)
//     {
//         MeshFilter mf = obj.GetComponent<MeshFilter>();
//         if (mf == null || mf.sharedMesh == null)
//         {
//             Debug.LogError($"对象 {obj.name} 缺少 MeshFilter 或 Mesh");
//             return null;
//         }
//
//         // 创建临时对象
//         GameObject tempObj = new GameObject($"PB_{obj.name}");
//         tempObj.transform.position = obj.transform.position;
//         tempObj.transform.rotation = obj.transform.rotation;
//         tempObj.transform.localScale = obj.transform.localScale;
//
//         // 复制 Mesh
//         MeshFilter tempMF = tempObj.AddComponent<MeshFilter>();
//         tempMF.sharedMesh = Object.Instantiate(mf.sharedMesh);
//
//         // 转换为 ProBuilderMesh
//         ProBuilderMesh pbMesh = ProBuilderMesh.Create(tempObj);
//         pbMesh.Refresh();
//
//         return pbMesh;
//     }
//
//     /// <summary>
//     /// 将 ProBuilderMesh 转换为普通 Unity Mesh
//     /// </summary>
//     private static Mesh ConvertToUnityMesh(ProBuilderMesh pbMesh)
//     {
//         if (pbMesh == null)
//             return null;
//
//         // 导出为普通 Mesh
//         Mesh mesh = pbMesh.mesh;
//         Mesh result = Object.Instantiate(mesh);
//
//         result.RecalculateNormals();
//         result.RecalculateBounds();
//
//         return result;
//     }
// #endif
}
