# Unity CSG 立方体合并系统

## 📁 代码结构

### 核心实现类（无 UI，纯逻辑）

1. **SimpleCSG.cs** - 简化版 CSG 算法
   - 提供 `Union()` 方法进行布尔合并
   - 尝试去除交集内部面
   - 适用于轴对齐的立方体
   - 无依赖，跨平台兼容

2. **UnityNativeMeshCombiner.cs** - Unity 原生合并器
   - 使用 Unity 内置的 `Mesh.CombineMeshes()`
   - 不处理交集，保留所有面
   - 性能最优，内存占用最小
   - 支持合并多个对象

3. **GlassMaterialCreator.cs** - 玻璃材质创建工具
   - 创建透明玻璃材质
   - 支持自定义透明度、光滑度、金属度
   - 提供 5 种预设材质

4. **MeshSaver.cs** - Mesh 保存工具
   - 保存 Mesh 为 .asset 资源文件
   - 保存游戏对象为 Prefab
   - 仅在编辑器模式下可用

### 测试演示类（包含 UI）

5. **CSGCubeMergeDemo.cs** - 主测试类
   - 集成所有合并方案
   - 提供完整的 GUI 界面
   - 只包含测试逻辑，调用独立的实现类

## 🎮 使用方法

### 快速开始

1. 在场景中创建空物体
2. 添加 `CSGCubeMergeDemo` 组件
3. 运行场景

### 操作说明

**键盘快捷键：**
- `C` - 创建两个有交集的立方体
- `1` - 方案1：SimpleCSG 合并（去除内部面）
- `2` - 方案2：Unity 原生合并（保留所有面）
- `M` - 保存 Mesh 为 .asset 文件
- `P` - 保存为 Prefab
- `X` - 清理所有对象

**GUI 按钮：**
- 左上角提供完整的按钮界面
- 所有操作都可以通过鼠标点击完成

## 📊 合并方案对比

| 方案 | 实现类 | 性能 | 内存 | 交集处理 | 移动端 |
|------|--------|------|------|---------|--------|
| **方案1: SimpleCSG** | SimpleCSG | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | 去除内部面 | ✅ 推荐 |
| **方案2: Unity 原生** | UnityNativeMeshCombiner | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 保留重叠 | ✅ 最快 |

## 💻 代码示例

### 使用 SimpleCSG 合并

```csharp
// 合并两个游戏对象
Mesh mergedMesh = SimpleCSG.Union(cube1, cube2);

// 创建合并后的对象
GameObject merged = new GameObject("Merged");
merged.AddComponent<MeshFilter>().mesh = mergedMesh;
merged.AddComponent<MeshRenderer>().material = myMaterial;
```

### 使用 Unity 原生合并

```csharp
// 合并两个对象
Mesh mergedMesh = UnityNativeMeshCombiner.Combine(cube1, cube2);

// 或合并多个对象
GameObject[] objects = { cube1, cube2, cube3 };
Mesh mergedMesh = UnityNativeMeshCombiner.Combine(objects);
```

### 创建玻璃材质

```csharp
// 自定义玻璃材质
Material glass = GlassMaterialCreator.CreateGlassMaterial(
    new Color(1f, 0.5f, 0.5f, 0.3f), // 颜色 + 透明度
    0.9f,  // 光滑度
    0.1f   // 金属度
);

// 使用预设
Material blueGlass = GlassMaterialCreator.Presets.BlueGlass();
```

### 保存 Mesh

```csharp
// 保存 Mesh 为资源文件
MeshSaver.SaveMeshAsset(mesh, "MyMesh");

// 从游戏对象保存 Mesh
MeshSaver.SaveMeshFromGameObject(gameObject, "MyMesh");

// 保存为 Prefab
MeshSaver.SaveAsPrefab(gameObject, "MyPrefab");
```

## 💡 推荐工作流程

### 移动端最佳实践

1. **编辑器预处理**
   - 在 Unity 编辑器中运行 CSGCubeMergeDemo
   - 使用方案1（SimpleCSG）或方案2（Unity 原生）合并
   - 保存为 Mesh 资源或 Prefab

2. **运行时加载**
   - 移动端直接加载预处理好的 Mesh
   - 零运行时计算开销
   - 最佳性能表现

### 开发调试

- 使用透明玻璃材质可以清楚看到交集处理效果
- 对比方案1和方案2的结果
- 检查顶点数和三角形数的差异

## 📦 保存的文件

所有保存的文件位于：`Assets/SavedMeshes/`

- `Mesh_YYYYMMDD_HHMMSS.asset` - Mesh 资源
- `Prefab_YYYYMMDD_HHMMSS.prefab` - Prefab 预制体
- `Prefab_YYYYMMDD_HHMMSS_Mesh.asset` - Prefab 的 Mesh

## 🔧 扩展新方案

如需添加新的合并方案：

### 1. 创建新的实现类

```csharp
public static class MyCustomMeshCombiner
{
    public static Mesh Combine(GameObject obj1, GameObject obj2)
    {
        // 你的合并算法
        return mergedMesh;
    }
}
```

### 2. 在 CSGCubeMergeDemo 中添加调用

```csharp
public void MergeWithCustomMethod()
{
    if (!ValidateCubes()) return;
    
    Mesh mergedMesh = MyCustomMeshCombiner.Combine(cube1, cube2);
    
    if (mergedMesh != null)
    {
        CreateMergedObject(mergedMesh, "MergedCube_Custom");
        Debug.Log("✓ 自定义合并成功！");
        LogMeshInfo(mergedMesh);
    }
}
```

### 3. 添加快捷键和 GUI

```csharp
// 在 Update() 中
if (Input.GetKeyDown(KeyCode.Alpha3))
{
    MergeWithCustomMethod();
}

// 在 DrawMergeButtons() 中
if (GUILayout.Button("方案3: 自定义合并 (3)", GUILayout.Height(35)))
{
    MergeWithCustomMethod();
}
```

## ⚠️ 注意事项

1. **SimpleCSG 限制**
   - 仅适用于轴对齐的立方体
   - 对于复杂几何体效果可能不完美
   - 不支持旋转的立方体

2. **保存功能**
   - 仅在编辑器模式下可用（使用 `#if UNITY_EDITOR`）
   - 运行时无法保存资源文件

3. **性能建议**
   - 移动端使用预处理方案
   - 避免运行时进行 CSG 计算
   - 优先使用方案2（Unity 原生，最快）

## 📚 API 参考

### SimpleCSG

```csharp
public static Mesh Union(GameObject obj1, GameObject obj2)
```

### UnityNativeMeshCombiner

```csharp
public static Mesh Combine(GameObject obj1, GameObject obj2)
public static Mesh Combine(GameObject[] objects)
```

### GlassMaterialCreator

```csharp
public static Material CreateGlassMaterial(Color color, float smoothness = 0.9f, float metallic = 0.1f)

// 预设
public static Material BlueGlass()
public static Material GreenGlass()
public static Material RedGlass()
public static Material ClearGlass()
public static Material FrostedGlass()
```

### MeshSaver

```csharp
public static bool SaveMeshAsset(Mesh mesh, string fileName = null, string savePath = "Assets/SavedMeshes")
public static bool SaveAsPrefab(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
public static bool SaveMeshFromGameObject(GameObject gameObject, string fileName = null, string savePath = "Assets/SavedMeshes")
```

## 📝 版本历史

- v2.0 - 重构版本
  - 将实现逻辑提取到独立的类
  - CSGCubeMergeDemo 只包含测试逻辑
  - 添加 UnityNativeMeshCombiner 类
  - 添加 MeshSaver 类
  - 更好的代码复用性

- v1.0 - 初始版本
  - 集成 SimpleCSG 和简单合并
  - 支持透明玻璃材质
  - 支持保存 Mesh 和 Prefab
