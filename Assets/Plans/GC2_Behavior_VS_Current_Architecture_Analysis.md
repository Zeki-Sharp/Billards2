# GC2 Behavior 与当前项目行为架构对比

> **目的**：对比 GC2 Behavior 包与当前项目，提取可学习的架构优化思路
>
> **关联文档**：`Damage_System_Architecture_Analysis.md`（伤害系统专项）

---

## 一、核心架构对比

### 1.1 设计理念差异

| 维度 | GC2 Behavior | 当前项目 |
|------|--------------|----------|
| **设计目标** | 通用 AI 系统 | 专用游戏系统 |
| **核心模式** | Graph + Node + Processor | Event + Strategy + Component |
| **数据管理** | RuntimeData 分离 | 组件变量存储 |
| **行为组合** | 节点树组合 | 策略模式 + 代码逻辑 |
| **状态管理** | 统一 Status 枚举 | 各自定义状态 |

### 1.2 GC2 核心特点

- **Graph-Based**：ScriptableObject 图表 + 可视化编辑
- **多模式**：Behavior Tree、State Machine、GOAP、Utility Board
- **RuntimeData 分离**：状态与配置完全分离，支持多实例
- **统一接口**：所有节点返回 Status（Ready/Running/Success/Failure）

### 1.3 当前项目特点

- **事件驱动**：GameEventBus 松耦合通信
- **策略模式**：IMovementBehavior、IAttackBehavior 接口
- **组件化**：职责细分（PlayerBehavior、MovementController、StateMachine）
- **配置驱动**：ScriptableObject（EnemyData、PlayerData）

---

## 二、当前项目的核心问题

### 2.1 复合行为硬编码

**问题**：IntervalMovement、Flee 等复合行为逻辑固定，难以复用

**表现**：
```
IntervalMovement: 
- 停止/移动切换逻辑硬编码在类内部
- 无法复用到其他场景（玩家间歇攻击）

Flee:
- "逃离+靠近"判断逻辑混在一起
- 无法拆分复用单个行为
```

**GC2 方案**：通过 Composite（Selector、Sequence）+ Decorator（Condition、Repeat）组合

**理想架构**：
```
原子行为：MoveTowards、MoveAway、Idle
组合器：Sequence、Selector、Repeat
条件节点：DistanceCondition、StateCondition

Flee = Selector {
  Sequence { Condition: 距离<最小值, Task: MoveAway }
  Sequence { Condition: 距离>最大值, Task: MoveTowards }
  Task: Idle
}
```

---

### 2.2 状态与行为绑定太深

**问题**：运行时状态（isDashing、isMoving）存储在 Behavior 组件中，新增行为需要修改多处

**表现**：
- 冲刺行为需要在多处检查 `isDashing`
- 行为权限判断散落各处
- 状态查询依赖组件引用

**GC2 方案**：RuntimeData + Blackboard 模式

**理想架构**：
```
RuntimeState（持久状态）：
- currentPhase, currentState, lastActionTime

Blackboard（共享数据）：
- "CanDash": bool
- "IsDashing": bool
- "DashDirection": Vector2

行为查询 Blackboard，不依赖组件状态
```

---

### 2.3 攻击系统不可配置 ✅ 已解决

**原问题**：每种攻击组合需要写新类 

**原表现**：
- 碰撞伤害、停止伤害、间隔伤害分散在各处
- 新增攻击组合需要写新 AttackBehavior 类
- 触发条件硬编码

**✅ 当前解决方案**：规则驱动的 DamageSystem
- **DamageRuleConfig**：通过配置定义攻击条件（Trigger、Tag、State、Damage）
- **DamageProfile**：组合多个规则形成攻击配置集
- **支持的触发类型**：Collision（碰撞/范围）、Stopped（停止）
- **状态条件**：requireSourceState、requireTargetNotState（支持复杂条件判断）
- **详见**：`Damage_System_Architecture_Analysis.md`、`NewDamageSystem_Migration_Guide.md`

---

## 三、可借鉴的关键设计

### 3.1 RuntimeData 状态分离

**GC2 做法**：
```
Graph（配置数据）
  ↓ 不变
RuntimeData（运行时状态）
  ↓ 每个 Processor 独立
Processor（执行器）
```

**当前项目应用**：
```
EnemyData（配置）+ EnemyRuntimeState（运行时）分离
- isMoving、isDead 移入 RuntimeState
- Behavior 组件只负责逻辑，不存储状态
```

**优势**：状态保存/恢复、多实例运行、序列化

---

### 3.2 统一的行为状态接口

**GC2 做法**：所有节点返回 `Status`（Ready/Running/Success/Failure）

**当前项目应用**：
```csharp
// 定义统一返回值
public enum BehaviorStatus {
    Success, Failure, Running, Ready
}

// 修改接口
public interface IMovementBehavior {
    BehaviorStatus Execute(...);  // 而不是 void 或 Vector2
}
```

**优势**：上层可根据返回值决策，支持行为组合

---

### 3.3 Blackboard 共享数据模式

**GC2 做法**：Beliefs 系统，行为间共享数据

**当前项目应用**：
```csharp
// Blackboard 存储动态状态
blackboard.Set("CanDash", true);
blackboard.Set("IsDashing", true);

// 行为查询 Blackboard
if (blackboard.Get<bool>("CanDash")) {
    ExecuteDash();
}

// 伤害系统查询状态
if (blackboard.Get<bool>("IsDashing") && damageConfig.dealDamageWhileDashing) {
    DealDamage();
}
```

**优势**：状态解耦、权限集中管理、易于扩展

---

### 3.4 行为树组合模式

**GC2 做法**：Composite + Decorator + Task 节点组合

**当前项目应用（简化版）**：
```
核心节点：
- Composite: Sequence（顺序）、Selector（选择）
- Decorator: Condition（条件）、Repeat（重复）
- Task: 原子行为（MoveTowards、Attack、Idle）

示例：IntervalMovement 重构
Sequence {
  Repeat(idleRounds) { Task: Idle }
  Repeat(moveRounds) { Task: Follow }
}
```

**优势**：行为复用、减少硬编码、易于组合

---

## 四、优化建议（按优先级）

### ✅ 已完成项

#### 1. Blackboard 模式 ✅
- **状态**：已完成（新伤害系统迁移）
- **实现**：
  ```csharp
  Blackboard {
      Set<T>(key, value), Get<T>(key), TryGet<T>(key, out value)
      SetOwner(GameObject)  // 关联所有者
  }
  BlackboardExtensions {
      GetBlackboard()  // 自动创建
      TryGetBlackboard()  // 不自动创建
  }
  ```
- **应用**：
  - `CanAttack` 状态控制（玩家/敌人攻击时机）
  - `IsTrap` 状态控制（敌人陷阱无敌）
  - DamageSystem 规则判断

#### 2. 规则驱动的伤害系统 ✅
- **状态**：已完成（Phase 0-5 全部完成）
- **实现**：
  - DamageRuleConfig（规则配置）
  - DamageProfile（规则集合）
  - DamageSystem（规则判断引擎）
  - CollisionEvent/StoppedEvent（事件抽象）
- **应用**：
  - 玩家碰撞/范围攻击
  - 敌人近战/远程/陷阱攻击
  - 状态条件判断（requireSourceState、requireTargetNotState）

---

### ⭐⭐⭐ 高优先级（1-2 周，立即实施）

#### 3. 统一行为返回值（BehaviorStatus）✅
- **状态**：已完成（Week 3）
- **工作量**：中
- **影响**：IMovementBehavior、IAttackBehavior
- **产出**：
  ```csharp
  public enum BehaviorStatus {
      Success, Failure, Running, Ready
  }
  
  public interface IMovementBehavior {
      BehaviorStatus ExecuteMovement(EnemyRuntimeState runtimeState, ...);
  }
  public interface IAttackBehavior {
      BehaviorStatus ExecuteTelegraph(EnemyRuntimeState runtimeState, ...);
      BehaviorStatus ExecuteAttack(EnemyRuntimeState runtimeState, ...);
      BehaviorStatus CleanupAttack(EnemyRuntimeState runtimeState, ...);
  }
  ```
- **实现文件**：
  - `BehaviorStatus.cs`（枚举定义）
  - 所有 Behavior 类已更新签名

#### 4. 提取 RuntimeState 类 ✅
- **状态**：已完成（Week 3）
- **工作量**：小
- **影响**：EnemyBehavior、PlayerBehavior
- **产出**：
  ```csharp
  EnemyRuntimeState {
      currentPhase, currentMovementState, currentAttackState,
      lastActionTime, lastAttackTime, lastMoveTime,
      isMoving, isDead, currentDirection, targetPosition,
      intervalCurrentRound, intervalIsInIdlePhase,
      isTrapMode, thornCurrentRound, thornLastActivateRound,
      thornLastDamageTime
  }
  ```
- **实现文件**：`EnemyRuntimeState.cs`
- **应用**：所有 Movement/Attack Behavior 已使用 runtimeState 参数

---

### ⭐⭐ 中优先级（2-4 周，重要但非紧急）

#### 5. 拆解复合行为为原子行为
- **目标**：提高行为复用性
- **工作量**：中
- **重构对象**：
  - IntervalMovement → Sequence + Repeat + Idle/Follow
  - Flee → Selector + Condition + MoveAway/MoveTowards
- **产出**：
  - 原子行为：MoveTowards, MoveAway, Idle
  - 组合器：Sequence, Selector, Repeat
  - 条件节点：DistanceCondition, StateCondition

#### 6. 简化版行为树
- **目标**：支持复杂行为组合
- **工作量**：大
- **实现范围**：
  - 基础节点（Composite、Decorator、Task）
  - 不需要可视化编辑器
  - 通过代码/ScriptableObject 构建

#### 7. 集中化行为更新（BehaviorManager）
- **目标**：性能优化
- **工作量**：中
- **产出**：
  - 统一管理所有敌人更新
  - 支持分帧更新
  - 可配置更新频率

---

### ⭐ 低优先级（长期优化，可选）

#### 8. Utility-Based Selection
- **目标**：动态行为选择
- **适用场景**：复杂 AI 决策
- **工作量**：大

#### 9. 嵌套状态机
- **目标**：支持子状态
- **适用场景**：复杂状态管理
- **工作量**：大

#### 10. Parameters 系统
- **目标**：动态参数传递
- **工作量**：中

---

### ❌ 不建议实现

以下 GC2 功能对当前项目过于复杂：
- 可视化编辑器（开发成本高）
- GOAP 系统（不需要目标导向规划）
- 完整 Graph 系统（学习成本高）
- Action Plan（敌人行为相对简单）

---

## 五、重构执行顺序

### 5.1 为什么先伤害后行为？

**依赖分析**：
```
Blackboard（基础设施，1天）
    ↓ 被使用
┌─ DamageSystem（查询状态，9天）
└─ BehaviorSystem（设置状态，10天）
```

**优势**：
- ✅ 伤害系统独立性强，只需 Blackboard 最小集
- ✅ 解决燃眉之急（冲撞、撞墙场景）
- ✅ 为行为系统提供基础设施
- ✅ 风险低、工作量小
- ✅ 无循环依赖

---

### 5.2 实施路线图

#### **Phase 0：Blackboard 基础设施** ✅ 已完成
```
- [x] 实现 Blackboard 类（Get/Set/TryGet）
- [x] MonoBehaviour 扩展（GetBlackboard/TryGetBlackboard）
- [x] SetOwner 机制（关联所有者）
```

#### **Phase 1：伤害系统重构** ✅ 已完成
```
阶段 1-5 全部完成：
- [x] 玩家碰撞攻击迁移
- [x] 敌人近战攻击迁移（主动检测）
- [x] 敌人陷阱攻击迁移（状态控制）
- [x] 玩家范围攻击迁移（Stopped 事件）
- [x] 远程攻击简化（跟随敌人移动）
- [x] 旧系统完全清理

产出：✅ 伤害系统完整可用，所有战斗伤害统一由 DamageSystem 处理
```

#### **Phase 2：行为系统重构（进行中）**
```
Week 3: ✅ 已完成
- [x] Day 11-13: RuntimeState + BehaviorStatus（3 天）
  - 创建 BehaviorStatus 枚举
  - 创建 EnemyRuntimeState 类
  - 更新 IMovementBehavior/IAttackBehavior 接口
  - 迁移所有 Behavior 实现类

Week 4: ✅ 已完成
- [x] Day 14-16: IntervalMovement V2 重构（✅ 完成）
  - 创建 IntervalMovementConfig_V2 配置类
  - 实现 IntervalMovementBehavior_V2（Sequence + Repeat + Atomic）
  - 修复原子行为返回值（Running → Success）
  - 测试通过：移动-静止循环正常
- [x] Day 17-18: FleeBehavior V2 重构（✅ 完成）
  - 扩展条件系统（BehaviorConditionConfig）
  - 创建 FleeMovementConfig_V2 配置类
  - 实现 FleeBehavior_V2（Selector + Conditional + Atomic）
  - 添加 moveSpeed 字段修复
- [x] Day 19-20: 架构统一（PhaseSequence）（✅ 完成）
  - 重命名 IntervalMovement_V2 → PhaseSequenceMovementBehavior
  - 添加 PhaseSelectionMode（Sequential/Conditional）
  - 统一配置类（PhaseSequenceConfig）
  - 支持两种模式：顺序执行 + 条件选择

产出：✅ 所有复合行为统一为 PhaseSequence 系统
```

**Phase 0-1 已完成**：伤害系统重构（约 6-10 天）
**Phase 2 进行中**：行为系统重构（Week 3 完成，Week 4 待实施）
**总时长**：20 天（4 周）

#### **Phase 3：可选优化（Week 5+）**
```
- 简化版行为树
- BehaviorManager 集中更新
- Utility-Based Selection
- 其他优化（按需）
```

---

## 六、预期收益

### 技术收益（当前已实现）
- ✅ **伤害系统解耦**：规则驱动，新增攻击模式无需修改代码
- ✅ **状态管理统一**：Blackboard 统一状态查询，避免组件依赖
- ✅ **事件驱动架构**：CollisionEvent/StoppedEvent/DamageEvent 分层清晰
- ✅ **双重伤害消除**：规则层过滤，无需运行时缓存

### 功能收益（当前已实现）
- ✅ **复合攻击模式**：通过 DamageProfile 配置组合多种攻击
- ✅ **状态条件控制**：CanAttack、IsTrap 等状态精确控制伤害时机
- ✅ **攻击范围动态读取**：支持从 PlayerData 动态读取攻击范围
- ✅ **主动检测机制**：范围攻击不依赖被动触发，精确控制攻击时机

### 待实现收益（行为系统重构后）
- ✅ **可维护性提升**：RuntimeState 分离（已完成 Week 3）
- ✅ **统一状态接口**：BehaviorStatus 返回值（已完成 Week 3）
- ⏳ **行为复用**：原子行为拆解（Week 4 待实施）
- ⏳ **全局阶段冲突解决**：Behavior Sequence System（Week 4 待实施）
- ⏳ **集中更新优化**：BehaviorManager 统一管理（可选）

---

## 七、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 学习成本高 | 中 | 渐进式迁移，先简单场景 |
| 重构工作量大 | 高 | 按优先级分阶段实施 |
| 调试更抽象 | 中 | 提供日志工具、可视化 |
| 现有代码兼容 | 中 | 保留旧接口，逐步迁移 |

---

## 八、参考资源

### GC2 Behavior 核心文件
- `Processor.cs`：运行时执行器
- `Graph.cs`：行为图基类
- `TNode.cs`：节点基类
- `RuntimeData.cs`：运行时数据

### 当前项目核心文件
- `PlayerStateMachine.cs`：玩家状态机
- `EnemyBehavior.cs`：敌人行为
- `IMovementBehavior.cs`：移动接口
- `GameEventBus.cs`：事件总线

---

**文档版本**：v8.0  
**创建日期**：2025-11-01  
**最后更新**：2025-11-02  
**维护者**：AI Assistant  
**变更记录**：
- v8.0: Week 4 完成（Phase 4-6），架构统一为 PhaseSequence 系统
- v7.0: Week 4 Phase 4 完成（IntervalMovement V2），修复原子行为返回值问题
- v6.0: Week 3 完成（RuntimeState + BehaviorStatus），标记陷阱攻击问题（Legacy Issue #1）
- v5.0: 伤害系统重构完成，更新当前状态，重新调整优先级和路线图
- v4.0: 澄清概念混淆，移除不必要系统，明确先伤害后行为的执行顺序
- v3.0: 精简内容，去除重复示例，添加优先级明确的优化建议
- v2.0: 补充复合行为拆解、Blackboard 解决方案
- v1.0: 初始版本
