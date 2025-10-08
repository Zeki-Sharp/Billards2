# 技能系统架构重构计划

## 问题背景

### 当前架构问题
1. **概念混淆**：RemovalCondition 混合了"效果移除"和"触发重置"两个不同的职责
2. **语义不清**：瞬时效果和持续效果使用相同的配置接口，导致配置意图不明确
3. **职责混乱**：ActionEffect（瞬时）和PropertyEffect（持续）的生命周期管理逻辑混合在一起

### 具体问题表现
- 瞬时效果（如治疗）需要配置"移除条件"来重置触发条件，语义不直观
- 持续效果（如buff）的"移除"和"触发重置"被混在一起
- 配置时难以区分"何时移除效果"和"何时重置触发条件"

## 重构目标

### 核心原则
1. **职责分离**：效果移除和触发重置完全分离
2. **类型区分**：ActionEffect和PropertyEffect使用不同的配置接口
3. **语义清晰**：配置名称和用途一一对应，无歧义

### 预期效果
- 配置意图一目了然
- 不同效果类型有专门的配置选项
- 扩展新功能时不会影响现有逻辑

## 重构方案

### 1. 效果类型重新分类

#### ActionEffect（瞬时效果）
**特点：**
- 执行后立即完成，无持续状态
- 不需要效果移除配置
- 需要触发重置配置

**典型例子：**
- 治疗技能（HealEffect）
- 伤害技能（DamageEffect）
- 传送技能（TeleportEffect）

#### PropertyEffect（持续效果）
**特点：**
- 有生命周期，需要管理持续状态
- 需要效果移除配置
- 需要触发重置配置

**典型例子：**
- 属性提升buff（StatModifierEffect）
- 护盾效果（ShieldEffect）
- 持续回血（RegenerationEffect）

### 2. 配置接口重新设计

#### TriggerResetConfig（触发重置配置）
**职责：** 管理技能何时可以再次触发

**配置选项：**
- Immediate：立即重置（ActionEffect常用）
- OnPlayerPhaseEnded：回合结束重置
- OnConditionMet：满足特定条件时重置
- Never：永不复位（一次性技能）

**适用范围：** 所有技能都需要

#### EffectRemovalConfig（效果移除配置）
**职责：** 管理持续效果的生命周期

**配置选项：**
- Duration：持续时间后移除
- OnPlayerPhaseEnded：回合结束时移除
- OnConditionMet：满足特定条件时移除
- Never：永不移除

**适用范围：** 只有PropertyEffect需要

### 3. 技能配置结构重组

#### 新的SkillConfig结构
```
SkillConfig
├── 基本信息
│   ├── skillName
│   ├── description
│   └── skillIcon
├── 效果配置
│   ├── effectType（Heal/StatModifier等）
│   └── 具体效果参数（根据effectType显示）
├── 触发重置配置（所有技能）
│   └── triggerResetConfig
└── 效果移除配置（仅PropertyEffect）
    └── effectRemovalConfig
```

#### 配置示例对比

**ActionEffect配置：**
```
治疗技能：
- effectType: Heal
- healAmount: 20
- triggerResetConfig: Immediate
- （无effectRemovalConfig）
```

**PropertyEffect配置：**
```
伤害提升技能：
- effectType: StatModifier
- targetStat: "Damage"
- modifierValue: 1.5
- triggerResetConfig: Immediate
- effectRemovalConfig: Duration(30s)
```

### 4. 接口重新定义

#### 新增接口
- **ITriggerResetCondition**：专门管理触发重置逻辑
- **IEffectRemovalCondition**：专门管理效果移除逻辑

#### 移除/重构接口
- **IRemovalCondition**：拆分为上述两个专用接口
- **RemovalConditionConfig**：拆分为TriggerResetConfig和EffectRemovalConfig

### 5. SkillInstance重构

#### 新的属性结构
```
SkillInstance
├── 基础组件（不变）
│   ├── trigger
│   ├── condition
│   └── effect
├── 触发重置组件（所有技能）
│   └── triggerResetCondition
└── 效果移除组件（仅PropertyEffect）
    └── effectRemovalCondition
```

#### ProcessEvent流程重构
```
1. 检查触发器和条件
2. 执行效果
3. 检查触发重置条件（所有技能）
   - 如果满足，重置condition
4. （PropertyEffect额外）检查效果移除条件
   - 由管理系统定期检查，不在ProcessEvent中处理
```

## 实施步骤

### 阶段1：接口重构
1. 创建新的接口定义
   - ITriggerResetCondition
   - IEffectRemovalCondition
   - TriggerResetConfig
   - EffectRemovalConfig

2. 创建基础实现类
   - ImmediateTriggerResetCondition
   - DurationEffectRemovalCondition
   - OnPhaseEndedTriggerResetCondition
   - 等

### 阶段2：配置系统重构
1. 重构SkillConfig结构
   - 添加triggerResetConfig字段
   - 添加effectRemovalConfig字段（条件显示）
   - 移除原有的removalConditionConfig

2. 更新Odin Inspector配置
   - 根据effectType显示不同配置选项
   - PropertyEffect显示效果移除配置
   - ActionEffect隐藏效果移除配置

### 阶段3：技能实例重构
1. 重构SkillInstance类
   - 添加新的组件属性
   - 重构ProcessEvent方法
   - 更新初始化逻辑

2. 更新SkillManager
   - 修改技能创建逻辑
   - 更新效果管理流程

### 阶段4：效果系统适配
1. 重构现有Effect实现
   - 明确区分ActionEffect和PropertyEffect
   - 更新效果配置和创建逻辑

2. 创建新的Effect类型
   - 为未来扩展做准备

### 阶段5：迁移和测试
1. 现有技能配置迁移
   - 将现有配置转换为新格式
   - 确保功能一致性

2. 全面测试
   - 验证ActionEffect行为
   - 验证PropertyEffect行为
   - 验证配置正确性

## 风险评估

### 高风险
- **配置迁移复杂度**：现有技能配置需要手动迁移
- **接口变更影响**：可能影响其他依赖技能系统的模块

### 中风险
- **测试覆盖**：需要全面测试各种配置组合
- **性能影响**：新的配置结构可能影响性能

### 缓解措施
1. **渐进式迁移**：保持新旧系统并存，逐步迁移
2. **详细测试计划**：制定完整的测试用例
3. **性能监控**：监控重构后的性能表现
4. **文档更新**：及时更新相关文档和注释

## 预期收益

### 架构收益
- **职责清晰**：每个组件职责单一明确
- **扩展性强**：新增效果类型和配置选项更容易
- **维护性好**：配置意图清晰，减少理解成本

### 开发收益
- **配置直观**：不同类型效果有专门的配置界面
- **调试友好**：问题定位更容易
- **文档清晰**：架构文档更容易编写和理解

### 用户体验收益
- **配置简单**：不会出现"瞬时效果配置移除条件"的困惑
- **功能稳定**：减少因配置错误导致的bug

## 总结

这次重构的核心是**概念分离**和**职责明确**。通过将"效果移除"和"触发重置"完全分离，为不同类型的效果提供专门的配置接口，可以大大提升系统的可理解性和可维护性。

重构后的架构将更加清晰：ActionEffect专注于瞬时执行和触发重置，PropertyEffect专注于持续效果的生命周期管理，两者各司其职，互不干扰。

这种设计不仅解决了当前的问题，也为未来的功能扩展奠定了良好的基础。
