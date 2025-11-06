# 收集者角色设计方案

> **创建时间**：2025年11月  
> **状态**：设计阶段  
> **优先级**：⭐⭐ 中优先级

---

## 📋 需求描述

### 角色定位
**收集者（Collector）** - 基于掉落物机制的战术型角色

### 核心机制

**被动效果：掉落物补充**
- 每回合开始时，检查场上掉落物数量
- 如果少于3个，自动补充到3个
- 示例：上回合3个，吃了1个剩2个 → 本回合生成1个 → 保持3个

**主动效果：收集打击**
- 触发时机：玩家回合结束时
- 伤害计算：本回合拾取的掉落物数量 × 伤害系数
- 目标选择：距离最近的敌人
- 副作用：清空本回合拾取计数

---

## 🎯 技术设计

### 需要的系统组件

**1. 掉落物追踪系统**
- **当前状态**：已有 `ItemPickup`，但没有追踪"本回合拾取数量"
- **需要新增**：
  - 全局或角色级别的拾取计数器
  - 回合结束时重置计数

**2. 掉落物补充系统**
- **需要新增**：
  - 监听回合开始事件
  - 检测场上掉落物数量
  - 自动生成不足的掉落物

**3. 收集打击技能**
- **需要新增**：
  - 监听回合结束事件
  - 读取本回合拾取数量
  - 找到最近的敌人
  - 造成伤害

---

## 📐 详细设计

### 系统1：掉落物拾取追踪

#### 方案A：在 GameSession 中追踪（推荐）⭐

```csharp
// GameSession 或新建 DropItemTracker Manager
public class DropItemTracker : SingletonManager
{
    // 本回合每个角色拾取的掉落物数量
    private Dictionary<string, int> currentTurnPickups = new Dictionary<string, int>();
    
    // 监听拾取事件
    void OnItemPickedUp(string characterID, ItemConfig item)
    {
        if (!currentTurnPickups.ContainsKey(characterID))
        {
            currentTurnPickups[characterID] = 0;
        }
        
        currentTurnPickups[characterID]++;
    }
    
    // 监听回合结束，重置计数
    void OnPlayerTurnEnded()
    {
        currentTurnPickups.Clear();
    }
    
    // 获取角色本回合拾取数量
    public int GetCurrentTurnPickups(string characterID)
    {
        return currentTurnPickups.TryGetValue(characterID, out int count) ? count : 0;
    }
}
```

**优点：**
- ✅ 集中管理，易于访问
- ✅ 支持所有角色追踪
- ✅ 自动重置

**缺点：**
- ⚠️ 需要新建 Manager 或扩展现有的
- ⚠️ 需要发布拾取事件

#### 方案B：在 CharacterInstance 中追踪

```csharp
// CharacterInstance.cs
public class CharacterInstance
{
    // ... 现有字段 ...
    
    // 本回合拾取的掉落物数量
    [System.NonSerialized]
    public int currentTurnPickupCount = 0;
}

// ItemPickup.cs 拾取时
var character = teamData.GetCharacter(pickerCharacterID);
if (character != null)
{
    character.currentTurnPickupCount++;
}

// 回合结束时重置
character.currentTurnPickupCount = 0;
```

**优点：**
- ✅ 数据就近存储
- ✅ 不需要新 Manager

**缺点：**
- ⚠️ 需要修改 CharacterInstance
- ⚠️ 需要在多处调用重置

---

### 系统2：掉落物自动补充

#### 实现方式：角色被动技能

```csharp
// DropItemReplenishEffect.cs (新建)
public class DropItemReplenishEffect : IEffect
{
    public int targetCount = 3;  // 目标数量
    public ItemConfig itemToSpawn;  // 要生成的掉落物类型
    
    // 在玩家回合开始时触发
    public bool ExecuteEffect(SkillArgs args)
    {
        // 1. 检测场上掉落物数量
        int currentCount = CountItemsInScene();
        
        // 2. 计算需要补充的数量
        int needSpawn = targetCount - currentCount;
        
        if (needSpawn <= 0)
        {
            return false; // 不需要补充
        }
        
        // 3. 生成掉落物
        for (int i = 0; i < needSpawn; i++)
        {
            SpawnDropItem(itemToSpawn);
        }
        
        return true;
    }
    
    private int CountItemsInScene()
    {
        var items = FindObjectsByType<ItemPickup>();
        return items.Length;
    }
}
```

**技能配置：**
```yaml
技能：掉落物补充
  ├─ Trigger: OnPhaseStartedTrigger (需要新建，监听玩家回合开始)
  ├─ Condition: AlwaysTrueCondition
  ├─ Effect: DropItemReplenishEffect
  │   ├─ targetCount: 3
  │   └─ itemToSpawn: [拖拽 ItemConfig]
  └─ Reset: ImmediateResetCondition (每回合都可触发)
```

---

### 系统3：收集打击技能

#### 实现方式：回合结束触发的伤害技能

```csharp
// CollectorStrikeEffect.cs (新建)
public class CollectorStrikeEffect : IEffect
{
    public float damagePerItem = 5f;  // 每个掉落物的伤害系数
    
    public bool ExecuteEffect(SkillArgs args)
    {
        // 1. 获取角色ID
        string characterID = GetOwnerCharacterID();
        
        // 2. 获取本回合拾取数量
        int pickupCount = DropItemTracker.Instance.GetCurrentTurnPickups(characterID);
        
        if (pickupCount == 0)
        {
            return false; // 没拾取，不触发
        }
        
        // 3. 计算伤害
        float totalDamage = pickupCount * damagePerItem;
        
        // 4. 找到最近的敌人
        GameObject nearestEnemy = FindNearestEnemy();
        
        if (nearestEnemy == null)
        {
            return false; // 没有敌人
        }
        
        // 5. 造成伤害
        DealDamageToEnemy(nearestEnemy, totalDamage);
        
        // 6. 清空计数（在 Tracker 中处理，回合结束时自动清空）
        
        return true;
    }
}
```

**技能配置：**
```yaml
技能：收集打击
  ├─ Trigger: OnPhaseEndedTrigger (需要新建，监听玩家回合结束)
  ├─ Condition: AlwaysTrueCondition
  ├─ Effect: CollectorStrikeEffect
  │   └─ damagePerItem: 5
  └─ Reset: ImmediateResetCondition (每回合都可触发)
```

---

## 🔧 需要创建的新组件

### 核心组件（必需）

**1. DropItemTracker.cs**
- 路径：`Assets/Scripts/Core/Manager/`
- 职责：追踪每个角色本回合拾取的掉落物数量
- 接口：
  - `OnItemPickedUp(characterID, itemConfig)` - 拾取时调用
  - `GetCurrentTurnPickups(characterID)` - 获取拾取数量
  - `ResetTurnPickups()` - 回合结束时清空

**2. OnPhaseStartedTrigger.cs**
- 路径：`Assets/Scripts/SkillSystem/Triggers/`
- 职责：监听回合开始事件（PlayerPhase）
- 用于：掉落物补充技能

**3. OnPhaseEndedTrigger.cs**
- 路径：`Assets/Scripts/SkillSystem/Triggers/`
- 职责：监听回合结束事件
- 用于：收集打击技能

**4. DropItemReplenishEffect.cs**
- 路径：`Assets/Scripts/SkillSystem/Effects/`
- 职责：补充掉落物到目标数量

**5. CollectorStrikeEffect.cs**
- 路径：`Assets/Scripts/SkillSystem/Effects/`
- 职责：根据拾取数量对最近敌人造成伤害

### 配置组件

**6. OnPhaseStartedTriggerConfig.cs**
**7. OnPhaseEndedTriggerConfig.cs**
**8. DropItemReplenishEffectConfig.cs**
**9. CollectorStrikeEffectConfig.cs**

---

## 🎮 实际效果演示

### 场景示例

**回合1开始：**
```
场上0个掉落物
  ↓
触发"掉落物补充"技能
  ↓
生成3个掉落物
  ↓
场上3个掉落物
```

**回合1进行中：**
```
玩家拾取了2个掉落物
  ↓
DropItemTracker: character_collector 本回合拾取 +2
  ↓
场上剩1个掉落物
```

**回合1结束时：**
```
触发"收集打击"技能
  ↓
读取拾取数量: 2个
  ↓
计算伤害: 2 × 5 = 10点
  ↓
找到最近的敌人: Enemy_A
  ↓
造成10点伤害
  ↓
重置拾取计数: 0
```

**回合2开始：**
```
场上1个掉落物（上回合剩的）
  ↓
触发"掉落物补充"技能
  ↓
生成2个掉落物（补充到3个）
  ↓
场上3个掉落物
```

---

## ⚠️ 关键技术问题

### 问题1：掉落物计数范围

**问题：** 只计数"收集者角色"拾取的？还是所有角色拾取的？

**方案A：** 只计数收集者自己拾取的
- 更符合"角色特性"
- 技能效果和角色绑定

**方案B：** 计数所有角色拾取的
- 鼓励团队拾取
- 伤害可能过高

**推荐：方案A**

---

### 问题2：掉落物保证数量的作用范围

**问题：** 3个掉落物是全局的？还是只生成特定类型？

**方案A：** 全局（所有掉落物类型）
- 包括击杀掉落、技能掉落等
- 简单

**方案B：** 只生成指定类型（如回血道具）
- 可配置
- 更灵活

**推荐：方案B（可配置掉落物类型）**

---

### 问题3：找最近敌人的标准

**距离计算：**
- 从收集者角色球的位置？
- 还是从玩家队伍的中心位置？

**推荐：从收集者角色球的位置**

---

### 问题4：补充和触发时机 ✅

**GameFlowState 切换顺序：**
```
PlayerPhase → PlayerPhaseEnd → EnemyPhase
```

**已确认：**
- ✅ 已有 `GameFlowState.PlayerPhaseEnd` 状态
- ✅ 掉落物补充：监听 `PlayerPhase`（回合开始）
- ✅ 收集打击：监听 `PlayerPhaseEnd`（回合结束）

**实现：**
- 使用现有的 `GameEventBus.OnGameFlowStateChanged` 事件
- Trigger 中过滤 `PlayerPhase` 或 `PlayerPhaseEnd`

---

## 📝 实施计划

### 阶段1：基础系统（预计3-4小时）

**1. 创建 DropItemTracker Manager**
- 追踪拾取数量
- 监听拾取和回合事件
- 提供查询接口

**2. 修改 ItemPickup**
- 拾取时发布事件或调用 Tracker

**3. 修改 ItemPickup 发布拾取事件**
- 添加或使用现有的 `GameEventBus.OnItemPickedUp` 事件
- 传递参数：characterID, itemConfig

### 阶段2：Trigger 创建（预计1小时）

**1. PhaseStateTrigger（需要新建）**
- 监听 `GameEventBus.OnGameFlowStateChanged`
- 配置参数：要监听的 `GameFlowState`（如 `PlayerPhase`、`PlayerPhaseEnd`）
- 用于：掉落物补充、收集打击
- 参考：`DamageTrigger` 的过滤逻辑

**2. PhaseStateTriggerConfig**
- 继承 `TriggerBase`
- 配置要监听的阶段（单选或多选）

### 阶段3：Effect 创建（预计2-3小时）

**1. DropItemReplenishEffect**
- 检测场上掉落物
- 生成补充

**2. CollectorStrikeEffect**
- 读取拾取数量
- 找最近敌人
- 造成伤害

### 阶段4：技能配置和测试（预计1-2小时）

- 创建两个 SkillConfig SO
- 配置到 PlayerData.initialSkills
- 测试完整流程

---

## ❓ 待确认问题

### 需要你确认的设计决策：

1. ✅ **拾取计数范围**
   - 只计数收集者自己拾取的
   - 物品设置为只能收集者角色拾取（`ItemPickupRestriction.SpecificCharacter`）

2. ✅ **掉落物类型**
   - 只生成指定类型的特殊掉落物
   - 在技能配置中指定 `ItemConfig`

3. ✅ **伤害系数**
   - 可配置，每个掉落物造成的伤害可调整
   - 支持技能升级（Lv1: 5点，Lv2: 8点，Lv3: 10点）

4. ✅ **补充时机**
   - 玩家回合开始时（`GameFlowState.PlayerPhase`）

5. ✅ **收集打击触发条件**
   - 只要本回合拾取了就触发（拾取数 > 0）

6. ✅ **目标选择**
   - 距离收集者角色球最近的敌人

---

## 🎨 角色设定建议

### 推荐配置

**角色名称：** 收集者 / 拾荒者  
**角色职业：** 辅助 / 战术  
**特点：** 鼓励主动拾取掉落物，转化为伤害输出

**数值参考：**
- 掉落物保证数量：3个
- 每个掉落物伤害系数：
  - Lv1: 5点
  - Lv2: 8点
  - Lv3: 10点
- 补充的掉落物类型：回血道具（配置化）

---

## 🔗 相关系统

### 依赖系统

1. **掉落物系统**
   - `ItemPickup.cs`
   - `ItemConfig.cs`
   - `DropItemEffect.cs`

2. **技能系统**
   - `SkillManager`
   - ITrigger, IEffect 接口

3. **回合系统**
   - `GameEventBus` 事件
   - `PhaseController`

4. **敌人系统**
   - 查找所有存活敌人
   - 计算距离

---

**请确认这些设计问题，然后我们开始实现！** 🤔📝
