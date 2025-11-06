# 场景管理器整理方案

> **创建时间**：2025年11月  
> **用途**：优化场景 Hierarchy 结构，分类管理所有管理器

---

## 📊 **当前问题分析**

### **现状：**
- 管理器分散在多个位置（Manager 文件夹、顶级对象、SkillManager 下）
- 层级混乱，不清晰
- 有些管理器应该在同一个对象上，但分散了

---

## ⚠️ **重要技术限制**

### **DontDestroyOnLoad 对象必须在根级别！**

**原因：**
- Unity 调用 `DontDestroyOnLoad(gameObject)` 时，会将对象移到特殊场景
- 如果对象是子级，会自动脱离父对象，变成根对象
- **所以跨场景保留的管理器不能放在文件夹下！**

---

## ✅ **修正后的推荐方案**

### **方案A：按生命周期分类（推荐）⭐⭐⭐**

```
【根级别 - 跨场景保留对象】（不能作为子对象）
├─ GameManager (PersistAcrossScenes = true)
├─ GameSession (PersistAcrossScenes = true)
├─ SceneTransitionManager (PersistAcrossScenes = true)
├─ LevelManager (PersistAcrossScenes = true)
├─ DropItemTracker (PersistAcrossScenes = true)
└─ TurnPenaltyManager (PersistAcrossScenes = true)

【场景级别管理器】（可以分类整理）
Level1
├─ FlowControllers (GameObject)
│  ├─ GameFlowController
│  ├─ PlayerPhaseController
│  └─ EnemyPhaseController
│
├─ 【SceneManagers】（场景级别）
│  ├─ GameFlowController (SingletonManager)
│  ├─ PlayerPhaseController (SingletonManager)
│  ├─ EnemyPhaseController (SingletonManager)
│  └─ UIController (SingletonManager)
│
├─ 【CombatManagers】（战斗系统）
│  ├─ DamageSystem (SingletonManager)
│  ├─ DamageProcessor (SingletonManager)
│  ├─ SkillManager (SingletonManager)
│  ├─ WeakPointManager (SingletonManager)
│  └─ DamageTextManager (SingletonManager)
│
├─ 【SpawnManagers】（生成系统）
│  ├─ EnemyManager (SingletonManager)
│  ├─ EnemySpawner (MonoBehaviour)
│  ├─ ItemSpawner (MonoBehaviour)
│  └─ PlayerSpawner (MonoBehaviour)
│
├─ 【PlayerManagers】（玩家系统）
│  ├─ PlayerTurnManager (SingletonManager)
│  ├─ PlayerInputPermissionManager (SingletonManager)
│  ├─ GlobalInputManager (MonoBehaviour)
│  ├─ CharacterSelectionController (MonoBehaviour)
│  ├─ BallSelectionManager (MonoBehaviour)
│  └─ ChargeController (MonoBehaviour)
│
├─ 【OtherManagers】（其他）
│  ├─ SkillStateManager (MonoBehaviour)
│  ├─ SkillSelectionManager (SingletonManager)
│  ├─ DeathManager (SingletonManager)
│  ├─ EffectManager (SingletonManager)
│  ├─ TimeManager (SingletonManager)
│  ├─ TrajectorySimulationManager (SingletonManager)
│  └─ MovementManager (如果存在)
│
└─ 【Scene Objects】（场景对象）
   ├─ Wall
   ├─ CameraRig
   ├─ EventSystem
   ├─ Canvas
   ├─ Enemy Group
   ├─ PlayerGroup
   ├─ ItemParent
   └─ Global Volume
```

---

## 🎯 **方案B：按功能领域分类** ⭐⭐

```
Level1
├─ 【Core】（核心系统）
│  └─ GameManager, GameSession, SceneTransitionManager
│
├─ 【Flow】（流程控制）
│  └─ GameFlowController, PlayerPhaseController, EnemyPhaseController
│
├─ 【Combat】（战斗）
│  └─ DamageSystem, DamageProcessor, SkillManager, WeakPointManager
│
├─ 【Player】（玩家）
│  └─ PlayerSpawner, PlayerTurnManager, CharacterSelectionController...
│
├─ 【Enemy】（敌人）
│  └─ EnemyManager, EnemySpawner
│
├─ 【Item】（道具）
│  └─ ItemSpawner, DropItemTracker
│
└─ 【UI】（界面）
   └─ UIController, DamageTextManager
```

---

## 🔧 **可以合并到同一个对象的管理器**

### **组1：跨场景管理器（必须在根级别）⭐⭐⭐**

**❌ 不能合并！必须各自独立！**

**这些管理器必须在根级别：**
- GameManager
- GameSession
- SceneTransitionManager
- LevelManager
- DropItemTracker
- TurnPenaltyManager

**原因：**
- 都设置了 `PersistAcrossScenes = true`
- Unity 会自动将它们移到 DontDestroyOnLoad 场景
- 即使你把它们放在文件夹下，Unity 也会自动分离到根级别
- **所以不要尝试整理它们，保持根级别即可**

---

### **组2：Flow Controllers（流程控制器）⭐**

**可以放在同一个 GameObject 上：**
```
GameObject: "FlowControllers"
Components:
  - GameFlowController
  - PlayerPhaseController
  - EnemyPhaseController
```

**原因：**
- 都负责流程控制
- 场景级别（不跨场景）
- 职责明确，互相协作

---

### **组3：Combat Systems（战斗系统）⭐**

**可以放在同一个 GameObject 上：**
```
GameObject: "CombatManagers"
Components:
  - DamageSystem
  - DamageProcessor
  - SkillManager
  - WeakPointManager
```

**原因：**
- 都和战斗相关
- 场景级别
- 紧密协作

---

### **组4：Spawners（生成器）⭐**

**可以放在同一个 GameObject 上：**
```
GameObject: "Spawners"
Components:
  - EnemyManager
  - EnemySpawner
  - ItemSpawner
  - PlayerSpawner
```

**原因：**
- 都负责生成对象
- 都需要配置 SpawnRangeConfig
- 职责相似

---

### **组5：Player Systems（玩家系统）⭐**

**可以放在同一个 GameObject 上：**
```
GameObject: "PlayerManagers"
Components:
  - PlayerTurnManager
  - PlayerInputPermissionManager
  - CharacterSelectionController
  - BallSelectionManager
  - ChargeController
```

**原因：**
- 都和玩家控制相关
- 场景级别
- 互相协作

---

### **组6：Trackers（追踪器）⭐**

**可以放在同一个 GameObject 上：**
```
GameObject: "Trackers"
Components:
  - DropItemTracker
  - TurnPenaltyManager
  - DeathManager
```

**原因：**
- 都负责追踪/监控
- 跨场景保留
- 独立职责

---

## ⚠️ **不建议合并的管理器**

### **单独保留的：**

1. **UIController** - UI 系统，职责独立
2. **SkillStateManager** - 技能状态管理，逻辑复杂
3. **SkillSelectionManager** - 技能选择，独立功能
4. **DamageTextManager** - 特效显示，独立职责
5. **EffectManager** - 效果管理，独立系统
6. **TimeManager** - 时间管理，全局功能
7. **TrajectorySimulationManager** - 轨迹模拟，独立功能

---

## 📋 **推荐的最终结构**

```
【根级别 - 跨场景保留】（DontDestroyOnLoad，不要移动）
├─ GameManager ⭐
├─ GameSession ⭐
├─ SceneTransitionManager ⭐
├─ LevelManager ⭐
├─ DropItemTracker ⭐
└─ TurnPenaltyManager ⭐

【场景级别】
Level1
├─ FlowControllers (GameObject)
│  ├─ GameFlowController
│  ├─ PlayerPhaseController
│  └─ EnemyPhaseController
│
├─ CombatManagers (GameObject)
│  ├─ DamageSystem
│  ├─ DamageProcessor
│  ├─ SkillManager
│  └─ WeakPointManager
│
├─ Spawners (GameObject)
│  ├─ EnemyManager
│  ├─ EnemySpawner
│  ├─ ItemSpawner
│  └─ PlayerSpawner
│
├─ PlayerManagers (GameObject)
│  ├─ PlayerTurnManager
│  ├─ PlayerInputPermissionManager
│  ├─ CharacterSelectionController
│  ├─ BallSelectionManager
│  └─ ChargeController
│
├─ Trackers (GameObject)
│  ├─ DropItemTracker
│  ├─ TurnPenaltyManager
│  └─ DeathManager
│
├─ UIManager (GameObject)
│  ├─ UIController
│  └─ DamageTextManager
│
├─ SkillStateManager (GameObject)
├─ SkillSelectionManager (GameObject)
├─ EffectManager (GameObject)
├─ TimeManager (GameObject)
├─ TrajectorySimulationManager (GameObject)
│
└─ 【Scene Objects】
   ├─ Wall
   ├─ CameraRig
   ├─ EventSystem
   ├─ Canvas
   ├─ Enemy Group
   ├─ PlayerGroup
   ├─ ItemParent
   └─ Global Volume
```

---

## 🔄 **操作步骤**

### **步骤1：创建分类文件夹对象**
1. 在 Hierarchy 中右键 → Create Empty
2. 命名为 `CoreManagers`
3. 重复创建：`FlowControllers`, `CombatManagers`, `Spawners`, `PlayerManagers`, `Trackers`, `UIManager`

### **步骤2：移动管理器组件**
1. 将对应的 GameObject 拖到分类文件夹下
2. **或者**：将组件从旧对象复制到新对象上

### **步骤3：删除空对象**
1. 移动完成后，删除旧的空 GameObject
2. 保持 Hierarchy 整洁

### **步骤4：保存为预制体（可选）**
1. 将整理好的管理器保存为 `ManagersPrefab`
2. 其他场景直接拖入这个预制体

---

## 💡 **建议优先级**

### **高优先级（立即整理）：**
- ✅ 合并 Core Managers（跨场景保留的）
- ✅ 合并 Flow Controllers（流程控制）
- ✅ 合并 Combat Managers（战斗系统）

### **中优先级（有时间再整理）：**
- ⭐ 合并 Spawners
- ⭐ 合并 Player Managers

### **低优先级（可选）：**
- ⭐ 创建预制体
- ⭐ 其他细节优化

---

## ⚠️ **注意事项**

1. **跨场景对象不能放在子级** ⭐⭐⭐
   - `PersistAcrossScenes = true` 的管理器必须在根级别
   - Unity 会自动将它们移到 DontDestroyOnLoad 场景
   - 不要尝试把它们放在文件夹下，会被自动分离

2. **SingletonManager 可以共存**
   - 多个 SingletonManager 可以在同一个 GameObject 上（仅限场景级别的）
   - 它们各自独立，不会冲突

3. **不要移动场景引用**
   - 如果某个管理器被其他组件引用（Inspector 拖拽），移动后需要重新拖拽

4. **保持执行顺序**
   - SingletonManager 使用 `DefaultExecutionOrder` 控制顺序
   - 在同一个 GameObject 上也会按顺序执行

5. **测试验证**
   - 整理后务必测试所有功能
   - 确保没有引用丢失

---

## 🎯 **预期效果**

**整理前：**
- 20+ 个管理器分散在不同位置
- Hierarchy 混乱，难以查找

**整理后：**
- 7-8 个分类文件夹
- 结构清晰，一目了然
- 易于维护和扩展

---

**按照这个方案整理即可！有任何问题随时问我！** 🚀

