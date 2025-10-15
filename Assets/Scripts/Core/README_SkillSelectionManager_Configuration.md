# SkillSelectionManager 配置说明

## 📋 **概述**

SkillSelectionManager 是技能选择系统的核心组件，负责管理关卡完成后的技能选择流程，与 LevelManager 和 SkillManager 协同工作。

## 🎯 **核心功能**

1. **监听关卡完成**：自动响应关卡完成事件
2. **随机技能选择**：从技能库中随机选择3个技能（去重）
3. **技能去重机制**：避免选择玩家已有的技能
4. **技能添加**：将选中的技能添加到玩家技能列表
5. **关卡切换**：技能选择完成后自动进入下一关卡

## ⚙️ **配置步骤**

### **步骤1：在场景中设置 SkillSelectionManager**

1. 在 Level1 场景中创建一个空的 GameObject
2. 命名为 "SkillSelectionManager"
3. 添加 `SkillSelectionManager` 脚本组件

### **步骤2：配置技能库**

在 SkillSelectionManager 组件的 Inspector 中：

1. **自动发现技能**：
   - 勾选 "自动发现技能"（默认启用）
   - 系统会自动找到项目中所有的 `SkillConfig` 资产
   - 无需手动添加技能到列表中

2. **技能选择数量**：
   - 设置为 3（每次关卡完成后提供3个技能选择）
   - 可根据需要调整

3. **调试信息**：
   - 勾选 "显示调试信息" 以查看详细日志

### **步骤3：确保依赖组件存在**

确保场景中有以下组件：

1. **SkillManager**：
   - SkillSelectionManager 会调用 `AddSkill()` 方法添加技能
   - 会读取 `activeSkills` 列表进行去重

2. **LevelManager**：
   - SkillSelectionManager 会调用 `LoadNextLevel()` 进入下一关卡

3. **GameEventBus**：
   - 事件系统正常工作

## 🔄 **工作流程**

### **技能选择流程**
```
1. 关卡完成 → LevelManager.PublishLevelCompleted()
   ↓
2. SkillSelectionManager.OnLevelCompleted()
   ↓
3. StartSkillSelection() - 启动技能选择
   ↓
4. GenerateRandomSkillSelection() - 生成随机技能选择
   ↓
5. GameEventBus.PublishSkillSelectionStarted() - 发布技能选择开始事件
   ↓
6. 等待玩家选择技能 → OnSkillSelected()
   ↓
7. AddSkillToPlayer() - 添加技能到玩家
   ↓
8. GameEventBus.PublishSkillSelectionCompleted() - 发布完成事件
   ↓
9. levelManager.LoadNextLevel() - 进入下一关卡
```

### **技能去重机制**
```
1. 获取玩家已有技能：skillManager.activeSkills
   ↓
2. 过滤技能库：allAvailableSkills.Where(skill => !playerSkills.Contains(skill))
   ↓
3. 从可用技能中随机选择3个
   ↓
4. 使用 Fisher-Yates 洗牌算法确保随机性
```

## 📊 **事件系统**

### **监听的事件**
- `OnLevelCompleted(int levelIndex, LevelConfig levelConfig)` - 关卡完成

### **发布的事件**
- `OnSkillSelectionStarted(List<SkillConfig> availableSkills)` - 技能选择开始
- `OnSkillSelected(SkillConfig selectedSkill, List<SkillConfig> availableSkills)` - 技能选择
- `OnSkillAddedToPlayer(SkillConfig skill)` - 技能添加到玩家
- `OnSkillSelectionCompleted()` - 技能选择完成

## 🎮 **公共接口**

### **状态查询**
```csharp
List<SkillConfig> GetCurrentSelection()           // 获取当前可选择的技能列表
bool IsSkillSelectionActive()                     // 检查技能选择是否激活
int GetAvailableSkillCount()                      // 获取技能库中的技能数量
```

### **控制方法**
```csharp
void StartSkillSelection()                        // 启动技能选择
void OnSkillSelected(SkillConfig selectedSkill)   // 处理技能选择
void RefreshSkillLibrary()                        // 刷新技能库（重新发现技能）
```

## 🔧 **调试功能**

### **Context Menu 调试方法**
- **强制启动技能选择**：测试技能选择流程
- **刷新技能库**：重新发现所有技能配置
- **显示当前选择**：查看当前可选择的技能
- **显示技能库信息**：查看技能库状态和配置

### **调试日志示例**
```
SkillSelectionManager: 自动发现 15 个技能配置
  - 击杀掉落回血
  - 撞墙回复生命值
  - 碰撞连击
  - 治疗术
  - 范围攻击
  - 速度提升
  - 护盾
  - 伤害提升
  - 过渡技能
  - ...
SkillSelectionManager: 初始化完成
SkillSelectionManager: 技能库包含 15 个技能
SkillSelectionManager: 关卡 1 完成，准备启动技能选择
SkillSelectionManager: 玩家已有 2 个技能
  - 碰撞连击
  - 治疗术
SkillSelectionManager: 从 13 个可用技能中选择了 3 个技能
SkillSelectionManager: 技能选择启动，提供 3 个技能选择
  - 技能 1: 范围攻击
  - 技能 2: 速度提升
  - 技能 3: 护盾
SkillSelectionManager: 玩家选择了技能 - 范围攻击
SkillSelectionManager: 成功添加技能到玩家 - 范围攻击
SkillSelectionManager: 技能选择完成
SkillSelectionManager: 通知 LevelManager 进入下一关卡
```

## ⚠️ **注意事项**

1. **技能库配置**：
   - 确保技能库包含足够的技能供选择
   - 避免技能配置为空或无效

2. **技能去重**：
   - 系统会自动过滤玩家已有的技能
   - 如果所有技能都已被学习，会直接进入下一关卡

3. **组件依赖**：
   - 确保 SkillManager 和 LevelManager 存在
   - 确保事件系统正常工作

4. **技能数量**：
   - 如果可用技能少于配置的选择数量，会提供所有可用技能
   - 如果技能库为空，会直接进入下一关卡

## 🎯 **与现有系统的集成**

### **与 LevelManager 的集成**
- LevelManager 完成关卡后发布 `OnLevelCompleted` 事件
- SkillSelectionManager 监听此事件并启动技能选择
- 技能选择完成后调用 `LevelManager.LoadNextLevel()`

### **与 SkillManager 的集成**
- 读取 `SkillManager.activeSkills` 进行去重
- 调用 `SkillManager.AddSkill()` 添加新技能
- 新技能会自动初始化并开始工作

### **与事件系统的集成**
- 发布技能选择相关事件供 UI 系统使用
- 监听关卡完成事件启动技能选择流程

## 🔮 **未来扩展**

1. **技能分类系统**：不同类型的技能分别选择
2. **技能稀有度**：根据稀有度调整选择概率
3. **技能预览**：显示技能详细信息和效果
4. **技能升级**：已学习的技能可以升级强化
5. **技能重置**：允许玩家重新选择技能

## 📝 **配置示例**

### **技能库配置示例**
```
技能库列表：
[0] CollisionComboSkill (碰撞连击)
[1] HealSkill (治疗术)
[2] AreaAttackSkill (范围攻击)
[3] SpeedBoostSkill (速度提升)
[4] ShieldSkill (护盾)
[5] DamageBoostSkill (伤害提升)
[6] TransitionSkill (过渡技能)
[7] ... (更多技能)
```

### **技能选择数量**
- 默认值：3
- 建议范围：2-5
- 可根据游戏平衡性调整
