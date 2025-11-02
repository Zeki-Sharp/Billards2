# NodeCanvas 集成计划

## 📋 迁移目标

**从**：基于 ScriptableObject 的 `PhaseSequenceConfig` 配置系统  
**到**：基于 NodeCanvas 的可视化行为树系统

**核心优势**：
- ✅ 可视化编辑行为树，更直观
- ✅ 实时调试，可在运行时查看节点执行状态
- ✅ 更灵活的行为组合和条件判断
- ✅ 易于理解和维护

---

## 🎯 架构设计

### 当前架构（Phase 6 完成）
```
EnemyBehavior
  └─ PhaseSequenceMovementBehavior
       └─ SequenceBehavior / SelectorBehavior
            └─ RepeatDecorator / ConditionalDecorator
                 └─ MoveTowardsBehavior / MoveAwayBehavior / IdleBehavior
```

### 目标架构（Phase 7）
```
EnemyBehavior
  └─ BehaviourTreeOwner (NodeCanvas 组件)
       └─ BehaviourTree Asset (可视化编辑)
            └─ 自定义 Action 节点
                 └─ 调用原子行为（MoveTowards/MoveAway/Idle）
```

---

## 📝 迁移步骤

### **Step 1: 创建自定义 Action 节点** ⭐
**目的**：让 NodeCanvas 能够调用现有的原子行为

**需要创建的节点**：
1. `ActionMoveTowards` - 向目标靠近
2. `ActionMoveAway` - 远离目标
3. `ActionIdle` - 保持静止

**实现方式**：
- 继承 NodeCanvas 的 `ActionTask` 基类
- 在 `OnExecute()` 中调用现有的原子行为类
- 返回 NodeCanvas 的 `Status`（Success/Running/Failure）
- 从 Blackboard 读取敌人数据和配置

**关键点**：
- ✅ 复用现有的原子行为类，不重复实现逻辑
- ✅ 适配 `BehaviorStatus` → NodeCanvas `Status`
- ✅ 适配 `EnemyRuntimeState` → NodeCanvas Blackboard

---

### **Step 2: 创建自定义 Condition 节点**
**目的**：支持条件判断（距离）

**需要创建的节点**：
1. `ConditionCheckDistance` - 检查与玩家距离

**实现方式**：
- 继承 NodeCanvas 的 `ConditionTask` 基类
- 复用现有的 `BehaviorConditionConfig` 逻辑

**可选节点（暂不实现）**：
- `ConditionCheckBlackboard` - 检查 Blackboard 状态（未来有需求再添加）

---

### **Step 3: 集成 NodeCanvas 到 EnemyBehavior**
**目的**：让敌人使用 NodeCanvas 行为树而不是 SO 配置

**修改内容**：
1. 为 Enemy Prefab 添加 `BehaviourTreeOwner` 组件
2. 修改 `EnemyBehavior.InitializeBehavior()`：
   - 检测是否有 `BehaviourTreeOwner`
   - 如果有，配置并启动行为树
   - 如果没有，回退到 `PhaseSequenceMovementBehavior`（兼容模式）
3. 修改 `EnemyBehavior.ExecuteMovePhase()`：
   - 如果使用 NodeCanvas，调用 `BehaviourTreeOwner.Tick()`
   - 否则使用原有逻辑

**关键点**：
- ✅ 保持向后兼容：旧 SO 配置仍然可用
- ✅ 渐进式迁移：可以逐个敌人迁移

---

### **Step 4: Blackboard 数据同步**
**目的**：确保 NodeCanvas Blackboard 和 `EnemyRuntimeState` 数据一致

**同步方案**：
- **方案 A（推荐）**：继续使用 `EnemyRuntimeState`，NodeCanvas 节点从中读写
- **方案 B**：完全迁移到 NodeCanvas Blackboard，弃用 `EnemyRuntimeState`

**推荐方案 A 的理由**：
- ✅ 最小改动，不破坏现有系统
- ✅ `EnemyRuntimeState` 已经过测试和验证
- ✅ 避免大规模重构其他系统（如 DamageSystem）

**实现方式**：
- 在 Action 节点中，通过 `agent.GetComponent<EnemyBehavior>()` 获取 `runtimeState`
- 在 Condition 节点中，同样通过组件访问

---

### **Step 5: 创建行为树资产**
**目的**：为每种敌人类型创建可视化行为树

**创建步骤**：
1. 在 Unity 中右键 → NodeCanvas → Behaviour Tree Asset
2. 打开 NodeCanvas 编辑器
3. 使用自定义节点搭建行为树
4. 保存为 `.asset` 文件

**示例：Follow_Contact_Enemy 行为树**
```
[Root]
  └─ Sequencer (Loop)
       └─ ActionMoveTowards (1次)
```

**示例：Flee_Range_Enemy 行为树**
```
[Root]
  └─ Selector
       ├─ Sequence
       │    ├─ ConditionCheckDistance (< 5)
       │    └─ ActionMoveAway (1次)
       └─ Sequence
            ├─ ConditionCheckDistance (> 7)
            └─ ActionMoveTowards (3次)
```

---

### **Step 6: 迁移现有敌人配置**
**目的**：将 SO 配置的敌人迁移到 NodeCanvas

**迁移流程**：
1. 为敌人 Prefab 添加 `BehaviourTreeOwner` 组件
2. 创建对应的行为树资产
3. 在 `BehaviourTreeOwner` 中分配行为树
4. 配置 Blackboard 变量（如敌人数据、玩家引用）
5. 测试行为是否正确
6. 删除或注释掉 `phaseSequenceConfig`（可选，保留作为备份）

**迁移顺序（建议）**：
1. 先迁移最简单的：`Follow_Contact_Enemy` (只有 MoveTowards)
2. 再迁移中等复杂的：`Follow_Thorn_Enemy` (MoveTowards + Idle)
3. 最后迁移最复杂的：`Flee_Range_Enemy` (条件选择)

---

### **Step 7: 清理旧系统（可选）**
**时机**：所有敌人迁移完成并测试通过后

**可删除的内容**：
- `PhaseSequenceMovementBehavior.cs`
- `PhaseSequenceConfig` 及相关配置类
- `SequenceBehavior.cs` / `SelectorBehavior.cs`
- `RepeatDecorator.cs` / `ConditionalDecorator.cs`
- `PhaseAtomicBehaviorWrapper`（内部类）

**保留的内容**：
- ✅ 原子行为类（`MoveTowardsBehavior`, `MoveAwayBehavior`, `IdleBehavior`）
- ✅ 原子行为配置类（`MoveTowardsConfig`, `MoveAwayConfig`, `IdleConfig`）
- ✅ `BehaviorStatus` 枚举
- ✅ `EnemyRuntimeState` 类

**原因**：NodeCanvas 节点需要调用这些核心组件

---

## ⚠️ 注意事项

### **1. 回合制游戏的特殊性**
- ❗ NodeCanvas 默认是实时更新的（每帧 Tick）
- ❗ 我们的游戏是回合制的（每回合 Tick 一次）
- ✅ **解决方案**：在 `EnemyBehavior.ExecuteMovePhase()` 中手动调用 `Tick()`，而不是让 NodeCanvas 自动更新

### **2. Status 语义适配**
- `BehaviorStatus.Success` ≈ NodeCanvas `Status.Success`
- `BehaviorStatus.Running` ≈ NodeCanvas `Status.Running`
- ❗ **关键**：在回合制中，单次移动应返回 `Success`，跨回合序列由 NodeCanvas 的 Sequencer 管理

### **3. 共享行为树实例**
- ❗ NodeCanvas 的 BehaviourTree 是共享的（类似 SO）
- ❗ 如果多个敌人使用同一个行为树资产，状态会污染
- ✅ **解决方案**：
  - **方案 A**：每个敌人实例化自己的行为树（推荐）
  - **方案 B**：所有状态存储在 `EnemyRuntimeState` 中，行为树无状态

### **4. Blackboard 系统对比与整合方案**

#### **两个 Blackboard 的功能对比**

| 特性 | 项目 Blackboard | NodeCanvas Blackboard |
|------|----------------|----------------------|
| **核心功能** | 简单键值对存储 | 完整的变量管理系统 |
| **实现方式** | 静态扩展方法 + Dictionary | MonoBehaviour 组件 |
| **作用域** | 每个 GameObject 独立 | 组件级，支持父子继承 |
| **类型支持** | 泛型 `Set<T>` / `Get<T>` | 强类型 Variable 类 |
| **Inspector 支持** | ❌ 无可视化 | ✅ 完整 Inspector 编辑 |
| **序列化** | ❌ 运行时存储 | ✅ 支持保存和加载 |
| **用途** | 行为树节点间共享状态 | 行为树全局变量和参数 |

#### **当前使用情况**

**项目 Blackboard**：
- 用于存储**行为树执行状态**（如 `SequenceIndex`, `RepeatCount`）
- 用于 DamageSystem 规则判断（如 `IsTrap`, `CanAttack`）
- 轻量级，无 Unity 依赖
- **使用次数**：17 个文件，124 处引用

**NodeCanvas Blackboard**：
- 行为树节点的**全局变量系统**
- 在 Inspector 中配置参数（如玩家引用、配置数据）
- 支持变量绑定和数据同步
- **必需**：NodeCanvas 行为树节点依赖它传递参数

#### **整合方案：保持两个系统并行** ✅

**推荐：不整合，两个系统各司其职**

**原因**：
1. ✅ **功能互补，不冲突**：
   - 项目 Blackboard：行为树**内部状态**（临时、跨回合）
   - NodeCanvas Blackboard：行为树**输入参数**（配置、引用）

2. ✅ **改动成本极高**：
   - 迁移到 NodeCanvas Blackboard 需要重构 DamageSystem
   - 需要修改 17 个文件的 124 处引用
   - 破坏现有 PhaseSequence 系统

3. ✅ **性能无影响**：
   - 项目 Blackboard 是静态 Dictionary，零开销
   - NodeCanvas Blackboard 只在节点访问时使用

#### **命名冲突解决**

**方案：使用完整命名空间** ✅

```csharp
// 在自定义 NodeCanvas Action 节点中
using NCBlackboard = NodeCanvas.Framework.Blackboard;  // NodeCanvas 的
// 不引入项目 Blackboard 的命名空间，直接用扩展方法

public class ActionMoveTowards : ActionTask
{
    protected override void OnExecute()
    {
        // 使用 NodeCanvas Blackboard 获取参数
        var enemyData = blackboard.GetVariable<EnemyData>("enemyData");
        
        // 使用项目 Blackboard 管理状态（通过扩展方法）
        var myBlackboard = agent.gameObject.GetBlackboard();
        myBlackboard.Set("currentPhase", "moving");
    }
}
```

**无需重命名**：
- 项目 Blackboard 通过扩展方法使用，不直接引用类名
- NodeCanvas Blackboard 在节点中通过 `blackboard` 参数访问

### **5. 兼容性和回退**
- ✅ 保持旧系统作为 fallback，确保迁移过程安全
- ✅ 可以同时支持两种系统，逐步迁移
- ✅ 如果 NodeCanvas 出现问题，可以快速回退到 SO 配置

---

## 🎨 可视化优势

### **调试和查看**
- 运行时可以在 NodeCanvas 编辑器中看到：
  - ✅ 当前执行到哪个节点（高亮显示）
  - ✅ 每个节点的执行状态（Success/Running/Failure）
  - ✅ Blackboard 变量的实时值
  - ✅ 条件判断的结果

### **设计和迭代**
- ✅ 可视化拖拽节点，直观理解行为逻辑
- ✅ 快速调整节点顺序和连接
- ✅ 容易发现逻辑错误和死循环
- ✅ 支持注释和分组，提高可读性

---

## 📊 工作量评估

| 任务 | 预计工作量 | 优先级 |
|------|------------|--------|
| Step 1: 创建自定义 Action 节点 | 2-3 小时 | ⭐⭐⭐ |
| Step 2: 创建自定义 Condition 节点 | 0.5-1 小时 | ⭐⭐⭐ |
| Step 3: 集成到 EnemyBehavior | 1-2 小时 | ⭐⭐⭐ |
| Step 4: Blackboard 数据同步 | 1 小时 | ⭐⭐ |
| Step 5: 创建行为树资产（3个敌人）| 1-2 小时 | ⭐⭐ |
| Step 6: 测试和调试 | 2-3 小时 | ⭐⭐⭐ |
| Step 7: 清理旧系统 | 1 小时 | ⭐ |
| **总计** | **8.5-13 小时** | - |

---

## 🚀 迁移建议

### **阶段 1：基础集成（MVP）**
- 完成 Step 1-4
- 迁移最简单的敌人（Follow_Contact_Enemy）
- 验证基础功能正常

### **阶段 2：全面迁移**
- 完成 Step 5-6
- 迁移所有敌人
- 充分测试

### **阶段 3：清理优化（可选）**
- 完成 Step 7
- 删除旧系统代码
- 整理文档

---

## 🔄 兼容性策略

### **双系统并存**
```csharp
// EnemyBehavior.cs 伪代码
void InitializeBehavior()
{
    var btOwner = GetComponent<BehaviourTreeOwner>();
    if (btOwner != null && btOwner.behaviour != null)
    {
        // 使用 NodeCanvas
        useNodeCanvas = true;
    }
    else if (phaseSequenceConfig != null)
    {
        // 使用旧系统
        movementBehavior = new PhaseSequenceMovementBehavior();
    }
    else
    {
        Debug.LogError("未配置移动行为！");
    }
}

void ExecuteMovePhase()
{
    if (useNodeCanvas)
    {
        // NodeCanvas 行为树
        btOwner.Tick();
    }
    else
    {
        // 旧系统
        movementBehavior.ExecuteMovement(...);
    }
}
```

---

## ✅ 完成标志

迁移成功的标准：
- ✅ 所有敌人使用 NodeCanvas 行为树
- ✅ 行为与 SO 配置版本完全一致
- ✅ 无编译错误和运行时错误
- ✅ 可视化编辑器正常工作
- ✅ 运行时调试功能正常
- ✅ 旧系统代码已清理（可选）

---

## 📚 参考资源

- **NodeCanvas 文档**：https://nodecanvas.paradoxnotion.com/documentation/
- **NodeCanvas API**：查看 `Assets/ParadoxNotion/NodeCanvas/` 中的源码
- **现有行为系统**：`Assets/Scripts/Enemy/Behaviors/` 中的原子行为类

---

*创建时间：Phase 6 完成后*  
*状态：待执行*

