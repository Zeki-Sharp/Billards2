# 回合制状态多态配置改造计划

## 🎯 目标
- 让技能依旧通过引用单个 `TurnBasedStatusData` 资产施加状态。
- 将状态配置改造成多态结构，不同状态在配置层即可选择对应组件与参数。
- 支持中毒类需求：按当前叠层造成伤害，结算后层数递减。
- 保持现有事件驱动与解耦架构，遵循最小改动原则。

## 🔍 当前问题
- `TurnBasedStatusData` 只有基础回合与伤害字段，无法描述堆叠、衰减等差异。
- `TurnBasedStatusEffect` 写死了点燃逻辑，无法按配置切换状态组件。
- `TurnBasedStatusComponent` 缺少层数与多态回合处理的抽象。
- 技能端无法通过配置扩展出中毒、冰冻等特殊堆叠规则。

## 🧩 方案概述
1. **抽象配置基类**：将 `TurnBasedStatusData` 改为抽象 ScriptableObject，保留通用信息，新增虚方法（获取组件类型、初始化运行时、叠层处理）。
2. **具体配置派生类**：为点燃、中毒等状态创建派生配置 SO，暴露各自参数（如 `damagePerStack`、`stackDecayPerTurn`）。
3. **组件层对接**：状态组件继承 `TurnBasedStatusComponent`，支持 `currentStacks` 等字段；根据派生配置传入的参数完成结算。
4. **技能效果改造**：`TurnBasedStatusEffect` 仅与抽象配置交互，通过配置提供的组件类型创建/获取状态实例，再调用配置的初始化与叠加逻辑。
5. **数据迁移与兼容**：迁移已有点燃资产为新派生类型，更新技能引用；补齐默认实现保持旧行为。

## 🛠️ 实施步骤

### Phase 1 — 基础抽象搭建（优先级：高）
- 将 `TurnBasedStatusData` 改为抽象类，新增：
  - `public abstract System.Type GetComponentType();`
  - `public virtual void ApplyInitialValues(TurnBasedStatusComponent component);`
  - `public virtual void OnStackApplied(TurnBasedStatusComponent component);`
- 在 `TurnBasedStatusComponent` 中加入 `currentStacks`、通用栈查询与通知接口。
- 调整 `TurnBasedStatusEffect`：使用配置返回的组件类型执行添加/叠加，不再写死点燃。

### Phase 2 — 中毒派生实现（优先级：高）
- 新建 `PoisonStatusData : TurnBasedStatusData`，字段示例：`initialStacks`、`stackGain`、`damagePerStack`、`decayPerTurn`、`maxStacks`。
- 新建 `PoisonStatus : TurnBasedStatusComponent`，实现：
  - `OnTurnTrigger()` 时 `damage = currentStacks * damagePerStack`。
  - 结算后 `currentStacks = Mathf.Max(0, currentStacks - decayPerTurn)`，同步 `remainingTurns`。
- 在 `ApplyInitialValues` / `OnStackApplied` 中维护栈数与伤害参数。

### Phase 3 — 其他状态迁移与回归测试（优先级：中）
- 将点燃改成 `BurningStatusData` + `BurningStatus`，保持旧行为（回合数累加、伤害固定）。
- 复查 UI、日志、事件监听是否按新字段更新。
- 针对施加/叠层/回合结算/清除流程编写或更新自动化测试与手动用例。

## 📦 数据与配置注意事项
- 迁移资产时保留原有 GUID，避免技能引用失效；必要时使用 Odin 序列化升级或一次性脚本迁移。
- Inspector 通过 Odin 多态绘制展示派生类型，确保策划能够直接选择中毒或其他状态并编辑参数。
- 若存在旧的技能引用基础类资产，需要批量替换为派生实例。

## ✅ 验收标准
- 技能拖入任意派生状态配置即可正确施加对应状态，无需代码改动。
- 中毒状态能够按叠层造成伤害，并在每个回合自动减少层数。
- 点燃等旧状态功能保持一致，无额外回归问题。
- UI 与日志能正确显示层数、回合和伤害等信息。

