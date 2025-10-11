# 生成系统通用策略层重构计划

## 当前实现问题分析

### 现状：过度特化的策略实现

**当前实现方式：**
- `WaveListSpawnStrategy : ISpawnStrategy<EnemyData>` - 专门为敌人设计
- `DeathDropStrategy : ISpawnStrategy<ItemConfig>` - 专门为道具设计
- `FixedSpawnStrategy<T>` - 通用实现
- `ListSpawnStrategy<T>` - 通用实现
- `ConditionalDropStrategy<T>` - 通用实现

**问题分析：**
1. **混合架构**：既有通用实现，又有特化实现
2. **职责混乱**：特化策略包含了特定业务逻辑（如波次管理、掉落表查询）
3. **失去复用性**：特化策略无法用于其他对象类型
4. **维护困难**：相似逻辑分散在多个类中
5. **配置提供者已存在**：`WaveConfigProvider` 和 `DropTableProvider` 已经存在且功能完整

### 与原始计划的差异

**原始计划目标：**
- 完全通用的策略层
- 通过配置提供者注入实现特定逻辑
- 策略与对象类型完全解耦

**当前实现偏离：**
- 策略与特定对象类型绑定
- 业务逻辑混入策略层
- 失去了通用性的核心价值

**关键发现：**
- 配置提供者（`WaveConfigProvider`、`DropTableProvider`）已经存在且功能完整
- 重构的重点不是创建新配置提供者，而是改变策略层的使用方式
- 需要将特化策略替换为通用策略+配置提供者注入

---

## 通用策略架构设计

### 核心设计原则

1. **完全通用**：策略与对象类型无关
2. **配置驱动**：通过配置提供者注入特定逻辑
3. **职责单一**：策略只管"如何决定生成内容"
4. **无状态**：每次调用独立，不保存业务状态

### 三种通用策略

#### 1. ListSpawnStrategy<T>（列表生成策略）
**职责**：从预设列表中获取生成内容

**配置方式**：
- 直接配置对象列表（简单场景）
- 注入配置提供者（复杂场景）

**适用场景**：
- 敌人波次生成：注入 `WaveConfigProvider`
- 道具批次生成：注入 `ItemPoolProvider`
- 特效序列生成：注入 `EffectSequenceProvider`

#### 2. FixedSpawnStrategy<T>（固定生成策略）
**职责**：生成固定种类和数量的对象

**配置方式**：
- 配置模板对象 + 数量
- 支持权重随机选择（可选）

**适用场景**：
- 技能道具生成：每回合2个血瓶
- 敌人增援：每波次额外3个小兵
- 环境道具：每关固定5个宝箱

#### 3. ConditionalDropStrategy<T>（条件掉落策略）
**职责**：基于条件动态决定生成内容

**配置方式**：
- 注入条件检查器
- 注入掉落表提供者
- 配置概率参数

**适用场景**：
- 击杀掉落：注入 `DropTableProvider`
- 技能解锁掉落：注入 `SkillDropProvider`
- 条件奖励：注入 `ConditionRewardProvider`

---

## 重构风险分析

### 中风险：策略切换

**风险描述**：
- 从特化策略切换到通用策略可能影响现有功能
- 配置提供者注入机制需要验证兼容性
- 可能影响现有Trigger的配置方式

**具体风险点**：
1. **接口兼容性**：通用策略与现有配置提供者的接口匹配
2. **配置方式变化**：Trigger层的配置方式可能需要调整
3. **功能验证**：需要确保所有现有功能正常工作

**缓解措施**：
- 充分测试策略切换过程
- 保留原有配置方式作为fallback
- 逐步验证每个功能模块

### 低风险：配置复杂度增加

**风险描述**：
- 通用策略的配置方式可能与特化策略不同
- 策划需要理解配置提供者注入的概念
- 调试时可能需要检查多个组件

**缓解措施**：
- 提供清晰的配置示例
- 增加详细的配置验证
- 提供调试工具和日志

### 低风险：性能影响

**风险描述**：
- 额外的配置提供者调用可能影响性能
- 通用接口的虚函数调用开销

**缓解措施**：
- 性能测试和优化
- 必要时使用缓存机制

---

## 重构实施策略

### 阶段1：完善通用策略基础架构

**目标**：确保通用策略与现有配置提供者兼容

**任务清单**：
- 验证 `ListSpawnStrategy<T>` 与 `WaveConfigProvider` 的兼容性
- 验证 `ConditionalDropStrategy<T>` 与 `DropTableProvider` 的兼容性
- 完善 `FixedSpawnStrategy<T>` 的配置灵活性
- 测试通用策略的配置提供者注入机制

**验收标准**：
- 通用策略可以正确使用现有配置提供者
- 配置提供者注入机制正常工作
- 所有策略类型功能完整

### 阶段2：重构Trigger使用通用策略

**目标**：将Trigger从特化策略切换到通用策略

**任务清单**：
- `WaveSpawnTrigger` 改用 `ListSpawnStrategy<EnemyData>` + 注入现有 `WaveConfigProvider`
- `DeathDropTrigger` 改用 `ConditionalDropStrategy<ItemConfig>` + 注入现有 `DropTableProvider`
- 验证通用策略与现有配置提供者的兼容性
- 保持原有接口兼容性

**验收标准**：
- 现有功能完全正常
- 通用策略正确使用现有配置提供者
- 无性能回退

### 阶段3：清理和优化

**目标**：移除特化策略类，完善系统

**任务清单**：
- 删除 `WaveListSpawnStrategy` 特化策略类
- 删除 `DeathDropStrategy` 特化策略类
- 验证所有功能使用通用策略
- 完善通用策略的配置注入机制

**验收标准**：
- 特化策略类完全移除
- 所有功能使用通用策略
- 代码复用性显著提升

### 阶段4：实现新功能

**目标**：使用通用策略实现新功能

**任务清单**：
- 创建 `SkillItemSpawnTrigger` 使用 `FixedSpawnStrategy<ItemConfig>`
- 实现技能每回合生成固定道具功能
- 测试新功能与现有功能的兼容性
- 完善配置和调试工具

**验收标准**：
- 新功能正常工作
- 与现有功能无冲突
- 配置简单直观

---

## 配置示例对比

### 重构前：特化策略配置

```
WaveSpawnTrigger:
├── WaveListSpawnStrategy (特化)
│   ├── configProvider: WaveConfigProvider
│   ├── generateInitialEnemies: true
│   └── generateWaveEnemies: true

DeathDropTrigger:
├── DeathDropStrategy (特化)
│   ├── dropTableProvider: DropTableProvider
│   └── enableDebugLog: true
```

### 重构后：通用策略配置

```
WaveSpawnTrigger:
├── ListSpawnStrategy<EnemyData> (通用)
│   ├── configProvider: WaveConfigProvider (注入)
│   └── enableDebugLog: true

DeathDropTrigger:
├── ConditionalDropStrategy<ItemConfig> (通用)
│   ├── conditionChecker: SkillConditionChecker (注入)
│   ├── dropProvider: DropTableProvider (注入)
│   └── enableDebugLog: true

SkillItemSpawnTrigger (新增):
├── FixedSpawnStrategy<ItemConfig> (通用)
│   ├── itemTemplate: HealthPotion
│   ├── spawnCount: 2
│   └── enableDebugLog: true
```

---

## 架构优势

### 1. 真正的通用性
- 同一个策略可以用于任何对象类型
- 新增对象类型无需修改策略代码
- 策略逻辑与业务逻辑完全分离

### 2. 高度的可复用性
- 配置提供者可以在多个策略中复用
- 策略可以在多个Trigger中复用
- 减少代码重复，提高维护性

### 3. 灵活的扩展性
- 新增策略类型无需修改现有代码
- 配置提供者可以独立扩展
- 支持运行时动态配置

### 4. 清晰的职责分离
- **策略层**：只管"如何决定生成内容"
- **配置层**：只管"提供什么数据"
- **触发层**：只管"何时生成"
- **执行层**：只管"如何生成"

---

## 实施建议

### 1. 渐进式重构
- 保持现有功能正常运行
- 逐步迁移，每个阶段充分测试
- 保留fallback机制，确保可以回退

### 2. 配置友好
- 提供配置模板和向导
- 增加配置验证和错误提示
- 提供可视化配置工具

### 3. 文档完善
- 详细的配置说明
- 使用示例和最佳实践
- 常见问题解答

### 4. 团队培训
- 新架构设计理念培训
- 配置方式培训
- 调试和排错培训

---

## 成功标准

### 功能完整性
- 所有现有功能正常工作
- 新功能（技能主动生成）正常工作
- 无功能回退

### 架构质量
- 策略层完全通用
- 职责分离清晰
- 代码复用性显著提升

### 可维护性
- 配置简单直观
- 调试工具完善
- 文档完整清晰

### 扩展性
- 易于添加新策略类型
- 易于添加新对象类型
- 支持复杂的策略组合

通过这次重构，生成系统将真正实现"高内聚、低耦合"的设计目标，为后续功能开发提供强大而灵活的基础架构。
