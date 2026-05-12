# 表面箭头玩法 Unity 实现

## 组件

新增两个脚本：

- `SurfaceArrowManager`：管理实体模型查询和箭头之间的碰撞查询。
- `SurfaceArrow`：单个箭头/小蛇，负责路径、动画状态、点击触发和 Mesh 生成。
- `SurfaceOrbitCamera`：让相机围绕模型中心旋转和缩放。

## 使用方式

1. 在场景中创建一个空物体，添加 `SurfaceArrowManager`。
2. 如果使用现有体素模型，把 `VoxelChunk` 拖到 `voxelChunk` 字段。
3. 如果不用 `VoxelChunk`，可以在 `boxes` 里配置若干整数包围盒作为实体模型。
4. 创建箭头物体，添加 `SurfaceArrow`。
5. 给箭头配置 `path`，第 0 个点是箭头头部，第 1 个点决定箭头移动方向。
6. 运行后点击箭头触发移动。

`SurfaceArrowManager.coordinateRoot` 是坐标根节点。`boxes` 和 `SurfaceArrow.path` 都按这个根节点的局部坐标填写；脚本会在生成 Mesh、点击检测、碰撞检测时转换到世界坐标。这样整个模型父节点有位置或旋转时，箭头仍然能贴在模型表面。

示例 demo 会自动给 `Main Camera` 挂上 `SurfaceOrbitCamera`：

- 右键拖拽：绕模型中心旋转。
- 鼠标滚轮：缩放距离。
- 观察中心：示例模型整体包围盒中心。

## 路径规则

`SurfaceArrow.path` 是一组世界坐标点：

```text
path[0] = 箭头头部
path[1] = 脖子/下一段
path[2...] = 身体
```

箭头可以横跨多个平面。渲染时每一段都会根据所在位置重新计算表面法线，并生成贴在表面的单层面片段。

路径点是模型局部坐标，不是世界坐标。若模型整体挂在一个父节点下移动或旋转，只需要把这个父节点设置为 `SurfaceArrowManager.coordinateRoot`。

## 行为逻辑

点击箭头后：

1. 从 `path[0] - path[1]` 得到头部方向。
2. 沿这个方向向前探测。
3. 如果碰到其他箭头，进入 `Hitting -> Shaking -> Retreating`。
4. 如果碰到实体模型，也进入撞击回退流程。
5. 如果没有碰撞，进入 `Moving`，超过 `maxTravelDistance` 后销毁自身。

状态机：

```text
Idle -> Moving -> Destroy
Idle -> Hitting -> Shaking -> Retreating -> Idle
```

## 视觉生成

`SurfaceArrow` 不依赖 `LineRenderer`。

- 身体段：程序生成小四边形面片 Mesh。
- 箭头头部：程序生成三角形面片 Mesh。
- 每段根据 `SurfaceArrowManager.GetSurfaceNormal()` 计算朝外方向。
- Mesh 会沿表面法线外偏移 `surfaceOffset`，避免 Z-fighting。
- `MeshCollider` 使用同一个生成 Mesh，因此可以直接点击箭头。

## 和 React 版对应关系

- `isSolidPoint` -> `SurfaceArrowManager.IsSolidPoint`
- `getSurfaceNormal` -> `SurfaceArrowManager.GetSurfaceNormal`
- `buildVisualPath` -> `SurfaceArrow.BuildVisualPath`
- `getAnimatedPath` -> `SurfaceArrow.GetAnimatedPath`。Unity 版保留小蛇式路径推进：头部沿箭头方向前进，身体沿原表面路径跟随，不做整条刚性平移。
- `renderSegments` -> `SurfaceArrow.RebuildMesh`
- `handleClick` -> `SurfaceArrow.TryActivate`

## 相机环绕

`SurfaceOrbitCamera` 使用 `target + yaw/pitch/distance` 控制相机位置：

- `target` 是整个模型中心。
- `yaw` 控制水平绕转。
- `pitch` 控制俯仰角，并限制在 `minPitch` 到 `maxPitch`。
- `distance` 控制相机到中心的距离，并限制在 `minDistance` 到 `maxDistance`。

它默认使用右键拖拽旋转，避免和左键点击箭头冲突。

## 当前版本限制

- 移动逻辑按头部方向直线探测，和提供的 React 版一致。
- 箭头视觉可以横跨多个平面，但移动不是完整的 surface graph 爬行。
- 若后续需要“沿模型表面绕角移动”，应新增 `SurfaceGrid.Step()`，用表面拓扑替换当前直线探测。
