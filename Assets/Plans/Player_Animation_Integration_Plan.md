# 玩家角色动画集成计划

> **创建日期**：2025年11月10日  
> **状态**：设计阶段  
> **优先级**：⭐⭐

---

## 📋 需求概述

### 功能描述
- 将导入的三套角色动画（`Idle`、`Run`、`Attack`）接入现有玩家预制体。
- 在球体待机时播放 `Idle`，物理移动中播放 `Run`，碰撞造成伤害瞬间播放 `Attack`。
- 不破坏当前多角色/事件驱动架构，保持 `PlayerVisualController` 作为视觉层入口。

### 关键约束
- 继续遵循组件解耦：数据、模型、视图分离，逻辑集中在视觉控制器层。
- 复用现有事件（`PlayerStateMachine` 状态、`GameEventBus` 碰撞/伤害事件），不新增冗余轮询。
- 采用最小修复原则，在原有脚本基础上增量扩展。

---

## 🎯 设计目标

1. **统一动画控制**  
   - 所有角色复用同一基础 Animator 状态机，通过覆盖资源实现差异化。
2. **数据驱动角色配置**  
   - 在 `PlayerData` 中新增动画配置引用，避免硬编码。
3. **稳定事件触发**  
   - 依托状态机和伤害事件驱动动画切换，确保 Idle/Run/Attack 切换准确可靠。
4. **易于扩展**  
   - 未来新增角色或动画组时，只需要新建配置，无需改动核心脚本。

---

## 🧩 资源与配置改动

### 1. Animator 与 AnimationClip 资产
- 创建基础 `AnimatorController`（建议放置在 `Assets/Art/Animations/Player/`）：
  - 参数：`bool IsMoving`、`Trigger AttackTrigger`。
  - 状态：`Idle`（默认）、`Run`、`Attack`。  
    - `Idle ↔ Run` 使用 `IsMoving` 的切换条件。  
    - `Any State → Attack` 使用 `AttackTrigger`，`Attack` 状态结束后根据 `IsMoving` 返回对应状态。
- 将新导入的 `Idle/Run/Attack` `AnimationClip` 资产按角色命名归档，例如 `Anim_PlayerA_Idle.anim`。
- 基于基础控制器，为每个角色生成 `AnimatorOverrideController`（或等价配置 ScriptableObject），覆盖三条状态的 Clip。

### 2. 数据配置
- 在 `PlayerData` 中新增两个字段：  
  - `RuntimeAnimatorController animatorController`（可直接放基础 Controller 或 Override Controller）  
  - `float attackTriggerCooldown`（默认 0.1 秒，用于节流攻击动画）  
- 若某角色没有 Override 控制器，保持字段为空即可继续使用基础动画/静态图标。

### 3. 预制体调整
- 在 `Player` 预制体的 `Image (1)` 对象上添加 `Animator` 组件，指定基础 `AnimatorController`。
- 确保 `Image (1)` 的 `SpriteRenderer` 材质保持不变（防止现有闪烁特效失效）。
- 检查场景中所有玩家实例是否引用了最新的 `Player` 预制体。

---

## 🔄 脚本级改动步骤

### 1. `PlayerVisualController`
1. 新增 `Animator characterAnimator` 序列化引用，并在 `OnValidate`/Inspector 中校验。
2. 在 `ApplyVisuals(PlayerData playerData)` 中：
   - 继续设置静态图标（兼容旧配置）。  
   - 读取 `playerData.animatorOverrideController` 和 `playerData.attackTriggerCooldown`，应用 Override 控制器并缓存冷却时间。  
   - 初始化动画参数（`IsMoving = false`，重置 Trigger）。
3. 生命周期处理：
   - `OnEnable` 订阅以下事件：  
     - `PlayerStateMachine.OnStateChanged` → 更新 `IsMoving`。  
     - `GameEventBus.OnBallStarted` / `OnBallStopped` → 兜底同步移动状态。  
     - `GameEventBus.OnDamage` → 当 `damageEvent.Source == gameObject` 时触发 `AttackTrigger`。  
   - `OnDisable` 清理订阅。
4. 维护状态：
   - 内部缓存 `PlayerStateMachine`、`BallPhysics`、`PlayerBehavior` 引用，避免重复查找。
   - 防止攻击动画过度触发：添加冷却计时或在触发后等待状态机回到 Idle/Run。

### 2. 事件过滤与辅助功能
- 在视觉控制器内部实现归属校验：对 `GameEventBus` 的全局事件过滤 `characterID` 或 `GameObject` 实例。
- 如需要更精准的“撞击瞬间”触发，可联动 `DamageSystem` `CollisionEvent`：  
  - 订阅 `GameEventBus.OnCollision`，当 `evt.Source == gameObject` 且规则允许时再触发 `AttackTrigger`。  
  - 选择其中一种事件即可，优先使用 `OnDamage`，确保只在造成实际伤害时播放。

### 3. 兼容性与回退策略
- 若某角色缺少 Override 控制器：  
  - `Animator` 保持在 `Idle` 状态，不触发运行或攻击。  
  - `PlayerVisualController` 应记录警告日志，但不中断流程。
- 对于未来新增动画层（如受击、死亡）：  
  - 预留 `Animator` 参数接口，保持扩展性。

---

## ✅ 执行顺序建议

1. **资产整理**：导入/命名三类动画 Clip，创建基础 Animator 控制器或 Override 资源。  
2. **数据补齐**：为各个 `PlayerData` 设置对应的 `RuntimeAnimatorController` 与冷却时间。  
3. **Prefab 配置**：更新 `Player` 预制体（添加 `Animator`、拖入引用）。  
4. **脚本扩展**：按步骤修改 `PlayerVisualController`，实现事件驱动逻辑。  
5. **联调验证**：  
   - 检查 Idle ↔ Run 切换随状态机变化。  
   - 模拟碰撞确认 Attack 触发。  
   - 验证旧角色无配置时的回退行为。  
6. **文档与资产标注**：在项目说明中记录新的动画配置流程，确保团队成员一致。

---

## 🧪 测试要点

- 单角色测试：在编辑器播放模式下观察 `IsMoving` 参数变化。  
- 多角色并存：切换角色或回合时，确认不同 `PlayerData` 使用各自的动画覆盖。  
- 极端场景：  
  - 发射力度为 0（直接停留在 Idle）。  
  - 快速连续撞击敌人时 Attack Trigger 不丢失、不连播。  
  - 玩家死亡或禁用时取消事件订阅，不产生报错。

---

## 📎 相关依赖

- `PlayerStateMachine`（提供状态切换事件）。  
- `GameEventBus`（广播物理与伤害事件）。  
- `DamageSystem`（触发伤害事件）。  
- `EffectManager`（可扩展添加视觉特效，与动画配合使用）。

---

## ⚠️ 风险与缓解

| 风险 | 描述 | 缓解措施 |
|------|------|----------|
| 动画与 VFX 冲突 | `Image (1)` 上已有特效脚本（如 `MM Scale/Position Shaker`） | 在测试中确认动画与特效共存，必要时通过层级拆分或禁用冲突效果 |
| 事件触发过于频繁 | 撞击帧率高导致 Attack 动画被反复触发 | 在视觉控制器中实现触发冷却或“正在攻击”标记 |
| 未配置动画的角色报错 | 旧数据缺少动画配置 | 提供默认回退逻辑，并在 Editor 下打印一次性警告 |

---

## 📌 后续扩展建议

- 若后续需要更多动画字段，可考虑重新抽象 `PlayerAnimationProfile` 或其他数据容器，以便复用。  
- 结合 `ScriptableRender` 特效或 `Animator` 分层，实现攻击时的光效同步。  
- 为角色选择界面复用同一动画配置，保持角色展示一致性。


