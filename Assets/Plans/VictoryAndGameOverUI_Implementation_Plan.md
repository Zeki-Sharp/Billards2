# 游戏胜利和失败界面实现计划

## 项目概述

为游戏添加胜利和失败界面，显示玩家在整个游戏过程中的统计数据（消灭敌人数、通过关卡数），并提供重新开始功能返回角色选择界面。

## 需求分析

### 核心需求

1. **失败界面**：当玩家血量为0时显示失败界面
2. **胜利界面**：当玩家完成最后一个关卡时显示胜利界面
3. **统计显示**：两个界面都显示以下数据：
   - 消灭敌人数（跨所有关卡的总数）
   - 通过关卡数
4. **重新开始**：点击按钮返回角色选择场景（CharacterSelection）

### 界面要求

- 界面设计简单清晰
- 统计信息以中文显示
- 重新开始按钮响应点击事件

## 现有资源

### UI预制体（已存在）

1. **Victory Panel** (`Assets/Prefabs/UI/Victory Panel.prefab`)
   - 标题："Victory!"
   - 统计文本：消灭敌人数、通过关卡数
   - Restart按钮

2. **GameOverPanel** (`Assets/Prefabs/UI/GameOverPanel.prefab`)
   - 标题："Game Over"
   - 统计文本：消灭敌人数、通过关卡数
   - Restart按钮

### 现有系统

1. **GameEventBus**：提供游戏事件总线
   - `OnGameOver`：游戏失败事件
   - `OnGameCompleted`：游戏完成事件（所有关卡）
   - `OnDeath`：死亡事件

2. **LevelManager**：关卡管理器
   - 管理关卡进度
   - 统计每个关卡的击杀数
   - 跨场景保持（DontDestroyOnLoad）

3. **GameManager**：游戏总管理器
   - 检测玩家血量为0触发失败

4. **SceneTransitionManager**：场景切换管理器
   - 处理场景加载
   - 管理角色数据传递

5. **GameRuntimeData**：运行时数据管理（静态类）
   - 保存跨关卡的临时数据
   - 提供数据清理功能

## 架构设计

### 核心组件

1. **VictoryPanel脚本**：管理胜利界面逻辑
2. **GameOverPanel脚本**：管理失败界面逻辑
3. **GameRuntimeData扩展**：添加总击杀数统计

### 数据流向

**失败流程**：
```
玩家血量为0 → GameManager触发OnGameOver → GameOverPanel显示 → 显示统计数据
```

**胜利流程**：
```
完成最后关卡 → LevelManager触发OnGameCompleted → VictoryPanel显示 → 显示统计数据
```

**重启流程**：
```
点击Restart → 清理GameRuntimeData → 加载CharacterSelection场景
```

## 详细实现方案

### 1. 数据统计系统

#### 总击杀数统计

**需要扩展的位置**：
- **GameRuntimeData**：添加总击杀数的存储和访问方法
- **LevelManager**：在敌人死亡时同步更新总击杀数

**统计时机**：
- 订阅`GameEventBus.OnDeath`事件
- 每次敌人死亡时累加计数
- 跨关卡保持统计

#### 关卡数统计

**数据来源**：
- 使用`LevelManager.currentLevelIndex`
- 通过关卡数 = currentLevelIndex + 1

### 2. 胜利界面（VictoryPanel）

#### 职责

- 订阅`GameEventBus.OnGameCompleted`事件
- 激活界面并显示统计数据
- 处理Restart按钮点击事件

#### UI组件引用

- 敌人数统计文本（TextMeshProUGUI）
- 关卡数统计文本（TextMeshProUGUI）
- 重新开始按钮（Button）

#### 显示时机

当`OnGameCompleted`事件触发时（所有关卡完成）

### 3. 失败界面（GameOverPanel）

#### 职责

- 订阅`GameEventBus.OnGameOver`事件
- 激活界面并显示统计数据
- 处理Restart按钮点击事件

#### UI组件引用

与VictoryPanel相同

#### 显示时机

当`OnGameOver`事件触发时（玩家血量为0）

### 4. 重新开始功能

#### 执行流程

1. 调用`GameRuntimeData.ClearAllData()`清理所有运行时数据
2. 使用`SceneManager.LoadScene("CharacterSelection")`加载角色选择场景

#### 数据清理

- 清空总击杀数
- 清空血量数据
- 清空所有临时属性数据

### 5. 场景集成

#### 预制体放置

- 在每个游戏关卡场景（Level1~Level5）的Canvas下放置两个面板预制体
- 初始状态设为不激活（Active = false）

#### 触发机制

- 通过事件驱动自动显示，无需手动控制
- 游戏暂停（可选）：显示面板时设置`Time.timeScale = 0`

## 触发条件分析

### 失败触发

**当前实现**：
- 位置：`GameManager.CheckLoseCondition()`
- 条件：`PlayerCore.GetCurrentHealth() <= 0`
- 检测方式：Update轮询
- 事件发布：`GameEventBus.PublishGameOver()`

### 胜利触发

**当前实现**：
- 位置：`LevelManager.GameCompleted()`
- 条件：`currentLevelIndex >= sceneNames.Length`（完成所有关卡）
- 触发方式：在`LoadNextLevel()`检测到没有下一关时调用
- 事件发布：`GameEventBus.PublishGameCompleted()`

**注意**：不使用`GameManager.GameWin()`，因为它基于分数/波次系统，与关卡完成机制不一致。

## 关键问题与解决方案

### 问题1：缺少总击杀数统计

**问题**：`LevelManager.killedEnemyCount`每个关卡独立，场景切换时重置

**解决方案**：
- 在`GameRuntimeData`中添加跨关卡的总击杀数静态变量
- `LevelManager`在每次敌人死亡时同步更新两个计数器

### 问题2：预制体缺少脚本组件

**问题**：预制体只有UI结构，没有逻辑脚本

**解决方案**：
- 创建`VictoryPanel.cs`和`GameOverPanel.cs`
- 在Unity中附加到对应预制体

### 问题3：玩家死亡处理不完整

**问题**：`PlayerCore.Die()`方法当前为空

**影响**：失败触发依赖`GameManager.CheckLoseCondition()`的轮询，已经可用，但效率较低

**建议**：保持现有机制，`GameManager`的轮询检测已经可以正常工作

## Unity编辑器配置清单

### Victory Panel预制体配置

1. 添加`VictoryPanel.cs`组件
2. 拖拽敌人数统计文本到脚本字段
3. 拖拽关卡数统计文本到脚本字段
4. 拖拽Restart按钮到脚本字段
5. 配置按钮OnClick事件指向重启方法
6. 设置初始状态为不激活（Active = false）

### GameOverPanel预制体配置

同Victory Panel

### 场景配置

1. 在Level1~Level5每个场景的Canvas下放置两个面板
2. 确保Canvas使用Overlay模式或Screen Space - Camera模式
3. 确认CharacterSelection场景在Build Settings中

## 实施步骤

### 第一阶段：数据系统扩展

1. 扩展`GameRuntimeData`添加总击杀数功能
2. 修改`LevelManager.OnDeath()`同步更新总击杀数

### 第二阶段：UI脚本创建

1. 创建`VictoryPanel.cs`脚本
2. 创建`GameOverPanel.cs`脚本
3. 实现事件订阅和UI更新逻辑
4. 实现Restart按钮功能

### 第三阶段：Unity配置

1. 检查预制体结构，确认UI组件名称
2. 附加脚本到预制体
3. 配置脚本字段引用
4. 配置按钮事件

### 第四阶段：场景集成

1. 在所有游戏关卡场景中放置面板
2. 测试失败流程（故意让玩家死亡）
3. 测试胜利流程（完成所有关卡）
4. 测试重启功能

### 第五阶段：优化和测试

1. 验证数据统计准确性
2. 测试跨关卡数据保持
3. 测试界面显示效果
4. 优化用户体验（可选：添加Time.timeScale暂停）

## 技术约束

1. **场景名称依赖**：重启功能依赖"CharacterSelection"场景名称，需确保一致
2. **事件系统依赖**：完全依赖`GameEventBus`事件系统
3. **单例依赖**：依赖`LevelManager.Instance`单例访问
4. **DontDestroyOnLoad**：`LevelManager`跨场景保持，需确保正确初始化

## 扩展建议（可选）

以下功能不在当前需求范围内，但可以考虑未来添加：

1. **动画效果**：面板显示时的淡入动画
2. **音效**：胜利/失败音效
3. **更多统计**：游戏时长、最高连击数等
4. **背景模糊**：显示面板时模糊背景游戏画面
5. **按钮扩展**：添加"继续游戏"、"返回主菜单"等选项

## 注意事项

1. **数据清理**：重启游戏时务必清理`GameRuntimeData`所有数据
2. **事件取消订阅**：脚本销毁时正确取消事件订阅，避免内存泄漏
3. **Time.timeScale**：如果使用游戏暂停，UI交互不受影响
4. **预制体实例化**：当前方案假设预制体已放置在场景中，如需动态加载需调整
5. **Build Settings**：确保所有场景都添加到Build Settings
6. **测试流程**：重点测试数据在场景切换时的保持和重置

## 总结

本计划实现了一套简单清晰的胜利/失败界面系统，核心特点：

- **事件驱动**：完全基于`GameEventBus`事件系统
- **数据统一**：使用`GameRuntimeData`统一管理统计数据
- **松耦合**：界面脚本独立，易于维护和扩展
- **最小修改**：只扩展必要的数据和脚本，不改动现有核心逻辑

实现后，玩家将在游戏结束时看到清晰的统计信息，并可以方便地重新开始游戏。

