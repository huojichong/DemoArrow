using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
#endif

/// <summary>
/// Mesh 保存工具
/// 提供保存 Mesh 为资源文件、Prefab 和 FBX 的功能
/// </summary>
public static class MeshSaver
{
    /// <summary>
    /// 保存 Mesh 为 .asset 资源文件
    /// </summary>
    /// <param name="mesh">要保存的 Mesh</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveMeshAsset(Mesh mesh, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
#if UNITY_EDITOR
        if (mesh == null)
        {
            Debug.LogError("Mesh 为空，无法保存");
            return false;
        }

        // 生成文件名
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"Mesh_{timestamp}";
        }

        string fullPath = $"{savePath}/{fileName}.asset";

        // 确保目录存在
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        // 创建 Mesh 副本
        Mesh meshToSave = Object.Instantiate(mesh);
        meshToSave.name = fileName;

        // 保存为资源文件
        AssetDatabase.CreateAsset(meshToSave, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Mesh 已保存到: {fullPath}");
        Debug.Log($"  顶点数: {meshToSave.vertexCount}");
        Debug.Log($"  三角形数: {meshToSave.triangles.Length / 3}");

        // 在 Project 窗口中高亮显示
        EditorGUIUtility.PingObject(meshToSave);

        return true;
#else
        Debug.LogWarning("保存 Mesh 功能仅在编辑器模式下可用");
        return false;
#endif
    }

    /// <summary>
    /// 保存游戏对象为 Prefab（包含 Mesh 和材质）
    /// </summary>
    /// <param name="gameObject">要保存的游戏对象</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveAsPrefab(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
#if UNITY_EDITOR
        if (gameObject == null)
        {
            Debug.LogError("游戏对象为空，无法保存");
            return false;
        }

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("游戏对象没有有效的 Mesh");
            return false;
        }

        // 生成文件名
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"Prefab_{timestamp}";
        }

        string meshFileName = $"{fileName}_Mesh";
        string meshPath = $"{savePath}/{meshFileName}.asset";
        string prefabPath = $"{savePath}/{fileName}.prefab";

        // 确保目录存在
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        // 保存 Mesh
        Mesh meshToSave = Object.Instantiate(meshFilter.sharedMesh);
        meshToSave.name = meshFileName;
        AssetDatabase.CreateAsset(meshToSave, meshPath);

        // 创建 Prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Prefab 已保存到: {prefabPath}");
        Debug.Log($"✓ Mesh 已保存到: {meshPath}");

        // 在 Project 窗口中高亮显示
        EditorGUIUtility.PingObject(prefab);

        return true;
#else
        Debug.LogWarning("保存 Prefab 功能仅在编辑器模式下可用");
        return false;
#endif
    }

    /// <summary>
    /// 从游戏对象保存 Mesh
    /// </summary>
    /// <param name="gameObject">包含 MeshFilter 的游戏对象</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveMeshFromGameObject(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
        if (gameObject == null)
        {
            Debug.LogError("游戏对象为空");
            return false;
        }

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("游戏对象没有有效的 Mesh");
            return false;
        }

        return SaveMeshAsset(meshFilter.sharedMesh, fileName, savePath);
    }

    /// <summary>
    /// 导出游戏对象为 FBX 格式
    /// 需要安装 FBX Exporter 包
    /// </summary>
    /// <param name="gameObject">要导出的游戏对象</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否导出成功</returns>
    public static bool ExportAsFBX(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
#if UNITY_EDITOR
        if (gameObject == null)
        {
            Debug.LogError("游戏对象为空，无法导出");
            return false;
        }

        // 生成文件名
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"Mesh_{timestamp}";
        }

        string fullPath = $"{savePath}/{fileName}.fbx";

        // 确保目录存在
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        try
        {
            // 使用 FBX Exporter 导出
            string exportedPath = ModelExporter.ExportObject(fullPath, gameObject);

            if (!string.IsNullOrEmpty(exportedPath))
            {
                AssetDatabase.Refresh();
                Debug.Log($"✓ FBX 已导出到: {exportedPath}");

                // 在 Project 窗口中高亮显示
                Object fbxAsset = AssetDatabase.LoadAssetAtPath<Object>(exportedPath);
                if (fbxAsset != null)
                {
                    EditorGUIUtility.PingObject(fbxAsset);
                }

                return true;
            }
            else
            {
                Debug.LogError("FBX 导出失败");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FBX 导出时出错: {e.Message}");
            Debug.LogWarning("提示：需要安装 FBX Exporter 包");
            Debug.LogWarning("安装方法：Window > Package Manager > 搜索 'FBX Exporter' > Install");
            return false;
        }
#else
        Debug.LogWarning("导出 FBX 功能仅在编辑器模式下可用");
        return false;
#endif
    }

    /// <summary>
    /// 导出多个游戏对象为单个 FBX 文件
    /// </summary>
    /// <param name="gameObjects">要导出的游戏对象数组</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否导出成功</returns>
    public static bool ExportMultipleAsFBX(GameObject[] gameObjects, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
#if UNITY_EDITOR
        if (gameObjects == null || gameObjects.Length == 0)
        {
            Debug.LogError("没有提供要导出的对象");
            return false;
        }

        // 生成文件名
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"MergedMeshes_{timestamp}";
        }

        string fullPath = $"{savePath}/{fileName}.fbx";

        // 确保目录存在
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        try
        {
            // 使用 FBX Exporter 导出多个对象
            string exportedPath = ModelExporter.ExportObjects(fullPath, gameObjects);

            if (!string.IsNullOrEmpty(exportedPath))
            {
                AssetDatabase.Refresh();
                Debug.Log($"✓ FBX 已导出到: {exportedPath}");
                Debug.Log($"  导出了 {gameObjects.Length} 个对象");

                // 在 Project 窗口中高亮显示
                Object fbxAsset = AssetDatabase.LoadAssetAtPath<Object>(exportedPath);
                if (fbxAsset != null)
                {
                    EditorGUIUtility.PingObject(fbxAsset);
                }

                return true;
            }
            else
            {
                Debug.LogError("FBX 导出失败");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FBX 导出时出错: {e.Message}");
            Debug.LogWarning("提示：需要安装 FBX Exporter 包");
            Debug.LogWarning("安装方法：Window > Package Manager > 搜索 'FBX Exporter' > Install");
            return false;
        }
#else
        Debug.LogWarning("导出 FBX 功能仅在编辑器模式下可用");
        return false;
#endif
    }

    /// <summary>
    /// 导出游戏对象为 OBJ 格式（Blender 完全兼容）
    /// </summary>
    /// <param name="gameObject">要导出的游戏对象</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="savePath">保存路径（相对于 Assets）</param>
    /// <returns>是否导出成功</returns>
    public static bool ExportAsOBJ(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
    {
#if UNITY_EDITOR
        if (gameObject == null)
        {
            Debug.LogError("游戏对象为空，无法导出");
            return false;
        }

        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("游戏对象没有有效的 Mesh");
            return false;
        }

        // 生成文件名
        if (string.IsNullOrEmpty(fileName))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileName = $"Mesh_{timestamp}";
        }

        string fullPath = $"{savePath}/{fileName}.obj";

        // 确保目录存在
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }

        try
        {
            Mesh mesh = meshFilter.sharedMesh;
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();

            // 生成 OBJ 文件内容
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 头部信息
            sb.AppendLine("# Exported from Unity");
            sb.AppendLine($"# {System.DateTime.Now}");
            sb.AppendLine($"# Vertices: {mesh.vertexCount}");
            sb.AppendLine($"# Triangles: {mesh.triangles.Length / 3}");
            sb.AppendLine();

            // 如果有材质，生成 MTL 文件引用
            if (renderer != null && renderer.sharedMaterial != null)
            {
                string mtlFileName = $"{fileName}.mtl";
                sb.AppendLine($"mtllib {mtlFileName}");
                sb.AppendLine();

                // 生成 MTL 文件
                GenerateMTLFile($"{savePath}/{mtlFileName}", renderer.sharedMaterial);
            }

            // 顶点坐标
            Vector3[] vertices = mesh.vertices;
            foreach (Vector3 v in vertices)
            {
                sb.AppendLine($"v {v.x} {v.y} {v.z}");
            }
            sb.AppendLine();

            // 法线
            Vector3[] normals = mesh.normals;
            if (normals != null && normals.Length > 0)
            {
                foreach (Vector3 n in normals)
                {
                    sb.AppendLine($"vn {n.x} {n.y} {n.z}");
                }
                sb.AppendLine();
            }

            // UV 坐标
            Vector2[] uvs = mesh.uv;
            if (uvs != null && uvs.Length > 0)
            {
                foreach (Vector2 uv in uvs)
                {
                    sb.AppendLine($"vt {uv.x} {uv.y}");
                }
                sb.AppendLine();
            }

            // 材质组
            if (renderer != null && renderer.sharedMaterial != null)
            {
                sb.AppendLine($"usemtl {renderer.sharedMaterial.name}");
            }

            // 面（三角形）
            int[] triangles = mesh.triangles;
            bool hasUVs = uvs != null && uvs.Length > 0;
            bool hasNormals = normals != null && normals.Length > 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i] + 1;     // OBJ 索引从 1 开始
                int i2 = triangles[i + 1] + 1;
                int i3 = triangles[i + 2] + 1;

                if (hasUVs && hasNormals)
                {
                    sb.AppendLine($"f {i1}/{i1}/{i1} {i2}/{i2}/{i2} {i3}/{i3}/{i3}");
                }
                else if (hasNormals)
                {
                    sb.AppendLine($"f {i1}//{i1} {i2}//{i2} {i3}//{i3}");
                }
                else if (hasUVs)
                {
                    sb.AppendLine($"f {i1}/{i1} {i2}/{i2} {i3}/{i3}");
                }
                else
                {
                    sb.AppendLine($"f {i1} {i2} {i3}");
                }
            }

            // 写入文件
            System.IO.File.WriteAllText(fullPath, sb.ToString());

            AssetDatabase.Refresh();

            Debug.Log($"✓ OBJ 已导出到: {fullPath}");
            Debug.Log($"  顶点数: {mesh.vertexCount}");
            Debug.Log($"  三角形数: {triangles.Length / 3}");
            Debug.Log("  提示: OBJ 格式可直接导入 Blender");

            // 在 Project 窗口中高亮显示
            Object objAsset = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
            if (objAsset != null)
            {
                EditorGUIUtility.PingObject(objAsset);
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OBJ 导出时出错: {e.Message}");
            return false;
        }
#else
        Debug.LogWarning("导出 OBJ 功能仅在编辑器模式下可用");
        return false;
#endif
    }

    /// <summary>
    /// 生成 MTL 材质文件
    /// </summary>
    private static void GenerateMTLFile(string path, Material material)
    {
#if UNITY_EDITOR
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("# Material file exported from Unity");
        sb.AppendLine();
        sb.AppendLine($"newmtl {material.name}");

        // 获取颜色
        if (material.HasProperty("_Color"))
        {
            Color color = material.GetColor("_Color");
            sb.AppendLine($"Kd {color.r} {color.g} {color.b}");
            sb.AppendLine($"d {color.a}");
        }

        // 获取光滑度
        if (material.HasProperty("_Glossiness"))
        {
            float glossiness = material.GetFloat("_Glossiness");
            sb.AppendLine($"Ns {glossiness * 100}");
        }

        System.IO.File.WriteAllText(path, sb.ToString());
#endif
    }
}
