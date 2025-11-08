# 敌人回合触发条件完善方案（含攻击范围修正）

## 目标
- 让“玩家回合 → 敌人回合”的切换条件从“所有玩家球停止”升级为“所有玩家球与敌人都停止”。
- 修复远程敌人攻击范围在首回合不展示/误判的遗留问题，保证预告与实际命中完全对齐。
- 保留现有回合同步机制，尽量复用 `BallPhysics` 与事件系统，避免引入新的全局依赖。

## 关键影响点（不写代码，列出修改位置）
- `PlayerTurnManager`：回合结束判定逻辑（`OnAnyBallStopped`, `AreAllPlayerBallsStopped`）。
- `EnemyManager`：新增敌人运动状态查询（`AreAllEnemiesStopped`）。
- `EnemyBehavior` / `RangedAttackBehavior` / `AttackRange`：预告与攻击阶段的显隐、父子关系、碰撞判定。
- Prefab：若 `AttackArea` 结构需要调整（例如抛物线指示器拆分为独立子节点），需要同步处理。

## 修改步骤

### 1. 回合结束条件升级
1. 在 `PlayerTurnManager` 中增加公共方法 `TryCompleteTurn()`：在 `OnAnyBallStopped` 中统一调用，只有满足以下全部条件才触发 `OnTurnComplete`：
   - 发射次数已经用尽（`isWaitingForAllBallsToStop == true`）。
   - `AreAllPlayerBallsStopped()` 返回 true（现有逻辑）。
   - `EnemyManager.Instance.AreAllEnemiesStopped()` 返回 true（新方法）。
2. 在 `EnemyManager` 中编写 `AreAllEnemiesStopped()`：遍历激活/预告中的敌人，检查他们的 `BallPhysics`（或等价运动状态）是否全部 `IsMoving() == false`。
3. 若敌人移动结束时没有自动抛 `OnBallStopped`，在 `EnemyBehavior.ExecuteMovePhase()` 完成后显式调用一次 `GameEventBus.PublishBallStopped`，确保玩家/敌人停下都能触发监控逻辑。


## 注意事项
- 玩家与敌人的 `BallPhysics` 参数需要对齐（停止阈值、阻尼等），否则出现“玩家觉得敌人还在缓慢滑动但系统已判定停止”的情况。
- 在回合切换处增加容错日志（例如玩家或敌人仍在移动时触发回合结束），便于未来排查。
- 检查所有敌人类型（近战、远程、陷阱）是否都正确挂载 `BallPhysics`；对不动的敌人可直接返回 true 或维持现状。

## 验收标准
- 同时有玩家球和敌人球在场，只有当两者都完全静止后才切换到敌人回合；提前/延迟的回合切换不会再出现。
- 远程敌人首回合就能展示攻击预告，玩家走出可视范围不会再被误判命中。
- 运行时 Console 无新的 Null 引用或调试日志报错；所有预设资产在场景中正常显示。


