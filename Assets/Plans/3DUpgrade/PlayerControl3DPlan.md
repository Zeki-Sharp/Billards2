# 玩家控制子系统 3D 升级计划

> 目标：将输入、蓄力、发射、球体物理、事件通知等玩家控制链路完全迁移到 3D，实现与场景/敌人一致的坐标、物理与交互体验。本计划概述需要覆盖的脚本范围与阶段顺序，不展开逐行实现细节。

---

## 1. 改造范围
- **输入与事件链**：`GlobalInputManager`, `GameEventBus` 中与 `Vector2` 鼠标坐标、发射事件相关的接口。
- **蓄力/发射逻辑**：`ChargeSystem`, `ChargeController`, `PlayerTurnManager`（事件统计、发射次数）。
- **玩家行为与物理**：`PlayerBehavior`, `PlayerAttackManager`, `BallPhysics`, `PlayerStateMachine`, 以及与玩家球碰撞/停止相关的事件（`GameEventBus.OnCollision/OnStopped`）。
- **辅助表现**：`AimLine/*`, `AimController`, `AimLineLandingPointManager`, `DamageText` 等直接读取玩家方向或速度的组件。

---

## 2. 阶段划分（子系统内部）
1. **Stage P0 - 输入与事件**（进行中）
   - 将 `GlobalInputManager` 改为 `Camera.ScreenPointToRay + Physics.Raycast`（已实现：2025-11-15）。
   - `GameEventBus` 中与玩家发射、瞄准相关的委托改为 `Vector3`/3D 数据结构（已实现：2025-11-15）。
   - 订阅者（`PlayerTurnManager`, `PlayerStateMachine`, `TopBarController`) 与 `ChargeController` 完成新签名适配。
2. **Stage P1 - 蓄力/发射**
   - `ChargeSystem`、`ChargeController`、`PlayerTurnManager` 把方向/速度/状态字段迁移到 `Vector3`，并在 Inspector 配置中增加 Z 轴约束选项。
3. **Stage P2 - 球体物理**
   - `BallPhysics` 替换为 `Rigidbody` + 3D 碰撞体，实现反弹、摩擦、停速逻辑；必要时提供平面约束组件。
4. **Stage P3 - 辅助组件**
   - 调整 `AimLine` 渲染、落点预测、UI 提示等工具以兼容 3D 轨迹；对 Damage/Stopped 事件的监听者做类型同步。

---

## 3. 输出物
- 更新后的 `Player` Prefab（使用 3D 物理组件）。
- 3D 版本玩家控制文档，记录需要的 Inspector 配置、输入映射、测试场景。
- 快速验证场景：`PlayerControl_3DPrototype.unity`（可与敌人场景隔离）。

---

## 4. 风险提示
- 输入射线与 UI 交互冲突：需要验证 `GraphicRaycaster` / UI 事件不会截断 3D Raycast。
- 事件签名更改会影响大量监听者，必须先梳理并批量替换，避免破坏编译。
- 物理参数（质量、阻尼、反弹）需重新调参，建议在测试场景中设置调试面板。

---

## 5. 下一步
1. 在 `DependencyAudit.md` 中将玩家相关脚本标记为“进行中”。
2. 依据 Stage P0→P3 顺序创建任务卡（或在 PhaseA_TASKList 表中登记）。
3. 准备初版 3D 玩家场景，并记录调试命令/脚本入口。

