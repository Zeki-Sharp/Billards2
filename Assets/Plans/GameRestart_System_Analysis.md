# 游戏重启系统分析报告

## 问题描述

当玩家点击Restart按钮时，游戏应该完全重置到初始状态。但目前存在问题：**跨场景保留的管理器（DontDestroyOnLoad）没有被重置**，导致游戏状态残留，影响新游戏的正常运行。

## DontDestroyOnLoad管理器清单

### 已确认使用DontDestroyOnLoad的管理器

| 管理器 | 位置 | 作用 | 是否有重置方法 |
|--------|------|------|----------------|
| **UIController** | Assets/Scripts/UI/ | UI中央管理器 | ❌ 无 |
| **LevelManager** | Assets/Scripts/Core/ | 关卡管理器 | ❌ 无 |
| **SkillManager** | Assets/Scripts/SkillSystem/ | 技能管理器 | ❌ 无（但有ReinitializeSkillInstances） |
| **SkillSelectionManager** | Assets/Scripts/Core/ | 技能选择管理器 | ❌ 无 |
| **SceneTransitionManager** | Assets/Scripts/Core/ | 场景切换管理器 | ✅ 有ClearSelectedCharacter |

### 不使用DontDestroyOnLoad的管理器（会随场景销毁重建）

| 管理器 | 说明 |
|--------|------|
| **GameManager** | 每个场景独立，不跨场景 |
| **EnemyPhaseController** | 已注释DontDestroyOnLoad，每场景重建 |
| **PlayerPhaseController** | 已注释DontDestroyOnLoad，每场景重建 |
| **DamageProcessor** | 已注释DontDestroyOnLoad，每场景重建 |
| **DamageTextManager** | 已注释DontDestroyOnLoad，每场景重建 |
| **WeakPointManager** | 不使用DontDestroyOnLoad |

### 静态数据类

| 数据类 | 是否有重置方法 |
|--------|----------------|
| **GameRuntimeData** | ✅ 有ClearAllData |

## 问题分析

### 问题1：SkillManager保留技能数据

**现状**：
- `activeSkills` List保持技能配置
- 场景切换时调用 `ReinitializeSkillInstances()` 重建实例
- **但不清空技能列表**

**问题**：
- Restart后，玩家之前获得的技能仍然存在
- 新游戏应该从0技能开始，但实际带着旧技能

**影响**：
- 🔴 严重 - 游戏平衡被破坏

---

### 问题2：LevelManager保留关卡进度

**现状**：
- `currentLevelIndex` 跨场景保持
- 没有重置方法

**问题**：
- Restart后，关卡索引仍然是失败时的值
- 应该从Level1开始，但可能从Level3开始

**影响**：
- 🔴 严重 - 游戏流程错误

---

### 问题3：SkillSelectionManager保留状态

**现状**：
- 管理技能选择流程
- 跨场景保持
- 没有重置方法

**问题**：
- 内部状态可能残留
- 可能影响新游戏的技能选择

**影响**：
- 🟡 中等 - 可能导致技能选择异常

---

### 问题4：UIController加载的面板残留

**现状**：
- 动态加载的面板缓存在 `loadedPanels` 字典中
- 没有清理机制

**问题**：
- VictoryPanel/GameOverPanel实例一直存在
- 虽然隐藏了，但仍占用内存
- 面板内部状态可能残留

**影响**：
- 🟢 轻微 - 内存浪费，但不影响功能

---

### 问题5：SceneTransitionManager的选中角色数据

**现状**：
- 有 `ClearSelectedCharacter()` 方法
- **但Restart时没有调用**

**问题**：
- 静态角色数据残留
- 虽然会重新选择，但数据不干净

**影响**：
- 🟢 轻微 - 会被覆盖，影响不大

---

## 当前Restart流程分析

### 现有流程

```
点击Restart按钮
    ↓
VictoryPanel/GameOverPanel.OnRestartButtonClicked()
    ↓
1. UIController.HidePanel(this) - 隐藏面板
2. GameRuntimeData.ClearAllData() - 清理运行时数据
3. Time.timeScale = 1f - 恢复时间
4. SceneManager.LoadScene("CharacterSelection") - 加载场景
    ↓
CharacterSelection场景加载
    ↓
❌ 所有DontDestroyOnLoad管理器仍然保留旧状态！
    ↓
玩家重新选择角色
    ↓
进入Level1
    ↓
🔴 问题出现：
- LevelManager.currentLevelIndex 不是0
- SkillManager.activeSkills 还有旧技能
- 其他状态也可能残留
```

---

## 解决方案设计

### 方案A：中央重置管理器（推荐）

**设计思路**：
- 创建一个 `GameRestartManager` 统一管理重置逻辑
- 所有DontDestroyOnLoad管理器提供 `ResetState()` 方法
- Restart时调用 `GameRestartManager.RestartGame()`

**优点**：
- ✅ 统一管理，不会遗漏
- ✅ 职责清晰，易于维护
- ✅ 扩展性好，新管理器只需注册

**缺点**：
- ⚠️ 需要修改所有管理器添加Reset方法
- ⚠️ 增加一个新的管理器类

---

### 方案B：销毁并重建（激进方案）

**设计思路**：
- Restart时直接销毁所有DontDestroyOnLoad管理器
- 场景加载时自动重建
- 不保留任何状态

**优点**：
- ✅ 完全清理，保证干净
- ✅ 不需要写Reset逻辑

**缺点**：
- ❌ 需要处理单例重建逻辑
- ❌ 可能导致引用丢失
- ❌ 复杂度高，风险大

---

### 方案C：事件广播重置（轻量方案）

**设计思路**：
- 在GameEventBus中添加 `OnGameRestart` 事件
- 各管理器订阅此事件并重置自己
- Restart时发布事件

**优点**：
- ✅ 符合现有事件驱动架构
- ✅ 解耦，各管理器自己负责重置
- ✅ 改动较小

**缺点**：
- ⚠️ 仍需要每个管理器实现Reset逻辑
- ⚠️ 需要确保所有管理器都订阅了事件

---

## 推荐方案：方案C（事件广播）+ 集中调用

### 混合方案设计

**结合方案A和C的优点**：
1. 在GameEventBus添加 `OnGameRestart` 事件
2. 各管理器提供 `ResetState()` 方法
3. 创建简单的 `GameRestartHelper` 静态类
4. Restart时调用Helper统一重置

**实现概要**：

```csharp
// GameEventBus
public static event System.Action OnGameRestart;
public static void PublishGameRestart() => OnGameRestart?.Invoke();

// GameRestartHelper（静态类）
public static class GameRestartHelper
{
    public static void RestartGame()
    {
        // 1. 发布重启事件
        GameEventBus.PublishGameRestart();
        
        // 2. 直接调用关键管理器的重置（双保险）
        SkillManager.Instance?.ResetState();
        LevelManager.Instance?.ResetState();
        SkillSelectionManager.Instance?.ResetState();
        UIController.Instance?.ResetState();
        
        // 3. 清理静态数据
        GameRuntimeData.ClearAllData();
        SceneTransitionManager.ClearSelectedCharacter();
        
        // 4. 恢复游戏状态
        Time.timeScale = 1f;
        
        // 5. 加载场景
        SceneManager.LoadScene("CharacterSelection");
    }
}

// 各管理器添加
public void ResetState()
{
    // 重置各自的状态
}
```

---

## 需要添加的Reset方法

### 1. SkillManager.ResetState()

**需要重置的内容**：
- `activeSkills.Clear()` - 清空技能列表
- `skillInstances.Clear()` - 清空技能实例
- `dropItemSkillNames.Clear()` - 清空掉落技能记录

---

### 2. LevelManager.ResetState()

**需要重置的内容**：
- `currentLevelIndex = 0` - 重置到第一关
- `totalEnemyCount = 0` - 重置敌人计数
- `killedEnemyCount = 0` - 重置击杀计数
- `isLevelActive = false`
- `isLevelCompleted = false`

---

### 3. SkillSelectionManager.ResetState()

**需要重置的内容**：
- 清空可能的内部状态
- 重置选择流程

---

### 4. UIController.ResetState()

**需要重置的内容**：
- `loadedPanels.Clear()` - 清空加载的面板缓存
- 销毁动态加载的面板对象
- `currentPopupPanel = null`
- `currentFullScreenPanel = null`
- `isGamePaused = false`

---

### 5. SceneTransitionManager

**已有方法**：
- ✅ `ClearSelectedCharacter()` - 已存在，只需调用

---

## 调用时机和位置

### 修改位置

**VictoryPanel.OnRestartButtonClicked()**：
```csharp
void OnRestartButtonClicked()
{
    // 统一调用重启逻辑
    GameRestartHelper.RestartGame();
}
```

**GameOverPanel.OnRestartButtonClicked()**：
```csharp
void OnRestartButtonClicked()
{
    // 统一调用重启逻辑
    GameRestartHelper.RestartGame();
}
```

---

## 扩展建议

### 未来可能需要重置的系统

1. **PlayerStatsManager**
   - 清空修饰器列表
   - 重置缓存

2. **GameFlowController**
   - 重置阶段状态
   - 但它不使用DontDestroyOnLoad，可能不需要

3. **其他未来添加的管理器**
   - 只需实现 `ResetState()` 方法
   - 订阅 `OnGameRestart` 事件

---

## 实施优先级

### 高优先级（必须修复）

1. ✅ **SkillManager.ResetState()**
   - 清空技能列表
   - 影响最大

2. ✅ **LevelManager.ResetState()**
   - 重置关卡索引
   - 影响游戏流程

### 中优先级（建议修复）

3. ✅ **SkillSelectionManager.ResetState()**
   - 重置选择状态

4. ✅ **UIController.ResetState()**
   - 清理UI缓存

### 低优先级（可选）

5. ⚪ **SceneTransitionManager**
   - 已有ClearSelectedCharacter，只需调用

---

## 测试验证清单

重置完成后，需要验证以下项目：

### 数据重置验证

- [ ] 技能列表清空（SkillManager.activeSkills.Count == 0）
- [ ] 关卡索引重置为0（LevelManager.currentLevelIndex == 0）
- [ ] 总击杀数清零（GameRuntimeData.GetTotalEnemyKills() == 0）
- [ ] 血量数据清空
- [ ] 选中角色数据清空

### 游戏流程验证

- [ ] Restart后从CharacterSelection开始
- [ ] 重新选择角色后进入Level1（不是其他关卡）
- [ ] 没有携带之前的技能
- [ ] 击杀数从0开始计数
- [ ] 关卡进度正常

### Console日志验证

- [ ] 看到"SkillManager: 重置状态完成"
- [ ] 看到"LevelManager: 重置状态完成"
- [ ] 看到"[GameRuntimeData] 所有数据已清理"
- [ ] 没有空引用错误

---

## 注意事项

### 1. 重置顺序很重要

**建议顺序**：
```
1. 隐藏UI面板
2. 发布OnGameRestart事件（让各系统响应）
3. 直接调用关键管理器的ResetState（双保险）
4. 清理静态数据（GameRuntimeData）
5. 恢复游戏状态（Time.timeScale）
6. 加载CharacterSelection场景
```

### 2. 单例重建问题

**潜在风险**：
- 如果销毁单例管理器，下次进入场景时需要重建
- 重建逻辑需要正确处理

**解决方案**：
- 不销毁管理器，只重置内部状态
- 保持单例实例，清空数据即可

### 3. 事件订阅问题

**风险**：
- Reset后，事件订阅关系可能断裂

**确保**：
- Reset不影响事件订阅
- 或者Reset后重新订阅

### 4. 引用丢失问题

**风险**：
- 某些组件引用其他对象，Reset后引用可能失效

**解决**：
- Reset方法中重新查找引用（如FindFirstObjectByType）

---

## 总结

当前游戏重启系统存在**严重缺陷**：跨场景管理器没有重置机制，导致游戏状态混乱。

**必须实现**：
1. 在各DontDestroyOnLoad管理器中添加 `ResetState()` 方法
2. 创建统一的重启入口（GameRestartHelper或GameEventBus.OnGameRestart）
3. 确保Restart按钮调用统一重置流程

**推荐方案**：
- 采用事件广播 + 直接调用的混合方案
- 既保证灵活性，又确保关键系统一定被重置
- 最小化修改现有代码

实施后，游戏Restart功能将完全可靠，为玩家提供干净的重新开始体验。

