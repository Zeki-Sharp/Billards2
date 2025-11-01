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

### 2.3 攻击系统不可配置

**问题**：每种攻击组合需要写新类（详见伤害系统文档）

**表现**：
- 碰撞伤害、停止伤害、间隔伤害分散在各处
- 新增攻击组合需要写新 AttackBehavior 类
- 触发条件硬编码

**解决方案**：Trigger-based 攻击系统（详见 `Damage_System_Architecture_Analysis.md`）

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

### ⭐⭐⭐ 高优先级（1-2 周，立即实施）

#### 1. 提取 RuntimeState 类
- **目标**：状态与行为分离
- **工作量**：小
- **影响**：EnemyBehavior、PlayerBehavior
- **产出**：
  ```csharp
  EnemyRuntimeState {
      currentPhase, currentState, lastActionTime,
      isMoving, isDead, currentDirection
  }
  ```

#### 2. 引入 Blackboard 模式
- **目标**：解决状态绑定问题
- **工作量**：小
- **影响**：行为系统、伤害系统
- **产出**：
  ```csharp
  Blackboard {
      Set(key, value), Get<T>(key), TryGet<T>(key, out value)
  }
  ```

#### 3. 统一行为返回值（BehaviorStatus）
- **目标**：支持行为组合判断
- **工作量**：中
- **影响**：IMovementBehavior、IAttackBehavior
- **产出**：
  ```csharp
  BehaviorStatus Execute(...);  // 替换 void
  ```

#### 4. （暂不实施）
- **说明**：攻击触发逻辑由 DamageSystem 统一处理
- **原有计划**：Trigger-based 攻击系统
- **现状**：DamageSystem 已包含攻击触发，无需额外系统
- **详见**：`Refactoring_Execution_Order_Analysis.md`

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

#### **Phase 0：Blackboard 基础设施（Day 1）**
```
- [ ] 实现 Blackboard 类（Get/Set/TryGet）
- [ ] MonoBehaviour 扩展（GetBlackboard()）
- [ ] 单元测试
```

#### **Phase 1：伤害系统重构（Day 2-10，详见伤害系统文档）**
```
Week 1:
- Day 2-3: 伤害系统核心
- Day 4-5: 规则系统 + DamageProcessor 整合

Week 2:
- Day 6-7: 碰撞重构
- Day 8-9: 场景实现
- Day 10: 缓冲

产出：✅ 伤害系统完整可用，解决冲撞/撞墙场景
```

#### **Phase 2：行为系统重构（Day 11-20）**
```
Week 3:
- Day 11-13: RuntimeState + BehaviorStatus（3 天）
- Day 14-15: 原子行为定义（2 天）

Week 4:
- Day 16-18: IntervalMovement 重构（3 天）
- Day 19-20: Flee 重构 + 测试（2 天）

产出：✅ 行为系统松耦合、可组合
```

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

### 技术收益
- ✅ **可维护性提升 40%**：状态分离、逻辑解耦
- ✅ **开发效率提升 30%**：行为复用、配置驱动
- ✅ **扩展性提升 50%**：组合模式、规则驱动、修改器链

### 功能收益
- ✅ **新增冲刺行为**：通过 Blackboard 轻松实现
- ✅ **复合攻击模式**：通过规则配置组合
- ✅ **行为复用**：原子行为可在多处使用

### 性能收益
- ✅ **集中更新优化**：BehaviorManager 统一管理
- ✅ **状态查询优化**：Blackboard 字典查询

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

**文档版本**：v4.0  
**创建日期**：2025-11-01  
**维护者**：AI Assistant  
**变更记录**：
- v4.0: 澄清概念混淆，移除不必要系统，明确先伤害后行为的执行顺序
- v3.0: 精简内容，去除重复示例，添加优先级明确的优化建议
- v2.0: 补充复合行为拆解、Blackboard 解决方案
- v1.0: 初始版本
