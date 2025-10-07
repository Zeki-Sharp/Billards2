# 技能系统 - 第一阶段最小验证版本

## 概述

这是技能系统的第一阶段最小验证版本，实现了基础的 Trigger + Condition + Effect 组合模式。

## 已实现的功能

### 1. 接口定义
- `ITrigger` - 触发器接口
- `ICondition` - 条件接口  
- `IEffect` - 效果接口

### 2. 具体实现

#### 触发器
- `CollisionTrigger` - 碰撞事件检测
  - 监听 `GameEventBus.OnAttack` 事件
  - 检测 AttackType 为 "Hit" 的碰撞事件

#### 条件
- `CountCondition` - 计数条件
  - 检查碰撞次数是否达到阈值（默认3次）
  - 支持条件重置

#### 效果
- `StatModifierEffect` - 数值调整效果
  - 修改玩家属性（攻击力、血量、移动速度等）
  - 通过 `GameEventBus.PublishEffectEvent` 触发表现效果

### 3. 测试脚本
- `TestSkillChain` - 完整技能链路测试
  - 组合：`CollisionTrigger` + `CountCondition(3)` + `StatModifierEffect(攻击力+50%)`
  - 测试场景：碰撞3次后攻击力+50%

## 使用方法

### 1. 在Unity中使用测试脚本

1. 在场景中创建一个空的GameObject
2. 添加 `TestSkillChain` 脚本
3. 运行游戏，让玩家碰撞敌人
4. 观察控制台日志，验证技能链路工作流程

### 2. 测试流程

1. **第一次碰撞** → 条件未满足 → 技能不触发
2. **第二次碰撞** → 条件未满足 → 技能不触发  
3. **第三次碰撞** → 条件满足 → 技能触发 → 攻击力+50%

### 3. 验证要点

- 事件监听和分发机制
- Trigger + Condition + Effect 组合模式
- 技能执行流程
- 与现有系统的集成
- 表现效果触发

## 文件结构

```
Assets/Scripts/SkillSystem/
├── Interfaces/
│   ├── ITrigger.cs          # 触发器接口
│   ├── ICondition.cs        # 条件接口
│   └── IEffect.cs           # 效果接口
├── Triggers/
│   └── CollisionTrigger.cs  # 碰撞触发器
├── Conditions/
│   └── CountCondition.cs    # 计数条件
├── Effects/
│   └── StatModifierEffect.cs # 数值调整效果
├── TestSkillChain.cs        # 测试脚本
└── README.md               # 说明文档
```

## 下一步计划

这个最小验证版本验证了架构的可行性，为后续的三层架构实现（SkillEventManager + SkillSystem + SkillDefinition）奠定了基础。

## 注意事项

- 这是最小验证版本，只实现了基础功能
- 尚未实现三层架构和技能管理系统
- 当前版本直接监听事件，后续会通过三层架构分发
- 保持与现有事件系统的兼容性
