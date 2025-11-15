# 3D 升级依赖清单（持续更新）

> 目的：集中记录所有与 2D 实现强绑定的依赖（Vector2、Physics2D、SpriteRenderer 等），评估迁移优先级和处理策略。随着项目推进在此表更新，无需分散到其他文档。

---

## 1. Vector2 / 平面空间依赖
- **覆盖范围**：`Assets/Scripts` 中 400+ 处 `Vector2` 使用，集中在敌人行为、玩家发射、物理/轨迹、伤害、生成/地图、UI。
- **P0（必须直接改为 Vector3）**
  - `Enemy/Behaviors/*`, `EnemyRuntimeState`, `GameEventBus`
  - `PlayerBehavior`, `PlayerTurnManager`, `ChargeSystem`, `ChargeController`
  - `Core/Physics/BallPhysics`, `Combat/AttackRange`, `AimLine/*`, `TrajectoryPredictor/*`
  - `DamageSystem`, `LevelHazards/BouncePad`
- **P1（数据仍可为 2D，但在使用点转为 3D）**
  - `SpawnSystem/*`, `MapSystem/*`, `WeakPointManager`, `WaveConfigProvider`
- **P2（长期保留 2D）**
  - UI/Canvas 工具：`UI/Drawing/*`, `DamageText`, `MapViewUI` 等。
- **行动**：Phase A 期间完成 `Vector2UsageAudit` 清单，并为 P0 系统排定直接改写顺序；完成后在此表打勾。
  - 2025-11-15：玩家输入/事件链（`GlobalInputManager`, `GameEventBus`, `ChargeController`, `PlayerStateMachine`, `PlayerTurnManager`, `TopBarController`）已切换到 `Vector3` 事件流程。

## 2. Physics2D / 2D 刚体依赖
- **关键脚本**
  - `DamageSystem`（`Physics2D.OverlapCircleAll/OverlapBoxAll`）
  - `Player/Input/GlobalInputManager`（`Raycast`、`OverlapPointAll`）
  - `SpawnRangeConfig`, `PlayerSpawner`, `EnemySpawner`
  - `GameManager`（设置 `Physics2D.gravity`）
  - `Combat/AttackRange`, `Calculator/TrajectoryPredictor/*`, `Gameplay/Hole`
- **策略**
  1. Phase A 输出完整脚本 + API 表，评估可立即替换的 2D Physics API。
  2. Phase B 起直接将 `BallPhysics`、`AttackRange`、输入/生成检测改写为 `Rigidbody` + `Physics`，旧 2D 实现保留在回退分支而非运行时桥接。
  3. 分阶段替换辅助逻辑（生成检测、输入射线等），最后统一关闭 `Physics2D`.

## 3. SpriteRenderer / 2D Animator 资源
- **受影响目录**
  - `Assets/Sprites/` 全部角色/敌人贴图。
  - `Assets/Prefabs/Player/*`, `Assets/Prefabs/Enemies/*`, `Assets/Prefabs/UI/*` 中依赖 `SpriteRenderer` 或 2D Animator 的 Prefab。
  - `Assets/Anim/Character/`、`TextMesh Pro` 示例素材等。
- **迁移要点**
  - 建立 `SpriteBasedAssets` 清单，列出需要 3D 化的 Prefab、绑定脚本、依赖材质。
  - UI Sprite 保留，将共享贴图移至 `UI/Icons` 等命名空间，避免与 3D 资源混杂。

## 4. 输入 / 黑板 / 插件
- **输入**：`GlobalInputManager`, `AimController` 依赖 `Vector2` 鼠标坐标 → 需要 ScreenPointToRay + 3D Raycast。
- **黑板/事件**：`Blackboard`, `GameEventBus`, 各种 `DamageSystemEvents` 中的 `Vector2` 字段需同步升级；
- **第三方**：DOTween、Feel、GameCreator、NiceVibrations 支持 3D，但需要记录 3D 版本 API 与性能差异。

---

## 5. 更新记录
- `2025-11-15`：建立初始清单，引用旧文档中 Vector2/Physics2D/Sprite 依赖情况。
- （后续在此表新增日期 + 变更摘要）

