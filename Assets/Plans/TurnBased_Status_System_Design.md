# 回合制状态系统设计方案

## 📋 **文档信息**

- **创建时间**: 2025-11-05
- **版本**: v1.0
- **状态**: 设计阶段
- **优先级**: 中

---

## 🎯 **系统目标**

### **核心需求**
1. 支持回合制DoT（持续伤害）效果：点燃、中毒、流血等
2. 支持回合制Buff/Debuff：加速、减速、护盾等
3. 基于回合数计时，不是实时秒数
4. 支持层数叠加，可配置叠加规则
5. 在特定回合阶段触发效果（如敌人回合结束）
6. 易于扩展，添加新状态无需修改核心代码

### **非目标**
- ❌ 不支持实时状态效果（秒数计时）
- ❌ 不需要中心Manager统一tick（回合事件已解耦）
- ❌ 不需要复杂的状态机转换

---

## 🏗️ **系统架构**

### **三层架构**

```
┌─────────────────────────────────────────────────┐
│  Layer 1: 配置层 (ScriptableObject)               │
│  TurnBasedStatusData                            │
│  职责: 定义状态的静态配置                          │
│  - 状态ID、名称、图标                             │
│  - 基础回合数、基础伤害                           │
│  - 堆叠规则、触发时机                             │
│  - 特效预制体                                    │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  Layer 2: 运行时基类 (MonoBehaviour)              │
│  TurnBasedStatusComponent                       │
│  职责: 管理状态的运行时逻辑                        │
│  - 监听回合事件                                  │
│  - 管理剩余回合数                                │
│  - 处理堆叠逻辑                                  │
│  - 自动清理（回合数为0）                          │
│  - 提供抽象方法供子类实现具体效果                  │
└─────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│  Layer 3: 具体状态类 (继承基类)                    │
│  BurningStatus, PoisonStatus, FreezingStatus... │
│  职责: 实现具体的状态效果                          │
│  - 重写 OnTurnTrigger() 方法                     │
│  - 定义具体的效果逻辑（造成伤害、减速等）           │
└─────────────────────────────────────────────────┘
```

---

## 📦 **核心组件设计**

### **1. TurnBasedStatusData (ScriptableObject)**

**用途**: 配置状态效果的静态数据

**主要字段**:
- **基本信息**: statusID, displayName, icon, description
- **回合配置**: baseDurationInTurns（基础回合数）
- **伤害配置**: baseDamagePerTurn（基础每回合伤害，DoT类型使用）
- **触发时机**: triggerPhase（PlayerPhaseEnd / EnemyPhaseEnd）
- **堆叠规则**: stackMode, maxStacks
  - TurnAddition: 回合累加，伤害不变
  - DamageAddition: 伤害累加，回合不变
  - Both: 都累加
  - Refresh: 刷新回合数，不累加
- **视觉效果**: vfxPrefab, effectColor

**优势**:
- 可配置，设计师可以在Inspector中调整
- 无需代码即可创建新的状态效果配置
- 易于平衡调整

---

### **2. TurnBasedStatusComponent (抽象基类)**

**用途**: 所有回合制状态的通用逻辑基类

**核心职责**:
1. **事件监听**: 监听 GameEventBus.OnGameFlowStateChanged
2. **回合计数**: 管理 remainingTurns，每次触发后递减
3. **堆叠管理**: 实现 AddStack() 方法，根据配置的 stackMode 处理
4. **生命周期**: OnEnable/OnDisable 订阅/取消订阅事件
5. **自动清理**: remainingTurns <= 0 时自动 Destroy(this)
6. **特效管理**: 生成/销毁视觉特效

**抽象方法**:
- `OnTurnTrigger()`: 子类必须实现，定义每回合触发时的具体效果
- `OnStatusApplied()`: 可选重写，状态首次施加时的初始化逻辑
- `OnStatusRemoved()`: 可选重写，状态移除时的清理逻辑

**关键设计点**:
- MonoBehaviour，挂载到目标实体（敌人/玩家）身上
- 每个实体独立管理自己的状态
- 不需要中心Manager，完全事件驱动
- 利用Unity生命周期自动清理（实体销毁时自动销毁组件）

---

### **3. 具体状态类（继承基类）**

**BurningStatus (点燃)**:
- 继承 TurnBasedStatusComponent
- OnTurnTrigger(): 构造DamageEvent，调用 IDamageable.OnDamageReceived()
- 造成火焰魔法伤害

**PoisonStatus (中毒)**:
- 继承 TurnBasedStatusComponent
- OnTurnTrigger(): 造成毒伤害
- 可能有不同的伤害计算（如基于最大血量百分比）

**FreezingStatus (冰冻)**:
- 继承 TurnBasedStatusComponent
- OnStatusApplied(): 禁用移动
- OnTurnTrigger(): 减速或冻结效果递减
- OnStatusRemoved(): 恢复移动

---

### **4. TurnBasedStatusEffect (IEffect)**

**用途**: 技能系统中施加状态的效果

**核心逻辑**:
```
ExecuteEffect(args):
1. 从 args 获取目标（DamageEvent.Target）
2. 检查目标是否已有对应状态组件
3. 如果没有：AddComponent<具体状态类>()，调用 Initialize()
4. 如果有：调用 AddStack() 叠加
```

**关键点**:
- 不直接造成伤害，而是施加状态组件
- 状态组件自己管理生命周期和效果触发

---

## 🔄 **工作流程**

### **点燃技能触发流程**

```
1. 范围攻击角色停止，触发范围伤害
   ↓
2. DamageSystem 处理伤害，发布 DamageEvent
   └─ TriggerType = Stopped
   └─ Target = Enemy
   ↓
3. SkillManager 监听 OnDamage 事件
   ↓
4. DamageTrigger 检查条件
   └─ triggerTypes 包含 Stopped? ✅
   └─ 来源是范围攻击角色? ✅
   ↓
5. BurningEffect.ExecuteEffect()
   └─ 检查敌人是否已有 BurningStatus
       ├─ 没有 → AddComponent<BurningStatus>()
       │          └─ Initialize(data, source)
       └─ 有 → AddStack()（回合数累加）
   ↓
6. 敌人回合结束
   ↓
7. BurningStatus 监听到 EnemyPhaseEnd 事件
   ↓
8. OnTurnTrigger() 被调用
   └─ 构造 DamageEvent，造成火焰伤害
   └─ remainingTurns--
   └─ 如果 remainingTurns <= 0，Destroy(this)
   ↓
9. 重复步骤 6-8，直到回合数耗尽
```

---

## 🎨 **堆叠规则详解**

### **StackMode 枚举**

```
enum StackMode
{
    TurnAddition,     // 回合累加，伤害不变（点燃用这个）
    DamageAddition,   // 伤害累加，回合不变
    Both,             // 都累加
    Refresh,          // 刷新回合数，不累加
    NoStack           // 不可叠加（只刷新）
}
```

### **具体示例**

**点燃（TurnAddition）**:
```
第一次: 2回合，5伤害
再次施加: +2回合
结果: 4回合，5伤害
```

**中毒（Both）**:
```
第一次: 3回合，2伤害
再次施加: +3回合，+2伤害
结果: 6回合，4伤害（每回合）
```

**护盾（Refresh）**:
```
第一次: 2回合，10护盾值
再次施加: 刷新
结果: 2回合，10护盾值（不累加）
```

---

## 🔧 **技能系统集成**

### **如何在技能中使用**

**SkillConfig 配置**:
```
Skill: 点燃技能
├─ Trigger: DamageTrigger
│   ├─ triggerTypes = [Stopped]  ← 只在范围攻击时触发
│   └─ targetTag = "Enemy"
│
├─ Effect: TurnBasedStatusEffect
│   ├─ statusData = BurningStatusData（拖入配置）
│   └─ statusComponentType = typeof(BurningStatus)
│
└─ Source Character Name: "范围攻击角色"
```

---

## ⚙️ **分散组件 vs 中心管理器**

### **选择分散组件的理由** ⭐

**回合制的特点**:
- ✅ 不需要每帧Update（只在回合事件时触发）
- ✅ PhaseController 已经统一了回合切换
- ✅ 事件驱动，完全解耦

**分散组件的优势**:
1. **自动生命周期管理**
   - 实体销毁 → 组件自动销毁
   - 不需要手动清理 Dictionary
   
2. **职责分散**
   - 每个实体管理自己的状态
   - 不需要一个大Manager承担所有职责
   
3. **Inspector 友好**
   - 可以直接在Inspector中看到实体的状态
   - 易于调试

4. **事件性能**
   - 10个敌人 = 10个事件监听器
   - 但回合事件触发频率很低（每回合1次）
   - 性能完全可接受

**中心Manager的问题**:
- ❌ 需要维护 GameObject → Status 映射
- ❌ 需要手动处理实体销毁时的清理
- ❌ 集中式职责过重
- ❌ 回合制不需要统一tick，事件已经统一了

**结论**: 分散组件更适合回合制状态系统

---

## 📁 **文件结构**

```
Assets/Scripts/SkillSystem/
├─ TurnBasedStatus/
│   ├─ Core/
│   │   ├─ TurnBasedStatusData.cs        (ScriptableObject配置)
│   │   ├─ TurnBasedStatusComponent.cs   (MonoBehaviour抽象基类)
│   │   └─ StackMode.cs                  (堆叠模式枚举)
│   │
│   ├─ Statuses/
│   │   ├─ BurningStatus.cs              (点燃)
│   │   ├─ PoisonStatus.cs               (中毒)
│   │   └─ FreezingStatus.cs             (冰冻)
│   │
│   └─ Effects/
│       └─ TurnBasedStatusEffect.cs      (IEffect实现，施加状态)
│
└─ Triggers/
    └─ DamageTrigger.cs                  (已存在，监听伤害事件)
```

---

## 🔄 **实现步骤**

### **Phase 1: 核心框架（优先级：高）**

**目标**: 建立基础架构，实现点燃效果

**任务**:
1. 创建 `StackMode` 枚举
2. 创建 `TurnBasedStatusData` ScriptableObject
3. 创建 `TurnBasedStatusComponent` 抽象基类
   - 监听回合事件
   - 管理回合计数
   - 实现堆叠逻辑
   - 抽象方法：OnTurnTrigger()
4. 创建 `BurningStatus` 具体类
   - 继承基类
   - 实现 OnTurnTrigger() 造成伤害
5. 创建 `TurnBasedStatusEffect` IEffect实现
   - 施加状态组件到目标
6. 完善 `DamageTrigger` 触发器
   - 已基本实现，可能需要微调

**验收标准**:
- 范围攻击角色停止时，敌人被点燃
- 敌人每回合结束时受到火焰伤害
- 多次点燃会累加回合数
- 回合数耗尽后状态自动移除

---

### **Phase 2: 扩展其他状态（优先级：中）**

**目标**: 验证系统扩展性，添加2-3种其他状态

**任务**:
1. 创建 `PoisonStatusData` 配置
2. 创建 `PoisonStatus` 类（造成毒伤害）
3. 创建 `FreezingStatusData` 配置
4. 创建 `FreezingStatus` 类（减速/冻结）

**验收标准**:
- 无需修改基类，只需创建子类
- 每种状态有独特的表现和逻辑
- 多种状态可以同时存在于一个敌人身上

---

### **Phase 3: UI显示（优先级：低）**

**目标**: 在敌人血条或头顶显示状态图标

**任务**:
1. 创建状态图标UI组件
2. 查询实体的 TurnBasedStatusComponent 列表
3. 显示图标和剩余回合数
4. 响应状态添加/移除事件

**验收标准**:
- 被点燃的敌人头顶显示火焰图标
- 显示剩余回合数
- 多个状态叠加显示

---

## 🎯 **关键设计决策**

### **决策1: 分散组件而非中心Manager** ⭐

**选择**: 每个实体身上挂载 MonoBehaviour 状态组件

**理由**:
- 回合制不需要中心tick，PhaseController已经统一了
- 事件驱动，完全解耦
- 自动生命周期管理
- Inspector 友好，易于调试

---

### **决策2: 基于回合数而非秒数**

**选择**: 使用 `remainingTurns` 而不是 `timeRemaining`

**理由**:
- 回合制游戏的自然单位是回合，不是秒
- 一个回合的实际时长不固定
- 玩家理解更直观（"点燃3回合"）

---

### **决策3: 监听回合事件而非Update**

**选择**: 监听 `GameFlowState.EnemyPhaseEnd` 等事件

**理由**:
- 回合事件触发频率低（每回合1次）
- 精确控制触发时机
- 性能优异（不需要每帧轮询）

---

### **决策4: 抽象基类而非接口**

**选择**: `TurnBasedStatusComponent : MonoBehaviour` (抽象类)

**理由**:
- 需要 MonoBehaviour 的生命周期（OnEnable/OnDisable）
- 需要共享字段（remainingTurns, statusData）
- 需要共享逻辑（事件监听、堆叠、清理）
- 抽象类更适合"有共享实现的模板模式"

---

## 🔌 **与现有系统的集成**

### **技能系统集成**

```
SkillManager
├─ 已监听 OnDamage 事件 ✅
├─ 调用 SkillInstance.ProcessEvent() ✅
│
SkillInstance
├─ DamageTrigger.CheckEvent() 检查条件
├─ TurnBasedStatusEffect.ExecuteEffect()
│   └─ 添加状态组件到敌人
│
敌人身上
└─ BurningStatus 组件
    ├─ 监听回合事件
    └─ 自动触发效果
```

**无缝集成**:
- ✅ 复用现有触发器系统（DamageTrigger）
- ✅ 复用现有效果系统（IEffect）
- ✅ 只需添加新的 Effect 类型

---

### **伤害系统集成**

```
DamageEvent 结构体
└─ TriggerType: DamageTriggerType  ✅ 已存在！
    ├─ Collision
    ├─ Stopped
    ├─ Interval
    └─ Skill
```

**完美契合**:
- ✅ DamageEvent 已经有 TriggerType 字段
- ✅ DamageTrigger 可以过滤特定类型的伤害
- ✅ 无需修改伤害系统

---

## 📈 **扩展性分析**

### **添加新状态的步骤**

**示例：添加"流血"状态**

1. **创建配置** (5分钟)
   - 右键 > Create > Game > Turn Based Status Data
   - 设置：statusID="Bleeding", duration=3, damage=3

2. **创建状态类** (10分钟)
   ```csharp
   public class BleedingStatus : TurnBasedStatusComponent
   {
       protected override void OnTurnTrigger()
       {
           // 造成物理伤害（而不是魔法伤害）
           // 其他逻辑由基类处理
       }
   }
   ```

3. **创建施加效果** (5分钟)
   - 复用 TurnBasedStatusEffect，只需配置不同的 statusData

4. **配置技能** (5分钟)
   - 在 SkillConfig 中添加 DamageTrigger + TurnBasedStatusEffect

**总计：25分钟添加一个新状态！**

---

### **支持的状态类型**

**DoT类型（持续伤害）**:
- 点燃：火焰伤害
- 中毒：毒伤害
- 流血：物理伤害
- 腐蚀：护甲降低

**Buff类型（增益）**:
- 加速：移动速度提升
- 护盾：额外护甲值
- 狂暴：攻击力提升

**Debuff类型（减益）**:
- 减速：移动速度降低
- 虚弱：攻击力降低
- 冰冻：无法移动

**所有这些只需继承基类，实现 OnTurnTrigger() 即可！**

---

## ⚠️ **潜在风险和解决方案**

### **风险1: 多个状态组件性能**

**问题**: 如果一个敌人身上有5个状态，就有5个事件监听器

**分析**:
- 回合事件触发频率低（每回合1次，可能10秒1次）
- 10个敌人 x 5个状态 = 50个监听器
- 对于回合制游戏，完全可接受

**结论**: 不是问题 ✅

---

### **风险2: 状态之间的交互**

**问题**: 如果"点燃"和"冰冻"互相抵消怎么办？

**解决方案**:
- 在 TurnBasedStatusComponent 中添加 `OnOtherStatusApplied()` 回调
- 子类可以检查其他状态并做出反应
- 或者在 TurnBasedStatusEffect 中检查兼容性

**预留扩展接口即可**

---

### **风险3: 状态持久化（切场景）**

**问题**: 敌人身上的状态在切场景后会丢失

**分析**:
- 当前设计：敌人不跨场景，状态丢失不是问题
- 如果需要：可以在 TeamData/EnemyData 中保存状态快照
- 或者让状态组件支持序列化

**结论**: 当前阶段不需要处理，预留接口即可

---

## 📊 **实现难度评估**

### **核心框架（Phase 1）**

**难度**: ⭐⭐ (中等)

**预计工作量**: 2-3小时

**关键点**:
- 抽象基类设计要合理（平衡通用性和灵活性）
- 堆叠逻辑要完善
- 事件监听要正确

---

### **扩展新状态（Phase 2）**

**难度**: ⭐ (简单)

**预计工作量**: 每个状态 30分钟

**关键点**:
- 基类设计得好，扩展很简单
- 只需实现 OnTurnTrigger()

---

## ✅ **设计优势总结**

### **相比现有 RuntimeStatusEffect**:
- ✅ 完全适配回合制（不是"妥协"或"兼容"）
- ✅ 基于回合数，不是秒数
- ✅ 事件驱动，不是每帧Update

### **相比单独的 BurningStatus**:
- ✅ 高度抽象，易于扩展
- ✅ 配置化，无需写代码添加新状态
- ✅ 统一的堆叠/计数逻辑

### **架构清晰度**:
- ✅ 三层架构，职责分明
- ✅ 配置层、运行时层、具体实现层分离
- ✅ 符合开闭原则（对扩展开放，对修改封闭）

---

## 🎯 **下一步行动**

### **立即开始（推荐）**:
1. 实现 Phase 1 核心框架
2. 验证点燃效果
3. 根据实际效果调整设计

### **或者继续讨论**:
- 堆叠规则是否需要更复杂的配置？
- 是否需要状态之间的互斥/抵消机制？
- 是否需要状态免疫系统？

---

**这个设计既保持了高度抽象，又完全适配回合制，且不需要中心Manager。你觉得如何？** 🎯

