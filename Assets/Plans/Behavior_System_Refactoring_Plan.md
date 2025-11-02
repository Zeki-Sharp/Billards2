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

#### **Step 1：扩展条件系统**（30分钟）

**新增类型**：
```csharp
public enum ConditionType { Distance, State, Always }
public enum ComparisonOperator { LessThan, GreaterThan, Equal, NotEqual }

public class ConditionConfig {
    public ConditionType type;
    public ComparisonOperator op;
    public float value;
}
```

**更新 ConditionalDecorator**：支持条件配置

---

#### **Step 2：创建配置类**（20分钟）

**文件**：`MovementConfig.cs`

**新增配置**：
```csharp
public class FleeMovementConfig_V2 {
    public FleePhaseConfig[] phases;  // 按优先级配置
}

public class FleePhaseConfig {
    public ConditionConfig condition;
    public AtomicMovementType actionType;
    public MoveTowardsConfig moveTowardsConfig;
    public MoveAwayConfig moveAwayConfig;
}
```

---

#### **Step 3：实现 FleeBehavior_V2**（45分钟）

**文件**：`FleeBehavior_V2.cs`

**核心逻辑**：构建 Selector + Conditional + Atomic，缓存到 Blackboard

---

#### **Step 4：配置与测试**（30分钟）

**Inspector 配置**：
```
Flee Config V2:
  Phases:
    - Condition: Distance < 2, Action: MoveAway
    - Condition: Distance > 5, Action: MoveTowards
    - Condition: Always, Action: Idle
```

**测试验证**：玩家靠近/远离时敌人正确响应

---

---

## 📋 Phase 6：架构统一（MovementType 简化）

### 6.1 设计目标

**核心理念**：统一所有移动行为为"有条件的原子行为序列"

**统一方案**：
- `PhaseSequence` 取代 `IntervalMovement`、`Flee`、`FollowPlayer`
- 新增 `PhaseSelectionMode`：Sequential（顺序）、Conditional（条件选择）

### 6.2 配置示例

**间歇移动**（Sequential）：
```
phases: [Idle x2, MoveTowards x3], loop: true
```

**逃离行为**（Conditional）：
```
phases: [
  MoveAway x1 (Distance<2),
  MoveTowards x1 (Distance>5),
  Idle x1 (Always)
], loop: true
```

### 6.3 实施步骤

| 步骤 | 内容 | 时间 |
|------|------|------|
| 1 | 重命名 IntervalMovement_V2 → PhaseSequence | 15分钟 |
| 2 | 移除冗余 MovementType | 10分钟 |
| 3 | 统一 EnemyLevelConfig 配置字段 | 10分钟 |
| 4 | 简化 BehaviorFactory | 20分钟 |
| 5 | 迁移现有配置 | 30分钟 |
| 6 | 测试验证 | 30分钟 |
| **总计** | | **1小时55分钟** |

---

## 📋 Phase 7：NodeCanvas 可视化（可选）

### 7.1 实施策略

**前提**：Phase 6 完成，SO 配置系统已统一

**目标**：将 SO 配置的行为树迁移到 NodeCanvas 图形化编辑器

**优势**：
- ✅ SO 配置作为保底，零风险
- ✅ NodeCanvas 提供更直观的可视化编辑
- ✅ 支持运行时调试（节点高亮）

### 7.2 集成步骤

| 步骤 | 内容 | 工作量 |
|------|------|--------|
| 1 | 创建自定义 Task 节点（MoveTowards/Away/Idle） | 1-1.5天 |
| 2 | 创建自定义 Condition 节点（Distance/State） | 1天 |
| 3 | EnemyBehavior 集成 BehaviourTreeOwner | 1-2天 |
| 4 | Blackboard 命名冲突处理 | 0.5-1天 |
| 5 | 迁移配置到图形化 | 0.5-1天 |
| 6 | 测试验证 | 1天 |
| **总计** | | **5-7天** |

### 7.3 Blackboard 冲突解决

**冲突**：
```csharp
using YourProject;  // 你的 Blackboard
using NodeCanvas.Framework;  // NC Blackboard
```

**解决方案**：命名空间别名
```csharp
using NCBlackboard = NodeCanvas.Framework.Blackboard;
using ProjectBlackboard = Blackboard;  // 你的 Blackboard
```

---

## 📋 Phase 8：清理与优化

### 8.1 旧系统清理

**时机**：Phase 6 完成并稳定运行 1 周后（或 Phase 7 完成后）

**清理内容**：
1. 标记废弃旧行为类（`[Obsolete]`）
2. 迁移所有配置到 PhaseSequence（或 NodeCanvas）
3. 删除旧代码

---

### 8.2 性能优化

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
| **Week 4-1** | Phase 4: IntervalMovement V2 | ✅ 已完成 | 开发 |
| **Week 4-2** | Phase 5: Flee V2 + 条件系统 | ⏳ 进行中 | 开发 |
| **Week 4-3** | Phase 6: 架构统一（PhaseSequence） | 1-2 天 | 开发 |
| **Week 4-4** | 测试与验证 | 0.5 天 | 开发+测试 |
| **Week 5-6** | Phase 7: NodeCanvas 可视化（可选） | 5-7 天 | 开发 |
| **Week 7** | Phase 8: 旧系统清理 | 1 天 | 开发 |

**核心路线**：
- **必须完成**：Phase 4-6（SO 配置系统）→ 约 1 周
- **可选升级**：Phase 7（NodeCanvas 可视化）→ 额外 1-1.5 周
- **清理优化**：Phase 8 → 稳定后执行

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
| M4: 架构统一（PhaseSequence） | 移除冗余 MovementType | Week 4 Day 4-5 |
| M5: SO 配置系统完成 | 所有敌人正常工作 | Week 4 结束 |
| M6: NodeCanvas 可视化（可选） | 图形化编辑器集成 | Week 5-6 |
| M7: 旧系统清理 | 旧代码删除 | Week 7 |

---

## 🔗 参考文档

1. **架构分析**：`GC2_Behavior_VS_Current_Architecture_Analysis.md`
2. **遗留问题**：`Legacy_Issues.md`
3. **伤害系统**：`Damage_System_Architecture_Analysis.md`

---

**文档版本**：v3.0  
**创建日期**：2025-11-01  
**最后更新**：2025-11-02  
**维护者**：AI Assistant  
**状态**：Phase 5 进行中 → Phase 6-7 规划完成  
**变更记录**：
- v3.0: 添加 Phase 7（NodeCanvas 可视化）规划，更新实施路线
- v2.0: Phase 4 (IntervalMovement V2) 完成并测试通过
- v1.0: 初始版本，规划文档创建

