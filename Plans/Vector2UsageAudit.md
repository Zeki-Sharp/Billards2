# Vector2 使用情况梳理（3D 升级基线）

> 目的：明确全项目 `Vector2` 依赖的分布、用途与迁移优先级，为从 2D 平面坐标过渡到 3D 世界坐标提供改造清单。

---

## 1. 统计概览
- **代码范围**：`Assets/Scripts` 目录内共有 **63** 个脚本直接引用 `UnityEngine.Vector2`。
- **使用动机**：
  1. **世界空间**：敌人/玩家移动、碰撞、投射、生成等逻辑假设在 XY 平面。
  2. **参数配置**：ScriptableObject、行为树、技能与生成配置使用 `Vector2` 存储距离、区域或方向。
  3. **UI/工具**：自定义线段绘制、地图 UI、输入/瞄准线在屏幕或 2D Canvas 中使用 `Vector2`。
- **总体原则**：3D 升级后，需将“世界空间”类 `Vector2` 升级为 `Vector3` 并兼容 Z 轴；“屏幕/UI”可继续使用 `Vector2`（因为本质是 2D 布局）。

---

## 2. 分类清单（模块 → 代表脚本 → 用途说明）

### 2.1 敌人行为 / 移动系统（高优先级）
- `Enemy/Behaviors/PhaseSequenceMovementBehavior`, `MoveTowardsBehavior`, `MoveAwayBehavior`, `IdleBehavior`, `SequenceBehavior`, `SelectorBehavior`, `RepeatDecorator`, `ConditionalDecorator`, `IMovementBehavior`, `BaseMovementBehavior`, `RangedAttackBehavior`.
- **用途**：所有行为树节点与装饰器的输入/输出位置、方向、目标点均为 `Vector2`。`PhaseSequence` 通过 `Vector2` 计算目标点，`RangedAttack` 使用 `Vector2` 指向玩家。
- **迁移建议**：
  - 将 `IMovementBehavior.ExecuteMovement`、`EnemyRuntimeState.currentDirection/targetPosition` 改为 `Vector3`，并在初期提供 `Vector3` ↔ `Vector2` 适配器以兼容旧实现。
  - 原子行为内部的运动计算、黑板数据、条件判断全部切换到 `Vector3`，默认约束 Z=0；后续依据关卡高度再释放 Z。

### 2.2 敌人运行时状态与聚合逻辑（高优先级）
- `Enemy/EnemyRuntimeState`, `Enemy/EnemyBehavior`, `GameEventBus`.
- **用途**：运行时状态保存方向、目标点；事件消息体（如 `OnEnemyMove`）传递 `Vector2` 表示位置。
- **迁移建议**：先改数据层（`EnemyRuntimeState`）类型，随后沿着事件链修改监听者。需要同步更新序列化数据（若有存档/回放）。

### 2.3 玩家与发射流程（高优先级）
- `Player/PlayerBehavior`, `PlayerTurnManager`, `PlayerAttackManager`, `ChargeSystem`, `ChargeController`, `PlayerSpawner`, `PlayerStateMachine`, `ChargeBarUI`, `GlobalInputManager`.
- **用途**：发射方向、受力、输入坐标、充能条进度等均使用 `Vector2`。`PlayerBehavior` 下的大部分事件（如 `OnCharacterLaunched`）签名包含 `Vector2 direction`。
- **迁移建议**：统一事件/接口签名为 `Vector3`，但在 UI 输入阶段保留 `Vector2`，通过 `Camera.ScreenPointToRay` 映射到 3D 世界。`ChargeBarUI` 等 UI 可不改。

### 2.4 物理、轨迹与碰撞系（高优先级）
- `Core/Physics/BallPhysics`, `AimLine/AimController`, `AimLineRenderer`, `AimLineLandingPointManager`, `Calculator/TrajectoryPredictor` & `TrajectorySimulationManager`, `SimulationObjectReplicator`, `Wall/Wall`, `WallManager`, `LevelHazards/BouncePad`.
- **用途**：基于 2D 刚体和 `Vector2` 进行反射、角度计算、轨迹投射、墙体法线等；`BallPhysics` 通过 `Vector2.Reflect` 计算反弹。
- **迁移建议**：迁移到 3D 物理（`Rigidbody` & `Collider`），全部改为 `Vector3`，同时将 `Physics2D` API 替换为 `Physics`。轨迹预测需改用 3D 抛物线/弹道模拟。

### 2.5 战斗 / 伤害系统（高优先级）
- `Core/Manager/DamageSystem`, `Combat/AttackRange`, `EventSystem/DamageSystemEvents`, `SkillSystem/WeakPointManager`, `WeakPointData`, `WeakPointMarker`, Turn-Based Status（`PoisonStatus`, `BurningStatus`）。
- **用途**：范围计算、三角形/盒碰撞、弱点定位等均用 `Vector2`；事件中记录接触点、法线、速度。
- **迁移建议**：与物理迁移同步。`DamageSystem` 的几何函数（如 `IsValidTriangle`, `IsColliderIntersectTriangle`）需改为 3D，需要额外处理 `Vector3` 叉积和体积检测；弱点定位需使用 `Transform.position` 的 `Vector3`。

### 2.6 生成 / 地图系统（中优先级）
- `SpawnSystem/Core/BaseSpawner`, `SpawnRangeConfig`, `ConfigProviders/WaveConfigProvider`, `MapSystem/*`（`MapGenerator`, `MapPlayerTracker`, `MapView`, `MapViewUI`, `Utilities/DottedLineRenderer`, `ScrollNonUI`）。
- **用途**：生成范围、地图节点位置/路径、滚动效果均使用 `Vector2`（部分 `Vector2Int`）。这些数据主要描述平面布局。
- **迁移建议**：地图/策划层可以继续使用 2D 数据，但当节点需放置在 3D 世界时，应提供转换函数（如 `MapToWorldPosition(Vector2 grid)` 返回 `Vector3`）。生成系统若直接实例化 3D 实体，需改配置字段为 `Vector3` 或提供高度参数。

### 2.7 UI / 绘图实用工具（低优先级）
- `UI/Drawing/UILineRenderer`, `UIPrimitiveBase`, `Utilities/BezierPath`, `CableCurve`, `MapSystem/View/MapViewUI`, `AimLine` 系列的 UI 组件。
- **用途**：基于 Canvas 或屏幕坐标的线条、曲线绘制——继续依赖 `Vector2` 更合适。
- **迁移建议**：保留 `Vector2`，仅在需要从 3D 世界投射至 UI 时，在调用处转换：`RectTransformUtility.WorldToScreenPoint` → `Vector2`.

---

## 3. 迁移优先级与改造策略

| 优先级 | 分类 | 原因 | 改造策略 |
| --- | --- | --- | --- |
| P0 | 敌人行为/运行时状态、玩家发射、物理/轨迹、伤害系统 | 直接驱动世界坐标，阻塞 3D 场景运行 | 统一接口签名为 `Vector3`，建立临时适配器，完成 `Physics2D` → `Physics` 替换 |
| P1 | 生成系统、地图系统 | 与场景摆放关系紧密，但可暂以 2D 数据驱动 | 增加 `ToWorldPosition` 转换与高度参数，改造完成后再替换所有调用 |
| P2 | UI/工具、配置层 | 屏幕/Canvas 坐标本身是 2D | 保持 `Vector2`，仅处理 3D → 2D 投影逻辑 |

---

## 4. 逐步实施建议
1. **建立适配层**：在 `Core/Math/VectorAdapter`（建议新建）中提供 `Vector3 ToWorld(Vector2 source, float defaultZ = 0f)` 等方法；迁移期间，先让关键系统依赖适配层，降低一次性替换风险。
2. **接口签名调整顺序**：
   - `IMovementBehavior`, `BaseMovementBehavior`, `EnemyRuntimeState`.
   - `PlayerTurnManager` 事件与 `BallPhysics` 力量接口。
   - `DamageSystemEvents` 与 `CollisionEvent` 数据结构。
3. **Physics API 替换**：先在 `BallPhysics` 建立抽象，切换到 `Rigidbody` 后再扩散到墙体/陷阱/伤害几何代码。
4. **数据配置同步**：更新 `Scriptable Objects` 中的向量字段（MoveTowards/MoveAway 配置等）。可通过自定义 `OnValidate` 或编辑器工具批量升级。
5. **测试覆盖**：针对每个阶段，使用独立测试场景验证（移动/发射/伤害），并在 `Plans` 中记录已迁移文件列表，确保回溯。

---

## 5. 下一步行动
- 基于此清单，为 P0 模块创建任务卡（Movement、Player、Physics、Damage 四条链路），标明影响脚本与负责人。
- 创建 `VectorAdapter` 雏形并在 `BaseMovementBehavior` 中试接入，验证编译范围与潜在报错。
- 为 `DamageSystem` 与 `BallPhysics` 分别设计 3D 几何/物理单元测试，确保迁移后行为一致。
- 继续在 `Plans` 目录维护“ Vector2 → Vector3 已迁移文件表”，便于追踪进度。

