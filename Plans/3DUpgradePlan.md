# 3D 升级总体规划

> 目标：在不破坏现有可交付能力的前提下，将当前 2D 弹球战斗项目分阶段迁移为 3D，实现渲染、物理、交互与内容生产能力的全面升级。

---

## 1. 范围与指导原则
- **保持可回退**：始终维持 2D 主分支可运行，3D 迁移在独立分支和独立场景中推进。
- **模块化迁移**：按系统拆分（渲染/资源、物理/移动、战斗/反馈、UI/交互、关卡）逐块切换，避免单点耦合。
- **数据与表现分离**：ScriptableObject、黑板、行为树继续承载逻辑数据，3D 只替换表现与物理。
- **最小破坏**：优先通过适配层（Vector2→Vector3、Rigidbody2D→Rigidbody）过渡，必要时提供双写期。

---

## 2. 当前系统快照（关键依赖）
- **行为树/移动**：`PhaseSequenceMovementBehavior` 等核心仍以 `Vector2` 和 2D 平面假设为基础，需要接口升级。
- **运行时状态**：`EnemyRuntimeState`、`DamageSystem`、输入/碰撞事件等大量脚本直接使用 2D 物理组件。
- **资源结构**：Prefab 基于 SpriteRenderer、Animator(2D)，场景采用正交相机与 2D Tile/Sprite 布局。
- **工具链**：DOTween、Feel、NiceVibrations、GameCreator 等插件在 3D 中可复用，但需重新配置路径/绑定。

---

## 3. 迁移框架（阶段拆分）
### 阶段 A：准备与基线
- 建立 `feature/3d-upgrade` 分支与 `MainScene_3D` 场景副本。
- 在 `Plans` 目录维护迁移日志与模块 Checklist。
- 评估并记录所有依赖 2D 物理/向量的脚本与 ScriptableObject 字段，输出改造列表。

### 阶段 B：引擎与项目配置
- 新建 3D 渲染管线（URP/HDRP）资产，配置场景默认灯光、阴影、后处理（需在 Unity 中手动完成）。
- 更新 `ProjectSettings`：启用 3D Physics，调整 Layer/Tag、碰撞矩阵、输入系统 3D Raycast 设置。
- 配置摄像机体系：主摄像机由 Orthographic 切换为 Perspective，必要时新建 Cinemachine 虚拟机位。

### 阶段 C：资源与场景再构
- 为主要角色/敌人/投射物创建 3D 占位模型与材质，使用 Prefab Variant 继承逻辑脚本。
- 重建场景地形、碰撞体和导航元素（如需 NavMesh → 3D 版本）。
- 审核旧的 Sprite/Tiles 资源，确认无用后标记待删除；若需保留 2D UI/Icon，请移入专用文件夹。

### 阶段 D：脚本与逻辑适配
- 抽象统一的 `VectorAdapter` 或工具方法，先在 `BaseMovementBehavior` 等基础类中支持 `Vector3` 参数，逐步替换子类（示例：PhaseSequence、MoveTowards、MoveAway）。
- 替换物理组件：`Rigidbody2D/Collider2D` → `Rigidbody/Collider`，同步更新事件监听与 `DamageSystem` 命中计算。
- 更新行为树与黑板数据：所有坐标、方向字段调整为 `Vector3`，确保 Conditional/Repeat Decorator 等缓存逻辑可复用。
- 通知 UI/反馈系统对接 3D 坐标（世界转屏幕，指示器、血条、特效定位等）。

### 阶段 E：动画、特效与反馈
- 重建 Animator Controller（3D 动画剪辑、状态机、Avatar Mask），重新绑定 BlendTree/参数。
- 调整 DOTween/Feel/NiceVibrations 的路径与对齐逻辑，确保支持 Z 轴与 3D 轨迹。
- 审核粒子、后处理、光效，确保性能与视觉一致。

### 阶段 F：系统验证与优化
- 每完成一个模块（移动、战斗、摄像机等）在独立测试场景回归；编写 Unity Test Runner 用例覆盖关键逻辑。
- 关注性能（Draw Call、批处理、物理开销），适时引入对象池、LOD、阴影裁剪等优化策略。
- 结合 Profiler/Frame Debugger 做性能基线和迁移前后对比。

---

## 4. 风险与对策
- **坐标/物理双写期冲突**：通过适配层与阶段性 Feature Flag 控制，避免 2D/3D 逻辑同时驱动同一对象。
- **资源替换开销大**：先导入占位模型验证流程，再逐步替换正式美术资源；使用 Prefab Variant 降低返工。
- **插件兼容性**：确认 DOTween、GameCreator、NiceVibrations 等在 3D 场景下的使用手册，必要时分支替换或升级版本。
- **团队协作**：前期完成命名规范/目录结构文档，明确不同角色（程序/美术/策划）的交付接口。

---

## 5. 交付物与里程碑
1. **P0**：3D 项目骨架可运行（摄像机/角色占位/基础移动），提交演示视频与配置清单。
2. **P1**：完整战斗 Loop（敌人行为树、碰撞、伤害反馈）在 3D 场景中跑通；提供性能基线报告。
3. **P2**：内容与视觉完善（真·模型、动画、特效），完成与 2D 主分支的对比评审，确定切换计划。
4. **P3**：回归测试与发布候选，包含自动化测试结果、已知问题清单、上线准备文档。

---

## 6. 下一步行动
- 整理现有脚本/配置的 `Vector2` 使用点清单，评估所需适配工作量。
- 建立 3D 场景原型（地形 + 摄像机 + 主角 + 一个敌人）验证最小可行闭环。
- 开始撰写各系统的详细迁移任务单与排期，纳入项目管理工具。

