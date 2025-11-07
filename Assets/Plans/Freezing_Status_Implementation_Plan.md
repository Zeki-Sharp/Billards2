# 冰冻状态实施计划

## 🎯 目标
- 引入“冰冻”回合制状态，受影响的敌人在冻结期间跳过自身行动。
- 复用现有多态状态框架（`TurnBasedStatusData` + `TurnBasedStatusBehaviourConfig` + `TurnBasedStatusComponent`）。
- 与技能系统、回合事件、UI 状态栏无缝协作，保证点燃/中毒等既有状态不受影响。

## 🔍 现状分析
- `TurnBasedStatusData` 已支持多态行为配置，点燃/中毒样例可参考。
- `TurnBasedStatusEffect` 通过 `statusData.GetComponentType()` 动态添加状态组件，新增状态只需提供新的行为/组件类型。
- 敌人回合流程：`EnemyPhaseController` 触发 `EnemyManager.ExecutePhase()`，随后遍历 `Enemy` 并调用 `Enemy.StartPhase()`；真正的阶段逻辑位于 `EnemyBehavior.ExecuteAttackPhase/ExecuteMovePhase/ExecuteTelegraphPhase()` 及 `ExecuteMovePhaseCoroutine()`。
- 当前没有“跳过行动”的统一接口，如需冻结需在上述入口检查状态并短路，同时确保 `EnemyManager` 的阶段推进事件（如 `Enemy.OnMoveComplete`）仍按预期触发，避免卡住流程。
- 状态 UI 通过 `TurnBasedStatusComponent` 发布的 `TurnBasedStatusChanged` 事件刷新，可直接复用。

## 🧩 设计思路
1. 扩展行为配置：`FreezingStatusBehaviourConfig` 持有持续回合、叠加策略（刷新/延长/不可叠加）。
2. 新建运行时组件：`FreezingStatus : TurnBasedStatusComponent`，负责：
   - 施加时标记受控目标不可行动；
   - 每次在配置的阶段触发时阻断行动并递减回合；
   - 状态移除时恢复正常行动。
3. 与敌人行动系统对接：在敌人侧提供统一的“跳过行动”接口（详见 Phase 2），状态脚本仅需调用该接口即可阻断本回合行动。
4. 技能层面：配置新的 `TurnBasedStatusData`（冰冻）并在技能 SO 中引用；可沿用 `DamageTrigger`。确保命名唯一（避免 `SkillManager` key 冲突）。

## 🛠️ 实施步骤

### Phase 1 — 行为与组件（优先级：高）
1. 在 `TurnBasedStatusBehaviourConfig.cs` 中新增 `FreezingStatusBehaviourConfig`，字段包括：
   - `durationInTurns`（基础持续回合数）
   - `stackMode`（Refresh：刷新，Extend：增加剩余回合，不允许叠层则直接忽略）
2. 实现行为方法：
   - `ApplyInitialValues`：设置剩余回合、重置叠层；
   - `OnStackApplied`：根据 `stackMode` 调整 `remainingTurns`；
   - `GetDebugDescription`：返回回合数信息。
3. 新建 `FreezingStatus.cs`（继承 `TurnBasedStatusComponent`）：
   - `OnStatusApplied()`：调用敌人控制接口使其冻结（例如设置 `EnemyActionBlocker` 组件或向 `GameEventBus` 发布冻结事件）。
   - `OnTurnTrigger()`：提醒控制层跳过行动（若控制层在事件周期内处理），无需造成伤害；
   - `OnStatusRemoved()`：解除冻结标记。

### Phase 2 — 敌人行动对接（优先级：高）
1. 设计统一接口 `IEnemyTurnSkipper`（暂定），由 `Enemy` 提供基础实现：
   - `bool RequestSkipOnce(object source, string reason)`：标记本回合需要跳过当前阶段。
   - `void ClearSkipRequest(object source)`：若状态提前解除，可清除请求。
   - 实现需保证：
     - 在阶段开始前记录 Skip 标记；
     - 被跳过时依旧调用 `OnEnemyPhaseComplete`/`OnMoveComplete`，避免 `EnemyManager` 卡阶段；
     - 支持多个来源（可用简单列表记录）。
2. 在 `Enemy.StartPhase()` 内增加统一检查：若检测到 Skip 请求，则根据具体阶段采取动作：
   - 攻击/预告：直接触发完成事件并输出日志；
   - 移动：立即触发 `OnMoveComplete`，不进入协程。
3. `FreezingStatus` 通过接口调用 `RequestSkipOnce(this, "Freezing")`，并在状态移除后调用 `ClearSkipRequest`；在每个回合的 `OnTurnTrigger()` 可再次请求，以维持持续冻结。
4. 将需求记录在代码中（注释或 TODO），暂不实现复杂逻辑；未来可扩展 `RequestSkipUntilUnfrozen` 等更高级别接口。
5. 若有额外表现（动画/特效），仍通过 `TurnBasedStatusData.vfxPrefab` 等字段支持。

### Phase 2x — 扩展回合锁（备用方案）
- 若未来需要更复杂的控制需求，可在 `IEnemyTurnSkipper` 基础上拓展：
  - 添加 `RequestSkipUntilRelease`、`IsTurnLocked` 等方法，支持持续锁定与状态查询。
  - 在 `EnemyRuntimeState` 中维护 `HashSet<object> activeTurnLocks`，当集合为空时才放行行动。
  - 通过 `GameEventBus` 发布 `EnemyTurnLocked` / `EnemyTurnUnlocked` 事件，供 UI 或 AI 监听。
  - 结合行为树或动画系统，在锁定期间暂停移动/攻击动画。此阶段仅记录思路，暂不实现。

### Phase 3 — 资产与技能配置（优先级：中）
1. 创建 `FreezingStatus` ScriptableObject（位于 `Assets/Scriptable Objects/Status/`）：
   - 选择 `FreezingStatusBehaviourConfig` 类型；
   - 设置持续回合数、图标、颜色。
2. 新建或修改对应技能（如“冰霜射击”）：
   - `effectConfig` 指向新冰冻状态；
   - 确认触发条件与行为符合策划需求（可能与普通攻击/范围攻击对应）。
3. 确保技能名称唯一，避免 `SkillManager` key 重复；必要时调整描述文本。

### Phase 4 — 测试与验证（优先级：中）
1. 手动测试：
   - 玩家对敌人施加冰冻后，敌人本回合立即跳过行动，回合数递减；
   - 叠加技能后按配置刷新或延长回合；
   - 冻结结束时敌人恢复正常行动。
2. 验证与其他状态共存：
   - 同时附加点燃/中毒，确认 UI 能显示多个图标，DoT 仍正常触发。
3. 检查日志：`TurnBasedStatusEffect`、敌人控制器/行为配置应输出关键日志，便于发现逻辑问题。

- **行动阻断接口缺失**：需新增 `IEnemyTurnSkipper`（或同功能接口），并在 `Enemy` 中实现 `RequestSkipOnce`；状态脚本绝不直接操作具体行为逻辑。
- **多状态冲突**：冻结与其他控制类状态（眩晕、恐惧等）可能互斥；可在行为配置中加入标签，后续扩展互斥逻辑。
- **回合同步问题**：确保冻结回合数与游戏阶段同步，避免在玩家回合仍然递减。
- **性能与调试**：冻结状态无伤害结算，但会频繁查询/发布事件。保持事件监听粒度，避免重复订阅。

## ✅ 验收标准
- 冻结状态施加后，敌人本回合立即跳过行动，并在 UI 中显示剩余回合。
- 状态到期或被解除后，敌人恢复行动能力，相关标记/特效正确清理。
- 冻结与点燃、中毒同时存在时，各自逻辑互不影响，技能日志能正确反映多状态施加。
- 代码结构遵循现有多态框架，无需新增中心化 Manager。

