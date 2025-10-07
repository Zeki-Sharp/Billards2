# 技能系统架构设计计划

## 文档信息
- **创建日期**: 2024年12月
- **版本**: 1.0
- **状态**: 设计阶段
- **优先级**: 高
- **最后更新**: 2024年12月

## 概述

基于现有事件驱动架构，设计一个具有良好延展性的技能系统。技能系统由触发器(Trigger)和效果(Effect)两部分组成，支持玩家技能流派和复杂技能组合。

## 核心设计原则

### 1. 事件驱动架构
- 充分利用现有的 `GameEventBus` 事件系统
- 技能系统作为事件消费者，不重复造轮子
- 保持与现有系统的兼容性

### 2. 机制与表现分离
- 技能效果专注于游戏机制和数值变化
- 视觉效果、音效通过现有 `EffectManager` 处理
- 表现效果作为机制效果的自动副作用

### 3. 数据驱动设计
- 使用 ScriptableObject 配置技能
- 支持运行时动态加载和热更新
- 支持技能流派和技能树系统

## 整体架构设计

### 三层架构设计（简化版，避免过度设计）

```
┌─────────────────────────────────────────────────────────────┐
│                    技能系统三层架构                           │
├─────────────────────────────────────────────────────────────┤
│  SkillEventManager (事件监听层)                              │
│  ├── 监听 GameEventBus 事件                                  │
│  ├── 事件过滤和预处理                                        │
│  └── 分发给技能系统                                          │
├─────────────────────────────────────────────────────────────┤
│  SkillSystem (技能管理核心)                                  │
│  ├── 技能注册和管理                                          │
│  ├── 条件检查和效果执行协调                                  │
│  ├── 技能生命周期管理                                        │
│  └── 性能监控（简化版）                                      │
├─────────────────────────────────────────────────────────────┤
│  SkillDefinition (技能定义层)                                │
│  ├── Trigger System (触发器系统)                             │
│  │   ├── ITrigger (触发器接口)                              │
│  │   ├── TriggerData (触发器数据)                           │
│  │   └── 具体触发器实现 (碰撞、击杀、蓄力等)                  │
│  ├── Condition System (条件系统)                             │
│  │   ├── ICondition (条件接口)                              │
│  │   ├── ConditionData (条件数据)                           │
│  │   └── 具体条件实现 (计数、时间窗口、血量等)                │
│  ├── Effect System (效果系统)                                │
│  │   ├── IEffect (效果接口)                                 │
│  │   ├── EffectData (效果数据)                              │
│  │   └── 具体效果实现 (数值调整、状态效果、资源效果等)        │
│  └── SkillData (技能数据)                                   │
│      ├── SkillConfig (技能配置)                             │
│      └── SkillTree (技能树)                                 │
└─────────────────────────────────────────────────────────────┘
```

## 触发器系统设计

### 核心架构：Trigger + Condition 组合

**Trigger (触发器) - 事件检测器**
- 职责：检测游戏中的具体事件发生
- 只负责"检测到事件"这个动作
- 不判断事件是否满足技能触发条件

**Condition (条件) - 触发判断器**
- 职责：判断是否满足技能触发条件
- 基于触发器检测到的事件进行条件判断
- 决定技能是否应该被触发

### 触发器类型（事件检测）
- `CollisionTrigger` - 碰撞事件检测（撞敌人、撞墙）
- `KillTrigger` - 击杀事件检测
- `ChargingTrigger` - 蓄力事件检测
- `HealthChangeTrigger` - 血量变化事件检测
- `ResourceChangeTrigger` - 资源变化事件检测
- `TimeTrigger` - 时间事件检测

### 条件类型（条件判断）
- `CountCondition` - 计数条件（N次）- 用于连击等计数场景
- `TimeWindowCondition` - 时间窗口条件（X秒内Y次）
- `HealthCondition` - 血量条件（血量低于X%）
- `ResourceCondition` - 资源条件（资源大于X）
- `StateCondition` - 状态条件（处于某种状态）
- `AndCondition` - 与条件（组合条件）
- `OrCondition` - 或条件（组合条件）

### 技能组合结构
```csharp
Skill = Trigger + Condition + Effect
```

### 触发器特点
- 触发器只负责事件检测，不处理触发逻辑
- 条件判断器负责分析是否满足触发条件
- 支持条件组合（AND/OR逻辑）
- 通过技能系统统一管理，不直接监听事件

## 效果系统设计

### 效果分类（重新整理）

#### 核心机制效果
1. **数值调整效果 (StatModifierEffect)**
   - 攻击力、移动速度、蓄力速度调整
   - 碰撞伤害、击飞距离调整

2. **状态效果 (StatusEffect)**
   - 无敌、加速、减速、隐身、护盾状态

3. **资源效果 (ResourceEffect)**
   - 生命值恢复、能量恢复
   - 临时资源生成（金币、道具等）

4. **生成效果 (SpawnEffect)**
   - 生成临时道具、辅助单位
   - 生成环境障碍、特效区域

5. **环境效果 (EnvironmentEffect)**
   - 修改物理参数、创建临时碰撞体
   - 修改重力方向、创建传送门

6. **连锁效果 (ChainEffect)**
   - 触发其他技能、重置技能冷却
   - 修改其他技能效果、技能组合触发

### 效果特点
- 支持效果叠加和优先级
- 支持效果持续时间管理
- 自动触发表现效果（通过现有事件系统）

## 三层职责详细设计

### 1. SkillEventManager (事件监听层)
**职责：**
- 监听 `GameEventBus` 的所有相关事件
- 事件过滤和预处理
- 将事件分发给技能系统

**具体功能：**
- 统一事件订阅管理
- 事件类型过滤（只处理技能相关事件）
- 事件数据预处理和转换
- 事件分发到技能系统

### 2. SkillSystem (技能管理核心)
**职责：**
- 技能注册和管理
- 条件检查和效果执行协调
- 技能生命周期管理
- 性能监控（简化版）

**具体功能：**
- 技能注册表和查找
- 按事件类型分组存储
- 协调触发器和条件检查
- 协调效果执行
- 技能状态管理
- 简单的性能监控

### 3. SkillDefinition (技能定义层)
**职责：**
- 定义技能的数据结构
- 实现 Trigger + Condition + Effect 组合
- 提供技能配置和技能树

**具体功能：**
- Trigger System：事件检测
- Condition System：条件判断
- Effect System：效果执行
- SkillData：技能基础数据
- SkillConfig：技能配置
- SkillTree：技能树结构

## 事件系统集成方案

### 核心原则
- 采用三层架构，避免过度设计
- 事件监听层专门处理事件，技能管理核心专门管理技能
- 触发器不直接监听事件，通过三层架构分发
- 避免与现有事件系统冲突

### 集成架构
```
GameEventBus (事件发布)
    ↓
SkillEventManager (事件监听层)
    ↓
SkillSystem (技能管理核心)
    ↓
SkillDefinition (技能定义层)
    ├── ITrigger (事件检测)
    ├── ICondition (条件判断)
    └── IEffect (效果执行)
```

### 实现方式
1. SkillEventManager 统一监听 `GameEventBus` 事件
2. 事件过滤后分发给 SkillSystem
3. SkillSystem 查找相关技能并协调执行
4. SkillDefinition 层提供具体的 Trigger + Condition + Effect 实现
5. 三层职责清晰，避免过度设计

## 技能定义系统

### 数据结构
- `SkillData` - 技能基础数据
- `SkillConfig` - 技能配置（ScriptableObject）
- `SkillTree` - 技能树结构
- `SkillLevel` - 技能等级数据

### 技能特点
- 支持技能升级和解锁条件
- 支持技能流派分类
- 支持技能组合效果
- 支持技能热更新

## 技能流派系统

### 基于文档中的流派概念
1. **击飞流派**
   - 技能组合：`CollisionTrigger` + `SingleCondition` + `StatModifierEffect(击飞距离+100%)`
   - 连锁效果：击飞时触发碰撞伤害
   - 生成效果：击飞时生成冲击波

2. **连击流派**
   - 技能组合：`CollisionTrigger` + `CountCondition(3)` + `StatModifierEffect(连击伤害递增)`
   - 状态效果：连击时获得加速状态
   - 资源效果：连击时恢复能量

3. **撞墙流派**
   - 技能组合：`CollisionTrigger` + `WallCondition` + `StatModifierEffect(撞墙伤害+200%)`
   - 生成效果：撞墙时生成碎片
   - 连锁效果：撞墙时触发范围伤害

4. **资源流派**
   - 技能组合：`KillTrigger` + `SingleCondition` + `ResourceEffect(生成临时资源)`
   - 连锁效果：资源收集触发额外效果
   - 状态效果：资源管理影响技能效果

## 与现有系统集成

### 事件系统集成
- 利用现有 `GameEventBus` 监听游戏事件
- 通过事件触发技能条件检查
- 通过事件发布技能效果

### 现有组件集成
- 与 `PlayerCore` 集成，监听玩家状态
- 与 `EffectManager` 集成，播放技能特效
- 与 `GameManager` 集成，管理技能数据

### 表现效果处理
- 机制效果执行时自动触发表现效果
- 通过 `GameEventBus.PublishEffectEvent` 触发表现
- 保持现有表现系统的完整性

## 实现阶段规划

### 阶段0：原型验证（最小可行版本）
**目标：验证整体架构可行性，每个模块只实现一个功能**

#### 0.1 接口定义 ✅ **已完成**
- ✅ 定义 `ITrigger` 接口
- ✅ 定义 `ICondition` 接口
- ✅ 定义 `IEffect` 接口

#### 0.2 实现一个完整的技能链路 ✅ **已完成**
**触发器层：**
- ✅ `CollisionTrigger` - 碰撞事件检测
- ✅ 监听 `GameEventBus.OnAttack` 事件
- ✅ 检测碰撞事件是否发生

**条件层：**
- ✅ `CountCondition` - 计数条件
- ✅ 检查碰撞次数是否达到阈值（修改为2次）
- ✅ 支持条件重置

**效果层：**
- ✅ `StatModifierEffect` - 数值调整效果
- ✅ 修改玩家的某个属性（攻击力+100%）
- ✅ 集成 `PlayerStatsManager` 属性修饰器系统

#### 0.3 实现三层架构 ⚠️ **部分完成**
**当前实现方式（简化版）：**
- ✅ `TestSkillChain` - 直接管理技能链路（替代三层架构）
- ✅ 监听 `GameEventBus.OnAttack` 事件
- ✅ 协调 Trigger + Condition + Effect 执行

**原计划的三层架构：**
- ❌ `SkillEventManager` - 暂未实现（使用 `TestSkillChain` 替代）
- ❌ `SkillSystem` - 暂未实现（使用 `TestSkillChain` 替代）
- ✅ `SkillDefinition` - 通过 `TestSkillChain` 实现

#### 0.4 验证目标 ✅ **已完成**
- ✅ 验证事件监听和分发机制
- ✅ 验证 Trigger + Condition + Effect 组合模式
- ✅ 验证技能执行流程
- ✅ 验证与现有系统的集成
- ✅ 确认架构没有明显缺陷

#### 0.5 测试场景 ✅ **已验证**
1. ✅ 玩家碰撞敌人1次 → 条件未满足 → 技能不触发
2. ✅ 玩家碰撞敌人2次 → 条件满足 → 技能触发 → 攻击力+100%
3. ✅ 验证技能效果在回合结束时正确移除
4. ✅ 验证每次发射可重复触发技能

#### 0.6 发现并解决的问题 ⚠️ **重要发现**
**架构问题：**
1. ✅ **事件时机问题**：`OnBallStopped` 在球初始静止时误触发
   - **解决方案**：使用 `OnPlayerPhaseEnded` 替代，在玩家回合结束时移除效果
   
2. ✅ **属性引用问题**：`PlayerCore` 直接从 `PlayerData` 获取属性值
   - **解决方案**：修改为从 `PlayerStatsManager.GetFinalStat()` 获取最终计算值
   
3. ✅ **技能重置问题**：技能效果无法重复触发
   - **解决方案**：监听 `OnChargingStarted` 事件，每次发射时重置技能状态

**系统集成问题：**
4. ✅ **StatModifier系统集成**：成功集成属性修饰器系统
5. ✅ **GameEventBus事件扩展**：添加 `OnGameFlowStateChanged` 事件支持

**预期产出：**
- ✅ 一个可运行的最小技能系统
- ✅ 验证 Trigger + Condition + Effect 组合模式可行性
- ✅ 发现并解决关键架构问题
- ✅ 为后续开发提供参考和改进方向

---

## 架构改进建议

### 基于阶段0实施的经验教训

#### 1. **属性系统集成**
**发现**：现有系统直接从 `PlayerData` 获取属性值，忽略了修饰器系统
**建议**：所有属性引用都应该通过 `PlayerStatsManager.GetFinalStat()` 获取最终计算值
**影响范围**：`PlayerCore`、`EnemyBehavior`、所有涉及属性计算的系统

#### 2. **事件时机设计**
**发现**：某些事件（如 `OnBallStopped`）可能在系统初始化时误触发
**建议**：事件设计需要考虑系统初始化状态，避免误触发
**解决方案**：使用语义更明确的事件（如 `OnPlayerPhaseEnded`）

#### 3. **技能生命周期管理**
**发现**：技能效果需要明确的生命周期管理策略
**建议**：为技能效果定义清晰的触发、持续、移除条件
**实现**：通过 `RemovalCondition` 枚举支持多种移除策略

#### 4. **三层架构简化**
**发现**：当前使用 `TestSkillChain` 直接管理技能链路更简单有效
**建议**：采用 ScriptableObject + 管理器组件模式，替代完整三层架构
**权衡**：简化版本更易维护，与现有架构一致，扩展性足够

#### 5. **系统集成策略**
**发现**：新系统需要与现有系统深度集成
**建议**：优先考虑与现有架构的兼容性，避免破坏性修改
**原则**：渐进式集成，保持向后兼容

---

## 推荐架构方案

### ScriptableObject + 管理器组件模式

基于对现有架构的分析，推荐采用以下方案替代完整的三层架构：

#### 架构优势
1. **与现有模式一致**：
   - 遵循 `PlayerData`、`EnemyData` 的 ScriptableObject 配置模式
   - 使用 `PlayerStatsManager` 风格的管理器组件
   - 保持事件驱动架构

2. **简化但有效**：
   - 避免过度设计，减少维护成本
   - 易于理解和扩展
   - 支持 Inspector 可视化配置

3. **渐进式集成**：
   - 可以逐步替换 `TestSkillChain`
   - 保持向后兼容
   - 降低迁移风险

#### 核心组件设计

```csharp
// 1. 技能配置 ScriptableObject
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Game/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [Header("技能基本信息")]
    public string skillName;
    public string description;
    public Sprite skillIcon;
    
    [Header("技能组件配置")]
    public TriggerConfig triggerConfig;
    public ConditionConfig conditionConfig;
    public EffectConfig effectConfig;
    
    [Header("技能属性")]
    public SkillType skillType;
    public int requiredLevel = 1;
    public bool isActive = true;
}

// 2. 技能管理器组件
public class SkillManager : MonoBehaviour
{
    [Header("技能配置")]
    public List<SkillConfig> activeSkills = new List<SkillConfig>();
    
    private Dictionary<string, ISkill> instantiatedSkills = new Dictionary<string, ISkill>();
    
    // 管理技能生命周期
    // 监听 GameEventBus 事件
    // 协调技能执行
}
```

#### 与现有系统的集成点
1. **属性系统**：通过 `PlayerStatsManager` 管理技能效果
2. **事件系统**：通过 `GameEventBus` 监听和发布事件
3. **配置系统**：通过 ScriptableObject 支持可视化配置
4. **生命周期**：通过 MonoBehaviour 组件管理运行时状态

---

### 阶段1：基础框架（ScriptableObject + 管理器组件模式）
**目标：替换 TestSkillChain，建立可扩展的技能配置系统**

#### 1.1 技能配置系统
- 创建 `SkillConfig` ScriptableObject
  - 技能基本信息（名称、描述、图标）
  - 触发器配置（TriggerConfig）
  - 条件配置（ConditionConfig）
  - 效果配置（EffectConfig）
  - 技能属性（类型、等级、激活状态）

#### 1.2 技能管理器组件
- 创建 `SkillManager` MonoBehaviour 组件
  - 管理多个技能配置
  - 监听 GameEventBus 事件
  - 协调技能执行
  - 处理技能生命周期

#### 1.3 配置数据结构
- `TriggerConfig` - 触发器配置（类型、参数）
- `ConditionConfig` - 条件配置（类型、阈值）
- `EffectConfig` - 效果配置（类型、参数）
- 支持 Inspector 中的可视化配置

#### 1.4 接口完善
- 完善 `ITrigger`、`ICondition`、`IEffect` 接口
- 添加配置化支持
- 支持序列化和反序列化

#### 1.5 集成测试
- 将现有 TestSkillChain 逻辑迁移到新系统
- 验证多技能配置支持
- 确保与现有系统兼容性

### 阶段2：核心功能扩展
**目标：扩展技能类型，实现技能流派系统**

#### 2.1 触发器类型扩展
- `KillTrigger` - 击杀事件检测
- `ChargingTrigger` - 蓄力事件检测
- `HealthChangeTrigger` - 血量变化事件检测
- `TimeTrigger` - 时间事件检测

#### 2.2 条件类型扩展
- `TimeWindowCondition` - 时间窗口条件（X秒内Y次）
- `HealthCondition` - 血量条件（血量低于X%）
- `ResourceCondition` - 资源条件（资源大于X）
- `StateCondition` - 状态条件（处于某种状态）

#### 2.3 效果类型扩展
- `StatusEffect` - 状态效果（无敌、加速、减速等）
- `ResourceEffect` - 资源效果（生命恢复、能量恢复）
- `SpawnEffect` - 生成效果（生成道具、辅助单位）

#### 2.4 技能流派实现
- 击飞流派：碰撞 + 击飞距离增加
- 连击流派：连击计数 + 伤害递增
- 撞墙流派：撞墙 + 伤害加成
- 资源流派：击杀 + 资源生成

#### 2.5 性能优化
- 技能条件检查优化
- 效果执行批处理
- 内存池管理

### 阶段3：高级功能
- 实现技能树系统
- 实现技能组合效果和连锁效果
- 实现技能热更新
- 性能优化和调试工具
- 完善三层架构的性能监控

## 技术要点

### 性能优化
- 技能条件检查的优化
- 效果执行的批处理
- 内存池管理
- 触发器优先级管理

### 扩展性
- 三层模块化设计，支持插件式扩展
- 支持第三方技能包
- 支持技能组合和连锁效果
- 支持复杂的条件判断和条件组合
- Trigger + Condition 组合提供高度灵活性
- 各层独立扩展，互不影响

### 调试和维护
- 三层调试工具（每层独立的调试接口）
- 触发器状态监控
- 效果执行日志
- 性能分析工具
- 各层职责清晰，便于定位问题

## 注意事项

1. **保持现有系统稳定**：不破坏现有的事件驱动架构
2. **渐进式实现**：分阶段实现，确保每个阶段都能正常工作
3. **充分测试**：每个功能都要经过充分测试
4. **文档完善**：及时更新相关文档和注释
5. **性能监控**：关注技能系统对游戏性能的影响

## 总结

这个技能系统架构设计充分利用了现有的事件驱动架构，通过 **三层架构 + Trigger + Condition + Effect** 的组合模式实现了高度的模块化和可扩展性。系统设计遵循了机制与表现分离的原则，通过清晰的三层职责分工解决了单一组件职责过重的问题，同时避免了过度设计，为后续的功能扩展奠定了良好的基础。

### 核心优势
- **三层职责清晰**：每层专注特定职责，避免单一组件过重
- **避免过度设计**：三层架构既保持模块化又避免过度复杂
- **高度复用**：同一个 Trigger 可以配合不同的 Condition，同一个 Condition 可以配合不同的 Trigger
- **易于扩展**：各层独立扩展，互不影响
- **易于维护**：职责清晰，便于调试和定位问题
- **避免重复**：消除了 ComboTrigger 等重复概念，用 CountCondition 实现连击功能
- **性能可控**：三层架构便于性能优化和监控
