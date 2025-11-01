# 行为系统重构执行计划

> **目的**：将当前硬编码的复合行为重构为基于原子行为的可组合系统
>
> **关联文档**：`GC2_Behavior_VS_Current_Architecture_Analysis.md`（架构分析）

---

## 📊 当前状态总览

### ✅ 已完成（Week 3）

| 组件 | 状态 | 说明 |
|------|------|------|
| **基础设施** | ✅ 完成 | |
| BehaviorStatus 枚举 | ✅ | 统一行为返回值（Success/Failure/Running/Ready） |
| EnemyRuntimeState | ✅ | 状态与行为分离 |
| **原子行为** | ✅ 完成 | |
| MoveTowardsBehavior | ✅ | 向目标靠近（可配置最小距离） |
| MoveAwayBehavior | ✅ | 远离目标（可配置触发距离） |
| IdleBehavior | ✅ | 保持静止 |
| **装饰器** | ✅ 已创建 | |
| RepeatDecorator | ✅ | 重复执行 N 次 |
| ConditionalDecorator | ✅ | 条件判断（距离、状态） |
| **组合器** | ✅ 已创建 | |
| SequenceBehavior | ✅ | 顺序执行（AND 逻辑） |
| SelectorBehavior | ✅ | 选择执行（OR 逻辑） |

### ⏳ 待重构（Week 4）

| 复合行为 | 当前状态 | 问题 | 重构目标 |
|---------|---------|------|---------|
| IntervalMovementBehavior | 硬编码 | 回合计数、阶段切换逻辑混杂 | 拆解为 Idle + MoveTowards/MoveAway |
| FleeBehavior | 硬编码 | 距离判断、逃离/靠近逻辑混杂 | 拆解为 Selector + Conditional |

---

## 🎯 重构优先级评估

### 评估维度

| 行为 | 复杂度 | 使用频率 | 重构收益 | 优先级 |
|------|--------|----------|---------|--------|
| IntervalMovement | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | **P0** |
| Flee | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | **P1** |

**结论**：先重构 IntervalMovement（最复杂，最能体现新架构优势）

---

## 📋 Phase 4：IntervalMovement 重构 ✅ 已完成

### 4.1 当前实现分析

**现有逻辑结构**：
```
IntervalMovementBehavior {
    ├── 回合计数逻辑（intervalCurrentRound）
    ├── 阶段切换逻辑（intervalIsInIdlePhase）
    ├── 条件判断：if (isInIdlePhase)
    │   ├── True → 执行 Idle
    │   └── False → 执行 Follow 或 Flee
    └── 参数读取：idleRounds, moveRounds, movementMode
}
```

**问题**：
1. ❌ 回合计数与移动逻辑混在一起
2. ❌ 阶段切换需要手动管理状态
3. ❌ 无法灵活调整间歇模式（如"2静止-3移动-1静止"）

---

### 4.2 目标架构设计

**新逻辑结构**：
```
IntervalMovementBehavior_V2 {
    ├── 配置：
    │   ├── Phase 1: Idle (N 回合)
    │   ├── Phase 2: MoveTowards/MoveAway (M 回合)
    │   └── 是否循环
    └── 运行时：SequenceBehavior 自动管理阶段切换
}
```

**核心思路**：
- 使用 **SequenceBehavior** 管理阶段顺序
- 使用 **RepeatDecorator** 控制每阶段回合数
- 移除手动回合计数和阶段切换逻辑

---

### 4.3 重构步骤

#### **Step 1：创建配置类**（15分钟）

**文件**：`MovementConfig.cs`

**新增配置**：
```
IntervalMovementConfig_V2 {
    ├── Phase[] phases  // 阶段列表
    │   └── Phase {
    │       ├── behaviorType (Idle/MoveTowards/MoveAway)
    │       ├── roundCount (回合数)
    │       └── config (对应的配置)
    │   }
    └── loopPhases (是否循环)
}
```

**产出**：
- ✅ 可序列化的阶段配置
- ✅ 支持任意数量阶段
- ✅ 每阶段独立配置参数

---

#### **Step 2：实现 IntervalMovementBehavior_V2**（30分钟）

**文件**：`IntervalMovementBehavior_V2.cs`

**核心逻辑**：
```
ExecuteMovement() {
    1. 初始化时：
       - 根据配置创建 SequenceBehavior
       - 为每个 Phase 创建 RepeatDecorator + AtomicBehavior
       - 缓存到 Blackboard（避免每回合重建）
    
    2. 执行时：
       - 调用 SequenceBehavior.ExecuteMovement()
       - 根据返回状态判断：
         * Running → 继续执行当前阶段
         * Success → 所有阶段完成，重置（如果循环）
         * Failure → 错误处理
}
```

**伪代码示例**：
```
Step 1: 构建行为树
sequence = new SequenceBehavior();
foreach (phase in config.phases) {
    atomicBehavior = CreateAtomicBehavior(phase.behaviorType);
    repeatBehavior = new RepeatDecorator(atomicBehavior, phase.roundCount);
    sequence.AddChild(repeatBehavior);
}

Step 2: 每回合执行
status = sequence.ExecuteMovement(...);
if (status == Success && config.loopPhases) {
    ResetSequence();
}
```

**产出**：
- ✅ 新的 IntervalMovement 实现
- ✅ 自动管理阶段切换
- ✅ 支持灵活配置阶段

---

#### **Step 3：更新 BehaviorFactory**（10分钟）

**文件**：`BehaviorFactory.cs`

**修改内容**：
```
添加条件分支：
- if (使用新配置) → return new IntervalMovementBehavior_V2();
- else → return new IntervalMovementBehavior();  // 旧系统兼容
```

**产出**：
- ✅ 支持新旧两种实现
- ✅ 可通过配置切换
- ✅ 不破坏现有敌人配置

---

#### **Step 4：Unity 配置与测试**（20分钟）

**操作步骤**：
1. **创建测试敌人**：
   - 复制现有 Interval 敌人
   - 命名为 `TestEnemy_IntervalV2`

2. **配置新系统**：
   - Phase 1: Idle, 2 回合
   - Phase 2: MoveTowards, 3 回合
   - Loop: true

3. **对比测试**：
   - 旧敌人：使用 IntervalMovement（旧）
   - 新敌人：使用 IntervalMovement_V2
   - 观察行为是否一致

4. **验证点**：
   - ✅ 回合切换正确
   - ✅ 移动距离一致
   - ✅ 循环正常
   - ✅ 无报错

**产出**：
- ✅ 功能验证通过
- ✅ 性能对比数据
- ✅ 旧系统保留作为后备

---

### 4.4 关键设计决策

#### **决策 1：如何缓存行为树？**

**方案 A**：存储在 Blackboard（推荐）
- ✅ 每个敌人独立
- ✅ 自动生命周期管理
- ❌ 需要序列化支持

**方案 B**：存储在 EnemyRuntimeState
- ✅ 明确的状态管理
- ❌ 需要手动清理
- ❌ RuntimeState 膨胀

**选择**：方案 A（Blackboard）

---

#### **决策 2：如何处理配置迁移？**

**方案 A**：双配置共存（推荐）
```
IntervalMovementConfig  // 旧配置（保留）
IntervalMovementConfig_V2  // 新配置
```

**方案 B**：原地升级配置
```
IntervalMovementConfig {
    useV2: bool
    v1Config: {...}
    v2Config: {...}
}
```

**选择**：方案 A（避免破坏现有数据）

---

#### **决策 3：RepeatDecorator 如何跨回合计数？**

**关键问题**：当前回合执行完毕，下回合如何知道已重复几次？

**解决方案**：
- RepeatDecorator 使用 Blackboard 存储计数
- Key 格式：`"RepeatDecorator_{instanceId}_Count"`
- 序列完成后统一清理

**实现细节**：
```
ExecuteMovement() {
    currentCount = blackboard.Get("RepeatCount");
    if (currentCount < repeatCount) {
        ExecuteChild();
        currentCount++;
        blackboard.Set("RepeatCount", currentCount);
        return Running;
    } else {
        blackboard.Remove("RepeatCount");
        return Success;
    }
}
```

---

### 4.5 预期收益

| 维度 | 改进 | 说明 |
|------|------|------|
| **代码行数** | -40% | 移除回合计数、阶段切换逻辑 |
| **可读性** | +60% | 配置即文档，一目了然 |
| **可维护性** | +50% | 修改阶段只需改配置 |
| **可扩展性** | +100% | 支持任意阶段组合 |
| **复用性** | +80% | Idle、MoveTowards 可用于其他敌人 |

---

## 📋 Phase 5：Flee 重构

### 5.1 当前实现分析

**现有逻辑结构**：
```
FleeBehavior {
    ├── 条件 1：if (distance > approachDistance)
    │   └── 执行 MoveTowards
    ├── 条件 2：else if (distance > triggerDistance)
    │   └── 执行 Idle
    └── 条件 3：else
        └── 执行 MoveAway
}
```

**问题**：
1. ❌ 多重 if-else 嵌套
2. ❌ 距离判断逻辑硬编码
3. ❌ 无法灵活调整优先级

---

### 5.2 目标架构设计

**新逻辑结构**：
```
FleeBehavior_V2 = SelectorBehavior {
    ├── Conditional(distance > approachDistance) → MoveTowards
    ├── Conditional(distance < triggerDistance) → MoveAway
    └── Fallback → Idle
}
```

**核心思路**：
- 使用 **SelectorBehavior** 管理优先级（从上到下尝试）
- 使用 **ConditionalDecorator** 封装距离判断
- 第一个成功的行为即执行，其余跳过

---

### 5.3 重构步骤

#### **Step 1：创建配置类**（10分钟）

**文件**：`MovementConfig.cs`

**新增配置**：
```
FleeMovementConfig_V2 {
    ├── approachDistance (接近距离阈值)
    ├── triggerDistance (逃离距离阈值)
    ├── moveTowardsConfig (靠近时的配置)
    ├── moveAwayConfig (逃离时的配置)
    └── enableApproach (是否启用接近模式)
}
```

---

#### **Step 2：实现 FleeBehavior_V2**（20分钟）

**文件**：`FleeBehavior_V2.cs`

**核心逻辑**：
```
ExecuteMovement() {
    1. 初始化时：
       - 创建 SelectorBehavior
       - 添加 Conditional(接近) + MoveTowards
       - 添加 Conditional(逃离) + MoveAway
       - 添加 Idle（默认）
    
    2. 执行时：
       - 调用 SelectorBehavior.ExecuteMovement()
       - Selector 自动选择第一个满足条件的行为
}
```

**伪代码**：
```
Step 1: 构建选择器
selector = new SelectorBehavior();
if (config.enableApproach) {
    approachBehavior = new ConditionalDecorator(
        new MoveTowardsBehavior(),
        condition: distance > approachDistance
    );
    selector.AddChild(approachBehavior);
}
fleeBehavior = new ConditionalDecorator(
    new MoveAwayBehavior(),
    condition: distance < triggerDistance
);
selector.AddChild(fleeBehavior);
selector.AddChild(new IdleBehavior());

Step 2: 执行
return selector.ExecuteMovement(...);
```

---

#### **Step 3：测试验证**（15分钟）

**测试场景**：
1. **场景 1**：玩家距离 10 单位 → 敌人靠近
2. **场景 2**：玩家距离 2 单位 → 敌人逃离
3. **场景 3**：玩家距离 5 单位 → 敌人静止

**验证点**：
- ✅ 行为切换流畅
- ✅ 无抖动现象
- ✅ 距离阈值生效

---

### 5.4 预期收益

| 维度 | 改进 | 说明 |
|------|------|------|
| **代码行数** | -30% | 移除多重 if-else |
| **可读性** | +40% | Selector 结构清晰 |
| **可扩展性** | +60% | 添加新条件只需 AddChild |

---

## 📋 Phase 6：清理与优化

### 6.1 旧系统清理

**时机**：Phase 4-5 完成并稳定运行 1 周后

**清理内容**：
1. **标记废弃**：
   - `IntervalMovementBehavior` → `[Obsolete]`
   - `FleeBehavior` → `[Obsolete]`

2. **迁移现有配置**：
   - 将所有使用旧系统的敌人迁移到 V2
   - 更新 `BehaviorFactory` 移除旧分支

3. **删除旧代码**：
   - 确认无引用后删除旧文件
   - 更新文档移除旧系统说明

---

### 6.2 性能优化

**优化点 1：行为树缓存**
- 避免每回合重建行为树
- 使用对象池复用节点

**优化点 2：条件判断优化**
- 缓存距离计算结果
- 减少重复的 Transform 访问

**优化点 3：Blackboard 优化**
- 使用类型化 Key（避免字符串比较）
- 定期清理无用状态

---

## 📊 总体时间规划

| 阶段 | 任务 | 预估时间 | 人员 |
|------|------|---------|------|
| **Week 4-1** | Phase 4: IntervalMovement 重构 | 2 天 | 开发 |
| **Week 4-2** | Phase 5: Flee 重构 | 1 天 | 开发 |
| **Week 4-3** | 测试与验证 | 1 天 | 开发+测试 |
| **Week 4-4** | 文档更新与总结 | 0.5 天 | 开发 |
| **Week 5** | 稳定运行观察 | 5 天 | 全员 |
| **Week 6** | Phase 6: 旧系统清理 | 1 天 | 开发 |

**总计**：约 2 周完成核心重构，3 周完成清理

---

## 🎯 成功标准

### 功能验证
- ✅ 所有现有敌人行为不变
- ✅ 新系统性能 ≥ 旧系统
- ✅ 无运行时错误
- ✅ 配置文件向后兼容

### 代码质量
- ✅ 单一职责：每个行为类 < 100 行
- ✅ 可测试性：行为可单独测试
- ✅ 可读性：代码自文档化
- ✅ 可维护性：修改影响范围 < 2 个文件

### 文档完善
- ✅ 更新架构分析文档
- ✅ 创建使用示例文档
- ✅ 记录关键设计决策
- ✅ 更新 Legacy Issues

---

## 🚨 风险与应对

| 风险 | 影响 | 概率 | 应对措施 |
|------|------|------|---------|
| 行为树状态管理复杂 | 高 | 中 | 使用 Blackboard 统一管理 |
| 旧配置迁移困难 | 中 | 低 | 保留双配置共存 |
| 性能下降 | 高 | 低 | 对象池 + 缓存优化 |
| RepeatDecorator 计数错误 | 高 | 中 | 单元测试 + 日志监控 |

---

## 📝 关键里程碑

| 里程碑 | 验收标准 | 预期日期 |
|--------|---------|---------|
| M1: 原子行为测试通过 | ✅ 已完成 | Week 3 |
| M2: IntervalMovement_V2 可用 | ✅ 已完成 | Week 4 Day 2 |
| M3: FleeBehavior_V2 可用 | 功能完整、测试通过 | Week 4 Day 3 |
| M4: 全面测试通过 | 所有敌人正常工作 | Week 4 Day 4 |
| M5: 文档完善 | 架构文档更新 | Week 4 Day 5 |
| M6: 旧系统清理 | 旧代码删除 | Week 6 |

---

## 🔗 参考文档

1. **架构分析**：`GC2_Behavior_VS_Current_Architecture_Analysis.md`
2. **遗留问题**：`Legacy_Issues.md`
3. **伤害系统**：`Damage_System_Architecture_Analysis.md`

---

**文档版本**：v2.0  
**创建日期**：2025-11-01  
**最后更新**：2025-11-01  
**维护者**：AI Assistant  
**状态**：Phase 4 已完成 → Phase 5 待执行  
**变更记录**：
- v2.0: Phase 4 (IntervalMovement V2) 完成并测试通过
- v1.0: 初始版本，规划文档创建

