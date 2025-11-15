# 3D 升级总体流程规划

> 目的：建立从当前 2D 弹球战斗玩法迁移到 3D 版本的统一路线图，确保在持续可交付的前提下完成渲染、物理、交互、资源生产等系统级升级。本文档作为 `Plans/3DUpgrade/` 目录的主文档，后续所有子规划、Checklist 与阶段总结都应链接回此文。

---

## 1. 范围与原则
- **分支隔离**：所有 3D 改动在 `feature/3d-upgrade`（或等效）分支推进，2D 主分支保持可发布。
- **模块化推进**：按渲染/资源 → 物理/运动 → 战斗/反馈 → UI/交互 → 关卡/内容的顺序逐块切换，禁止在单 Manager 聚合所有逻辑。
- **数据/表现分离**：继续使用 ScriptableObject 与黑板存储逻辑数据，表现层通过组件化 Prefab、材质、Animator 重新实现。
- **最小破坏**：通过分阶段直接替换（Vector2→Vector3、2D Physics→3D Physics）与 Feature Flag 控制风险，逐步淘汰 2D 版本而非双栈维护。

---

## 2. 文档索引
- **UpgradeMasterPlan.md**：当前文档，概述目标、阶段、任务、状态。
- **PhaseA_TaskList.md**：Phase A 任务拆解与进度表。
- **DependencyAudit.md**：Vector2、Physics2D、Sprite 资源等依赖清单与迁移策略。
- **PlayerControl3DPlan.md**：玩家输入/蓄力/发射/物理改写路线（阶段 P0~P3）。
- （预留）`Vector2UsageAudit.md`、`Physics2DUsageAudit.md`、`SpriteBasedAssets.md` 等后续子文档，用于记录直接迁移的详细列表。

文档创建后请在此处补充链接与一句话描述，保持目录清晰。

---

## 3. 阶段划分

### 阶段 A：准备与基线
1. 建立 `3DUpgrade` 文档目录，记录迁移决策与 Checklist。
2. 复制关键场景为 `MainScene_3D`，保留 2D 场景作为对照。
3. 收集 `Vector2`、`Physics2D`、`SpriteRenderer` 等平面依赖的脚本与配置，输出清单。

### 阶段 B：项目/引擎配置
1. 创建 3D 渲染管线资产（URP/HDRP），重建默认灯光、阴影、后处理（Unity 编辑器内操作，记录配置）。
2. 更新 `ProjectSettings`：启用 3D Physics、调整 Layer/Tag/碰撞矩阵、配置新摄像机栈与输入系统。
3. 规划摄像机方案（主透视相机 + Cinemachine），定义基础镜头/跟随逻辑。

### 阶段 C：资源与场景重构
1. 角色/敌人/投射物建立 3D 占位模型与材质，使用 Prefab Variant 继承逻辑脚本。
2. 重新布置地形、障碍、碰撞体，准备 NavMesh 或其他 3D 导航方案。
3. 对旧的 2D 资源执行归档：可复用的移动到 UI/Icon 命名空间，废弃资源列入移除列表。

### 阶段 D：脚本与逻辑改写
1. 直接将核心抽象（`BaseMovementBehavior`、`EnemyRuntimeState`、`BallPhysics`、`DamageSystemEvents` 等）切换为 `Vector3` 与 3D 物理组件，删除 `Vector2`/2D 依赖。
2. 分系统推进：敌人行为树、玩家发射、轨迹预测、伤害判定、事件总线等逐个完成 3D 重写并移除旧代码。
3. 同步更新 ScriptableObject 配置与存档字段，如需兼容历史数据，提供一次性迁移脚本。

### 阶段 E：动画、特效、反馈
1. 重建 Animator Controller（3D 模型剪辑、Avatar、BlendTree），重新绑定 DOTween/Feel/NiceVibrations 动效。
2. 调整粒子、后处理、光效，确保性能可控并满足视觉目标。
3. 更新反馈/UI 系统：血条、指示器、落点提示等支持 3D 世界位到 Canvas 映射。

### 阶段 F：测试、优化与切换
1. 针对每个阶段编写独立测试场景与 Unity Test Runner 用例。
2. 使用 Profiler/Frame Debugger 建立性能基线，引入对象池、LOD、阴影裁剪等优化策略。
3. 在 P2/P3 里程碑前完成与 2D 版本的全功能对比，确认切换时间点与回退方案。

---

## 4. 里程碑
1. **P0（技术原型）**：3D 场景可运行，角色/敌人占位、摄像机、基础移动完成。
2. **P1（核心战斗）**：3D 中完成完整战斗循环（移动、碰撞、伤害、反馈），输出性能报告。
3. **P2（内容整合）**：正式美术资源、动画、关卡完成替换，与 2D 版本完成评审，对齐上线策略。
4. **P3（发布准备）**：完成回归测试、自动化测试、已知问题清单与发版文档。

---

## 5. 第一阶段（Phase A）任务拆解
1. **文档与目录**
   - 确认 `Plans/3DUpgrade/` 为唯一入口，迁移旧文档（Vector2 清单、升级计划等）到此目录。
   - 在 Master Plan 中维护索引，新增文档（Vector2 Usage、Physics Migration、Animation Plan 等）时同步登记。
2. **环境准备**
   - 创建 `feature/3d-upgrade` 分支（或确认现有分支），同步最新主干。
   - 复制主场景为 `MainScene_3D`，记录需要替换的资源/脚本列表。
3. **依赖清单**
   - 复核 `Vector2UsageAudit`，补充缺失脚本并标记 P0/P1。
   - 额外输出 `Physics2D` API 清单（`Rigidbody2D`, `Collider2D`, `Physics2D.Overlap...` 等）。
   - 列出所有 `SpriteRenderer` / `Animator(2D)` Prefab，标注对应逻辑脚本。
4. **改写顺序与策略**
   - 制定各系统的 3D 重写顺序（Movement → Player → Physics → Damage → Spawn/Map），明确依赖关系。
   - 评估对行为树、伤害系统、事件总线的接口影响，形成直接替换计划和回滚策略。
5. **计划排期**
   - 将上述任务拆分到项目管理工具，指定负责人与估时。
   - 设定每周同步节点，确保风险及时暴露。

---

## 6. 后续文档约定
- `Plans/3DUpgrade/` 内的所有 Markdown 需在首段注明目的与所处阶段。
- 如果涉及 Unity 配置或 Editor 操作，仅提供步骤说明，不直接修改配置，保持与用户协作原则一致。
- 当旧文档不再适用时，请在此目录中记录“废弃/替换”说明，确保知识库干净可追踪。

---

## 7. 当前状态 & 下一步
- 当前：Phase A 启动，目录创建完成，正在汇总 Vector2/Physics2D 清单并制定直接 3D 重写计划。
- 下一步：
  1. 将 `Vector2UsageAudit` 与其他参考文档迁移至本目录并更新索引。
  2. 输出 `PhaseA_TaskList.md`，细化任务负责人与时间节点。
  3. 在项目管理工具（或表格）登记 P0 任务（Movement、Player、Physics、Damage 四条链路）。
  4. 预约技术评审会议，确认 3D 改写顺序与回滚策略。

