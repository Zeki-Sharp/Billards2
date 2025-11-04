# 回合完成逻辑分析

> **创建日期**：2025-11-04  
> **问题类型**：回合结束条件设计  
> **严重程度**：⭐⭐⭐（高）

---

## 📋 问题描述

### 用户提出的核心问题
1. **发射次数**：球发射瞬间计数 ✅（已修复）
2. **回合完成**：不是"已发射的球停止"，而应该是**"所有玩家球停止"**
3. **原因**：玩家球可能撞到其他球，导致未发射的球也开始运动

---

## 🔍 当前逻辑分析

### 当前的回合完成条件（不完整）

```csharp
回合完成条件：
  completedCount >= launchedCount  ❌ 只检查已发射的球
```

### 问题场景

#### 场景1：撞击到未发射的玩家球
```
队伍：球1、球2、球3
配置：launchesPerTurn = 2

时间线：
  T0: 发射球1
      → launchedCount = 1
      → 球1 Flying
  
  T1: 发射球2
      → launchedCount = 2, remainingLaunches = 0
      → 球2 Flying
  
  T2: 球1 撞击到球3（未发射的球）
      → 球3 开始运动！ ⚠️
      → 球1 停止
          → OnCharacterCompleted("ball_1")
          → completedCount = 1
  
  T3: 球2 停止
      → OnCharacterCompleted("ball_2")
      → completedCount = 2
      → 检查: completedCount >= launchedCount (2 >= 2)
      → 回合结束！❌ 错误
      
  ⚠️ 问题：球3 还在运动中！
      → 切换到敌人回合
      → 球3 还在滚动...
      → 逻辑混乱
```

#### 场景2：撞击到敌人
```
时间线：
  T0: 发射球1
  T1: 发射球2, remainingLaunches = 0
  T2: 球1 撞击到敌人
      → 敌人开始运动！⚠️
      → 球1、球2 都停止
      → completedCount = 2 >= launchedCount = 2
      → 回合结束！❌
      
  ⚠️ 问题：敌人还在运动中！
      → 切换到敌人回合
      → 但敌人是被动运动，不是主动行为
      → 应该等敌人停止？还是不管？
```

---

## 🎯 应该的回合完成条件

### 核心原则
**发射次数用尽后，等待"场上所有相关实体"停止运动**

### 需要区分的实体类型

#### 1. 玩家球（必须等待）⭐⭐⭐
**来源**：`TeamData.characters[].ballInstance`  
**数量**：固定3个  
**检查**：是否运动（通过 `BallPhysics.IsMoving` 或监听 `OnBallStopped`）

**逻辑**：
- 发射次数用尽后，检查所有3个玩家球
- 只有**所有玩家球都停止**，才能回合结束
- 包括：主动发射的球 + 被撞击的球

---

#### 2. 敌人（需要设计决策）⭐⭐

**两种情况**：

**A. 敌人主动行为运动**
- 时机：敌人回合
- 处理：玩家回合不考虑

**B. 敌人被玩家撞击运动**
- 时机：玩家回合中
- 问题：应该等敌人停止吗？

**设计决策选项**：

**选项1：玩家回合等待敌人停止** ⭐⭐
```
优势：
  ✅ 物理一致性（所有运动都结束）
  ✅ 避免视觉混乱（敌人还在动就切回合）
  
劣势：
  ❌ 玩家可能等待很久（敌人滚很远）
  ❌ 需要区分敌人的"主动运动"和"被动运动"
```

**选项2：玩家回合不等待敌人** ⭐⭐⭐ （推荐）
```
优势：
  ✅ 简单直接
  ✅ 玩家不需要等待敌人
  ✅ 敌人运动延续到敌人回合也合理
  
劣势：
  ⚠️ 敌人可能在回合切换时还在运动
  
解决方案：
  → 敌人回合开始时，先等待所有敌人停止
  → 或：敌人回合中自然等待敌人停止
```

**推荐**：选项2（不等待敌人）

---

## 🔄 完整的回合结束流程（修正版）

### 方案：等待所有玩家球停止

```
回合开始
  remainingLaunches = 2
  launchedCount = 0
  ↓
发射球1
  → OnCharacterLaunched
      launchedCount = 1
      remainingLaunches = 1
  ↓
发射球2
  → OnCharacterLaunched
      launchedCount = 2
      remainingLaunches = 0  ✅ 发射次数用尽
      → 开始监控：等待所有玩家球停止
  ↓
【监控所有玩家球】
  订阅 OnBallStopped 事件
  
  球1 撞击球3（未发射）
    → 球3 开始运动
  
  球1 停止
    → OnBallStopped(ball_1)
        检查所有玩家球状态:
          球1: Stopped ✅
          球2: Moving
          球3: Moving
        → 不是所有球都停止，继续等待
  
  球2 停止
    → OnBallStopped(ball_2)
        检查所有玩家球状态:
          球1: Stopped ✅
          球2: Stopped ✅
          球3: Moving
        → 不是所有球都停止，继续等待
  
  球3 停止
    → OnBallStopped(ball_3)
        检查所有玩家球状态:
          球1: Stopped ✅
          球2: Stopped ✅
          球3: Stopped ✅
        → ✅✅ 所有玩家球都停止！
        → OnTurnComplete()  ← 回合结束
  ↓
切换到敌人回合
```

---

## 🔧 实施方案

### 修改 PlayerTurnManager

#### 新增字段
```csharp
private bool isWaitingForAllBallsToStop = false;  // 是否在等待所有球停止
private HashSet<GameObject> movingPlayerBalls;     // 当前运动中的玩家球
```

#### 修改逻辑

**1. 发射时启动监控**
```csharp
OnCharacterLaunched(characterID, direction, force)
{
    launchedCount++;
    remainingLaunches--;
    
    // ✅ 发射次数用尽，开始监控所有球
    if (remainingLaunches <= 0)
    {
        isWaitingForAllBallsToStop = true;
        StartMonitoringAllPlayerBalls();  // 订阅 OnBallStopped
    }
}
```

**2. 监听所有球停止事件**
```csharp
OnEnable()
{
    GameEventBus.OnCharacterLaunched += OnCharacterLaunched;
    GameEventBus.OnBallStopped += OnAnyBallStopped;  // 新增
}

OnAnyBallStopped(BallPhysics ball)
{
    // 只在监控期间处理
    if (!isWaitingForAllBallsToStop)
        return;
    
    // 检查是否是玩家球
    if (!IsPlayerBall(ball))
        return;
    
    // 检查所有玩家球是否都停止
    if (AreAllPlayerBallsStopped())
    {
        OnTurnComplete();  // 回合结束
    }
}
```

**3. 检查所有玩家球是否停止**
```csharp
bool AreAllPlayerBallsStopped()
{
    var teamData = GameSession.Instance?.GetTeamData();
    if (teamData == null) return false;
    
    foreach (var character in teamData.characters)
    {
        if (character.ballInstance == null)
            continue;
        
        // 检查球是否在运动
        BallPhysics physics = character.ballInstance.GetComponent<BallPhysics>();
        if (physics != null && physics.IsMoving)
        {
            return false;  // 还有球在动
        }
    }
    
    return true;  // 所有球都停止了
}
```

---

## ⚠️ 关于敌人的设计决策

### 当前建议：不等待敌人（推荐）

**原因**：
1. **简化逻辑**：只关心玩家球
2. **避免长时间等待**：敌人可能滚很远
3. **合理性**：敌人被撞后的运动可以延续到敌人回合

**潜在问题**：
- 敌人在回合切换时还在运动
- 敌人回合开始时，敌人可能不在预期位置

**解决方案**：
- 敌人回合开始时，先等待所有敌人停止
- 或：敌人的预告/攻击等行为会自然等待

### 未来可选：等待敌人（如果需要）

如果未来需要等待敌人，修改方式：
```csharp
AreAllRelevantEntitiesStopped()
{
    // 1. 检查所有玩家球
    if (!AreAllPlayerBallsStopped())
        return false;
    
    // 2. 检查所有敌人（可选）
    if (!AreAllEnemiesStopped())
        return false;
    
    return true;
}
```

---

## 📊 对比表格

| 方案 | 检查对象 | 优势 | 劣势 | 推荐度 |
|------|---------|------|------|--------|
| 当前方案 | 已发射的球 | 简单 | ❌ 遗漏被撞击的球 | ❌ 不推荐 |
| 方案A | 所有玩家球 | ✅ 完整 | 实现稍复杂 | ✅✅✅ 推荐 |
| 方案B | 玩家球+敌人 | ✅ 最完整 | ❌ 等待时间长，逻辑复杂 | ⭐ 可选 |

---

## 🚀 实施建议

### 第一阶段：修复玩家球检测（必须）
1. 订阅 `OnBallStopped` 事件
2. 添加 `AreAllPlayerBallsStopped()` 方法
3. 检查 `TeamData` 中所有3个球的运动状态
4. 当发射次数用尽且所有玩家球停止时，回合结束

### 第二阶段：敌人处理（可选）
**决策A**：暂不处理敌人被撞击
- 玩家回合只等玩家球
- 敌人运动延续到敌人回合
- 敌人回合开始时自然等待

**决策B**：玩家回合等待敌人
- 需要追踪敌人运动状态
- 实现复杂度提升
- 建议先实施方案A

---

## 💡 关键设计问题

### 问题：敌人被撞击后的运动属于哪个回合？

**情况**：
```
玩家回合中：
  球1 撞击敌人A
  → 敌人A 开始滚动
  → 球1、球2 都停止
  → 发射次数用尽
```

**选项1：等待敌人停止后再切换回合**
```
优势：物理一致性
劣势：等待时间不可控
```

**选项2：不等待，敌人运动延续到敌人回合**
```
优势：简单，玩家不等待
劣势：敌人在两个回合间运动
```

**建议**：先实施选项2，观察实际游戏体验后再决定

---

## 📝 需要修改的内容

### PlayerTurnManager.cs 修改点

#### 1. 新增字段
```
- isWaitingForAllBallsToStop（是否在等待所有球停止）
```

#### 2. 订阅事件
```
OnEnable():
  + GameEventBus.OnBallStopped
```

#### 3. 新增方法
```
+ OnAnyBallStopped(BallPhysics ball)
+ AreAllPlayerBallsStopped()
+ IsPlayerBall(BallPhysics ball)
```

#### 4. 修改逻辑
```
OnCharacterLaunched():
  if (remainingLaunches <= 0):
    isWaitingForAllBallsToStop = true  // 启动监控

OnAnyBallStopped():
  if (isWaitingForAllBallsToStop && AreAllPlayerBallsStopped()):
    OnTurnComplete()  // 回合结束
```

---

## ⚠️ 边界情况

### 1. 多个球互相碰撞
**场景**：球1撞球2，球2撞球3，连锁反应  
**处理**：等待所有球都停止 ✅  
**效果**：自然处理，无需特殊逻辑

### 2. 球撞墙反弹
**场景**：球撞墙后反弹继续运动  
**处理**：等待球停止 ✅  
**效果**：自然处理

### 3. 极端情况：球永远不停
**场景**：球卡在两堵墙之间来回弹（理论上）  
**处理**：依赖物理系统的阻尼，最终会停  
**备选**：添加超时机制（如10秒强制停止）

### 4. 敌人被撞飞很远
**场景**：球用很大力撞击敌人，敌人滚很远  
**处理**（方案2）：不等待敌人，切换到敌人回合  
**效果**：敌人运动延续到敌人回合，敌人回合开始时等待所有敌人停止

---

## 🔄 修改后的完整流程

```
回合开始
  remainingLaunches = 2
  isWaitingForAllBallsToStop = false
  ↓
发射球1, 发射球2
  remainingLaunches = 0
  isWaitingForAllBallsToStop = true  ← 开始监控
  ↓
【等待所有玩家球停止】
监听 OnBallStopped 事件
  
每次球停止时：
  → 检查是否是玩家球？
      是 → 检查所有玩家球是否都停止？
          是 → OnTurnComplete()  ← 回合结束
          否 → 继续等待
      否 → 忽略（敌人球）
  ↓
所有玩家球停止
  → OnTurnComplete()
  → 切换到敌人回合
```

---

## 📌 实施优先级

### P0 - 必须修复
- ⭐⭐⭐ 检查所有玩家球（而不是只检查已发射的球）

### P1 - 重要优化
- ⭐⭐ 添加超时保护（防止卡死）
- ⭐⭐ 优化日志输出（显示哪些球还在动）

### P2 - 可选扩展
- ⭐ 等待敌人停止（如果游戏体验需要）
- ⭐ 视觉反馈（显示"等待球停止"）

---

**文档版本**：1.1（实施完成）  
**创建日期**：2025-11-04  
**最后更新**：2025-11-04  
**状态**：✅ 玩家球停止检测已实施  
**设计决策**：只等待玩家球，不等待敌人（简单方案）  
**遗留问题**：敌人被撞击运动已记录到 `Legacy_Issues.md` #4

