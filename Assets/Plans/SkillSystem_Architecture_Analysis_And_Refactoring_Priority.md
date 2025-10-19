# 技能系统架构分析与重构优先级计划

> **文档版本**: v1.0  
> **创建日期**: 2025-10-19  
> **文档目的**: 全面分析当前技能系统架构存在的问题，并制定分优先级的重构计划  
> **重要说明**: ⚠️ 本文档仅包含问题分析和重构策略，不包含代码实现细节

---

## 📋 目录

1. [系统概览](#系统概览)
2. [架构问题分析](#架构问题分析)
3. [重构优先级划分](#重构优先级划分)
4. [实施路线图](#实施路线图)
5. [风险评估](#风险评估)
6. [成功指标](#成功指标)

---

## 系统概览

### 当前架构组成

```
技能系统架构
├── 数据层
│   ├── SkillConfig (ScriptableObject)
│   ├── TriggerConfig
│   ├── ConditionConfig
│   ├── EffectConfig
│   ├── ResetConditionConfig
│   └── EffectRemovalConfig / RemovalConditionConfig (重复)
│
├── 管理层
│   ├── SkillManager (单例，跨场景保留)
│   └── SkillStateManager
│
├── 运行时层
│   ├── SkillInstance
│   ├── ITrigger 接口及实现
│   ├── ICondition 接口及实现
│   ├── IEffect 接口及实现
│   ├── IResetCondition 接口及实现
│   └── IEffectRemovalCondition / IRemovalCondition (重复)
│
└── 集成层
    ├── DamageProcessor (伤害处理)
    ├── GameEventBus (事件总线)
    └── PlayerStatsManager (属性管理)
```

### 架构优点

- ✅ **接口抽象设计良好**: ITrigger/ICondition/IEffect清晰分离关注点
- ✅ **配置驱动**: ScriptableObject支持可视化配置
- ✅ **事件驱动**: 与GameEventBus良好集成
- ✅ **Odin Inspector集成**: 提升编辑器体验
- ✅ **单例模式**: 支持跨场景数据保留
- ✅ **扩展性**: 易于添加新的Trigger/Condition/Effect类型

---

## 架构问题分析

### 问题分类统计

| 问题类别 | 严重程度 | 影响范围 | 优先级 |
|---------|---------|---------|--------|
| 概念重复与冗余 | 🔴 严重 | 全系统 | P0 |
| 职责混淆 | 🟡 中等 | 核心类 | P1 |
| 过度抽象 | 🟡 中等 | 复合条件 | P2 |
| 性能问题 | 🟢 轻微 | 局部 | P3 |
| 代码质量 | 🟡 中等 | 全系统 | P1 |

---

### 🔴 P0级问题：概念重复与冗余

#### 问题1: 三套移除条件系统并存

**问题描述**:
系统中同时存在三套功能重叠的移除条件系统，造成严重的代码冗余和概念混淆。

**三套系统**:
1. `IRemovalCondition` (RemovalConditions目录)
   - 位置: `Assets/Scripts/SkillSystem/RemovalConditions/`
   - 类: OnPlayerPhaseEndedCondition, DurationCondition, ImmediateRemoveCondition, NeverRemoveCondition
   - 状态: **未被使用**，完全冗余

2. `IEffectRemovalCondition` (EffectRemovalConditions目录)
   - 位置: `Assets/Scripts/SkillSystem/Conditions/EffectRemovalConditions/`
   - 类: OnPhaseEndedEffectRemovalCondition, DurationEffectRemovalCondition, ValueComparisonEffectRemovalCondition等
   - 状态: **实际使用的系统**

3. `RemovalConditionConfig` (独立配置)
   - 位置: `Assets/Scripts/SkillSystem/Configs/RemovalConditionConfig.cs`
   - 状态: 与EffectRemovalConfig功能重叠

**接口对比**:
```
IRemovalCondition:
- Initialize()
- ShouldRemove(object eventData)
- Reset()

IEffectRemovalCondition:
- Initialize()
- ShouldRemoveEffect(object eventData)
- Reset()
```
两个接口几乎完全相同，仅方法名略有差异。

**影响**:
- 代码冗余度: **70%**
- 新人理解成本: 极高
- 维护成本: 需要同时维护两套相似系统
- Bug风险: 修改一处可能忘记另一处

**根本原因**:
历史遗留问题，重构过程中创建了新系统但未删除旧系统。

---

#### 问题2: DataExtractor重复实现

**问题描述**:
在4个不同的Config类中，重复实现了完全相同的`GetDataExtractor`方法。

**重复位置**:
1. `TriggerConfig.GetDataExtractor()`
2. `ConditionConfig.SingleConditionConfig.GetDataExtractor()`
3. `EffectRemovalConfig.GetDataExtractor()`
4. `ResetConditionConfig.SingleResetConditionConfig.GetDataExtractor()`

每个方法都是完全相同的switch-case代码块（约30行代码）。

**矛盾点**:
系统已经有`DataExtractors`静态类提供统一的数据提取器：
```
public static class DataExtractors {
    public static Func<object, float> HealthExtractor = ...
    public static Func<object, float> AttackExtractor = ...
    ...
}
```

但各Config类完全没有使用这个静态类，而是重新实现相同逻辑。

**影响**:
- 代码重复度: **100%** (4处完全相同)
- 维护成本: 修改提取逻辑需要改4个地方
- 一致性风险: 4处实现可能不一致

---

#### 问题3: StatModifier与SkillDamageModifier重复

**问题描述**:
系统中有两套功能重叠的修改器系统。

**两套系统**:
1. `StatModifier` (通用属性修改器)
   - 管理者: PlayerStatsManager
   - 适用: 所有属性（攻击、防御、速度等）
   - 实现: 通用的修改器模式

2. `SkillDamageModifier` (专门的伤害修改器)
   - 管理者: DamageProcessor
   - 适用: 仅伤害属性
   - 实现: IDamageModifier接口

**硬编码判断**:
在`StatModifierEffect.ExecuteEffect()`中：
```
if (targetStat == "Damage") {
    return ExecuteDamageModification();  // 特殊路径
} else {
    return ExecuteStatModification();    // 通用路径
}
```

这种硬编码的if-else违反了**开闭原则**（OCP）。

**问题分析**:
- 为什么伤害需要特殊处理？是否真的有必要？
- 如果有必要，是否应该所有属性都走统一的处理器模式？
- 两套系统增加了理解和维护成本

---

### 🟡 P1级问题：职责混淆

#### 问题4: SkillInstance职责过重

**当前职责**:
1. 组件生命周期管理 (Initialize, Reset)
2. 事件分发与处理 (ProcessEvent, HandleSkillExecutedEvent, HandlePhaseEndEvent)
3. 事件发布 (PublishSkillExecutedEvent)
4. 组件协调 (管理5个不同接口实例)
5. 唯一ID生成与管理
6. 调试日志输出

**违反原则**:
- 单一职责原则 (SRP)
- 一个类做了太多事情

**建议拆分方向**:
- 组件容器: 纯粹的数据持有者
- 事件处理器: 专门处理事件流转
- 生命周期管理器: 统一管理Initialize/Reset

---

#### 问题5: Config类的"伪工厂模式"

**问题描述**:
所有Config类（TriggerConfig, ConditionConfig等）都承担两种职责：
1. 配置数据存储 (ScriptableObject数据)
2. 对象创建工厂 (Create*方法)

**典型代码模式**:
```
public class TriggerConfig {
    // 职责1: 配置数据
    public TriggerType triggerType;
    public string targetTag;
    
    // 职责2: 工厂方法
    public ITrigger CreateTrigger() {
        switch (triggerType) {
            case TriggerType.Collision:
                return new CollisionTrigger();
            ...
        }
    }
}
```

**问题**:
- Config类职责混淆：既是数据又是工厂
- 每个Config类都重复实现工厂逻辑
- 缺少统一的对象创建管理
- 难以实现统一的对象池或缓存

**理想架构**:
- Config: 纯粹的配置数据
- Factory: 专门的对象创建工厂
- 分离关注点，各司其职

---

#### 问题6: 重置条件与效果移除条件语义混淆

**概念混淆**:
从实现来看，两个概念容易混淆：

**IResetCondition (重置条件)**:
- 目的: 控制技能何时可以再次触发
- 效果: 重置condition计数器 + 设置canExecute=true
- 示例: 技能执行后立即重置、回合结束重置

**IEffectRemovalCondition (效果移除条件)**:
- 目的: 控制持续效果何时移除
- 效果: 移除属性修改器（如攻击力加成）
- 示例: 持续30秒、回合结束移除、血量低于50%移除

**混淆点**:
很多情况下，两者配置相同：
- OnPhaseEndedResetCondition
- OnPhaseEndedEffectRemovalCondition

看起来在做同一件事（回合结束），但实际管理不同的生命周期。

**建议**:
- 明确文档说明两者区别
- 提供典型使用场景示例
- 考虑是否可以合并某些场景

---

### 🟡 P1级问题：代码质量

#### 问题7: 依赖查找过度使用

**问题描述**:
在多处使用`FindFirstObjectByType`进行运行时依赖查找。

**典型位置**:
1. `StatModifierEffect.GetTargetPlayer()`
   ```
   targetPlayer = Object.FindFirstObjectByType<PlayerCore>();
   ```

2. `SkillDamageModifier.ShouldRemove()`
   ```
   PlayerCore playerCore = Object.FindFirstObjectByType<PlayerCore>();
   ```

3. `SkillManager.Start()`
   ```
   skillStateManager = FindFirstObjectByType<SkillStateManager>();
   ```

**问题分析**:
- **性能开销**: 每次调用都遍历整个场景对象树
- **耦合度高**: 依赖Unity的查找机制
- **可能失败**: 可能找不到对象（返回null）
- **测试困难**: 难以进行单元测试

**替代方案**:
- 依赖注入 (构造函数/setter注入)
- 引用传递 (通过初始化方法传入)
- 服务定位器模式 (ServiceLocator)

---

#### 问题8: 事件订阅管理分散

**问题描述**:
事件订阅逻辑分散在多个类中，缺少统一管理。

**订阅位置**:
1. `SkillManager`: 订阅全局游戏事件
   - OnDamageProcessed
   - OnDeath
   - OnHealthChanged
   - OnGameFlowStateChanged
   - OnBallStopped

2. `ValueComparisonResetCondition`: 自己订阅事件
   - OnHealthChanged (在Initialize中订阅)

3. `SkillInstance`: 内部事件循环
   - PublishSkillExecutedEvent → HandleSkillExecutedEvent

**风险**:
- 内存泄漏: 忘记取消订阅
- 调试困难: 事件流向不清晰
- 生命周期管理: 订阅和取消时机难以把控

**示例问题**:
`ValueComparisonResetCondition`在`Reset()`中取消订阅，但何时调用`Reset()`不明确。

---

#### 问题9: 调试日志过多

**问题描述**:
生产代码中充斥大量`Debug.Log`，影响性能和可读性。

**统计**:
- `SkillConfig.cs`: 10+ Debug.Log
- `SkillManager.cs`: 15+ Debug.Log
- `StatModifierEffect.cs`: 20+ Debug.Log
- `SkillInstance.ProcessEvent()`: 每次调用输出4-5条日志

**影响**:
- 运行时性能开销（字符串拼接、控制台输出）
- 日志淹没重要信息
- 难以区分调试日志和错误日志

**建议**:
- 引入可配置的日志系统（日志级别）
- 开发模式启用详细日志，发布模式禁用
- 使用条件编译（#if UNITY_EDITOR）

---

#### 问题10: 过时代码和注释

**典型问题**:
1. 被注释掉但未清理的代码
   ```
   // GameEventBus.OnChargingStarted += OnNewShotStarted;
   ```
   但在Unsubscribe中仍然有：
   ```
   GameEventBus.OnChargingStarted -= OnNewShotStarted;
   ```

2. TODO注释未完成
   ```
   // TODO: 后续可以在 EffectManager 中添加专门的技能特效类型
   ```

3. 未使用的字段和方法

**影响**:
- 代码混乱，难以理解
- 新人可能不知道哪些是有效代码
- 维护负担

---

### 🟢 P2级问题：过度设计

#### 问题11: 复合条件系统使用率低

**问题描述**:
系统支持复合条件（CompositeCondition, CompositeResetCondition），但实际使用率很低。

**复杂性**:
- 引入And/Or逻辑组合
- 增加配置界面复杂度
- 增加理解成本

**实际需求**:
- 大部分技能只需要简单的单一条件
- 复合条件的真实需求场景有限

**评估**:
- 功能保留，但降低优先级
- 观察实际使用情况
- 如长期未使用，考虑移除以简化系统

---

#### 问题12: SkillInstance的事件自循环

**问题描述**:
`SkillInstance`中的事件发布-处理存在自循环。

**代码流程**:
```
ProcessEvent() 
  → ExecuteEffect()
  → PublishSkillExecutedEvent()
  → HandleSkillExecutedEvent() (自己调用自己)
```

**疑问**:
- 既然是自己调用自己，为什么需要"事件"机制？
- 这是事件驱动还是直接方法调用？
- 设计意图是什么？

**建议**:
- 明确是否需要真正的事件发布（让外部监听）
- 如果只是内部调用，改为直接方法调用
- 如果需要事件，通过EventBus发布让其他系统监听

---

### 🟢 P3级问题：性能优化

#### 问题13: 配置验证不足

**问题描述**:
- 部分关键字段有`[Required]`标记
- 但内部嵌套配置缺少验证
- 运行时错误处理返回null，但调用方可能不检查

**建议**:
- 完善Required标记
- 添加OnValidate验证
- 统一错误处理策略

---

#### 问题14: 初始化顺序不明确

**问题描述**:
`SkillInstance`构造函数中多个组件的Initialize调用顺序可能很重要，但没有文档说明。

**潜在风险**:
- 某些组件依赖其他组件先初始化
- 修改顺序可能导致bug

**建议**:
- 文档化初始化依赖关系
- 考虑使用分阶段初始化
- 添加初始化状态检查

---

## 重构优先级划分

### P0 - 立即处理（影响系统稳定性和可维护性）

**预计工作量**: 3-5个工作日  
**风险等级**: 低  
**收益**: 极高（消除70%代码冗余）

#### 任务清单

**P0.1: 删除IRemovalCondition系统** ⏱️ 0.5天
- **目标**: 彻底删除未使用的RemovalConditions目录
- **文件**: 
  - `Assets/Scripts/SkillSystem/RemovalConditions/`目录下所有文件
  - `RemovalConditionConfig.cs`
- **验证**: 确认无任何引用后删除
- **风险**: 低（未被使用）

**P0.2: 统一DataExtractor实现** ⏱️ 1天
- **目标**: 所有Config类复用`DataExtractors`静态类
- **修改范围**:
  - TriggerConfig
  - ConditionConfig
  - EffectRemovalConfig
  - ResetConditionConfig
- **方法**: 删除重复的GetDataExtractor方法，改为调用DataExtractors
- **收益**: 减少120行重复代码

**P0.3: 清理过时代码和注释** ⏱️ 1天
- **目标**: 删除所有注释掉的代码和无效TODO
- **范围**: 全系统扫描
- **包括**:
  - 注释掉的事件订阅
  - 未完成的TODO
  - 无用的字段和方法
- **工具**: 使用IDE的代码分析工具

**P0.4: 统一命名约定** ⏱️ 0.5天
- **目标**: 解决IRemovalCondition vs IEffectRemovalCondition命名不一致
- **决策**: 确认使用IEffectRemovalCondition
- **影响**: 可能需要重命名部分类和方法
- **注意**: 需要更新所有配置资产

**P0.5: 文档化重置与移除语义** ⏱️ 1天
- **目标**: 写清楚IResetCondition和IEffectRemovalCondition的区别
- **内容**:
  - 概念解释
  - 使用场景
  - 典型配置示例
  - 生命周期图表
- **位置**: 在README.md或单独的文档文件

---

### P1 - 短期优化（改善代码质量和架构）

**预计工作量**: 5-8个工作日  
**风险等级**: 中  
**收益**: 高（提升可维护性和扩展性）

#### 任务清单

**P1.1: 重构SkillInstance** ⏱️ 2天
- **目标**: 拆分职责，使用组合模式
- **方案**:
  - 创建SkillComponentContainer（纯数据持有）
  - 创建SkillEventHandler（事件处理）
  - 创建SkillLifecycleManager（生命周期）
- **原则**: 单一职责原则
- **风险**: 中等（影响核心类）

**P1.2: 引入工厂模式** ⏱️ 2天
- **目标**: 分离Config的数据和创建职责
- **方案**:
  - 创建SkillComponentFactory
  - 从Config类中提取Create*方法
  - Config类变为纯数据类
- **收益**: 统一对象创建，便于扩展（如对象池）

**P1.3: 实现依赖注入** ⏱️ 2天
- **目标**: 替换FindFirstObjectByType
- **方案**:
  - 构造函数注入PlayerCore引用
  - SkillManager初始化时收集依赖
  - 传递给SkillInstance
- **收益**: 性能提升 + 解耦 + 易测试

**P1.4: 事件订阅管理器** ⏱️ 2天
- **目标**: 统一管理所有事件订阅
- **方案**:
  - 创建EventSubscriptionManager
  - 集中管理订阅和取消订阅
  - 自动生命周期管理（避免泄漏）
- **收益**: 减少内存泄漏风险，易于调试

**P1.5: 可配置日志系统** ⏱️ 1天
- **目标**: 替换所有Debug.Log为可配置日志
- **方案**:
  - 创建SkillLogger类
  - 支持日志级别（Debug/Info/Warning/Error）
  - Inspector中可配置启用/禁用
- **收益**: 性能提升 + 灵活调试

---

### P2 - 长期改进（优化设计和性能）

**预计工作量**: 3-5个工作日  
**风险等级**: 低  
**收益**: 中（锦上添花）

#### 任务清单

**P2.1: 评估复合条件系统** ⏱️ 1天
- **目标**: 确认是否真的需要复合条件
- **方法**:
  - 统计实际使用情况
  - 收集需求反馈
  - 如果使用率低，考虑移除
- **决策**: 保留、简化或移除

**P2.2: 统一修改器系统** ⏱️ 2天
- **目标**: 解决StatModifier vs SkillDamageModifier重复
- **方案**:
  - 评估是否真的需要两套系统
  - 如果需要，明确各自职责
  - 如果不需要，统一为一套系统
- **风险**: 中等（涉及核心机制）

**P2.3: 完善配置验证** ⏱️ 1天
- **目标**: 添加更完善的配置验证
- **方法**:
  - 补充Required标记
  - 实现OnValidate方法
  - 添加配置合法性检查
- **收益**: 减少配置错误

**P2.4: 性能优化** ⏱️ 1天
- **目标**: 减少不必要的性能开销
- **包括**:
  - 缓存查找结果
  - 对象池（如果创建频繁）
  - 减少字符串拼接
- **收益**: 提升运行时性能

---

### P3 - 可选改进（非必要优化）

**预计工作量**: 按需进行  
**风险等级**: 极低  
**收益**: 低（可选）

#### 任务清单

**P3.1: 单元测试** ⏱️ 按需
- **目标**: 为核心逻辑添加单元测试
- **范围**:
  - Trigger/Condition/Effect逻辑
  - 重置和移除条件
- **收益**: 提升代码可靠性

**P3.2: 编辑器工具** ⏱️ 按需
- **目标**: 改善配置体验
- **功能**:
  - 技能配置向导
  - 配置校验工具
  - 可视化技能流程图
- **收益**: 提升策划配置效率

**P3.3: 性能监控** ⏱️ 按需
- **目标**: 添加性能监控和分析
- **功能**:
  - 技能执行耗时统计
  - 事件处理耗时
  - 内存占用监控
- **收益**: 便于发现性能瓶颈

---

## 实施路线图

### 阶段1: 清理冗余（第1周）

**目标**: 消除代码冗余，建立清晰的概念模型

```
Week 1
├── Day 1-2: P0.1 删除IRemovalCondition系统
├── Day 2-3: P0.2 统一DataExtractor实现
├── Day 3-4: P0.3 清理过时代码
├── Day 4-5: P0.4 统一命名约定
└── Day 5: P0.5 文档化重置与移除语义
```

**里程碑**: 代码冗余度从70%降至<10%

**验证标准**:
- 无重复接口和类
- 无注释掉的代码
- 所有概念有清晰文档

---

### 阶段2: 架构优化（第2-3周）

**目标**: 改善架构设计，提升代码质量

```
Week 2
├── Day 1-2: P1.1 重构SkillInstance
├── Day 3-4: P1.2 引入工厂模式
└── Day 5: 集成测试

Week 3
├── Day 1-2: P1.3 实现依赖注入
├── Day 3-4: P1.4 事件订阅管理器
├── Day 5: P1.5 可配置日志系统
└── 全面测试
```

**里程碑**: 
- 单一职责原则得到遵守
- 依赖查找改为依赖注入
- 事件管理统一化

**验证标准**:
- 每个类职责单一
- 无FindFirstObjectByType调用
- 所有事件订阅集中管理

---

### 阶段3: 精细打磨（第4周及以后）

**目标**: 长期优化和改进

```
Week 4+
├── P2.1 评估复合条件系统（按需）
├── P2.2 统一修改器系统（按需）
├── P2.3 完善配置验证（持续）
└── P3.* 可选改进（按需）
```

**里程碑**: 系统达到生产级质量

---

## 风险评估

### 高风险项

| 风险项 | 影响 | 概率 | 缓解措施 |
|--------|------|------|---------|
| 重构SkillInstance破坏现有功能 | 高 | 中 | 充分的回归测试；渐进式重构 |
| 统一修改器系统影响伤害计算 | 高 | 中 | 详细的需求分析；保留旧系统兼容 |
| 删除IRemovalCondition误删有用代码 | 中 | 低 | 全局搜索确认无引用 |

### 中风险项

| 风险项 | 影响 | 概率 | 缓解措施 |
|--------|------|------|---------|
| 依赖注入改造导致初始化问题 | 中 | 中 | 清晰的初始化流程文档 |
| 工厂模式增加理解成本 | 中 | 低 | 良好的文档和示例 |
| 事件管理重构影响现有逻辑 | 中 | 低 | 保持接口兼容性 |

### 低风险项

| 风险项 | 影响 | 概率 | 缓解措施 |
|--------|------|------|---------|
| 日志系统改造 | 低 | 低 | 逐步替换，不影响功能 |
| 配置验证增强 | 低 | 低 | 只是额外检查，不影响逻辑 |
| 清理过时代码 | 低 | 低 | 提交前代码审查 |

---

## 成功指标

### 定量指标

| 指标 | 当前值 | 目标值 | 衡量方法 |
|------|--------|--------|---------|
| 代码冗余度 | 70% | <10% | 代码重复检测工具 |
| 类职责数量 | SkillInstance: 6 | 每个类≤3 | 职责分析 |
| FindFirstObjectByType调用 | 15+ | 0 | 代码搜索 |
| Debug.Log数量 | 100+ | <20（可配置） | 代码搜索 |
| 未使用代码 | 多处 | 0 | 静态分析工具 |
| 配置验证覆盖率 | 40% | 90% | 手动检查 |

### 定性指标

| 指标 | 评估标准 |
|------|---------|
| 代码可读性 | 新人能在1小时内理解核心流程 |
| 可维护性 | 添加新技能类型≤30分钟 |
| 可测试性 | 核心逻辑可进行单元测试 |
| 文档完整性 | 所有核心概念有清晰文档 |
| 架构一致性 | 遵循统一的设计模式和原则 |

---

## 实施建议

### 开发流程

1. **分支管理**
   - 为每个P0/P1任务创建独立分支
   - 完成后合并到dev分支
   - 充分测试后合并到main

2. **代码审查**
   - 所有重构代码必须经过审查
   - 重点关注功能完整性和架构一致性

3. **测试策略**
   - 重构前：记录当前行为（截图/视频）
   - 重构中：增量测试
   - 重构后：完整回归测试

4. **文档更新**
   - 代码重构的同时更新文档
   - 保持文档与代码同步

### 渐进式重构原则

- ✅ 优先修复严重问题（P0）
- ✅ 每次改动保持可运行状态
- ✅ 频繁提交小改动，而非大批量修改
- ✅ 改一处，测一处
- ✅ 保持向后兼容（如果可能）

### 团队协作

- 定期同步进度（每日/每周）
- 及时沟通问题和风险
- 共享测试用例和重构经验

---

## 附录

### A. 关键文件清单

**需要删除的文件**:
```
Assets/Scripts/SkillSystem/RemovalConditions/
├── IRemovalCondition.cs
├── OnPlayerPhaseEndedCondition.cs
├── DurationCondition.cs
├── ImmediateRemoveCondition.cs
└── NeverRemoveCondition.cs

Assets/Scripts/SkillSystem/Configs/
└── RemovalConditionConfig.cs
```

**需要重点重构的文件**:
```
Assets/Scripts/SkillSystem/
├── SkillConfig.cs (添加验证)
├── SkillManager.cs (依赖注入)
├── SkillInstance.cs (拆分职责)
└── Configs/
    ├── TriggerConfig.cs (统一DataExtractor)
    ├── ConditionConfig.cs (统一DataExtractor)
    ├── EffectConfig.cs (工厂模式)
    ├── EffectRemovalConfig.cs (统一DataExtractor)
    └── ResetConditionConfig.cs (统一DataExtractor)

Assets/Scripts/SkillSystem/Effects/
└── StatModifierEffect.cs (依赖注入，移除硬编码)

Assets/Scripts/EventSystem/
└── SkillDamageModifier.cs (评估是否合并)
```

### B. 参考资料

**设计模式**:
- 工厂模式 (Factory Pattern)
- 策略模式 (Strategy Pattern)
- 组合模式 (Composite Pattern)
- 依赖注入 (Dependency Injection)

**设计原则**:
- SOLID原则
  - 单一职责原则 (SRP)
  - 开闭原则 (OCP)
  - 依赖倒置原则 (DIP)
- DRY原则 (Don't Repeat Yourself)
- KISS原则 (Keep It Simple, Stupid)

---

## 总结

当前技能系统功能完整，但存在**严重的代码冗余和概念混淆**问题。通过本重构计划的实施，预期达到：

✅ **消除70%代码冗余**  
✅ **明确职责分离**  
✅ **提升可维护性和扩展性**  
✅ **改善代码质量**  

重构应采用**渐进式、低风险**的策略，优先处理P0级问题，逐步优化架构设计。

---

**批准**: ________________  
**日期**: ________________  
**审核**: ________________

