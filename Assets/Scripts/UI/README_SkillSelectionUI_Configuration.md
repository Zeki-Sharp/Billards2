# SkillSelectionUI 配置说明（简化版）

## 📋 **概述**

SkillSelectionUI 是技能选择界面的简化版本，提供三个技能按钮和对应的技能名称显示，实现简洁的技能选择功能。

## 🎯 **核心功能**

1. **显示技能选择界面**：关卡完成后自动显示
2. **三个技能按钮**：对应三个可选择的技能
3. **技能名称显示**：每个按钮下方显示对应技能的名称
4. **点击选择**：点击按钮选择对应技能
5. **自动隐藏**：技能选择完成后自动隐藏

## ⚙️ **配置步骤**

### **步骤1：创建UI面板**

1. 在 Canvas 下创建一个 Panel，命名为 "SkillSelectionPanel"
2. 添加 `SkillSelectionUI` 脚本组件

### **步骤2：配置UI组件**

在 SkillSelectionUI 组件的 Inspector 中配置：

#### **UI面板**
- **技能选择面板**：拖入 SkillSelectionPanel

#### **技能按钮**
- **技能按钮1**：拖入第一个技能按钮
- **技能按钮2**：拖入第二个技能按钮  
- **技能按钮3**：拖入第三个技能按钮

#### **技能名称**
- **技能名称1**：拖入第一个技能名称文本组件
- **技能名称2**：拖入第二个技能名称文本组件
- **技能名称3**：拖入第三个技能名称文本组件

### **步骤3：UI布局建议**

```
SkillSelectionPanel
├── SkillButton1
│   └── SkillName1 (TextMeshPro)
├── SkillButton2
│   └── SkillName2 (TextMeshPro)
└── SkillButton3
    └── SkillName3 (TextMeshPro)
```

## 🔄 **工作流程**

### **技能选择流程**
```
1. 关卡完成 → SkillSelectionManager 启动技能选择
   ↓
2. GameEventBus.PublishSkillSelectionStarted()
   ↓
3. SkillSelectionUI.OnSkillSelectionStarted()
   ↓
4. ShowSkillSelection() - 显示UI面板
   ↓
5. UpdateSkillDisplay() - 更新技能名称
   ↓
6. 玩家点击技能按钮 → OnSkillButtonClicked()
   ↓
7. skillSelectionManager.OnSkillSelected() - 通知选择
   ↓
8. GameEventBus.PublishSkillSelectionCompleted()
   ↓
9. HideUI() - 隐藏界面
```

## 📊 **事件系统**

### **监听的事件**
- `OnSkillSelectionStarted(List<SkillConfig> availableSkills)` - 技能选择开始
- `OnSkillSelectionCompleted()` - 技能选择完成

### **调用的方法**
- `skillSelectionManager.OnSkillSelected(SkillConfig skill)` - 通知技能选择

## 🎮 **公共方法**

```csharp
void ShowSkillSelectionManually(List<SkillConfig> availableSkills)  // 手动显示技能选择
void HideSkillSelectionManually()                                   // 手动隐藏技能选择
bool IsUIActive()                                                   // 检查UI是否激活
```

## 🔧 **调试功能**

### **Context Menu 调试方法**
- **测试显示技能选择**：手动显示技能选择界面
- **隐藏技能选择**：手动隐藏界面
- **显示UI状态**：查看UI状态信息

### **调试日志示例**
```
SkillSelectionUI: 初始化完成
SkillSelectionUI: 收到技能选择开始事件，技能数量: 3
SkillSelectionUI: 显示技能选择界面，技能数量: 3
SkillSelectionUI: 选择技能 - 击杀掉落回血
SkillSelectionUI: 收到技能选择完成事件
SkillSelectionUI: 隐藏技能选择界面
```

## ⚠️ **注意事项**

1. **UI组件配置**：
   - 确保所有按钮和文本组件都已正确拖拽
   - 技能名称文本组件应该是 TextMeshProUGUI 类型

2. **技能数量**：
   - 如果可用技能少于3个，多余的按钮会被隐藏
   - 如果技能库为空，界面不会显示

3. **组件依赖**：
   - 确保 SkillSelectionManager 存在
   - 确保事件系统正常工作

## 🎯 **与现有系统的集成**

### **与 SkillSelectionManager 的集成**
- 监听 `OnSkillSelectionStarted` 事件显示界面
- 调用 `OnSkillSelected()` 方法通知技能选择
- 监听 `OnSkillSelectionCompleted` 事件隐藏界面

### **与事件系统的集成**
- 通过 GameEventBus 接收技能选择事件
- 无需直接引用其他组件，通过事件解耦

## 🔮 **未来扩展**

1. **技能图标**：为每个技能添加图标显示
2. **技能描述**：显示技能的详细描述
3. **动画效果**：添加按钮点击和界面切换动画
4. **音效支持**：添加按钮点击音效
5. **技能预览**：鼠标悬停时显示技能预览

## 📝 **配置示例**

### **Inspector 配置示例**
```
SkillSelectionUI (Script)
├── UI面板
│   └── 技能选择面板: SkillSelectionPanel
├── 技能按钮
│   ├── 技能按钮1: SkillButton1
│   ├── 技能按钮2: SkillButton2
│   └── 技能按钮3: SkillButton3
├── 技能名称
│   ├── 技能名称1: SkillName1 (TextMeshPro)
│   ├── 技能名称2: SkillName2 (TextMeshPro)
│   └── 技能名称3: SkillName3 (TextMeshPro)
└── 调试
    └── 显示调试信息: ✓
```

这个简化版本专注于核心功能，易于配置和使用，满足基本的技能选择需求。
