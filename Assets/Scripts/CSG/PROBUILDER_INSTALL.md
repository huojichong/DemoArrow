# ProBuilder 安装指南

## 📦 安装 ProBuilder

ProBuilder 是 Unity 官方的 3D 建模工具，支持 CSG 布尔运算。

### 安装步骤：

1. **打开 Package Manager**
   - Unity 菜单：`Window > Package Manager`

2. **搜索 ProBuilder**
   - 在搜索框输入：`ProBuilder`
   - 或者在左侧选择 `Unity Registry`

3. **安装**
   - 找到 `ProBuilder` 包
   - 点击右下角的 `Install` 按钮
   - 等待安装完成（约 1-2 分钟）

4. **验证安装**
   - 安装完成后，菜单栏会出现 `Tools > ProBuilder`
   - 控制台不再显示 ProBuilder 相关错误

## 🎮 使用方法

安装完成后：

1. **运行场景**
2. **按 C 键** - 创建两个立方体
3. **按 5 键** - 使用 ProBuilder 进行布尔运算
4. **按 O 键** - 导出为 OBJ 格式

或者使用 GUI 按钮：
- "方案5: ProBuilder - Unity官方 (5)"

## ✅ ProBuilder 的优势

1. **Unity 官方工具**
   - 官方支持和维护
   - 与 Unity 完美集成
   - 定期更新

2. **完整的 CSG 功能**
   - 并集（Union）
   - 差集（Difference）
   - 交集（Intersection）

3. **高质量结果**
   - 正确的拓扑结构
   - 无内部顶点
   - 适合导出到 Blender

4. **免费**
   - 完全免费
   - 无需额外购买

## 📊 方案对比

| 方案 | 质量 | 性能 | 依赖 | 推荐 |
|------|------|------|------|------|
| 方案1-4 | ⭐⭐⭐ | ⭐⭐⭐⭐ | 无 | 简单场景 |
| **方案5: ProBuilder** | **⭐⭐⭐⭐⭐** | **⭐⭐⭐** | **ProBuilder包** | **✅ 推荐** |

## ⚠️ 注意事项

1. **仅编辑器模式**
   - ProBuilder 的 CSG 功能仅在编辑器中可用
   - 运行时使用需要先导出 Mesh

2. **推荐工作流程**
   - 编辑器中使用 ProBuilder 合并
   - 保存为 Mesh 资源（按 M 键）
   - 运行时加载保存的 Mesh

3. **性能考虑**
   - ProBuilder 运算比简单合并慢
   - 但结果质量最好
   - 适合预处理，不适合实时运算

## 🔧 故障排除

### 问题：找不到 ProBuilder
**解决方案：**
- 确保 Unity 版本 >= 2019.4
- 检查网络连接
- 尝试重启 Unity

### 问题：编译错误
**解决方案：**
- 等待 ProBuilder 完全安装
- 重新导入项目：`Assets > Reimport All`
- 重启 Unity

### 问题：布尔运算失败
**解决方案：**
- 确保立方体没有旋转（只支持轴对齐）
- 检查立方体是否有交集
- 查看控制台的详细错误信息

## 📚 更多资源

- [ProBuilder 官方文档](https://docs.unity3d.com/Packages/com.unity.probuilder@latest)
- [ProBuilder 教程](https://learn.unity.com/tutorial/probuilder)
- [Unity 论坛](https://forum.unity.com/forums/probuilder.146/)
