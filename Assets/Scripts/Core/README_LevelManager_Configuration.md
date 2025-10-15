# LevelManager 配置说明

## 📋 **概述**

LevelManager 是关卡完成与技能选择系统的核心组件，负责管理关卡流程、检测关卡完成条件，并与现有系统集成。

## 🎯 **核心功能**

1. **关卡列表管理**：手动配置关卡数组，按顺序加载关卡
2. **敌人计数**：统计关卡中的敌人总数，监听敌人死亡事件
3. **关卡完成检测**：当所有敌人被击杀时触发关卡完成
4. **关卡切换**：提供公共方法供外部调用切换到下一关卡
5. **事件发布**：发布关卡开始、完成等事件

## ⚙️ **配置步骤**

### **步骤1：在场景中设置 LevelManager**

1. 在 Level1 场景中创建一个空的 GameObject
2. 命名为 "LevelManager"
3. 添加 `LevelManager` 脚本组件

### **步骤2：配置场景列表**

在 LevelManager 组件的 Inspector 中：

1. **场景名称数组**：
   - 输入所有关卡场景的名称（按关卡顺序）
   - 例如：["Level1", "Level2", "Level3", "Level4"...]

2. **当前关卡索引**：
   - 设置为 0（第一关）
   - 系统会自动管理这个索引

3. **调试信息**：
   - 勾选 "显示调试信息" 以查看详细日志

### **步骤3：配置每个场景的关卡数据**

在每个关卡场景中：

1. **WaveConfigProvider 配置**：
   - 确保场景中有 WaveConfigProvider 组件
   - 在 WaveConfigProvider 的 Inspector 中拖入对应的 LevelConfig 资产
   - 例如：Level1 场景 → Level1Config 资产

2. **关卡数据独立性**：
   - 每个场景的关卡数据完全独立
   - 不需要在 LevelManager 中配置关卡数据
   - 只需要配置场景名称列表即可

### **步骤4：确保依赖组件存在**

确保场景中有以下组件：

1. **WaveConfigProvider**：
   - 每个场景都需要配置自己的 LevelConfig
   - LevelManager 会从 WaveConfigProvider 获取关卡配置

2. **EnemyBehavior**：
   - 敌人死亡时会发布 `OnDeath` 事件

3. **GameEventBus**：
   - 事件系统正常工作

## 🔄 **工作流程**

### **关卡开始流程**
```
1. LevelManager.Start() 
   ↓
2. InitializeLevelManager() - 订阅事件，获取组件引用
   ↓
3. LoadCurrentSceneLevel() - 加载当前场景关卡
   ↓
4. GetCurrentSceneLevelConfig() - 从 WaveConfigProvider 获取关卡配置
   ↓
5. CountTotalEnemies() - 统计敌人总数
   ↓
6. GameEventBus.PublishLevelStarted() - 发布关卡开始事件
```

### **关卡完成流程**
```
1. 敌人死亡 → EnemyBehavior.Die()
   ↓
2. GameEventBus.PublishDeath() - 发布死亡事件
   ↓
3. LevelManager.OnDeath() - 过滤敌人死亡，增加击杀计数
   ↓
4. 检查完成条件：killedEnemyCount >= totalEnemyCount
   ↓
5. CompleteCurrentLevel() - 完成当前关卡
   ↓
6. GameEventBus.PublishLevelCompleted() - 发布关卡完成事件
```

### **关卡切换流程**
```
1. 关卡完成 → CompleteCurrentLevel()
   ↓
2. GameEventBus.PublishLevelCompleted() - 发布关卡完成事件
   ↓
3. SkillSelectionManager 监听事件 → 启动技能选择
   ↓
4. 技能选择完成 → SkillSelectionManager 调用 LoadNextLevel()
   ↓
5. LoadNextLevelScene() - 加载下一关卡场景
   ↓
6. SceneTransitionManager.LoadScene() - 加载对应场景
   ↓
7. 新场景中的 LevelManager 从 WaveConfigProvider 获取关卡配置
```

## 📊 **事件系统**

### **发布的事件**
- `OnLevelStarted(int levelIndex, LevelConfig levelConfig)` - 关卡开始
- `OnLevelCompleted(int levelIndex, LevelConfig levelConfig)` - 关卡完成
- `OnGameCompleted()` - 所有关卡完成

### **监听的事件**
- `OnDeath(DeathData deathData)` - 死亡事件（过滤敌人死亡）

## 🎮 **公共接口**

### **关卡信息查询**
```csharp
int GetCurrentLevelIndex()              // 获取当前关卡索引
LevelConfig GetCurrentLevelConfig()     // 获取当前关卡配置
int GetTotalLevelCount()                // 获取关卡总数
float GetLevelProgress()                // 获取关卡完成进度 (0-1)
bool HasNextLevel()                     // 检查是否有下一关卡
bool IsCurrentLevelCompleted()          // 检查当前关卡是否完成
```

### **关卡控制**
```csharp
void LoadNextLevel()                    // 加载下一关卡（由外部调用）
```

## 🔧 **调试功能**

### **Context Menu 调试方法**
- **显示关卡信息**：查看当前关卡状态
- **强制完成当前关卡**：测试关卡完成逻辑
- **跳转到下一关卡**：测试关卡切换逻辑

### **调试日志示例**
```
LevelManager: 初始化完成
LevelManager: 加载关卡 1 - 关卡1
LevelManager: 初始敌人数量: 2
LevelManager: 波次 1 敌人数量: 3
LevelManager: 波次 2 敌人数量: 2
LevelManager: 波次 3 敌人数量: 4
LevelManager: 波次 4 敌人数量: 1
LevelManager: 波次敌人总数量: 10
LevelManager: 关卡敌人总数: 12 (初始敌人 + 波次敌人)
LevelManager: 敌人死亡 1/12 - Enemy1
LevelManager: 敌人死亡 2/12 - Enemy2
...
LevelManager: 关卡 1 完成！
```

## ⚠️ **注意事项**

1. **场景列表配置**：
   - 确保场景名称数组按正确顺序配置
   - 每个场景名称对应一个关卡场景

2. **敌人计数准确性**：
   - 系统会统计初始敌人和波次敌人
   - 每个 `EnemySpawn` 的 `count` 字段表示该敌人类型要生成的数量
   - 确保所有敌人都被正确统计（初始敌人总数 + 所有波次敌人总数）

3. **事件订阅**：
   - LevelManager 使用单例模式，确保只有一个实例
   - 自动订阅和取消订阅事件，避免内存泄漏

4. **组件依赖**：
   - 确保每个场景都有 WaveConfigProvider 并配置了对应的 LevelConfig
   - 确保 EnemyBehavior 正确发布死亡事件（通过现有的死亡事件系统）

## 🎯 **与技能选择系统的集成**

LevelManager 只负责关卡管理，不直接处理技能选择：

1. **关卡完成** → 发布 `OnLevelCompleted` 事件
2. **SkillSelectionManager** 监听此事件，启动技能选择
3. **技能选择完成** → SkillSelectionManager 调用 `LevelManager.LoadNextLevel()`

这种设计确保了职责分离，LevelManager 专注于关卡管理，技能选择由专门的 SkillSelectionManager 处理。

## 🔮 **未来扩展**

1. **关卡解锁系统**：某些关卡需要特定条件解锁
2. **关卡评分系统**：根据完成时间、技能使用等评分
3. **关卡重试机制**：失败后重新开始当前关卡
4. **关卡预览**：显示下一关卡的预览信息
5. **存档系统**：保存关卡进度和技能选择
