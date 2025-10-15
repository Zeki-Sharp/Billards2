# LevelManager 实现细节

## 📋 **核心设计**

### **LevelManager 字段设计**
```csharp
public class LevelManager : MonoBehaviour
{
    [Header("关卡配置")]
    [SerializeField] private LevelConfig[] levelList;  // 手动配置的关卡数组
    [SerializeField] private int currentLevelIndex = 0;  // 当前关卡索引
    
    [Header("关卡完成检测")]
    [SerializeField] private int totalEnemyCount;  // 关卡敌人总数
    [SerializeField] private int killedEnemyCount;  // 已击杀敌人数
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private WaveConfigProvider waveConfigProvider;
    private SkillSelectionManager skillSelectionManager;
}
```

## 🎯 **核心方法**

### **1. 初始化方法**
```csharp
void Start()
{
    InitializeLevelManager();
    LoadCurrentLevel();
}

void InitializeLevelManager()
{
    // 获取组件引用
    waveConfigProvider = FindFirstObjectByType<WaveConfigProvider>();
    skillSelectionManager = FindFirstObjectByType<SkillSelectionManager>();
    
    // 订阅事件
    GameEventBus.OnEnemyDeath += OnEnemyDeath;
    GameEventBus.OnSkillSelectionCompleted += OnSkillSelectionCompleted;
    
    // 初始化敌人计数
    ResetEnemyCount();
}
```

### **2. 关卡加载方法**
```csharp
void LoadCurrentLevel()
{
    if (levelList == null || levelList.Length == 0)
    {
        Debug.LogError("LevelManager: 关卡列表为空！");
        return;
    }
    
    if (currentLevelIndex >= levelList.Length)
    {
        Debug.Log("LevelManager: 所有关卡已完成！");
        GameCompleted();
        return;
    }
    
    LevelConfig currentLevel = levelList[currentLevelIndex];
    
    // 设置关卡配置到 WaveConfigProvider
    if (waveConfigProvider != null)
    {
        waveConfigProvider.SetLevelConfig(currentLevel);
    }
    
    // 统计敌人总数
    CountTotalEnemies();
    
    if (showDebugInfo)
    {
        Debug.Log($"LevelManager: 加载关卡 {currentLevelIndex + 1} - {currentLevel.levelName}");
    }
}
```

### **3. 敌人计数方法**
```csharp
void CountTotalEnemies()
{
    totalEnemyCount = 0;
    killedEnemyCount = 0;
    
    if (levelList == null || currentLevelIndex >= levelList.Length)
        return;
    
    LevelConfig currentLevel = levelList[currentLevelIndex];
    
    // 统计初始敌人
    if (currentLevel.generateInitialEnemies)
    {
        totalEnemyCount += currentLevel.initialEnemies.Count;
    }
    
    // 统计波次敌人
    foreach (var wave in currentLevel.waves)
    {
        totalEnemyCount += wave.enemySpawns.Count;
    }
    
    if (showDebugInfo)
    {
        Debug.Log($"LevelManager: 关卡敌人总数: {totalEnemyCount}");
    }
}
```

### **4. 敌人死亡处理**
```csharp
void OnEnemyDeath(EnemyBehavior enemy)
{
    killedEnemyCount++;
    
    if (showDebugInfo)
    {
        Debug.Log($"LevelManager: 敌人死亡 {killedEnemyCount}/{totalEnemyCount}");
    }
    
    // 检查关卡完成条件
    if (killedEnemyCount >= totalEnemyCount)
    {
        CompleteCurrentLevel();
    }
}
```

### **5. 关卡完成处理**
```csharp
void CompleteCurrentLevel()
{
    if (showDebugInfo)
    {
        Debug.Log($"LevelManager: 关卡 {currentLevelIndex + 1} 完成！");
    }
    
    // 发布关卡完成事件
    GameEventBus.PublishLevelCompleted(currentLevelIndex);
    
    // 启动技能选择
    StartSkillSelection();
}
```

### **6. 技能选择处理**
```csharp
void StartSkillSelection()
{
    if (skillSelectionManager != null)
    {
        skillSelectionManager.StartSkillSelection();
    }
    else
    {
        Debug.LogError("LevelManager: 未找到 SkillSelectionManager！");
        // 直接进入下一关卡
        LoadNextLevel();
    }
}

void OnSkillSelectionCompleted()
{
    if (showDebugInfo)
    {
        Debug.Log("LevelManager: 技能选择完成，准备进入下一关卡");
    }
    
    LoadNextLevel();
}
```

### **7. 下一关卡加载**
```csharp
void LoadNextLevel()
{
    currentLevelIndex++;
    LoadCurrentLevel();
}
```

### **8. 游戏完成处理**
```csharp
void GameCompleted()
{
    if (showDebugInfo)
    {
        Debug.Log("LevelManager: 所有关卡完成！游戏通关！");
    }
    
    // 发布游戏完成事件
    GameEventBus.PublishGameCompleted();
    
    // 返回主菜单或显示通关界面
    ReturnToMainMenu();
}

void ReturnToMainMenu()
{
    // 使用 SceneTransitionManager 返回主菜单
    SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
    if (transitionManager != null)
    {
        // 假设主菜单场景名为 "MainMenu"
        transitionManager.LoadScene("MainMenu");
    }
}
```

## 🔧 **配置步骤**

### **1. 在 Unity 中配置 LevelManager**
1. 在场景中创建空的 GameObject，命名为 "LevelManager"
2. 添加 `LevelManager` 脚本
3. 在 Inspector 中配置：
   - **关卡列表**：拖入所有 `LevelConfig` 资产（按顺序）
   - **当前关卡索引**：设置为 0（第一关）
   - **调试信息**：勾选以显示调试日志

### **2. 关卡配置示例**
```
关卡列表数组：
[0] Level1 (LevelConfig)
[1] Level2 (LevelConfig)  
[2] Level3 (LevelConfig)
[3] Level4 (LevelConfig)
...
```

### **3. 场景设置**
- 确保场景中有 `WaveConfigProvider` 组件
- 确保场景中有 `SkillSelectionManager` 组件
- 确保 `GameEventBus` 事件系统正常工作

## 📊 **事件流程**

### **关卡进行流程**
1. `LevelManager.Start()` → `InitializeLevelManager()` → `LoadCurrentLevel()`
2. `LoadCurrentLevel()` → `waveConfigProvider.SetLevelConfig()` → `CountTotalEnemies()`
3. 敌人生成 → 敌人死亡 → `OnEnemyDeath()` → 检查完成条件

### **关卡完成流程**
1. 所有敌人死亡 → `CompleteCurrentLevel()` → `GameEventBus.PublishLevelCompleted()`
2. `StartSkillSelection()` → `skillSelectionManager.StartSkillSelection()`
3. 玩家选择技能 → `OnSkillSelectionCompleted()` → `LoadNextLevel()`

### **游戏完成流程**
1. 最后一关完成 → `LoadNextLevel()` → `currentLevelIndex >= levelList.Length`
2. `GameCompleted()` → `GameEventBus.PublishGameCompleted()` → `ReturnToMainMenu()`

## ⚠️ **注意事项**

1. **关卡列表配置**：确保关卡列表按正确顺序配置
2. **敌人计数准确性**：确保所有敌人都被正确统计
3. **事件订阅**：确保正确订阅和取消订阅事件，避免内存泄漏
4. **错误处理**：处理关卡列表为空、组件缺失等异常情况
5. **调试信息**：使用调试信息帮助排查问题

## 🎯 **扩展性**

### **未来可能的扩展**
1. **关卡解锁系统**：某些关卡需要特定条件解锁
2. **关卡评分系统**：根据完成时间、技能使用等评分
3. **关卡重试机制**：失败后重新开始当前关卡
4. **关卡预览**：显示下一关卡的预览信息
5. **存档系统**：保存关卡进度和技能选择
