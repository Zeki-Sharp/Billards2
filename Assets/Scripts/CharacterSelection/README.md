# 角色选择系统

## 概述
角色选择系统允许玩家从预定义的角色列表中选择一个角色进行游戏。系统包含数据配置、UI管理、按钮交互和状态管理等功能。

## 组件说明

### 1. CharacterSelectionData.cs
**角色选择数据配置**
- 存储所有可选角色的 PlayerData 列表
- 配置按钮预制体和容器
- 提供数据验证功能

**使用方式：**
1. 在 Project 窗口右键 → Create → Game → Character Selection → Character Selection Data
2. 配置 `availableCharacters` 列表，添加所有可选角色的 PlayerData
3. 配置 `characterButtonPrefab` 按钮预制体
4. 配置 `buttonContainer` 按钮容器

### 2. CharacterButton.cs
**角色按钮组件**
- 显示单个角色的信息（名称、图标、攻击模式）
- 处理点击事件和选中状态
- 提供视觉反馈

**预制体要求：**
- 必须包含 CharacterButton 组件
- 建议包含以下UI元素：
  - Button 组件
  - TextMeshProUGUI（角色名称）
  - Image（角色图标）
  - TextMeshProUGUI（攻击模式文本）
  - GameObject（选中指示器）

### 3. CharacterSelectionManager.cs
**角色选择管理器**
- 管理角色选择的核心逻辑
- 动态创建角色按钮
- 处理角色选择和状态管理
- 提供事件系统

**主要功能：**
- 自动从 CharacterSelectionData 读取角色列表
- 为每个角色创建按钮
- 管理选中状态
- 触发角色选择和开始游戏事件

### 4. CharacterSelectionUI.cs
**角色选择UI控制器**
- 管理UI显示和交互
- 提供动画效果
- 处理按钮事件

**UI元素：**
- 标题文本
- 说明文本
- 角色按钮容器
- 开始游戏按钮
- 选中角色文本
- 返回按钮

## 使用方法

### 1. 创建角色选择界面

1. **创建 GameObject**
   - 在场景中创建空的 GameObject，命名为 "CharacterSelection"

2. **添加组件**
   - 添加 `CharacterSelectionManager` 组件
   - 添加 `CharacterSelectionUI` 组件

3. **配置 CharacterSelectionData**
   - 创建 CharacterSelectionData 资源
   - 添加可选角色到列表
   - 配置按钮预制体和容器

4. **设置UI**
   - 创建UI Canvas
   - 设置标题、说明文本
   - 创建按钮容器（建议使用 GridLayoutGroup）
   - 创建开始游戏按钮和选中角色文本

### 2. 创建角色按钮预制体

1. **创建按钮预制体**
   - 创建 Button GameObject
   - 添加 CharacterButton 组件
   - 设置UI元素引用

2. **配置预制体**
   - 设置 Button 组件
   - 添加 TextMeshProUGUI（角色名称）
   - 添加 Image（角色图标）
   - 添加选中指示器

### 3. 监听事件

```csharp
// 监听角色选择事件
CharacterSelectionManager.OnCharacterSelected += OnCharacterSelected;

// 监听开始游戏事件
CharacterSelectionManager.OnStartGame += OnStartGame;

void OnCharacterSelected(PlayerData characterData)
{
    Debug.Log($"选择了角色: {characterData.playerName}");
}

void OnStartGame(PlayerData characterData)
{
    Debug.Log($"开始游戏，角色: {characterData.playerName}");
    // 在这里实现游戏开始逻辑
}
```

## 事件系统

### OnCharacterSelected
当玩家选择一个角色时触发
- 参数：`PlayerData characterData` - 选中的角色数据

### OnStartGame
当玩家点击开始游戏按钮时触发
- 参数：`PlayerData characterData` - 选中的角色数据

## 扩展功能

### 1. 添加新角色
1. 创建新的 PlayerData 资源
2. 配置角色属性（名称、图标、攻击模式等）
3. 将 PlayerData 添加到 CharacterSelectionData 的角色列表中

### 2. 自定义按钮样式
1. 修改角色按钮预制体的UI样式
2. 调整 CharacterButton 组件中的颜色和动画设置

### 3. 添加动画效果
1. 在 CharacterSelectionUI 中启用 `enableAnimations`
2. 调整 `buttonAnimationDuration` 和 `buttonAnimationCurve`

## 注意事项

1. **PlayerData 要求**
   - 每个角色的 PlayerData 必须设置 `playerName`
   - 建议设置 `playerIcon` 和 `attackMode`

2. **按钮预制体要求**
   - 必须包含 CharacterButton 组件
   - 建议包含完整的UI元素

3. **容器设置**
   - 建议使用 GridLayoutGroup 或 VerticalLayoutGroup 来排列按钮
   - 确保容器有足够的空间显示所有按钮

4. **事件处理**
   - 记得在适当的时候取消事件订阅，避免内存泄漏

## 调试功能

所有组件都提供了调试方法：
- 右键点击组件 → Context Menu 查看可用的调试选项
- 使用 `showDebugInfo` 开关控制调试日志输出
- 调用 `ShowDebugInfo()` 方法查看详细状态信息
