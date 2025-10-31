# 关卡完成与技能选择系统实现计划

## 📋 **功能概述**

实现关卡完成检测、结算流程和技能选择系统，让玩家在完成关卡后能够选择技能并进入下一关卡。

## 🎯 **核心流程**

```
关卡进行 → 所有敌人被击杀 → 关卡完成检测 → 技能选择 → 下一关卡
```

## 🏗️ **系统架构设计**

### **1. LevelManager（关卡管理器）**
- **职责**：管理关卡流程和进度
- **功能**：
  - 关卡完成检测（敌人计数）
  - 关卡进度管理（关卡列表和切换）
  - 关卡完成后的界面切换

### **2. SkillSelectionManager**
- **职责**：管理技能选择逻辑
- **功能**：
  - 从技能库中随机选择3个技能（去重）
  - 处理技能选择事件
  - 将选中技能添加到玩家技能列表
  - 通知关卡管理器进入下一关卡

### **3. SkillSelectionUI**
- **职责**：显示技能选择界面
- **功能**：
  - 显示关卡完成信息
  - 显示3个可选技能
  - 处理技能选择点击
  - 与 SkillSelectionManager 交互

### **4. LevelManager 关卡列表管理**
- **职责**：管理关卡列表和进度
- **内容**：
  - 手动配置的关卡列表（LevelConfig数组）
  - 当前关卡索引
  - 关卡切换逻辑

## 🔄 **实现阶段**

### **阶段1：LevelManager 核心功能**
1. 创建 `LevelManager` 组件
2. 添加关卡列表字段（手动配置LevelConfig数组）
3. 实现关卡完成检测（敌人计数）
4. 实现关卡进度管理（当前关卡索引）
5. 实现关卡切换逻辑
6. 集成到现有系统

### **阶段2：技能选择系统**
1. 创建 `SkillSelectionManager`
2. 实现随机选择3个技能（去重）
3. 实现技能添加到玩家系统
4. 实现技能选择完成后的关卡切换通知

### **阶段3：SkillSelectionUI**
1. 创建 `SkillSelectionUI` 界面
2. 实现技能选择界面显示
3. 处理技能选择点击事件
4. 集成关卡完成信息显示

### **阶段4：系统集成与测试**
1. 集成所有功能
2. 测试完整流程
3. 优化用户体验
4. 添加错误处理

## 📊 **数据结构设计**

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
}
```

### **关卡完成事件数据**
```csharp
public class LevelCompletedData
{
    public int levelIndex;
    public LevelConfig levelConfig;
    public float completionTime;
    public int enemiesKilled;
}
```

### **技能选择事件数据**
```csharp
public class SkillSelectedData
{
    public SkillConfig selectedSkill;
    public List<SkillConfig> availableSkills;
}
```

## 🎮 **事件流程**

### **关卡完成流程**
1. 敌人死亡 → `GameEventBus.OnEnemyDeath`
2. `LevelManager` 增加击杀计数，检查完成条件
3. 关卡完成 → `GameEventBus.OnLevelCompleted`
4. `LevelManager` 切换到技能选择界面

### **技能选择流程**
1. 关卡完成后自动进入技能选择 → `GameEventBus.OnSkillSelectionStarted`
2. `SkillSelectionManager` 随机选择3个技能
3. 玩家选择技能 → `GameEventBus.OnSkillSelected`
4. `SkillSelectionManager` 将技能添加到玩家
5. `SkillSelectionManager` 通知 `LevelManager` 加载下一关卡

### **关卡切换流程**
1. 技能选择完成 → `GameEventBus.OnSkillSelectionCompleted`
2. `LevelManager` 自动加载下一关卡场景
3. 新场景中的 `LevelManager` 加载对应关卡配置
4. 无下一关卡：游戏通关，返回主菜单

## 🔧 **技术要点**

### **1. 敌人计数准确性**
- 确保统计所有敌人类型（初始敌人 + 波次敌人）
- 关卡开始时统计敌人总数，敌人死亡时增加击杀计数
- 监听 `GameEventBus.OnEnemyDeath` 事件

### **2. 关卡场景切换**
- `LevelManager` 持有关卡列表数组和对应的场景名称数组
- 关卡完成后自动加载下一关卡场景
- 使用 `SceneTransitionManager.LoadScene()` 进行场景切换
- 新场景中的 `LevelManager` 自动加载对应关卡配置

### **3. 技能去重机制**
- 确保3个可选技能不重复
- 考虑玩家已有技能的去重（可选）
- 处理技能库不足3个的情况

### **4. 界面切换流畅性**
- 关卡完成后直接切换到技能选择界面
- 在技能选择界面显示关卡完成信息
- 确保UI响应及时

### **5. 关卡数据持久化**
- 保存当前关卡进度
- 处理游戏重启后的状态恢复
- 考虑关卡解锁机制（未来扩展）

## 🎯 **成功标准**

1. **关卡完成检测准确**：所有敌人被击杀后正确触发关卡完成
2. **技能选择功能完整**：能够随机选择3个技能供玩家选择
3. **关卡切换流畅**：技能选择完成后能够正确切换到下一关卡配置
4. **关卡列表管理**：能够手动配置关卡列表，按顺序加载关卡
5. **系统集成良好**：与现有系统无缝集成，不影响现有功能
6. **错误处理完善**：处理各种边界情况，如技能库不足、关卡列表为空等

## 📝 **注意事项**

1. **性能考虑**：避免在Update中频繁检查完成条件
2. **错误处理**：处理关卡列表为空、技能库为空等异常情况
3. **UI反馈**：提供清晰的状态提示和操作反馈
4. **代码复用**：利用现有的技能系统和UI框架
5. **测试覆盖**：确保各种边界情况都能正确处理
i