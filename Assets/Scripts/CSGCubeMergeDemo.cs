using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CSG 立方体合并演示 - 集成所有合并方案
/// 1. 简单合并（Unity 原生）
/// 2. SimpleCSG 合并（简化 CSG 算法）
/// </summary>
public class CSGCubeMergeDemo : MonoBehaviour
{
    [Header("立方体设置")]
    [Tooltip("第一个立方体的大小")]
    public Vector3 cube1Size = new Vector3(2f, 2f, 2f);

    [Tooltip("第一个立方体的位置")]
    public Vector3 cube1Position = new Vector3(0f, 0f, 0f);

    [Tooltip("第二个立方体的大小")]
    public Vector3 cube2Size = new Vector3(2f, 2f, 2f);

    [Tooltip("第二个立方体的位置（与第一个有交集）")]
    public Vector3 cube2Position = new Vector3(1f, 0f, 0f);

    [Header("材质设置")]
    public Material cube1Material;
    public Material cube2Material;
    public Material mergedMaterial;

    private GameObject cube1;
    private GameObject cube2;
    private GameObject mergedObject;

    void Start()
    {
        // 初始化材质 - 使用透明玻璃材质
        if (cube1Material == null)
        {
            cube1Material = GlassMaterialCreator.CreateGlassMaterial(
                new Color(1f, 0.3f, 0.3f, 0.4f), // 红色玻璃
                0.9f, 0.1f
            );
        }

        if (cube2Material == null)
        {
            cube2Material = GlassMaterialCreator.CreateGlassMaterial(
                new Color(0.3f, 0.3f, 1f, 0.4f), // 蓝色玻璃
                0.9f, 0.1f
            );
        }

        if (mergedMaterial == null)
        {
            mergedMaterial = GlassMaterialCreator.CreateGlassMaterial(
                new Color(0.3f, 1f, 0.3f, 0.5f), // 绿色玻璃
                0.95f, 0.1f
            );
        }
    }

    /// <summary>
    /// 创建两个有交集的立方体
    /// </summary>
    public void CreateCubes()
    {
        ClearAll();

        // 创建第一个立方体
        cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.name = "Cube1";
        cube1.transform.position = cube1Position;
        cube1.transform.localScale = cube1Size;
        cube1.GetComponent<MeshRenderer>().material = cube1Material;

        // 创建第二个立方体
        cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube2.name = "Cube2";
        cube2.transform.position = cube2Position;
        cube2.transform.localScale = cube2Size;
        cube2.GetComponent<MeshRenderer>().material = cube2Material;

        Debug.Log("已创建两个有交集的立方体");
        Debug.Log($"Cube1: 位置 {cube1Position}, 大小 {cube1Size}");
        Debug.Log($"Cube2: 位置 {cube2Position}, 大小 {cube2Size}");
    }

    /// <summary>
    /// 方案1：使用 SimpleCSG 合并（去除交集内部面）
    /// </summary>
    public void MergeWithSimpleCSG()
    {
        if (cube1 == null || cube2 == null)
        {
            Debug.LogWarning("请先创建立方体！按 C 键创建");
            return;
        }

        Debug.Log("开始 SimpleCSG 合并...");

        try
        {
            // 使用简化版 CSG 进行合并
            Mesh mergedMesh = SimpleCSG.Union(cube1, cube2);

            if (mergedMesh != null)
            {
                // 创建合并后的对象
                mergedObject = new GameObject("MergedCube_SimpleCSG");
                MeshFilter mf = mergedObject.AddComponent<MeshFilter>();
                MeshRenderer mr = mergedObject.AddComponent<MeshRenderer>();

                mf.mesh = mergedMesh;
                mr.material = mergedMaterial;

                // 隐藏原始立方体
                cube1.SetActive(false);
                cube2.SetActive(false);

                Debug.Log("✓ SimpleCSG 合并成功！");
                Debug.Log($"  顶点数: {mergedMesh.vertexCount}");
                Debug.Log($"  三角形数: {mergedMesh.triangles.Length / 3}");
            }
            else
            {
                Debug.LogError("SimpleCSG 合并失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleCSG 合并时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 方案2：简单合并（Unity 原生，不去除交集）
    /// </summary>
    public void SimpleMerge()
    {
        if (cube1 == null || cube2 == null)
        {
            Debug.LogWarning("请先创建立方体！按 C 键创建");
            return;
        }

        Debug.Log("开始简单合并...");

        MeshFilter mf1 = cube1.GetComponent<MeshFilter>();
        MeshFilter mf2 = cube2.GetComponent<MeshFilter>();

        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = mf1.sharedMesh;
        combine[0].transform = cube1.transform.localToWorldMatrix;
        combine[1].mesh = mf2.sharedMesh;
        combine[1].transform = cube2.transform.localToWorldMatrix;

        mergedObject = new GameObject("MergedCube_Simple");
        MeshFilter mergedMF = mergedObject.AddComponent<MeshFilter>();
        MeshRenderer mergedMR = mergedObject.AddComponent<MeshRenderer>();

        Mesh mergedMesh = new Mesh();
        mergedMesh.CombineMeshes(combine, true, true);
        mergedMesh.RecalculateNormals();
        mergedMesh.RecalculateBounds();
        mergedMesh.Optimize();

        mergedMF.mesh = mergedMesh;
        mergedMR.material = mergedMaterial;

        cube1.SetActive(false);
        cube2.SetActive(false);

        Debug.Log("✓ 简单合并成功！");
        Debug.Log($"  顶点数: {mergedMesh.vertexCount}");
        Debug.Log($"  三角形数: {mergedMesh.triangles.Length / 3}");
    }

    /// <summary>
    /// 保存合并后的 Mesh 为资源文件
    /// </summary>
    public void SaveMergedMesh()
    {
#if UNITY_EDITOR
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以保存！请先合并立方体");
            return;
        }

        MeshFilter meshFilter = mergedObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("合并对象没有有效的 Mesh！");
            return;
        }

        // 生成文件名（带时间戳）
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"MergedMesh_{timestamp}.asset";
        string savePath = $"Assets/SavedMeshes/{fileName}";

        // 确保目录存在
        if (!System.IO.Directory.Exists("Assets/SavedMeshes"))
        {
            System.IO.Directory.CreateDirectory("Assets/SavedMeshes");
        }

        // 创建 Mesh 资源的副本
        Mesh meshToSave = Object.Instantiate(meshFilter.sharedMesh);
        meshToSave.name = $"MergedMesh_{timestamp}";

        // 保存为资源文件
        AssetDatabase.CreateAsset(meshToSave, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Mesh 已保存到: {savePath}");
        Debug.Log($"  顶点数: {meshToSave.vertexCount}");
        Debug.Log($"  三角形数: {meshToSave.triangles.Length / 3}");

        // 在 Project 窗口中高亮显示
        EditorGUIUtility.PingObject(meshToSave);
#else
        Debug.LogWarning("保存 Mesh 功能仅在编辑器模式下可用");
#endif
    }

    /// <summary>
    /// 保存合并后的 Mesh 和材质为 Prefab
    /// </summary>
    public void SaveAsPrefab()
    {
#if UNITY_EDITOR
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以保存！请先合并立方体");
            return;
        }

        // 生成文件名（带时间戳）
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string meshFileName = $"MergedMesh_{timestamp}.asset";
        string prefabFileName = $"MergedCube_{timestamp}.prefab";
        string meshPath = $"Assets/SavedMeshes/{meshFileName}";
        string prefabPath = $"Assets/SavedMeshes/{prefabFileName}";

        // 确保目录存在
        if (!System.IO.Directory.Exists("Assets/SavedMeshes"))
        {
            System.IO.Directory.CreateDirectory("Assets/SavedMeshes");
        }

        // 保存 Mesh
        MeshFilter meshFilter = mergedObject.GetComponent<MeshFilter>();
        Mesh meshToSave = Object.Instantiate(meshFilter.sharedMesh);
        meshToSave.name = $"MergedMesh_{timestamp}";
        AssetDatabase.CreateAsset(meshToSave, meshPath);

        // 创建 Prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(mergedObject, prefabPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✓ Prefab 已保存到: {prefabPath}");
        Debug.Log($"✓ Mesh 已保存到: {meshPath}");

        // 在 Project 窗口中高亮显示
        EditorGUIUtility.PingObject(prefab);
#else
        Debug.LogWarning("保存 Prefab 功能仅在编辑器模式下可用");
#endif
    }

    /// <summary>
    /// 清理所有对象
    /// </summary>
    public void ClearAll()
    {
        if (cube1 != null) DestroyImmediate(cube1);
        if (cube2 != null) DestroyImmediate(cube2);
        if (mergedObject != null) DestroyImmediate(mergedObject);

        cube1 = null;
        cube2 = null;
        mergedObject = null;

        Debug.Log("已清理所有对象");
    }

    void Update()
    {
        // C 键：创建立方体
        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateCubes();
        }

        // 1 键：SimpleCSG 合并
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MergeWithSimpleCSG();
        }

        // 2 键：简单合并
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SimpleMerge();
        }

        // M 键：保存 Mesh
        if (Input.GetKeyDown(KeyCode.M))
        {
            SaveMergedMesh();
        }

        // P 键：保存为 Prefab
        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveAsPrefab();
        }

        // X 键：清理
        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearAll();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 450, 400));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label("CSG 立方体合并演示 - 集成版", titleStyle);

        GUILayout.Space(10);

        // 创建立方体
        if (GUILayout.Button("创建立方体 (C)", GUILayout.Height(35)))
        {
            CreateCubes();
        }

        GUILayout.Space(10);
        GUILayout.Label("合并方案：", GUI.skin.box);

        // 方案1：SimpleCSG
        if (GUILayout.Button("方案1: SimpleCSG 合并 - 去除内部面 (1)", GUILayout.Height(35)))
        {
            MergeWithSimpleCSG();
        }

        GUILayout.Space(5);

        // 方案2：简单合并
        if (GUILayout.Button("方案2: 简单合并 - 保留所有面 (2)", GUILayout.Height(35)))
        {
            SimpleMerge();
        }

        GUILayout.Space(10);
        GUILayout.Label("保存选项：", GUI.skin.box);

        // 保存 Mesh
        if (GUILayout.Button("保存 Mesh (M)", GUILayout.Height(35)))
        {
            SaveMergedMesh();
        }

        GUILayout.Space(5);

        // 保存 Prefab
        if (GUILayout.Button("保存为 Prefab (P)", GUILayout.Height(35)))
        {
            SaveAsPrefab();
        }

        GUILayout.Space(10);

        // 清理
        if (GUILayout.Button("清理 (X)", GUILayout.Height(35)))
        {
            ClearAll();
        }

        GUILayout.Space(10);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.box);
        infoStyle.wordWrap = true;
        GUILayout.Label("说明：\n" +
            "• 方案1: 尝试去除交集内部面（适合移动端）\n" +
            "• 方案2: 保留所有面，有重叠（最快）\n" +
            "• 保存的文件在 Assets/SavedMeshes/", infoStyle);

        GUILayout.EndArea();
    }
}
