using UnityEngine;

/// <summary>
/// CSG 立方体合并演示 - 集成所有合并方案
/// 只包含测试逻辑和 UI，具体实现在独立的类中
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
        InitializeMaterials();
    }

    /// <summary>
    /// 初始化材质
    /// </summary>
    private void InitializeMaterials()
    {
        if (cube1Material == null)
        {
            cube1Material = GlassMaterialCreator.CreateGlassMaterial(
                new Color(1f, 0.3f, 0.3f, 0.4f), 0.9f, 0.1f
            );
        }

        if (cube2Material == null)
        {
            cube2Material = GlassMaterialCreator.CreateGlassMaterial(
                new Color(0.3f, 0.3f, 1f, 0.4f), 0.9f, 0.1f
            );
        }

        if (mergedMaterial == null)
        {
            mergedMaterial = GlassMaterialCreator.CreateGlassMaterial(
                new Color(0.3f, 1f, 0.3f, 0.5f), 0.95f, 0.1f
            );
        }
    }

    /// <summary>
    /// 创建两个有交集的立方体
    /// </summary>
    public void CreateCubes()
    {
        ClearAll();

        cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.name = "Cube1";
        cube1.transform.position = cube1Position;
        cube1.transform.localScale = cube1Size;
        cube1.GetComponent<MeshRenderer>().material = cube1Material;

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
        if (!ValidateCubes()) return;

        Debug.Log("开始 SimpleCSG 合并...");

        try
        {
            Mesh mergedMesh = SimpleCSG.Union(cube1, cube2);

            if (mergedMesh != null)
            {
                CreateMergedObject(mergedMesh, "MergedCube_SimpleCSG");
                Debug.Log("✓ SimpleCSG 合并成功！");
                LogMeshInfo(mergedMesh);
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
    /// 方案2：使用 Unity 原生合并（不去除交集）
    /// </summary>
    public void MergeWithUnityNative()
    {
        if (!ValidateCubes()) return;

        Debug.Log("开始 Unity 原生合并...");

        try
        {
            Mesh mergedMesh = UnityNativeMeshCombiner.Combine(cube1, cube2);

            if (mergedMesh != null)
            {
                CreateMergedObject(mergedMesh, "MergedCube_UnityNative");
                Debug.Log("✓ Unity 原生合并成功！");
                LogMeshInfo(mergedMesh);
            }
            else
            {
                Debug.LogError("Unity 原生合并失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unity 原生合并时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 方案3：使用 AdvancedCSG 合并（完全去除内部顶点）
    /// </summary>
    public void MergeWithAdvancedCSG()
    {
        if (!ValidateCubes()) return;

        Debug.Log("开始 AdvancedCSG 合并...");

        try
        {
            Mesh mergedMesh = AdvancedCSG.Union(cube1, cube2);

            if (mergedMesh != null)
            {
                CreateMergedObject(mergedMesh, "MergedCube_AdvancedCSG");
                Debug.Log("✓ AdvancedCSG 合并成功！");
                LogMeshInfo(mergedMesh);
            }
            else
            {
                Debug.LogError("AdvancedCSG 合并失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AdvancedCSG 合并时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 方案4：使用 TrueCSG 合并（真正的布尔运算）
    /// </summary>
    public void MergeWithTrueCSG()
    {
        if (!ValidateCubes()) return;

        Debug.Log("开始 TrueCSG 布尔运算...");

        try
        {
            Mesh mergedMesh = TrueCSG.Union(cube1, cube2);

            if (mergedMesh != null)
            {
                CreateMergedObject(mergedMesh, "MergedCube_TrueCSG");
                Debug.Log("✓ TrueCSG 布尔运算成功！");
                LogMeshInfo(mergedMesh);
            }
            else
            {
                Debug.LogError("TrueCSG 布尔运算失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TrueCSG 布尔运算时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 方案5：使用 ProBuilder 合并（Unity 官方工具）
    /// </summary>
    public void MergeWithProBuilder()
    {
        if (!ValidateCubes()) return;

        Debug.Log("开始 ProBuilder 布尔运算...");

        try
        {
            Mesh mergedMesh = ProBuilderCSG.Union(cube1, cube2);
            
            if (mergedMesh != null)
            {
                CreateMergedObject(mergedMesh, "MergedCube_ProBuilder");
                Debug.Log("✓ ProBuilder 布尔运算成功！");
                LogMeshInfo(mergedMesh);
            }
            else
            {
                Debug.LogError("ProBuilder 布尔运算失败！");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ProBuilder 布尔运算时出错: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 保存合并后的 Mesh
    /// </summary>
    public void SaveMergedMesh()
    {
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以保存！请先合并立方体");
            return;
        }

        MeshSaver.SaveMeshFromGameObject(mergedObject);
    }

    /// <summary>
    /// 保存为 Prefab
    /// </summary>
    public void SaveAsPrefab()
    {
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以保存！请先合并立方体");
            return;
        }

        MeshSaver.SaveAsPrefab(mergedObject);
    }

    /// <summary>
    /// 导出为 FBX 格式
    /// </summary>
    public void ExportAsFBX()
    {
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以导出！请先合并立方体");
            return;
        }

        MeshSaver.ExportAsFBX(mergedObject);
    }

    /// <summary>
    /// 导出为 OBJ 格式（Blender 兼容）
    /// </summary>
    public void ExportAsOBJ()
    {
        if (mergedObject == null)
        {
            Debug.LogWarning("没有合并后的对象可以导出！请先合并立方体");
            return;
        }

        MeshSaver.ExportAsOBJ(mergedObject);
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

    #region 辅助方法

    /// <summary>
    /// 验证立方体是否存在
    /// </summary>
    private bool ValidateCubes()
    {
        if (cube1 == null || cube2 == null)
        {
            Debug.LogWarning("请先创建立方体！按 C 键创建");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 创建合并后的对象
    /// </summary>
    private void CreateMergedObject(Mesh mesh, string name)
    {
        mergedObject = new GameObject(name);
        MeshFilter mf = mergedObject.AddComponent<MeshFilter>();
        MeshRenderer mr = mergedObject.AddComponent<MeshRenderer>();

        mf.mesh = mesh;
        mr.material = mergedMaterial;

        cube1.SetActive(false);
        cube2.SetActive(false);
    }

    /// <summary>
    /// 输出 Mesh 信息
    /// </summary>
    private void LogMeshInfo(Mesh mesh)
    {
        Debug.Log($"  顶点数: {mesh.vertexCount}");
        Debug.Log($"  三角形数: {mesh.triangles.Length / 3}");
    }

    #endregion

    #region 输入处理

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CreateCubes();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            MergeWithSimpleCSG();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            MergeWithUnityNative();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            MergeWithAdvancedCSG();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            MergeWithTrueCSG();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            MergeWithProBuilder();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SaveMergedMesh();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SaveAsPrefab();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ExportAsFBX();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ExportAsOBJ();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            ClearAll();
        }
    }

    #endregion

    #region GUI

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 480, 800));

        DrawTitle();
        GUILayout.Space(10);

        DrawCreateButton();
        GUILayout.Space(10);

        DrawMergeButtons();
        GUILayout.Space(10);

        DrawSaveButtons();
        GUILayout.Space(10);

        DrawClearButton();
        GUILayout.Space(10);

        DrawInfo();

        GUILayout.EndArea();
    }

    private void DrawTitle()
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label("CSG 立方体合并演示 - 集成版", titleStyle);
    }

    private void DrawCreateButton()
    {
        if (GUILayout.Button("创建立方体 (C)", GUILayout.Height(35)))
        {
            CreateCubes();
        }
    }

    private void DrawMergeButtons()
    {
        GUILayout.Label("合并方案：", GUI.skin.box);

        if (GUILayout.Button("方案1: SimpleCSG 合并 - 去除内部面 (1)", GUILayout.Height(35)))
        {
            MergeWithSimpleCSG();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("方案2: Unity 原生合并 - 保留所有面 (2)", GUILayout.Height(35)))
        {
            MergeWithUnityNative();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("方案3: AdvancedCSG - 完全去除内部 (3)", GUILayout.Height(35)))
        {
            MergeWithAdvancedCSG();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("方案4: TrueCSG - 真正布尔运算 (4)", GUILayout.Height(35)))
        {
            MergeWithTrueCSG();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("方案5: ProBuilder - Unity官方 (5)", GUILayout.Height(35)))
        {
            MergeWithProBuilder();
        }
    }

    private void DrawSaveButtons()
    {
        GUILayout.Label("保存选项：", GUI.skin.box);

        if (GUILayout.Button("保存 Mesh (M)", GUILayout.Height(35)))
        {
            SaveMergedMesh();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("保存为 Prefab (P)", GUILayout.Height(35)))
        {
            SaveAsPrefab();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("导出为 OBJ - Blender (O)", GUILayout.Height(35)))
        {
            ExportAsOBJ();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("导出为 FBX (F)", GUILayout.Height(35)))
        {
            ExportAsFBX();
        }
    }

    private void DrawClearButton()
    {
        if (GUILayout.Button("清理 (X)", GUILayout.Height(35)))
        {
            ClearAll();
        }
    }

    private void DrawInfo()
    {
        GUIStyle infoStyle = new GUIStyle(GUI.skin.box);
        infoStyle.wordWrap = true;
        infoStyle.padding = new RectOffset(10, 10, 10, 100);

        GUILayout.Label("说明：\n" +
            "• 方案1: 尝试去除交集内部面\n" +
            "• 方案2: 保留所有面（最快）\n" +
            "• 方案3: 完全去除内部顶点\n" +
            "  和面（推荐）\n" +
            "• 保存的文件在\n" +
            "  Assets/SavedMeshes/", infoStyle);
    }

    #endregion
}
