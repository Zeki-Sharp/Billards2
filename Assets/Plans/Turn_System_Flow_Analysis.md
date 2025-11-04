# 回合系统流程问题分析

> **创建日期**：2025-11-04  
> **问题类型**：回合切换逻辑缺陷  
> **严重程度**：⭐⭐⭐（高）

---

## 📋 问题描述

### 用户期望的行为
1. **玩家回合**：规定数目的玩家运动后，切换到敌人回合
2. **敌人回合**：不能控制玩家移动，所有敌人行动结束后才切到下一个玩家回合
3. **新玩家回合**：可操控角色数目刷新

### 当前实际行为
- ❌ 敌人回合时玩家仍然可以点击和控制球体
- ❌ 回合切换不明确，玩家和敌人可能同时行动
- ❌ 发射次数刷新时机可能不正确

---

## 🔍 架构分析

### 当前系统组成

#### 1. 顶层流程控制
**GameFlowController** - 管理顶层阶段切换
```
GameFlowState:
  - None（初始）
  - PlayerPhase（玩家回合）
  - EnemyPhase（敌人回合）
  - PlayerPhaseEnd（过渡）
  - EnemyPhaseEnd（过渡）
```

**切换逻辑**：
```
Start → SwitchToPlayerPhase()
PlayerPhaseComplete → SwitchToEnemyPhase()
EnemyPhaseComplete → SwitchToPlayerPhase()
```

#### 2. 玩家阶段管理
**PlayerPhaseController** - 管理玩家回合子阶段
```
PlayerPhase:
  - None → PhaseStart → Playing → PhaseEnd
```

**Playing 阶段**：
- 调用 `PlayerTurnManager.StartTurn()` 重置发射次数
- 发布 `OnPlayerPlayingPhaseStarted` 事件
- 等待 `PlayerTurnManager.OnTurnComplete` 事件

**PhaseEnd 阶段**：
- 发布 `OnPlayerPhaseComplete` 事件
- GameFlowController 收到后切换到敌人阶段

#### 3. 发射次数管理
**PlayerTurnManager** - 管理回合内发射次数
```
配置: launchesPerTurn = 2（每回合需要发射2个球）
状态:
  - remainingLaunches（剩余发射次数）
  - launchedCount（已发射次数）
  - launchedCharacterIDs（已发射角色列表）
```

**流程**：
```
StartTurn():
  - remainingLaunches = launchesPerTurn
  - launchedCount = 0
  - launchedCharacterIDs.Clear()

OnCharacterCompleted(characterID):
  - launchedCharacterIDs.Add(characterID)
  - launchedCount++
  - remainingLaunches--
  - 如果 remainingLaunches <= 0:
      → 发布 OnTurnComplete 事件
```

#### 4. 角色选择管理
**CharacterSelectionController** - 管理选中状态

**限制检查**：
```
HandleBallClicked():
  ✅ 检查角色是否已完成发射
  ✅ 检查角色是否死亡
  ✅ 检查是否可以切换选择（蓄力门槛）
  ❌ 没有检查当前是否是玩家回合！
```

#### 5. 输入检测
**GlobalInputManager** - 检测原始输入

**权限检查**：
```
Update():
  ✅ 检查游戏是否暂停
  ❌ 没有检查当前游戏阶段（PlayerPhase/EnemyPhase）
  → HandleInput()
      → PublishBallClicked()
```

---

## ❌ 核心问题

### 问题1：敌人回合时玩家仍可选择球体 ⭐⭐⭐

**位置**：`CharacterSelectionController.HandleBallClicked()`

**问题**：
```csharp
void HandleBallClicked(GameObject ballObject)
{
    // 检查是否已完成发射 ✅
    // 检查是否死亡 ✅
    // 检查是否可切换选择 ✅
    // ❌ 没有检查是否在玩家回合
    
    SelectCharacter(characterID, ballObject);  // 即使在敌人回合也会执行！
}
```

**后果**：
- 敌人回合时点击球体仍然会触发选择
- 可能干扰敌人行为
- 回合界限不清晰

---

### 问题2：GlobalInputManager 不检查游戏阶段 ⭐⭐

**位置**：`GlobalInputManager.Update()`

**问题**：
```csharp
void Update()
{
    // 只检查游戏暂停 ✅
    if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
    {
        return;
    }
    
    // ❌ 没有检查是否在玩家回合
    HandleInput();  // 敌人回合时仍然检测输入
}
```

**后果**：
- 敌人回合时仍然发布输入事件
- 虽然后续可能被拦截，但不够优雅
- 浪费性能（不必要的射线检测）

---

### 问题3：输入权限检查不统一 ⭐

**现状**：
- `PlayerInputHandler` 使用 `PlayerInputPermissionManager.CanProcessInputInCurrentPhase()` ✅
- `GlobalInputManager` 不使用权限检查 ❌
- `CharacterSelectionController` 不使用权限检查 ❌

**问题**：
- 权限检查逻辑分散
- 容易遗漏检查点
- 维护困难

---

## 📊 当前流程图（有问题）

```
玩家回合开始
  ↓
PlayerPhaseController.StartPlayerPhase()
  → PhaseStart → Playing
  → PlayerTurnManager.StartTurn()
      → remainingLaunches = 2
  ↓
玩家操作（Playing 阶段）
  ├─ GlobalInputManager.HandleLeftClick()  ← 持续检测输入
  ├─ PublishBallClicked(球1)
  ├─ CharacterSelectionController.HandleBallClicked()  ← 选中球1
  ├─ 球1 发射、移动、停止
  ├─ PlayerTurnManager.OnCharacterCompleted()
  │   → remainingLaunches = 1
  ├─ PublishBallClicked(球2)
  ├─ 球2 发射、移动、停止
  ├─ PlayerTurnManager.OnCharacterCompleted()
  │   → remainingLaunches = 0
  │   → 发布 OnTurnComplete
  ↓
PlayerPhaseController.OnTurnComplete()
  → PhaseEnd
  → 发布 OnPlayerPhaseComplete
  ↓
GameFlowController.SwitchToEnemyPhase()
  → GameFlowState = EnemyPhase
  → 发布 GameFlowStateChanged(EnemyPhase)
  ↓
敌人回合开始
  ├─ EnemyPhaseController.StartEnemyPhase()
  ├─ 执行敌人行为（Attack → Move → Spawn → Telegraph）
  │
  ⚠️ 问题：此时 GlobalInputManager 仍在 Update() 中检测输入！
  │
  ├─ 如果玩家点击球体...
  │   ├─ GlobalInputManager.HandleLeftClick()  ❌ 仍然检测
  │   ├─ PublishBallClicked()  ❌ 仍然发布事件
  │   └─ CharacterSelectionController.HandleBallClicked()  ❌ 仍然选中球体
  │
  └─ 所有敌人阶段完成
      → 发布 OnEnemyPhaseComplete
  ↓
GameFlowController.SwitchToPlayerPhase()
  → 新的玩家回合
  → PlayerTurnManager.StartTurn()  ← 重置发射次数
```

---

## ✅ 应该的流程图

```
玩家回合
  ├─ GameFlowState = PlayerPhase
  ├─ GlobalInputManager 检测输入 ✅
  ├─ CharacterSelectionController 允许选择 ✅
  ├─ 玩家可操作球体
  └─ 发射次数用尽 → OnTurnComplete
  ↓
切换到敌人回合
  ├─ GameFlowState = EnemyPhase
  ├─ 发布 GameFlowStateChanged(PlayerPhaseEnd)
  ├─ 发布 GameFlowStateChanged(EnemyPhase)
  │
  ├─ GlobalInputManager.Update():
  │   └─ 检查 GameFlowController.IsPlayerPhase  ❌ → 返回
  │       → 不处理任何输入 ✅
  │
  ├─ CharacterSelectionController.HandleBallClicked():
  │   └─ 检查 GameFlowController.IsPlayerPhase  ❌ → 返回
  │       → 不允许选择 ✅
  │
  └─ 只有敌人执行行为
  ↓
敌人回合结束
  └─ 发布 OnEnemyPhaseComplete
  ↓
切换回玩家回合
  └─ PlayerTurnManager.StartTurn()
      → 发射次数刷新 ✅
```

---

## 🔧 修复方案（架构优化版 + 发射次数检查）

### 方案概述
**统一使用 `PlayerInputPermissionManager` 进行权限检查**，避免在业务脚本中散落合法性检查逻辑。

### 核心思路
1. `PlayerInputPermissionManager` 提供两个权限检查方法：
   - `CanProcessInputInCurrentPhase()` - 通用输入权限（检查阶段）
   - `CanSelectCharacter()` - 角色选择权限（检查阶段 + 发射次数）⭐
2. `GlobalInputManager` 使用通用输入权限
3. `CharacterSelectionController` 使用角色选择权限 ⭐

### 🎯 架构决策：保持输入检测与选择逻辑分离
**决定**：保持 `GlobalInputManager` 和 `CharacterSelectionController` 分开 ✅

**原因**：
1. ✅ 职责分离：输入检测 vs 业务逻辑
2. ✅ 解耦设计：其他系统也可订阅 `OnBallClicked`
3. ✅ 符合事件驱动架构
4. ✅ 易于维护和扩展

**权限检查分配**：
- `GlobalInputManager`：通用权限（阶段、暂停）
- `CharacterSelectionController`：选择权限（阶段 + 发射次数 + 角色状态）

---

### 修改点1：PlayerInputPermissionManager 改为全局单例

**当前问题**：
- `PlayerInputPermissionManager` 是挂在单个球体上的 `MonoBehaviour`
- `GlobalInputManager` 和 `CharacterSelectionController` 都是场景级单例，无法获取球体上的组件

**解决方案**：
- 将 `PlayerInputPermissionManager` 改为**场景级单例**
- 放在与 `GlobalInputManager` 相同的位置（场景根对象或管理器对象）
- 提供全局访问接口 `PlayerInputPermissionManager.Instance`

**改动内容**：
```
- 添加单例模式（Instance 属性）
- Awake() 中初始化单例
- 保留 CanProcessInputInCurrentPhase() 方法
- 保留 GameFlowController 引用
```

---

### 修改点2：GlobalInputManager 接入权限管理器

**位置**：`GlobalInputManager.Update()`

**改动**：
```
Update()
{
    // 检查游戏暂停 ✅
    if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        return;
    
    // ✅ 使用统一权限管理器检查阶段
    if (PlayerInputPermissionManager.Instance == null ||
        !PlayerInputPermissionManager.Instance.CanProcessInputInCurrentPhase())
        return;
    
    // 处理输入
    HandleInput();
}
```

**效果**：
- 敌人回合时不检测输入
- 与 PlayerInputHandler 使用相同的权限逻辑 ✅
- 架构统一

---

### 修改点3：CharacterSelectionController 接入权限管理器

**位置**：`CharacterSelectionController.HandleBallClicked()`

**改动**：
```
HandleBallClicked(GameObject ballObject)
{
    if (ballObject == null) return;
    
    // ✅ 使用统一权限管理器检查阶段
    if (PlayerInputPermissionManager.Instance == null ||
        !PlayerInputPermissionManager.Instance.CanProcessInputInCurrentPhase())
    {
        if (showDebugInfo)
            Debug.Log("CharacterSelectionController: 当前不在玩家回合，忽略点击");
        return;
    }
    
    // 原有检查逻辑...
    // 检查角色是否完成 ✅
    // 检查角色是否死亡 ✅
    // 检查是否可切换 ✅
}
```

**效果**：
- 二次防御
- 统一权限管理
- 日志清晰

---

## 🏗️ 架构优势

### 修改前（分散检查）
```
GlobalInputManager:
  ❌ 没有权限检查

CharacterSelectionController:
  ❌ 没有权限检查

PlayerInputHandler:（已废弃）
  ✅ 使用 PermissionManager
```

### 修改后（统一管理）
```
PlayerInputPermissionManager（单例）
  ├─ CanProcessInputInCurrentPhase()
  │   └─ 检查 GameFlowController.IsPlayerPhase
  │
  ├─ GlobalInputManager 调用
  ├─ CharacterSelectionController 调用
  └─ （未来所有需要权限的组件都调用）
```

**优势**：
1. ✅ 权限逻辑集中在一处，易于维护
2. ✅ 业务脚本只调用接口，不包含检查细节
3. ✅ 未来扩展方便（如添加暂停、技能选择等状态检查）
4. ✅ 单一职责原则，架构清晰

---

## 🔄 修复后的完整流程

```
=== 玩家回合 ===
GameFlowState = PlayerPhase
  ↓
PlayerPhaseController.StartPlayerPhase()
  → PlayerTurnManager.StartTurn()
      → remainingLaunches = 2
  ↓
【玩家可操作阶段】
  ├─ GlobalInputManager.Update():
  │   ├─ 检查暂停 ✅
  │   ├─ 检查是否玩家回合 ✅ (PlayerPhase)
  │   └─ HandleInput() ✅
  │
  ├─ CharacterSelectionController.HandleBallClicked():
  │   ├─ 检查是否玩家回合 ✅ (PlayerPhase)
  │   ├─ 检查角色是否完成 ✅
  │   ├─ 检查角色是否死亡 ✅
  │   └─ SelectCharacter() ✅
  │
  ├─ 玩家选择、发射球1
  ├─ 球1 Completed → remainingLaunches = 1
  ├─ 玩家选择、发射球2
  └─ 球2 Completed → remainingLaunches = 0
      → OnTurnComplete 事件
  ↓
切换到敌人回合
  ├─ PlayerPhaseController.OnTurnComplete()
  │   → PhaseEnd
  │   → OnPlayerPhaseComplete 事件
  │
  └─ GameFlowController.SwitchToEnemyPhase()
      → GameFlowState = EnemyPhase
      → 发布 GameFlowStateChanged(EnemyPhase)
  ↓
=== 敌人回合 ===
GameFlowState = EnemyPhase
  ↓
【玩家输入被禁用】
  ├─ GlobalInputManager.Update():
  │   ├─ 检查暂停 ✅
  │   ├─ 检查是否玩家回合 ❌ (EnemyPhase)
  │   └─ return  ← 不处理任何输入 ✅
  │
  ├─ CharacterSelectionController.HandleBallClicked():
  │   ├─ 检查是否玩家回合 ❌ (EnemyPhase)
  │   └─ return  ← 不允许选择 ✅
  │
  └─ 即使玩家点击，也不会有任何反应 ✅
  ↓
【敌人行动阶段】
  ├─ EnemyPhaseController.StartEnemyPhase()
  ├─ 执行敌人阶段序列：
  │   ├─ Attack（攻击）
  │   ├─ Move（移动）
  │   ├─ Spawn（生成）
  │   └─ Telegraph（预告）
  │
  └─ 所有阶段完成
      → OnEnemyPhaseComplete 事件
  ↓
切换回玩家回合
  ├─ GameFlowController.SwitchToPlayerPhase()
  │   → GameFlowState = PlayerPhase
  │   → 发布 GameFlowStateChanged(PlayerPhase)
  │
  └─ PlayerPhaseController.StartPlayerPhase()
      → PlayerTurnManager.StartTurn()
          → remainingLaunches = 2  ← 发射次数刷新 ✅
  ↓
=== 新的玩家回合 ===
（循环）
```

---

## 📝 需要修改的文件

### 1. PlayerInputPermissionManager.cs ⭐ 核心
**改动类型**：改为场景级单例  
**主要改动**：
- 添加单例模式（Awake, Instance 属性）
- 移除组件级引用（不再依赖球体）
- 保持权限检查方法不变

**改动行数**：约 15-20 行

---

### 2. GlobalInputManager.cs
**改动类型**：接入权限管理器  
**主要改动**：
- `Update()` 方法中调用 `PermissionManager.CanProcessInputInCurrentPhase()`
- 添加权限管理器引用（可选，直接用 Instance 也行）

**改动行数**：约 3-5 行

---

### 3. CharacterSelectionController.cs
**改动类型**：接入权限管理器  
**主要改动**：
- `HandleBallClicked()` 方法开头调用 `PermissionManager.CanProcessInputInCurrentPhase()`
- 添加简洁的日志

**改动行数**：约 5-8 行

---

### 4. PlayerInputHandler.cs（可选清理）
**改动类型**：标记为废弃或删除  
**说明**：
- 该组件已被 `GlobalInputManager` 替代
- 场景中不再使用
- 可以保留作为参考，或直接删除

---

## ⚠️ 边界情况

### 1. 阶段切换瞬间的输入
**场景**：玩家回合刚结束，输入事件还在队列中  
**处理**：阶段检查会自然拦截这些输入  
**效果**：安全，不会误操作

### 2. 敌人回合点击球体
**场景**：玩家在敌人回合点击球体  
**处理**：
- GlobalInputManager 不发布事件（源头拦截）
- 即使发布，CharacterSelectionController 也会拒绝（二次防御）  
**效果**：双重保护，完全禁止

### 3. 回合切换时的发射次数
**场景**：新玩家回合开始  
**处理**：
- `PlayerTurnManager.StartTurn()` 重置 `remainingLaunches`
- 清空 `launchedCharacterIDs`  
**效果**：正确，已有逻辑处理

---

## ✅ 验收标准

### 功能验收
- [ ] 玩家回合时可以点击和控制球体
- [ ] 敌人回合时点击球体无任何反应
- [ ] 发射次数用尽后自动切换到敌人回合
- [ ] 敌人回合结束后切回玩家回合
- [ ] 新玩家回合时发射次数正确刷新为2

### 日志验收
- [ ] 敌人回合时点击球体不产生选择相关日志
- [ ] 回合切换日志清晰明确
- [ ] 发射次数变化日志正确

### 用户体验验收
- [ ] 敌人回合时球体不响应点击（视觉上无反馈）
- [ ] 回合界限清晰，操作直观
- [ ] 无混乱或卡顿感

---

## 📌 相关组件依赖关系

```
GameFlowController（顶层）
  ├─ 管理 GameFlowState（PlayerPhase/EnemyPhase）
  ├─ 提供查询接口：IsPlayerPhase, IsEnemyPhase
  │
  ├─ PlayerPhaseController（玩家阶段）
  │   └─ PlayerTurnManager（发射次数）
  │       └─ 监听 OnCharacterCompleted
  │           → 计数、判断回合结束
  │
  └─ EnemyPhaseController（敌人阶段）
      └─ EnemyManager（敌人管理）
          → 执行敌人行为
```

**输入流**：
```
GlobalInputManager（原始输入）
  ├─ 应该检查：IsPlayerPhase ❌ 缺失
  └─ PublishBallClicked()
      ↓
CharacterSelectionController（选择管理）
  ├─ 应该检查：IsPlayerPhase ❌ 缺失
  └─ SelectCharacter()
```

---

## 🚀 实施步骤

### ✅ 步骤1：PlayerInputPermissionManager 改为全局单例（已完成）
- ✅ 继承 `SingletonManager<PlayerInputPermissionManager>` 基类
- ✅ 添加 `DefaultExecutionOrder(CONTROLLER)`
- ✅ 配置 `PersistAcrossScenes = false`（场景级单例）
- ✅ 新增 `CanSelectCharacter()` 方法（阶段 + 发射次数）⭐
- ✅ 保留 `CanProcessInputInCurrentPhase()` 方法（通用输入）
- ✅ 标记废弃方法为 `[Obsolete]`

**实施详情**：
- 文件：`PlayerInputPermissionManager.cs`
- 继承统一单例基类，架构规范
- 添加了 `CanSelectCharacter()` 方法（L86-124）
- 检查逻辑：玩家回合 + 剩余发射次数 > 0
- 添加了 `GetCurrentPhaseInfo()` 调试接口

---

### ✅ 步骤2：GlobalInputManager 接入权限管理器（已完成）
- ✅ `Update()` 中调用 `PermissionManager.CanProcessInputInCurrentPhase()`
- ✅ 敌人回合时不处理任何输入

**实施详情**：
- 文件：`GlobalInputManager.cs`
- 位置：`Update()` L118-135
- 在暂停检查之后、输入处理之前添加阶段检查
- 源头拦截，性能优化

---

### ✅ 步骤3：CharacterSelectionController 接入权限管理器（已完成）
- ✅ `HandleBallClicked()` 开头调用 `CanSelectCharacter()`
- ✅ 综合检查：阶段 + 发射次数
- ✅ 修复发射次数用尽后仍可选择的问题

**实施详情**：
- 文件：`CharacterSelectionController.cs`
- 位置：`HandleBallClicked()` L92-107
- 调用 `CanSelectCharacter()` 而不是 `CanProcessInputInCurrentPhase()`
- 同时检查玩家回合和剩余发射次数
- 发射次数为0时立即禁止选择，避免竞态条件

---

### 步骤4：测试验证（待执行）
**测试清单**：
- [ ] 玩家回合：可以点击和控制球体
- [ ] 敌人回合：点击球体无任何反应
- [ ] 发射次数用尽后自动切换到敌人回合
- [ ] 敌人行动完成后切回玩家回合
- [ ] 新玩家回合时发射次数正确刷新为2
- [ ] 日志显示"当前不在玩家回合，忽略点击"

---

## 🎯 预期效果

### 架构层面
- ✅ 权限检查逻辑集中管理
- ✅ 业务脚本解耦，只调用接口
- ✅ 未来扩展方便（如添加其他状态检查）

### 功能层面
- ✅ 敌人回合时完全禁止玩家输入
- ✅ 回合界限清晰明确
- ✅ 发射次数正确刷新

### 代码质量
- ✅ 符合单一职责原则
- ✅ 减少重复代码
- ✅ 提高可维护性

---

**文档版本**：1.3（完整修复版）  
**创建日期**：2025-11-04  
**最后更新**：2025-11-04  
**当前进度**：步骤1-3已完成 (3/4) - 75%  
**状态**：✅ 代码实施完成（含发射次数检查），待测试验证  
**架构决策**：保持输入检测与选择逻辑分离 ✅  
**下一步**：执行步骤4（测试验证）

**关键修复**：
- ✅ 添加了发射次数检查，避免回合结束过程中仍可选择
- ✅ 使用 `CanSelectCharacter()` 综合检查阶段和发射次数

