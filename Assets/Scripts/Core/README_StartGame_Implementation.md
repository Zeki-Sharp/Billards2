# 开始游戏功能实现说明

## 📋 **实现概述**

本实现提供了从角色选择界面到游戏场景的完整流程，包括角色数据的传递和注入。

## 🏗️ **架构组件**

### **1. SceneTransitionManager**
- **位置**: `Assets/Scripts/Core/SceneTransitionManager.cs`
- **职责**: 管理场景切换和数据传递
- **功能**:
  - 存储选中的角色数据
  - 提供场景加载接口
  - 跨场景数据传递

### **2. PlayerDataInjector**
- **位置**: `Assets/Scripts/Core/PlayerDataInjector.cs`
- **职责**: 在游戏场景中注入选中的角色数据
- **功能**:
  - 查找场景中的Player组件
  - 注入选中的角色数据
  - 支持延迟注入

### **3. CharacterSelectionManager (已修改)**
- **位置**: `Assets/Scripts/CharacterSelection/CharacterSelectionManager.cs`
- **新增功能**:
  - 集成场景切换逻辑
  - 设置选中角色数据
  - 调用场景加载

### **4. Player (已修改)**
- **位置**: `Assets/Scripts/Player/Player.cs`
- **新增功能**:
  - 添加 `SetPlayerData()` 公共方法
  - 支持运行时角色数据切换

## 🔄 **工作流程**

```
1. 用户在角色选择界面选择角色
   ↓
2. 点击"开始游戏"按钮
   ↓
3. CharacterSelectionManager.OnStartGameClicked()
   - 触发选择事件
   - 调用 SceneTransitionManager.SetSelectedCharacter()
   - 调用 SceneTransitionManager.LoadLevel1()
   ↓
4. 场景切换到 Level1
   ↓
5. PlayerDataInjector.Start()
   - 获取选中的角色数据
   - 查找场景中的Player组件
   - 调用 Player.SetPlayerData()
   ↓
6. Player.SetPlayerData()
   - 更新playerData字段
   - 重新分发数据给所有组件
   - 重新初始化所有组件
   ↓
7. 游戏开始，使用选中的角色配置
```

## ⚙️ **配置步骤**

### **步骤1: 设置SceneTransitionManager**
1. 在角色选择场景中创建一个空的GameObject
2. 添加 `SceneTransitionManager` 组件
3. 配置 `level1SceneName` 为 "Level1"

### **步骤2: 设置PlayerDataInjector**
1. 在 Level1 场景中创建一个空的GameObject
2. 添加 `PlayerDataInjector` 组件
3. 配置 `injectOnStart` 为 true

### **步骤3: 验证场景配置**
1. 确保 Level1 场景在 Build Settings 中
2. 确保场景名称与配置一致
3. 确保 Level1 场景中有Player预制体实例

## 🎯 **使用方法**

### **角色选择界面**
1. 用户选择角色
2. 点击"开始游戏"按钮
3. 系统自动处理后续流程

### **调试功能**
- **SceneTransitionManager**:
  - Context Menu: "显示当前选中角色"
  - Context Menu: "测试加载Level1"

- **PlayerDataInjector**:
  - Context Menu: "强制重新注入角色数据"
  - Context Menu: "显示注入状态"

## 🔧 **技术细节**

### **数据传递机制**
- 使用静态变量在SceneTransitionManager中存储选中角色
- DontDestroyOnLoad确保数据在场景切换时保持

### **注入时机**
- PlayerDataInjector在Start()时自动注入
- 支持延迟注入（异步场景加载场景）
- 支持强制重新注入（调试用）

### **错误处理**
- 检查选中角色数据是否存在
- 检查场景中的Player组件是否存在
- 提供备用场景加载方案

## 📝 **注意事项**

1. **场景名称**: 确保SceneTransitionManager中的sceneName与实际场景名称一致
2. **Player预制体**: 确保Level1场景中有Player预制体实例
3. **调试信息**: 启用showDebugInfo可以看到详细的执行日志
4. **单例模式**: SceneTransitionManager使用单例模式，确保场景中只有一个实例

## 🚀 **扩展建议**

1. **多场景支持**: 可以扩展支持加载不同的关卡场景
2. **数据持久化**: 可以添加保存/加载角色选择的功能
3. **加载界面**: 可以在场景切换时显示加载界面
4. **错误恢复**: 可以添加更完善的错误处理和恢复机制
