# 技能系统 - 阶段1实现

## 概述

这是技能系统的阶段1实现，使用 ScriptableObject + 管理器组件模式，替代了临时的 TestSkillChain。系统支持可视化配置，保持与现有架构的一致性。

## 核心组件

### 1. SkillConfig (ScriptableObject)
- **功能**：技能配置数据，支持在 Inspector 中可视化配置
- **位置**：`Assets/Scripts/SkillSystem/SkillConfig.cs`
- **创建方式**：右键 → Create → Game → Skill Config

### 2. SkillManager (MonoBehaviour)
- **功能**：技能管理器组件，管理所有技能配置和运行时状态
- **位置**：`Assets/Scripts/SkillSystem/SkillManager.cs`
- **使用方式**：添加到 GameObject 上，配置技能列表

### 3. 配置类（简化设计）
- **TriggerConfig**：触发器配置
  - 选择触发器类型后直接显示对应参数
  - 支持碰撞触发器参数：目标标签、攻击类型过滤
- **ConditionConfig**：条件配置
  - 选择条件类型后直接显示对应参数
  - 支持计数、时间窗口、血量条件参数
- **SkillEffectConfig**：技能效果配置
  - 选择效果类型后直接显示对应参数
  - 支持属性修改效果参数：目标属性、倍数、移除条件

## 使用方法

### 1. 创建技能配置

1. 在 Project 窗口中右键
2. 选择 Create → Game → Skill Config
3. 命名为 "CollisionComboSkill"
4. 配置参数：
   - **技能名称**：碰撞连击
   - **触发器类型**：Collision
   - **条件类型**：Count (阈值: 2)
   - **效果类型**：StatModifier (目标: Damage, 倍数: 2)
   - **移除条件**：OnPlayerPhaseEnded

### 2. 设置技能管理器

1. 在场景中创建一个空 GameObject
2. 添加 SkillManager 组件
3. 将创建的技能配置拖拽到 "Active Skills" 列表中

### 3. 测试技能

1. 运行游戏
2. 让玩家碰撞敌人2次
3. 观察控制台日志，确认技能触发
4. 验证攻击力是否提升100%

## 迁移说明

### 从 TestSkillChain 迁移的功能

- ✅ **事件监听**：OnAttack 和 OnChargingStarted
- ✅ **技能逻辑**：Trigger + Condition + Effect 组合
- ✅ **状态重置**：每次发射时重置技能状态
- ✅ **调试功能**：详细的日志输出

### 新增功能

- ✅ **可视化配置**：通过 Inspector 配置技能
- ✅ **多技能支持**：可以配置多个技能
- ✅ **运行时管理**：动态添加/移除技能
- ✅ **调试工具**：Context Menu 调试功能
- ✅ **简化配置设计**：选择类型后直接显示对应参数，无嵌套层级
- ✅ **Inspector 友好**：清晰的参数分组，避免复杂嵌套

## 配置示例

### 默认技能配置（碰撞连击）

```csharp
技能名称: "碰撞连击"
触发器类型: Collision
- targetTag = "Enemy"
- useAttackTypeFilter = true
- attackType = "Hit"

条件类型: Count
- requiredCount = 2

效果类型: StatModifier
- targetStat = "Damage"
- modifierValue = 2.0
- modifierType = PercentMult
- removalCondition = OnPlayerPhaseEnded
```

### 配置优势

- ✅ **单层结构**：选择类型后直接显示参数，无嵌套层级
- ✅ **Inspector 友好**：清晰的参数分组，易于配置
- ✅ **简单直观**：避免复杂的多态设计，降低学习成本
- ✅ **易于扩展**：添加新类型时只需添加对应的参数字段

## 扩展性

系统设计支持未来扩展：

- **更多触发器类型**：Kill、Charging、Health 等
- **更多条件类型**：TimeWindow、Health、Resource 等
- **更多效果类型**：Status、Resource、Spawn 等
- **技能流派系统**：基于技能类型的分类管理

## 注意事项

1. **组件依赖**：确保 PlayerStatsManager 已正确设置
2. **事件系统**：依赖 GameEventBus 事件系统
3. **属性引用**：PlayerCore 需要从 PlayerStatsManager 获取最终属性值
4. **生命周期**：技能效果会在玩家回合结束时自动移除