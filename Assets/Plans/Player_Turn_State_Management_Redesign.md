# 玩家回合状态管理与回合完成判定重构计划

> **文档目的**：重构多角色系统的玩家回合状态管理和回合完成判定机制
> 
> **范围**：状态流程、回合管理、角色选择限制
> 
> **不包含**：输入系统、技能系统（这些在其他文档中）

---

## 📋 当前问题诊断

### 问题1：状态定义不清晰

**当前状态系统的问题**：
- `PlayerStateMachine` 有 `Idle, Charging, Moving, MovingEnd` 状态
- `CharacterSelectionController` 维护"选中状态"（变量，不是正式状态）
- **"选中"和"已完成"都不是状态机状态**

**导致的问题**：
- 输入逻辑混乱（点击时不知道应该选中还是发射）
- 状态分散在两个组件中，容易冲突
- 无法清晰表达角色的完整生命周期

---

### 问题2：回合完成判定错误

**旧系统（单球）**：
```
PlayerStateMachine.OnPlayingComplete 触发
  ↓
PlayerPhaseController 立即切换到敌人回合
```

**新系统（多球）的问题**：
```
球1 发射完成 → PlayerStateMachine1.OnPlayingComplete ← ❌ 立即结束回合？
球2 还没发射 → 但回合已经结束了！
```

**根本问题**：
- 旧系统：1个球 = 1个回合
- 新系统：需要发射2个球才完成回合
- 需要统计发射次数，而不是监听单个球

---

### 问题3：切换选中时状态不正确

**当前流程问题**：
```
选中角色A → 进入 Charging
切换到角色B → ChargeController 停止蓄力
角色A 的 PlayerStateMachine → ❌ 还停在 Charging 状态！
```

**缺失的逻辑**：
- 取消选中时，角色应该从 Charging 切回 Idle
- 发射完成的角色应该进入 Completed 状态，不能再被选中

---

## 🎯 正确的状态流程设计

### 角色状态定义（PlayerStateMachine）

**状态枚举扩展**：
```
1. Idle        - 空闲：可以被选中，等待操作
2. Selected    - 已选中：等待蓄力输入（新增）
3. Charging    - 蓄力中：正在蓄力，显示瞄准线
4. Moving      - 发射移动中：物理运动
5. MovingEnd   - 移动结束：攻击判定阶段
6. Completed   - 本回合已完成：不能再发射，不能再被选中（新增）
```

---

### 完整状态转换流程

#### 阶段1：回合开始初始化
```
【玩家回合开始】
所有角色状态：Idle
PlayerTurnManager 初始化：
  - remainingLaunches = 2（剩余发射次数）
  - launchedCharacterIDs = []（空列表）
```

---

#### 阶段2：选中流程
```
【Idle 状态 + 左键点击自己】
  → Selected（已选中）
  
【Selected 状态的行为】：
  - 滚轮输入 ≥ 门槛值 → Charging（开始蓄力）
  - 滚轮输入 < 门槛值 + 点击其他球 → 切换选中
    - 旧球：Selected → Idle
    - 新球：Idle → Selected
  - 右键点击 → Idle（取消选中）
  - 点击已发射过的球 → 不允许（状态是 Completed）
```

---

#### 阶段3：蓄力流程
```
【Selected + 滚轮 ≥ 门槛值】
  → Charging（蓄力中）
  
【Charging 状态的行为】：
  - 左键点击 → Moving（发射！）
  - 右键点击 → Idle（取消选中，蓄力归0）
  - 滚轮输入 → 继续调节力度
  - 点击其他球 → 不允许（先取消当前选中）
```

---

#### 阶段4：发射流程
```
【Charging + 左键点击】
  → Moving（发射移动）
    ↓ 球停止运动
    ↓ MovingEnd（攻击判定、范围伤害）
    ↓ Completed（本回合该角色完成）
```

**Completed 状态的行为**：
- 不能再被选中（CharacterSelectionController 会阻止）
- 不能再发射
- 保持状态直到下一回合开始

---

#### 阶段5：回合完成判定
```
【每次发射完成】
PlayerTurnManager:
  - launchedCharacterIDs.Add(characterID)
  - remainingLaunches--
  
【检查回合完成条件】
if (remainingLaunches == 0):
  - 发布 OnPlayerTurnComplete 事件
  - PlayerPhaseController 响应
  - 切换到 PhaseEnd
  - 触发 OnPlayerPhaseComplete
  - GameFlowController 切换到敌人回合
```

---

#### 阶段6：回合重置（下一回合开始）
```
【下一玩家回合开始】
PlayerTurnManager:
  - 重置 remainingLaunches = 2
  - 清空 launchedCharacterIDs
  - 发布 OnTurnReset 事件
  
所有角色的 PlayerStateMachine:
  - Completed → Idle（重置状态）
  - 重新可以参与选择
```

---

## 🏗️ 架构设计

### 新增管理器：PlayerTurnManager

**定位**：回合级别的状态管理

**职责**：
- 管理本回合发射次数（remainingLaunches）
- 管理已发射角色列表（launchedCharacterIDs）
- 判断角色是否已完成发射（IsCharacterCompleted）
- 判断角色是否可被选中（IsCharacterAvailable）
- 判断回合是否结束（IsTurnComplete）
- 发布回合事件（OnTurnComplete, OnTurnReset）

**不负责**：
- 不管理选择状态（CharacterSelectionController）
- 不管理球体物理状态（PlayerStateMachine）
- 不管理阶段流程（PlayerPhaseController）

**执行顺序**：`CONTROLLER (0)`

**与其他管理器关系**：
```
PlayerPhaseController (阶段级)
    ↓ 监听 PlayerTurnManager.OnTurnComplete
    ↓ 触发 OnPlayerPhaseComplete
    
PlayerTurnManager (回合级)
    ↓ 管理发射次数和角色可用性
    ↓ 提供查询接口
    
CharacterSelectionController (选择级)
    ↓ 查询：PlayerTurnManager.IsCharacterAvailable(characterID)
    ↓ 根据结果决定能否选中
```

---

### 修改现有管理器

#### PlayerStateMachine 改造

**状态扩展**：
- 新增 `Selected` 状态
- 新增 `Completed` 状态

**状态转换逻辑**：
```
Idle → Selected（收到 OnCharacterSelected）
Selected → Charging（收到 OnCharacterChargingStarted，且滚轮≥门槛）
Selected → Idle（收到 OnCharacterDeselected 或 OnTurnReset）
Charging → Moving（收到 OnCharacterLaunched）
Charging → Idle（收到 OnCharacterDeselected，蓄力归0）
Moving → MovingEnd（球停止）
MovingEnd → Completed（处理完成）
Completed → Idle（收到 OnTurnReset）
```

**新增事件订阅**：
- `OnCharacterSelected(characterID)` - 切换到 Selected
- `OnCharacterDeselected(characterID)` - 从 Charging/Selected 切回 Idle
- `OnTurnReset()` - 从 Completed 切回 Idle

**移除的职责**：
- 不再触发 `OnPlayingComplete`（这是回合级事件，不是单个球的事件）

---

#### CharacterSelectionController 改造

**新增职责**：
- 查询 PlayerTurnManager.IsCharacterAvailable(characterID)
- 只有 Available 的角色才能被选中

**修改逻辑**：
```
SelectCharacter(characterID):
  1. 检查 PlayerTurnManager.IsCharacterAvailable(characterID)
  2. 如果不 Available → 拒绝选择，显示提示
  3. 如果 Available → 执行选择逻辑
```

**保持的职责**：
- 管理当前选中的角色ID
- 发布选择/取消选择事件
- 不管理回合发射次数（这是 PlayerTurnManager 的职责）

---

#### PlayerPhaseController 改造

**定位变更**：
- 保持轻量级流程编排器
- 对称 EnemyPhaseController 的设计
- 委托 PlayerTurnManager 处理回合逻辑

**移除的职责**：
- 不再监听 PlayerStateMachine.OnPlayingComplete（因为有多个球）
- 不统计发射次数（委托给 PlayerTurnManager）

**新增职责**：
- 启动时调用 PlayerTurnManager.StartTurn()
- 监听 PlayerTurnManager.OnTurnComplete
- 收到事件后切换到 PhaseEnd

**改造后的流程**：
```
StartPlayerPhase():
  → PhaseStart
  → Playing
  → PlayerTurnManager.StartTurn()（委托，对称 EnemyManager.ExecutePhase）
  
OnTurnComplete()（来自 PlayerTurnManager）:
  → PhaseEnd
  → OnPlayerPhaseComplete
```

**与 EnemyPhaseController 的对称性**：
- 都是轻量级流程编排器
- 都委托 Manager 处理具体逻辑
- 都监听 Manager 的完成事件

---

## 🔄 完整事件流

### 玩家回合完整流程

```
【回合开始】
GameFlowController.SwitchToPlayerPhase()
  ↓
PlayerPhaseController.StartPlayerPhase()
  ↓
PlayerTurnManager.ResetTurn()
  - remainingLaunches = 2
  - launchedCharacterIDs.Clear()
  - 发布 OnTurnReset
  ↓
所有 PlayerStateMachine:
  - Completed → Idle
  ↓
PlayerPhaseController.ExecutePlaying()
  - PlayerStateMachine.StartPlaying()
  - 切换到 Playing 阶段

---

【选中操作】
用户点击球A
  ↓
GlobalInputManager.PublishBallClicked(ballA)
  ↓
CharacterSelectionController:
  - 查询 PlayerTurnManager.IsCharacterAvailable(A)
  - 如果 Available → SelectCharacter(A)
  - 发布 OnCharacterSelected(A)
  ↓
PlayerStateMachine_A:
  - Idle → Selected

---

【蓄力操作】
Selected 状态 + 滚轮 ≥ 门槛
  ↓
ChargeController:
  - 启动蓄力
  - 发布 OnCharacterChargingStarted(A)
  ↓
PlayerStateMachine_A:
  - Selected → Charging

---

【切换选中（蓄力 < 门槛）】
Selected 状态 + 点击其他球B
  ↓
CharacterSelectionController:
  - 检查 PlayerTurnManager.IsCharacterAvailable(B)
  - 如果 Available → DeselectCharacter(A)
  - 发布 OnCharacterDeselected(A)
  - SelectCharacter(B)
  - 发布 OnCharacterSelected(B)
  ↓
PlayerStateMachine_A:
  - Selected → Idle
PlayerStateMachine_B:
  - Idle → Selected

---

【发射操作】
Charging 状态 + 左键点击
  ↓
ChargeController:
  - 发布 OnCharacterLaunched(A, direction, force)
  ↓
PlayerStateMachine_A:
  - Charging → Moving
  - 执行发射：playerBehavior.Launch()
  ↓
球A 移动
  ↓
球A 停止
  ↓
PlayerStateMachine_A:
  - Moving → MovingEnd
  - 执行攻击判定
  ↓
PlayerStateMachine_A:
  - MovingEnd → Completed
  - 通知 PlayerTurnManager.CharacterLaunched(A)
  ↓
PlayerTurnManager:
  - launchedCharacterIDs.Add(A)
  - remainingLaunches--
  - 检查：remainingLaunches == 0?
    - 是 → 发布 OnTurnComplete
    - 否 → 继续等待下一个发射

---

【回合结束】
PlayerTurnManager.OnTurnComplete 发布
  ↓
PlayerPhaseController.OnTurnComplete()
  - 切换到 PhaseEnd
  - 触发 OnPlayerPhaseComplete
  ↓
GameFlowController.SwitchToEnemyPhase()
  ↓
切换到敌人回合
```

---

## 📦 关键设计决策

### 1. 为什么需要 PlayerTurnManager？架构对称性分析

#### Enemy 侧架构
```
EnemyPhaseController（阶段控制器 - 轻量级）
  ↓ 管理阶段序列（Attack → Move → Spawn → Telegraph）
  ↓ 委托执行
EnemyManager（实体管理器 - 核心）
  ↓ 管理所有敌人实例
  ↓ 执行具体阶段逻辑
  ↓ 通知阶段完成
Enemy（单个敌人组件）
  └─ 管理单个敌人状态
```

#### Player 侧架构（对称设计）
```
PlayerPhaseController（阶段控制器 - 轻量级）
  ↓ 管理阶段流程（PhaseStart → Playing → PhaseEnd）
  ↓ 委托执行
PlayerTurnManager（回合管理器 - 核心，对称 EnemyManager）
  ↓ 管理所有玩家球实例
  ↓ 统计发射次数、管理已发射列表
  ↓ 通知回合完成
PlayerStateMachine（单个球组件，对称 Enemy）
  └─ 管理单个球的状态转换
```

#### 对称关系表

| 层级 | Enemy 侧 | Player 侧 | 职责 |
|------|---------|----------|------|
| 阶段控制 | EnemyPhaseController | PlayerPhaseController | 管理阶段流程，轻量级 |
| 实体管理 | **EnemyManager** | **PlayerTurnManager** | 管理所有实体，统计进度，核心组件 |
| 单体脚本 | Enemy | PlayerStateMachine | 管理单个实体状态 |

**为什么需要 PlayerTurnManager**：
- ✅ **架构对称性**：对应 EnemyManager 的角色
- ✅ **职责分离**：PhaseController 保持轻量级，只管流程
- ✅ **集中管理**：所有玩家球的回合逻辑集中在一个管理器
- ✅ **查询接口**：提供统一的查询接口（IsCharacterCompleted等）

---

### 2. 为什么需要 Selected 状态？

**问题**：
- 之前"选中"只是变量，不是状态
- 无法区分"被选中但未蓄力"和"正在蓄力"
- 输入逻辑混乱（点击时不知道应该选中还是发射）

**解决**：
- 状态机明确表达角色的完整生命周期
- Selected 状态：只等待蓄力输入
- Charging 状态：蓄力中，点击就是发射

---

### 3. 为什么需要 Completed 状态？

**问题**：
- 发射完成的角色应该不能再被选中
- 但如果不标记状态，无法区分"可以选"和"已完成"

**解决**：
- Completed 状态明确标记"已完成"
- CharacterSelectionController 查询时直接检查状态
- 下一回合开始时统一重置

---

### 4. 事件驱动 vs 直接调用

**设计原则**：
- PlayerTurnManager **不直接调用** PlayerStateMachine
- 通过事件驱动：OnTurnReset → 所有球响应 → Idle
- CharacterSelectionController **查询** PlayerTurnManager，但不直接修改

**优势**：
- 完全解耦
- 易于测试
- 符合事件驱动架构

---

## 🚀 实施步骤

### 步骤1：创建 PlayerTurnManager

**创建新文件**：
- `Assets/Scripts/Player/PlayerTurnManager.cs`

**核心功能**：
- 管理 remainingLaunches 和 launchedCharacterIDs
- 提供查询接口：IsCharacterAvailable, IsCharacterCompleted
- 订阅 OnCharacterLaunched 事件
- 发布 OnTurnComplete 和 OnTurnReset 事件

**Unity配置**：
- 场景中添加 PlayerTurnManager GameObject
- 执行顺序：CONTROLLER (0)

---

### 步骤2：改造 PlayerStateMachine

**状态扩展**：
- 添加 Selected 和 Completed 到枚举
- 实现状态转换逻辑

**事件订阅扩展**：
- OnCharacterSelected → Selected
- OnCharacterDeselected → Idle（如果在 Charging/Selected）
- OnTurnReset → Idle（如果在 Completed）

**移除职责**：
- 不再触发 OnPlayingComplete

---

### 步骤3：改造 CharacterSelectionController

**新增查询逻辑**：
- SelectCharacter 前查询 PlayerTurnManager.IsCharacterAvailable
- 如果不可用，拒绝选择

**保持原有职责**：
- 管理选中状态
- 发布选择事件

---

### 步骤4：改造 PlayerPhaseController

**移除旧的订阅**：
- 不再监听 PlayerStateMachine.OnPlayingComplete

**新增订阅**：
- 监听 PlayerTurnManager.OnTurnComplete
- 收到事件后切换到 PhaseEnd

**新增调用**：
- StartPlayerPhase 时调用 PlayerTurnManager.ResetTurn()

---

### 步骤5：改造 ChargeController

**取消选中时重置蓄力**：
- HandleCharacterDeselected 时调用 chargeSystem.ResetCharging()
- 确保蓄力归0

---

### 步骤6：集成测试

**测试场景**：
1. 选中 → 蓄力 → 发射 → 确认进入 Completed
2. 选中 → 切换选中 → 确认旧角色切回 Idle
3. 发射2个球 → 确认回合结束
4. 下一回合 → 确认 Completed 角色重置为 Idle
5. 尝试选中已完成的角色 → 确认被拒绝

---

## ✅ 预期效果

### 状态管理清晰

- ✅ 所有状态都在 PlayerStateMachine 中
- ✅ 状态转换逻辑明确
- ✅ 不会出现状态冲突

### 回合管理正确

- ✅ 正确统计发射次数
- ✅ 正确判断回合结束
- ✅ 正确重置下一回合

### 角色选择限制

- ✅ 已完成角色不能被选中
- ✅ 切换选中时状态正确重置
- ✅ 输入逻辑清晰（Selected 时只能蓄力，Charging 时只能发射或取消）

---

## 📝 注意事项

### Unity配置

**场景中需要添加**：
- PlayerTurnManager（新 GameObject）

**执行顺序**：
- PlayerTurnManager：CONTROLLER (0)
- PlayerStateMachine：COMPONENT (100)
- CharacterSelectionController：CONTROLLER (0)

### 事件订阅顺序

**确保订阅时机**：
- PlayerTurnManager 在 Start() 中订阅 OnCharacterLaunched
- PlayerStateMachine 在 Start() 中订阅所有事件
- PlayerPhaseController 在 Start() 中订阅 OnTurnComplete

### 回合重置时机

**重置触发点**：
- PlayerPhaseController.StartPlayerPhase() 时调用 ResetTurn()
- 确保在状态重置之前调用

---

## 📊 架构验证

### 职责分离验证

| 管理器 | 管理范围 | 是否职责清晰？ |
|--------|----------|---------------|
| PlayerPhaseController | 阶段流程 | ✅ 是 |
| PlayerTurnManager | 回合逻辑 | ✅ 是 |
| CharacterSelectionController | 选择操作 | ✅ 是 |
| PlayerStateMachine | 角色状态 | ✅ 是 |
| ChargeController | 蓄力协调 | ✅ 是 |

### 解耦验证

- ✅ PlayerTurnManager 不直接调用其他组件
- ✅ 通过事件通信
- ✅ CharacterSelectionController 只查询，不修改

### 扩展性验证

- ✅ 如果需要改成"发射3个球"，只需修改 remainingLaunches
- ✅ 如果需要限制"每个球只能发射一次"，在 PlayerTurnManager 中检查
- ✅ 如果增加新的回合规则，在 PlayerTurnManager 中扩展

---

## 🎯 总结

### 核心改变

1. **新增 PlayerTurnManager**：专门管理回合发射次数和角色可用性
2. **扩展 PlayerStateMachine 状态**：Selected 和 Completed
3. **明确状态转换逻辑**：每个状态的输入和输出都清晰
4. **正确的回合完成判定**：统计发射次数，不是监听单个球

### 架构优势

- ✅ 职责单一，每个管理器只做一件事
- ✅ 命名清晰，PlayerTurnManager 明确表达回合管理
- ✅ 易于维护，回合逻辑集中管理
- ✅ 易于扩展，新增回合规则只需修改 PlayerTurnManager

### 符合设计原则

- ✅ 单一职责原则
- ✅ 事件驱动架构
- ✅ 依赖倒置原则
- ✅ 开闭原则（易于扩展）

