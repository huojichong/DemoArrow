# Unity CSG 立方体合并系统

## 📁 代码结构

### 核心实现类（无 UI，纯逻辑）

1. **SimpleCSG.cs** - 简化版 CSG 算法
   - 纯静态类，提供 `Union()` 方法
   - 尝试去除交集内部面
   - 适用于轴对齐的立方体
   - 无依赖，跨平台兼容

2. **GlassMaterialCreator.cs** - 玻璃材质创建工具
   - 纯静态类，提供材质创建方法
   - 支持自定义透明度、光滑度、金属度
   - 提供 5 种预设材质（蓝、绿、红、透明、磨砂）

### 测试演示类（包含 UI）

3. **CSGCubeMergeDemo.cs** - 主测试类（集成所有方案）
   - 集成了所有合并方案
   - 提供完整的 GUI 界面
   - 支持保存 Mesh 和 Prefab

## 🎮 使用方法

### 快速开始

1. 在场景中创建空物体
2. 添加 `CSGCubeMergeDemo` 组件
3. 运行场景

### 操作说明

**键盘快捷键：**
- `C` - 创建两个有交集的立方体
- `1` - 方案1：SimpleCSG 合并（去除内部面）
- `2` - 方案2：简单合并（保留所有面）
- `M` - 保存 Mesh 为 .asset 文件
- `P` - 保存为 Prefab
- `X` - 清理所有对象

**GUI 按钮：**
- 左上角提供完整的按钮界面
- 所有操作都可以通过鼠标点击完成

## 📊 合并方案对比

| 方案 | 算法 | 性能 | 内存 | 交集处理 | 移动端 |
|------|------|------|------|---------|--------|
| **方案1: SimpleCSG** | 简化 CSG | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | 去除内部面 | ✅ 推荐 |
| **方案2: 简单合并** | Unity 原生 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 保留重叠 | ✅ 最快 |

## 💡 推荐工作流程

### 移动端最佳实践

1. **编辑器预处理**
   - 在 Unity 编辑器中运行 CSGCubeMergeDemo
   - 使用方案1（SimpleCSG）合并立方体
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

- `MergedMesh_YYYYMMDD_HHMMSS.asset` - Mesh 资源
- `MergedCube_YYYYMMDD_HHMMSS.prefab` - Prefab 预制体

## 🔧 扩展新方案

如需添加新的合并方案：

1. 在核心实现类中添加新的静态方法
2. 在 `CSGCubeMergeDemo.cs` 中添加调用方法
3. 在 GUI 中添加对应按钮
4. 添加快捷键绑定

示例：
```csharp
// 在 CSGCubeMergeDemo.cs 中添加
public void MergeWithNewMethod()
{
    // 调用新的合并算法
    Mesh mergedMesh = NewCSGAlgorithm.Merge(cube1, cube2);
    // ... 创建合并对象
}

// 在 Update() 中添加快捷键
if (Input.GetKeyDown(KeyCode.Alpha3))
{
    MergeWithNewMethod();
}

// 在 OnGUI() 中添加按钮
if (GUILayout.Button("方案3: 新方案 (3)", GUILayout.Height(35)))
{
    MergeWithNewMethod();
}
```

## ⚠️ 注意事项

1. **SimpleCSG 限制**
   - 仅适用于轴对齐的立方体
   - 对于复杂几何体效果可能不完美
   - 不支持旋转的立方体

2. **保存功能**
   - 仅在编辑器模式下可用
   - 运行时无法保存资源文件

3. **性能建议**
   - 移动端使用预处理方案
   - 避免运行时进行 CSG 计算
   - 优先使用方案2（最快）

## 📝 版本历史

- v1.0 - 初始版本，集成 SimpleCSG 和简单合并
- 支持透明玻璃材质
- 支持保存 Mesh 和 Prefab
