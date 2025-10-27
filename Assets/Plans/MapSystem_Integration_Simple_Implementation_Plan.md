# 地图系统集成实施计划（最简方案）

## 目标
实现角色选择→地图导航→关卡战斗的完整Roguelike流程，复用现有所有资源，最小改动。

## 游戏流程

```
CharacterSelection场景（已有）
    ↓ 选择角色
MapScene场景（新建）- 显示5层地图
    ↓ 点击第1层节点
Level1场景（已有）- 战斗
    ↓ 击杀所有敌人
【技能选择UI弹出】← 在Level1场景中覆盖显示（UIController跨场景）
    ↓ 选择技能
技能选择完成，UI隐藏
    ↓
返回MapScene - 地图更新，显示下一层可达节点
    ↓ 继续点击第2层节点
Level2场景（已有）- 战斗
    ↓ 击杀所有敌人
【技能选择UI弹出】← 同样在战斗场景中
    ↓ 选择技能
返回MapScene
    ↓ 
... 重复流程 ...
    ↓
Level5场景（Boss战）
    ↓ 击败Boss
【通关UI】
游戏结束
```

**关键点：**
- ✅ 技能选择UI在**战斗场景结束时**弹出
- ✅ UI通过UIController跨场景管理，无需在MapScene中配置
- ✅ 选择完成后自动返回MapScene
```

## 地图配置方案

### 地图层级设计
```
MapConfig配置：
├── 第0层（y=0）：2-3个MinorEnemy节点 → Level1
├── 第1层（y=1）：2-3个MinorEnemy节点 → Level2
├── 第2层（y=2）：2-3个MinorEnemy节点 → Level3
├── 第3层（y=3）：2-3个EliteEnemy节点 → Level4
└── 第4层（y=4）：1个Boss节点 → Level5
```

### 层级到场景的映射规则
- **Layer Y坐标 = 场景索引**
- y=0 → Level1
- y=1 → Level2
- y=2 → Level3
- y=3 → Level4
- y=4 → Level5

## 需要创建/修改的内容

### 阶段1：创建地图场景

**任务：**
1. 创建MapScene.unity场景
2. 放置MapObjects或MapObjectsUI预制体
3. 配置MapManager和MapView组件
4. 配置MapPlayerTracker组件
5. （可选）添加地图背景和装饰

**配置要点：**
- MapManager.config：使用DefaultMapConfig
- MapView.allMapConfigs：添加DefaultMapConfig
- MapView.nodePrefab：使用Node或UINode预制体
- MapView.linePrefab：使用LinePrefab
- MapPlayerTracker.lockAfterSelecting：true（点击后锁定地图）

**注意：**
- ❌ 不需要在MapScene中放置SkillSelectionUI
- ✅ SkillSelectionUI已经在UIController中（跨场景）
- ✅ 技能选择会在战斗场景结束时自动弹出

### 阶段2：配置DefaultMapConfig

**任务：**
1. 打开DefaultMapConfig资源
2. 配置5个MapLayer
3. 设置每层的节点数量和类型

**具体配置：**
```
Layer 0:
- nodeType: MinorEnemy
- numOfStartingNodes: min=2, max=3
- distanceFromPreviousLayer: min=3, max=4

Layer 1-3:
- nodeType: MinorEnemy
- distanceFromPreviousLayer: min=3, max=4

Layer 4:
- nodeType: Boss
- numOfPreBossNodes: min=1, max=1
- distanceFromPreviousLayer: min=3, max=4
```

### 阶段3：创建MapSceneController

**位置：** `Assets/Scripts/MapSystem/Manager/MapSceneController.cs`

**职责：**
- 管理地图场景的生命周期
- 监听从战斗场景返回
- 解锁地图可达节点
- 清理临时数据

**核心功能：**
- 场景加载时检测是否从战斗返回
- 如果是首次进入，生成新地图
- 如果是战斗返回，恢复地图状态
- 解锁MapPlayerTracker（允许继续选择节点）
- 清除GameRuntimeData中的临时标记

**注意：**
- ❌ 不负责触发技能选择（技能选择在战斗场景中自动触发）
- ✅ 只负责地图状态管理和节点解锁

### 阶段4：修改MapPlayerTracker.EnterNode()

**任务：**
实现节点类型→场景的映射逻辑

**实现要点：**
```
MinorEnemy/EliteEnemy/Boss节点：
1. 获取节点的层级（node.point.y）
2. 根据层级确定场景名称
3. 保存节点信息到GameRuntimeData
4. 加载对应战斗场景

其他节点类型（RestSite/Store/Treasure）：
- 暂不实现（显示"未实现"提示）
- 或直接在地图上显示简单UI
```

**层级→场景映射表：**
```
y=0 → "Level1"
y=1 → "Level2"
y=2 → "Level3"
y=3 → "Level4"
y=4 → "Level5"
```

### 阶段5：修改LevelManager.CompleteCurrentLevel()

**任务：**
战斗完成后返回地图而非直接进入下一关

**修改点：**
```
原逻辑：
CompleteCurrentLevel() → PublishLevelCompleted() → （SkillSelectionManager监听）

新逻辑：
CompleteCurrentLevel() → PublishLevelCompleted() → 返回MapScene
```

**实现方式：**
- 添加判断：是否来自地图系统
- 如果是：返回MapScene
- 如果不是：保持原有逻辑（向后兼容）

### 阶段6：验证技能选择UI集成

**当前系统已实现：**
- ✅ SkillSelectionUI通过UIController跨场景管理
- ✅ 监听GameEventBus.OnSkillSelectionStarted事件
- ✅ 在任何场景都能显示（包括Level1-5战斗场景）
- ✅ GameManager自动暂停/恢复游戏

**触发流程（无需修改）：**
```
战斗完成 → LevelManager发布OnLevelCompleted
    ↓
SkillSelectionManager监听 → StartSkillSelection()
    ↓
发布OnSkillSelectionStarted事件
    ↓
SkillSelectionUI监听 → ShowUI()
    ↓
【在战斗场景中显示技能选择】← 自动弹出
    ↓
玩家选择技能
    ↓
发布OnSkillSelectionCompleted
    ↓
SkillSelectionManager → ProceedToNextLevel()
    ↓
返回MapScene
```

**需要做的：**
- ✅ 确认UIController存在且配置了SkillSelectionUI
- ✅ 修改ProceedToNextLevel()返回MapScene而非LoadNextLevel()

### 阶段7：添加数据传递机制

**需要传递的数据：**

**从地图→战斗场景：**
- 当前节点层级（用于确定难度）
- 节点类型（MinorEnemy/Elite/Boss）

**从战斗→地图场景：**
- 战斗是否完成
- 是否需要技能选择

**实现方式：**
使用GameRuntimeData添加字段：
```
public static class GameRuntimeData
{
    // 新增：地图系统数据
    private static int currentMapLayer = -1;
    private static bool returnedFromCombat = false;
    
    public static void SetCurrentMapLayer(int layer) 
    public static int GetCurrentMapLayer()
    public static void MarkReturnFromCombat()
    public static bool IsReturnFromCombat()
}
```

## 场景配置清单

### MapScene.unity配置
- [ ] 创建场景
- [ ] 放置MapObjects或MapObjectsUI预制体
- [ ] 配置MapManager组件
- [ ] 配置MapView组件
- [ ] 配置MapPlayerTracker组件
- [ ] 添加MapSceneController脚本
- [ ] （可选）添加背景和装饰元素

**注意：**
- ❌ 不需要添加SkillSelectionUI（已在UIController中跨场景存在）
- ✅ 技能选择会在战斗场景结束时自动弹出

### DefaultMapConfig.asset配置
- [ ] 设置5层MapLayer
- [ ] 配置节点数量范围
- [ ] 配置节点类型
- [ ] 配置层级间距

### Build Settings配置
- [ ] 添加MapScene到Build Settings
- [ ] 确认场景顺序：CharacterSelection → MapScene → Level1-5

## 代码修改清单

### 新建脚本（1个）
- [ ] MapSceneController.cs - 地图场景控制器（约100行）

### 修改脚本（3个，每个只改1-2个方法）
- [ ] MapPlayerTracker.cs - EnterNode()方法（约30行代码）
- [ ] SkillSelectionManager.cs - ProceedToNextLevel()方法（约10行代码）
- [ ] GameRuntimeData.cs - 添加地图系统数据字段（约20行代码）

**总代码量：约160行新增/修改代码**

### 保持不变（零改动）
- ✅ Level1-5场景（完全复用）
- ✅ WaveConfigProvider配置
- ✅ 玩家系统（固定在场景中）
- ✅ 技能选择UI系统（完全复用，在战斗场景自动弹出）
- ✅ UIController（跨场景UI管理）
- ✅ SkillSelectionUI（通过事件监听自动显示）
- ✅ LevelManager（保持完成检测逻辑）
- ✅ 战斗系统（完全不改）

## 实施步骤

### 步骤1：配置地图
1. 打开DefaultMapConfig
2. 配置5个MapLayer
3. 设置节点类型和数量

### 步骤2：创建地图场景
1. 新建MapScene.unity
2. 放置地图相关预制体
3. 配置MapManager引用

### 步骤3：实现MapSceneController
1. 创建脚本
2. 实现场景加载监听
3. 实现技能选择触发逻辑
4. 实现地图状态管理

### 步骤4：扩展GameRuntimeData
1. 添加地图相关字段
2. 添加数据存取方法

### 步骤5：修改MapPlayerTracker
1. 实现EnterNode()节点处理
2. 实现层级→场景映射
3. 保存数据到GameRuntimeData
4. 加载对应战斗场景

### 步骤6：修改LevelManager
1. 检测是否来自地图系统
2. 战斗完成后返回MapScene
3. 保持技能选择事件发布

### 步骤7：配置Build Settings
1. 添加MapScene
2. 调整场景加载顺序

### 步骤8：测试完整流程
1. 角色选择
2. 进入地图
3. 点击节点进入战斗
4. 战斗完成返回地图
5. 技能选择
6. 继续游戏直到通关

## 关键设计点

### 层级到场景的映射
```
简单映射表（硬编码）：
private static readonly string[] LayerToSceneMap = 
{
    "Level1",  // Layer 0
    "Level2",  // Layer 1
    "Level3",  // Layer 2
    "Level4",  // Layer 3
    "Level5"   // Layer 4 (Boss)
};
```

### 场景切换标记
```
进入战斗前（MapPlayerTracker.EnterNode）：
GameRuntimeData.SetCurrentMapLayer(nodeLayer);
GameRuntimeData.SetFromMapSystem(true);
SceneManager.LoadScene(sceneName);

战斗完成后（SkillSelectionManager.ProceedToNextLevel）：
if (GameRuntimeData.IsFromMapSystem())
    SceneManager.LoadScene("MapScene");
else
    levelManager.LoadNextLevel(); // 保持原有逻辑
```

### 技能选择触发（当前系统已实现）
```
战斗场景中，敌人全灭：
LevelManager.CompleteCurrentLevel()
    ↓
GameEventBus.PublishLevelCompleted()
    ↓
SkillSelectionManager.OnLevelCompleted()
    ↓
SkillSelectionManager.StartSkillSelection()
    ↓
GameEventBus.PublishSkillSelectionStarted()
    ↓
SkillSelectionUI.OnSkillSelectionStarted()
    ↓
【技能选择UI在战斗场景中弹出】← 自动显示，无需额外代码
    ↓
玩家选择技能
    ↓
GameEventBus.PublishSkillSelectionCompleted()
    ↓
SkillSelectionManager.ProceedToNextLevel()
    ↓
返回MapScene
```

### MapScene加载时
```
MapSceneController检测：
if (GameRuntimeData.IsFromMapSystem())
{
    // 从战斗返回
    解锁MapPlayerTracker（允许继续选择节点）
    更新地图可达节点
    清除标记
}
else
{
    // 首次进入或从角色选择进入
    生成新地图
    初始化地图状态
}
```

## Unity配置要点

### MapConfig Inspector配置
```
DefaultMapConfig:
├── Grid Width: 3
├── Num Of Starting Nodes: min=2, max=3
├── Num Of Pre Boss Nodes: min=1, max=1
├── Extra Paths: 0-1
└── Layers (5个):
    ├── Layer 0: MinorEnemy, distance=3-4
    ├── Layer 1: MinorEnemy, distance=3-4
    ├── Layer 2: MinorEnemy, distance=3-4
    ├── Layer 3: EliteEnemy, distance=3-4
    └── Layer 4: Boss, distance=3-4
```

### 场景配置
```
CharacterSelection.unity:
- 选择后加载 "MapScene"

MapScene.unity:
- 包含地图UI
- 包含SkillSelectionUI（初始隐藏）
- 包含MapSceneController

Level1-5.unity:
- 保持现有配置
- 玩家固定在场景中
- WaveConfigProvider配置对应LevelConfig
```

## 数据流设计

### 跨场景数据传递
```
GameRuntimeData新增字段：
├── currentMapLayer: int           # 当前地图层级
├── isFromMapSystem: bool          # 是否来自地图系统
├── isReturnFromCombat: bool       # 是否从战斗返回
└── completedMapLayers: List<int>  # 已完成的层级
```

### 节点访问状态
```
MapManager.CurrentMap.path（已有）：
- 存储玩家访问过的节点
- 用于显示已访问和可访问节点
- 通过PlayerPrefs持久化
```

## 优势分析

### 最小改动
- ✅ Level1-5场景完全不改
- ✅ 战斗系统完全不改
- ✅ 技能系统完全不改
- ✅ 玩家系统完全不改

### 复用资源
- ✅ 复用5个关卡场景
- ✅ 复用SkillSelectionUI
- ✅ 复用地图插件所有功能
- ✅ 复用现有的事件系统

### 实现简单
- ✅ 只需新建1个场景
- ✅ 只需新建1个脚本（MapSceneController）
- ✅ 只需修改2个方法（EnterNode、CompleteCurrentLevel）
- ✅ 只需扩展GameRuntimeData

## 后续扩展方向

### 短期可选
- 添加RestSite节点（恢复生命UI）
- 添加Treasure节点（获得道具UI）
- 添加Store节点（商店UI）

### 中期扩展
- 不同层级使用不同难度配置
- 节点随机分配战斗配置
- 地图主题和视觉效果

### 长期优化
- 多个战斗场景
- 场景和配置组合系统
- 完全的配置驱动

## 验收标准

### 功能完整性
- [ ] 可以从角色选择进入地图
- [ ] 地图显示5层节点
- [ ] 点击节点进入对应战斗场景
- [ ] 战斗完成返回地图
- [ ] 技能选择正常显示
- [ ] 可以继续选择下一层节点
- [ ] 击败Boss后游戏通关

### 体验流畅性
- [ ] 场景切换流畅
- [ ] 数据正确保持
- [ ] UI显示正确
- [ ] 无卡顿和错误

### 代码质量
- [ ] 无编译错误
- [ ] 注释完整
- [ ] 逻辑清晰
- [ ] 易于扩展

## 注意事项

### 场景切换
- MapScene必须添加到Build Settings
- 场景加载时注意组件初始化顺序
- 使用GameRuntimeData传递数据而非静态变量

### 玩家状态
- 血量通过GameRuntimeData跨场景保持
- 技能通过SkillManager跨场景保持
- 玩家对象在每个战斗场景中固定存在

### 地图状态
- 地图进度通过MapManager.CurrentMap保存
- 使用PlayerPrefs持久化
- 场景切换后自动恢复

### 技能选择时机
- 每次战斗完成后自动触发
- 在战斗场景中显示（通过UIController跨场景UI）
- 选择完成后自动返回地图场景
- 地图场景加载时自动解锁下一层节点

### UI显示机制
- SkillSelectionUI是UIController的子对象（DontDestroyOnLoad）
- 通过事件系统自动显示/隐藏
- 不依赖具体场景
- 完全解耦的设计

## 实施优先级

### P0（核心功能）
1. 创建MapScene场景
2. 配置MapConfig
3. 实现EnterNode()场景跳转
4. 实现战斗完成返回地图

### P1（体验完善）
5. 集成技能选择UI
6. 添加地图状态保存
7. 完善场景切换过渡

### P2（可选优化）
8. 添加其他节点类型
9. 优化UI和视觉效果
10. 添加音效和反馈

## 预期效果

### 完整玩家体验流程：
```
1. CharacterSelection场景
   选择角色（撞击或范围攻击）
   
2. 进入MapScene
   看到随机生成的5层地图
   第1层节点可点击（脉动动画）
   
3. 点击第1层节点
   播放动画 → 进入Level1场景
   
4. Level1战斗场景
   击杀所有敌人
   
5. 【技能选择UI自动弹出】
   ← 在Level1场景中覆盖显示
   暗化背景，显示3个技能
   
6. 选择技能
   技能添加成功
   UI淡出消失
   
7. 自动返回MapScene
   第1层节点显示已访问（绿色+圆圈）
   第2层节点解锁（白色+脉动）
   
8. 继续点击第2层节点
   重复步骤3-7
   
9. 最终点击Boss节点
   进入Level5 Boss战
   
10. 击败Boss
    技能选择
    返回地图或通关界面
```

### Roguelike特性：
- ✅ 每次地图随机生成（路径、连接不同）
- ✅ 玩家选择路径（策略性）
- ✅ 战斗后立即选择技能成长
- ✅ 状态跨场景保持（血量、技能）
- ✅ 完整的闭环体验
- ✅ 视觉反馈清晰（节点状态、连线颜色）

