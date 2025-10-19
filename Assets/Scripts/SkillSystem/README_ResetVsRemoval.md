# 技能系统：重置条件 vs 效果移除条件

> **文档目的**: 明确说明 IResetCondition 和 IEffectRemovalCondition 的区别和使用场景  
> **创建日期**: 2025-10-19  
> **文档版本**: v1.0

---

## 📋 快速理解

| 特性 | IResetCondition（重置条件） | IEffectRemovalCondition（效果移除条件） |
|------|---------------------------|----------------------------------|
| **核心作用** | 控制技能何时可以**再次触发** | 控制持续效果何时**被移除** |
| **影响对象** | 技能的触发条件（Condition） | 技能的持续效果（如属性修改器） |
| **典型操作** | 重置计数器 + 允许再次执行 | 移除属性修改器 |
| **生命周期** | 管理技能的重用周期 | 管理效果的持续周期 |
| **必需性** | ✅ 所有技能都需要 | ⚠️ 仅持续效果需要 |

---

## 🎯 核心概念解释

### IResetCondition（重置条件）

**定义**: 决定技能何时可以再次触发执行。

**工作流程**:
1. 技能触发并执行后，进入"冷却"状态（`canExecute = false`）
2. 当重置条件满足时，解除冷却（`canExecute = true` + 重置计数器）
3. 技能可以再次触发

**代码位置**: `Assets/Scripts/SkillSystem/Interfaces/IResetCondition.cs`

```csharp
public interface IResetCondition
{
    string ConditionName { get; }
    void Initialize();
    bool ShouldReset(object eventData);  // 核心：决定何时重置
    void Reset();
    void SetTargetSkillInstanceId(string skillInstanceId);
}
```

---

### IEffectRemovalCondition（效果移除条件）

**定义**: 决定持续性效果何时从游戏中移除。

**工作流程**:
1. 技能执行后，应用持续效果（如攻击力+50%）
2. 效果持续存在，持续影响游戏状态
3. 当移除条件满足时，移除效果（攻击力恢复正常）

**代码位置**: `Assets/Scripts/SkillSystem/Conditions/IEffectRemovalCondition.cs`

```csharp
public interface IEffectRemovalCondition
{
    string ConditionName { get; }
    void Initialize();
    bool ShouldRemoveEffect(object eventData);  // 核心：决定何时移除效果
    void Reset();
}
```

---

## 🔄 生命周期对比

### 技能完整生命周期图

```
技能触发 ────────────────────────────────────────────────────────┐
    │                                                             │
    ▼                                                             │
检查触发器 (ITrigger)                                              │
    │                                                             │
    ▼                                                             │
检查条件 (ICondition) ──── 不满足 ──► 继续等待                      │
    │                                                             │
    │ 满足                                                         │
    ▼                                                             │
执行效果 (IEffect)                                                 │
    │                                                             │
    ├─► 【瞬时效果】                                               │
    │   └─► 立即完成                                              │
    │                                                             │
    └─► 【持续效果】                                               │
        ├─► 应用属性修改器 (如攻击力+50%)                          │
        │                                                         │
        │   [效果移除条件生命周期开始]                             │
        │   │                                                     │
        │   │  效果持续生效中...                                  │
        │   │  ↓                                                 │
        │   │  检查移除条件 (IEffectRemovalCondition)             │
        │   │  ↓                                                 │
        │   │  满足条件？                                        │
        │   │  ├─► 是 ──► 移除效果 (攻击力恢复)                  │
        │   │  └─► 否 ──► 继续生效                               │
        │   │                                                     │
        │   [效果移除条件生命周期结束]                             │
        │                                                         │
        └──────────────────────────────────────────────┐          │
                                                       │          │
[重置条件生命周期开始]                                  │          │
│                                                      │          │
│  技能进入冷却 (canExecute = false)                   │          │
│  ↓                                                   │          │
│  检查重置条件 (IResetCondition)                       │          │
│  ↓                                                   │          │
│  满足条件？                                          │          │
│  ├─► 是 ──► 重置技能 (canExecute = true)             │          │
│  │          └──────────────────────────────────────────────┘
│  │                                                    
│  └─► 否 ──► 继续冷却                                  
│                                                       
[重置条件生命周期结束]                                  
```

---

## 📖 使用场景与示例

### 场景1: 碰撞连击技能

**需求**: 碰撞敌人2次后，攻击力提升100%，持续30秒

**配置**:

| 组件 | 配置 |
|------|------|
| **Trigger** | `CollisionTrigger` (碰撞敌人) |
| **Condition** | `CountCondition` (计数=2) |
| **Effect** | `StatModifierEffect` (攻击力×2) |
| **ResetCondition** | `ImmediateResetCondition` (立即重置) |
| **EffectRemovalCondition** | `DurationEffectRemovalCondition` (30秒后移除) |

**生命周期**:
```
时间轴: 0s ───────── 2s ───────── 32s ────────►

       碰撞1次      碰撞2次       30秒后
         │           │            │
         ▼           ▼            ▼
      计数+1       触发技能     移除效果
                     │
                     ├─► 攻击力×2 (持续30秒)
                     └─► 立即重置 (可以再次碰撞计数)
```

**关键点**:
- ✅ **ResetCondition = Immediate**: 技能执行后立即可以再次计数
- ✅ **EffectRemovalCondition = Duration(30s)**: 攻击力加成持续30秒

---

### 场景2: 满血伤害加成

**需求**: 满血时伤害+100%，血量<100%时失效

**配置**:

| 组件 | 配置 |
|------|------|
| **Trigger** | `AlwaysTrueTrigger` (始终监听) |
| **Condition** | `ValueComparisonCondition` (血量=100%) |
| **Effect** | `StatModifierEffect` (伤害×2) |
| **ResetCondition** | `ValueComparisonResetCondition` (血量<100%时重置) |
| **EffectRemovalCondition** | `ValueComparisonEffectRemovalCondition` (血量<100%时移除) |

**生命周期**:
```
血量: 100% ─────── 95% ─────── 100% ──────►

      满血          受伤          回满血
       │            │             │
       ▼            ▼             ▼
    触发技能      移除效果       再次触发
       │            │             │
    伤害×2      伤害恢复      伤害×2
```

**关键点**:
- ✅ **ResetCondition 和 EffectRemovalCondition 配置相同**: 血量<100%时同时重置技能和移除效果
- ✅ 两者**职责不同但时机相同**

---

### 场景3: 回合制技能

**需求**: 回合开始时获得护盾，回合结束时护盾消失

**配置**:

| 组件 | 配置 |
|------|------|
| **Trigger** | `DataSourceTrigger` (回合开始事件) |
| **Condition** | `AlwaysTrueCondition` (无条件) |
| **Effect** | `StatModifierEffect` (防御力+50) |
| **ResetCondition** | `OnPhaseEndedResetCondition` (回合结束重置) |
| **EffectRemovalCondition** | `OnPhaseEndedEffectRemovalCondition` (回合结束移除) |

**生命周期**:
```
回合: 玩家回合1 ─── 回合结束 ─── 玩家回合2 ───►

        │            │            │
        ▼            ▼            ▼
     触发技能      移除+重置    再次触发
        │            │            │
     防御+50      防御恢复     防御+50
```

**关键点**:
- ✅ 每个回合都会触发一次
- ✅ ResetCondition 和 EffectRemovalCondition 时机相同

---

## 🤔 常见混淆点解答

### Q1: 为什么需要两个条件？不能合并吗？

**答**: 不能合并，因为它们管理**不同的生命周期**：

- **ResetCondition**: 管理技能的**触发周期**
  - 示例：技能执行后立即可以再次计数（Immediate）
  
- **EffectRemovalCondition**: 管理效果的**持续周期**
  - 示例：效果持续30秒才消失（Duration）

两者**可以独立配置不同的时机**。

---

### Q2: 什么时候两者配置相同？

**答**: 当希望"技能重置"和"效果移除"同时发生时：

**示例**:
- 满血伤害加成：血量<100%时，既要移除效果，也要重置技能（以便满血后再次触发）
- 回合制技能：回合结束时，既要移除效果，也要重置技能（以便下回合再次触发）

---

### Q3: 如果只配置了ResetCondition，没有EffectRemovalCondition会怎样？

**答**: 取决于效果类型：

- **瞬时效果**（如治疗）: 
  - ✅ 不需要 EffectRemovalCondition
  - 效果立即完成，无持续状态

- **持续效果**（如属性修改）:
  - ❌ 必须配置 EffectRemovalCondition
  - 否则效果会**永久存在**（直到场景切换）

---

### Q4: 重置条件和移除条件的事件来源一样吗？

**答**: 是的，它们都监听**相同的游戏事件**：

```csharp
// 两者都会收到这些事件
- OnDamageProcessed
- OnHealthChanged
- OnGameFlowStateChanged (回合结束等)
- OnSkillExecuted
```

但它们**对事件的响应不同**：
- **ResetCondition**: 响应后重置技能状态
- **EffectRemovalCondition**: 响应后移除效果

---

## 💻 代码实现对比

### ResetCondition 实现示例

```csharp
// 立即重置条件
public class ImmediateResetCondition : IResetCondition
{
    public bool ShouldReset(object eventData)
    {
        // 技能执行后立即重置
        if (eventData is SkillExecutedEventData skillEvent)
        {
            return skillEvent.SkillInstanceId == targetSkillInstanceId;
        }
        return false;
    }
}
```

**效果**: 技能执行后立即可以再次触发

---

### EffectRemovalCondition 实现示例

```csharp
// 持续时间移除条件
public class DurationEffectRemovalCondition : IEffectRemovalCondition
{
    private float duration = 30f;
    private float startTime;
    
    public void Initialize()
    {
        startTime = Time.time;
    }
    
    public bool ShouldRemoveEffect(object eventData)
    {
        // 30秒后移除效果
        return Time.time - startTime >= duration;
    }
}
```

**效果**: 效果应用30秒后自动移除

---

## 📚 配置资产示例

### 示例1: 碰撞连击技能配置

```yaml
技能名称: "碰撞连击"
触发器: CollisionTrigger (targetTag: "Enemy")
条件: CountCondition (requiredCount: 2)
效果: StatModifierEffect (Damage ×2.0)
重置条件: ImmediateResetCondition
效果移除: DurationEffectRemovalCondition (duration: 30s)
```

### 示例2: 满血伤害加成配置

```yaml
技能名称: "满血时伤害增加"
触发器: AlwaysTrueTrigger
条件: ValueComparisonCondition (Health == 1.0)
效果: StatModifierEffect (Damage ×2.0)
重置条件: ValueComparisonResetCondition (Health < 1.0)
效果移除: ValueComparisonEffectRemovalCondition (Health < 1.0)
```

---

## 🎨 设计原则

### 单一职责原则

- **IResetCondition**: 只负责技能的重用逻辑
- **IEffectRemovalCondition**: 只负责效果的持续时间管理

两者分离，各司其职，符合 **SOLID** 原则。

---

### 灵活组合

通过分离两个条件，可以实现各种复杂的技能机制：

| 重置条件 | 效果移除条件 | 效果 |
|---------|------------|------|
| Immediate | Duration(30s) | 快速连击，效果持续 |
| OnPhaseEnded | OnPhaseEnded | 回合制技能 |
| Never | Duration(10s) | 一次性技能，效果有限时 |
| ValueComparison | ValueComparison | 条件触发技能 |

---

## 🔍 调试技巧

### 如何查看当前状态？

在运行时，技能系统会输出详细日志：

```
[SkillInstance] 🎯 技能执行成功 - 碰撞连击
[StatModifierEffect] 应用修改器: Damage ×2.0
[ImmediateResetCondition] 检测到技能执行完毕，立即重置
[DurationEffectRemovalCondition] 开始计时: 30秒
...
[DurationEffectRemovalCondition] 30秒已到，移除效果
[StatModifierEffect] 移除修改器: Damage ×2.0
```

---

### 常见问题排查

| 问题 | 可能原因 | 解决方案 |
|------|---------|---------|
| 技能只触发一次 | ResetCondition未配置或不满足 | 检查重置条件配置 |
| 效果永不消失 | EffectRemovalCondition未配置 | 为持续效果添加移除条件 |
| 效果立即消失 | 移除条件立即满足 | 检查移除条件配置 |
| 技能频繁触发 | ResetCondition为Immediate | 如需冷却，改用其他重置条件 |

---

## 📊 总结表格

| 维度 | IResetCondition | IEffectRemovalCondition |
|------|----------------|------------------------|
| **目的** | 控制技能重用 | 控制效果持续 |
| **作用时机** | 技能执行后 | 效果应用后 |
| **影响对象** | 技能触发器+条件 | 持续效果（修改器） |
| **必需性** | 所有技能都需要 | 仅持续效果需要 |
| **典型配置** | Immediate, OnPhaseEnded, Never | Duration, OnPhaseEnded, ValueComparison |
| **生命周期** | 短（通常秒级） | 长（可达数十秒） |

---

## 🔗 相关文档

- [技能系统架构分析](../Plans/SkillSystem_Architecture_Analysis_And_Refactoring_Priority.md)
- [IResetCondition 接口](Interfaces/IResetCondition.cs)
- [IEffectRemovalCondition 接口](Conditions/IEffectRemovalCondition.cs)
- [SkillConfig 配置](SkillConfig.cs)

---

## 📝 更新日志

- **v1.0** (2025-10-19): 初始版本，完整文档化重置与移除条件的区别

