# TopBar常驻顶边栏实施计划

## 目标
创建一个跨场景常驻的顶边栏，显示玩家血条和技能状态，提供快速访问玩家信息的入口。

## 功能概述

### 顶边栏（TopBar）
- **位置：** 屏幕顶部，固定显示
- **生命周期：** 角色选择后出现，跨所有场景保持
- **内容：** 血条 + 技能按钮

### 技能状态面板（SkillStatusPanel）
- **触发：** 点击顶边栏的技能按钮
- **内容：** 显示所有已获得的技能列表
- **交互：** 可关闭，不影响游戏进行

---

## 分步实施方案

### 阶段1：创建TopBar基础结构

**目标：** 搭建TopBar的UI层级和基础布局

**任务：**
1. 在UIController的HUD Canvas下创建TopBar GameObject
2. 配置RectTransform（锚点、位置、大小）
3. 添加背景图片（半透明）
4. 设置Canvas Group（控制整体显隐）

**输出：**
- TopBar GameObject结构
- 基础布局完成

**验证：**
- TopBar在Game视图顶部显示
- 背景半透明
- 不遮挡重要游戏元素

---

### 阶段2：集成血条到TopBar

**目标：** 把现有的血量显示移到TopBar中

**任务：**
1. 在TopBar下创建HealthBarDisplay区域
2. 设置血条UI元素（背景、填充、文本）
3. 创建TopBarController脚本
4. 实现血量更新逻辑（监听GameEventBus.OnHealthChanged）
5. 测试血量显示是否正常

**输出：**
- TopBar显示血条
- 血量实时更新

**验证：**
- 战斗中受伤时血条减少
- 跨场景血量显示正确
- 文本显示"70/100"格式

---

### 阶段3：实现TopBar的显示/隐藏控制

**目标：** 控制TopBar在不同场景的显示状态

**任务：**
1. 在TopBarController中监听场景加载
2. 实现场景判断逻辑：
   - CharacterSelection场景：隐藏
   - MapScene场景：显示
   - Level1-5场景：显示
3. 实现平滑的淡入/淡出动画
4. 测试场景切换时的显示效果

**输出：**
- 角色选择前不显示TopBar
- 角色选择后始终显示

**验证：**
- CharacterSelection场景看不到TopBar
- 进入MapScene后TopBar淡入
- 场景切换时TopBar保持显示

---

### 阶段4：添加技能按钮到TopBar

**目标：** 在TopBar右侧添加技能按钮

**任务：**
1. 在TopBar下创建SkillButton
2. 设置按钮样式和图标
3. 调整TopBar布局（血条70%，按钮30%）
4. 添加按钮点击事件
5. 测试按钮交互

**输出：**
- TopBar右侧显示技能按钮
- 点击按钮有视觉反馈

**验证：**
- 按钮显示正常
- 点击有反馈（暂时只打印日志）
- 不影响血条显示

---

### 阶段5：创建SkillStatusPanel基础结构

**目标：** 创建技能状态面板的UI框架

**任务：**
1. 在UIController的Popup Canvas下创建SkillStatusPanel预制体
2. 创建SkillStatusPanel脚本（继承BasePanel）
3. 设置面板布局：
   - 标题栏（"已获得技能"）
   - 关闭按钮
   - 内容区域（ScrollView）
4. 配置面板样式（背景、边框、阴影）

**输出：**
- SkillStatusPanel预制体
- 基础脚本框架

**验证：**
- 可以手动显示/隐藏面板
- 面板居中显示
- 关闭按钮工作正常

---

### 阶段6：实现技能列表显示

**目标：** 在SkillStatusPanel中显示已获得的技能

**任务：**
1. 创建SkillItem预制体（单个技能的显示项）
2. 在SkillStatusPanel中实现技能列表生成：
   - 从SkillManager获取已获得的技能
   - 动态创建SkillItem
   - 显示技能名称、等级、描述
3. 处理空状态（没有技能时的显示）

**输出：**
- 技能列表正常显示
- 每个技能显示完整信息

**验证：**
- 打开面板看到所有已获得的技能
- 获得新技能后列表自动更新
- 技能升级后等级正确显示

---

### 阶段7：连接TopBar按钮和SkillStatusPanel

**目标：** 点击TopBar按钮打开技能面板

**任务：**
1. 在TopBarController中实现按钮点击逻辑
2. 通过UIController显示SkillStatusPanel
3. 测试打开/关闭流程
4. 处理面板打开时的游戏暂停（可选）

**输出：**
- 完整的交互流程

**验证：**
- 点击技能按钮打开面板
- 点击关闭按钮或背景关闭面板
- 面板打开时可以查看技能
- 地图场景和战斗场景都能使用

---

### 阶段8：优化和细节完善

**目标：** 完善UI细节和用户体验

**任务：**
1. 添加动画效果：
   - TopBar淡入/淡出
   - 面板打开/关闭动画
2. 添加音效反馈
3. 优化布局和样式
4. 添加技能图标显示
5. 性能优化

**输出：**
- 流畅的UI体验
- 视觉效果完善

**验证：**
- 动画流畅
- 无性能问题
- 视觉效果美观

---

## 架构设计

### UI层级结构
```
UIController (DontDestroyOnLoad)
├── HUD Canvas (Render Mode: Screen Space - Overlay, Sort Order: 10)
│   └── TopBar (新建)
│       ├── TopBarController (脚本)
│       ├── CanvasGroup (控制显隐)
│       ├── Background (半透明背景)
│       ├── HealthBarDisplay (血条区域)
│       │   ├── HealthBarBackground
│       │   ├── HealthBarFill
│       │   └── HealthText (TextMeshProUGUI)
│       └── SkillButton (技能按钮)
│           ├── ButtonBackground
│           ├── Icon
│           └── Text
│
└── Popup Canvas (Sort Order: 100)
    └── SkillStatusPanel (新建预制体)
        ├── SkillStatusPanel (脚本，继承BasePanel)
        ├── Background (半透明背景)
        ├── Header (标题栏)
        │   ├── TitleText ("已获得技能")
        │   └── CloseButton
        └── ScrollView (滚动视图)
            └── Content (动态生成SkillItem)
                ├── SkillItem (预制体)
                │   ├── Icon
                │   ├── NameText
                │   ├── LevelText
                │   └── DescriptionText
                └── ...
```

---

## 数据流设计

### 血量更新流程
```
PlayerCore受伤
    ↓
GameRuntimeData.SetCurrentHealth()
    ↓
GameEventBus.PublishHealthChanged()
    ↓
TopBarController.OnHealthChanged()
    ↓
更新TopBar血条显示
```

### 技能面板显示流程
```
点击TopBar技能按钮
    ↓
TopBarController.OnSkillButtonClicked()
    ↓
UIController.ShowPanel("SkillStatusPanel")
    ↓
SkillStatusPanel.OnShow()
    ↓
从SkillManager获取技能列表
    ↓
动态生成SkillItem显示
```

---

## 组件职责划分

### TopBarController
- **职责：** 管理TopBar的显示和数据更新
- **依赖：** GameEventBus（血量事件）、UIController（显示面板）
- **生命周期：** 跨场景保持

### SkillStatusPanel
- **职责：** 显示技能列表
- **依赖：** SkillManager（获取技能）
- **类型：** 继承BasePanel，由UIController管理

### SkillItem
- **职责：** 显示单个技能的信息
- **类型：** 可复用的UI预制体

---

## 场景显示控制

### TopBar显示逻辑
```
场景判断规则：
├── CharacterSelection → 隐藏
├── MapScene → 显示
├── Level1-5 → 显示
└── 其他场景 → 显示（默认）

特殊情况：
- 首次加载时隐藏
- 角色选择完成后显示
- 一旦显示后保持显示
```

### 实现方式
```
方案A：监听场景加载
- SceneManager.sceneLoaded事件
- 判断场景名称
- 显示/隐藏TopBar

方案B：通过事件触发
- 角色选择完成时显示
- 游戏重启时隐藏
```

**推荐：方案B（事件驱动，更清晰）**

---

## 技术实现要点

### Canvas配置
```
HUD Canvas:
- Render Mode: Screen Space - Overlay
- Sort Order: 10（显示在游戏元素之上）
- Pixel Perfect: true

Popup Canvas:
- Sort Order: 100（显示在所有UI之上）
```

### TopBar RectTransform
```
Anchor: 顶部拉伸（Left=0, Top=0, Right=1, Bottom=1）
Pivot: (0.5, 1)
Position: (0, 0, 0)
Height: 60px
```

### 血条配置
```
HealthBarFill:
- Type: Filled
- Fill Method: Horizontal
- Fill Origin: Left
- Fill Amount: 0-1（动态更新）
```

---

## 与现有系统的集成

### 和UIController集成
```
TopBar作为UIController的子对象：
- 自动获得DontDestroyOnLoad特性
- 统一的Canvas管理
- 和其他UI面板协调工作
```

### 和GameEventBus集成
```
监听的事件：
- OnHealthChanged → 更新血条
- OnCharacterSelected → 显示TopBar（可选）
- OnGameRestart → 隐藏TopBar

发布的事件：
- 无（TopBar是纯展示组件）
```

### 和SkillManager集成
```
SkillStatusPanel获取技能数据：
- SkillManager.GetAllActiveSkills()
- 遍历技能列表显示
```

---

## 实施优先级

### P0 - 核心功能（必须实现）
1. ✅ 创建TopBar UI结构
2. ✅ 实现血条显示
3. ✅ 监听血量事件更新
4. ✅ 场景显示/隐藏控制

### P1 - 扩展功能（重要）
5. ✅ 添加技能按钮
6. ✅ 创建SkillStatusPanel
7. ✅ 显示技能列表
8. ✅ 连接按钮和面板

### P2 - 优化细节（可选）
9. 🔄 添加动画效果
10. 🔄 添加技能图标
11. 🔄 优化视觉样式
12. 🔄 添加音效反馈

---

## 实施步骤详解

### 步骤1：Unity中创建TopBar UI（手动配置）
- 在UIController → HUD Canvas下创建TopBar
- 设置锚点、位置、大小
- 添加背景图片
- 创建血条UI元素（Background、Fill、Text）
- 添加CanvasGroup组件

**预期时间：** 5-10分钟

---

### 步骤2：创建TopBarController脚本（编写代码）
- 创建脚本文件
- 定义UI引用字段
- 实现血量更新方法
- 实现显示/隐藏方法
- 监听血量变化事件

**预期时间：** 10-15分钟

---

### 步骤3：配置TopBarController（Unity配置）
- 添加TopBarController组件到TopBar
- 拖拽配置UI引用
- 设置初始状态（隐藏）
- 测试血量显示

**预期时间：** 5分钟

---

### 步骤4：实现场景控制逻辑（编写代码）
- 监听场景加载事件
- 或监听角色选择完成事件
- 实现显示/隐藏逻辑
- 测试跨场景保持

**预期时间：** 10分钟

---

### 步骤5：测试TopBar血条功能（验证）
- 从角色选择开始测试
- 进入地图场景
- 进入战斗场景
- 验证血条始终显示且正确更新

**预期时间：** 5-10分钟

---

### 步骤6：添加技能按钮（Unity配置）
- 在TopBar右侧添加Button
- 设置按钮样式和文本
- 调整TopBar布局（血条左，按钮右）

**预期时间：** 5分钟

---

### 步骤7：创建SkillStatusPanel预制体（Unity配置）
- 在Popup Canvas下创建面板
- 设置标题、关闭按钮、ScrollView
- 创建SkillItem预制体
- 配置布局和样式

**预期时间：** 10-15分钟

---

### 步骤8：创建SkillStatusPanel脚本（编写代码）
- 继承BasePanel
- 实现OnShow()获取技能列表
- 动态生成SkillItem
- 实现关闭逻辑
- 处理空状态

**预期时间：** 15-20分钟

---

### 步骤9：连接按钮和面板（编写代码）
- TopBarController监听按钮点击
- 调用UIController.ShowPanel("SkillStatusPanel")
- 测试打开/关闭流程

**预期时间：** 5分钟

---

### 步骤10：完整测试和优化（验证）
- 测试完整交互流程
- 检查边界情况
- 优化性能和视觉效果
- 修复发现的问题

**预期时间：** 10-15分钟

---

## 关键设计决策

### 1. TopBar的生命周期管理

**方案：** 挂在UIController下，利用现有的DontDestroyOnLoad

**好处：**
- 不需要额外的单例管理
- 和其他UI统一管理
- 自动跨场景保持

---

### 2. 血条数据来源

**方案：** 监听GameEventBus.OnHealthChanged事件

**数据流：**
```
PlayerCore → GameRuntimeData → GameEventBus → TopBarController
```

**好处：**
- 事件驱动，实时更新
- 解耦，不依赖PlayerCore引用
- 跨场景工作正常

---

### 3. 技能面板的触发方式

**方案：** 按钮点击 → UIController显示面板

**流程：**
```
点击按钮 → TopBarController → UIController.ShowPanel() → 面板显示
```

**好处：**
- 利用现有的UIController面板管理
- 统一的显示/隐藏逻辑
- 自动处理暂停（如果需要）

---

### 4. 场景显示控制

**方案：** 监听GameEventBus事件而非场景加载

**事件：**
```
监听：
- OnCharacterSelected → 显示TopBar（角色选择后）
- OnGameRestart → 隐藏TopBar（重新开始）

或者：
直接在角色选择完成后显示，之后一直保持
```

**好处：**
- 语义清晰
- 不依赖场景名称
- 更灵活

---

## 预期效果

### TopBar显示效果
```
┌──────────────────────────────────────────────┐
│ HP ▓▓▓▓▓▓▓▓░░░░ 80/100        [📜 技能]    │
└──────────────────────────────────────────────┘
高度：60px
背景：半透明黑色(rgba 0,0,0,0.7)
血条：绿色到红色渐变（根据血量百分比）
按钮：半透明白色背景
```

### SkillStatusPanel显示效果
```
┌─────────────────────────────────┐
│  已获得技能               [✖]  │
├─────────────────────────────────┤
│  [🔥] 火球术             Lv.2  │
│       造成150%伤害              │
│  ─────────────────────────────  │
│  [⚡] 闪电链             Lv.1  │
│       连锁攻击3个敌人           │
│  ─────────────────────────────  │
│  [💪] 力量强化           Lv.1  │
│       伤害+20%                  │
└─────────────────────────────────┘
宽度：400px
高度：500px
背景：半透明黑色
居中显示
```

---

## 注意事项

### UI层级管理
- TopBar在HUD Canvas（Sort Order: 10）
- SkillStatusPanel在Popup Canvas（Sort Order: 100）
- 确保面板显示在TopBar之上

### 性能考虑
- 技能列表使用对象池（如果技能很多）
- 面板关闭时销毁SkillItem（避免常驻内存）
- 血条更新使用事件驱动（避免每帧Update）

### 跨场景兼容性
- TopBar必须在UIController加载后才能工作
- 确保UIController在所有场景都存在
- 处理场景重新加载时的状态恢复

---

## 验收标准

### 功能完整性
- [ ] TopBar在角色选择后显示
- [ ] 血条正确显示当前/最大血量
- [ ] 血条实时更新（受伤时减少）
- [ ] 跨场景保持显示（地图、战斗）
- [ ] 技能按钮可点击
- [ ] 点击按钮打开技能面板
- [ ] 技能面板显示所有已获得的技能
- [ ] 面板可关闭

### 体验流畅性
- [ ] 场景切换时TopBar不闪烁
- [ ] 血条更新流畅
- [ ] 面板打开/关闭流畅
- [ ] 无性能问题

### 视觉质量
- [ ] UI布局美观
- [ ] 颜色搭配合理
- [ ] 字体清晰可读
- [ ] 和游戏整体风格统一

---

## 后续扩展方向

### 短期可选
- 在TopBar添加金币显示
- 添加关卡进度显示
- 添加快捷键支持（按Tab打开技能面板）

### 长期优化
- 技能面板支持技能详细预览
- 添加技能树可视化
- 支持技能拖拽排序
- 添加技能统计（使用次数等）

---

## 总结

**整个TopBar功能分为10个小步骤，循序渐进实施。**

**核心优势：**
- ✅ 复用现有的UIController架构
- ✅ 利用GameEventBus事件系统
- ✅ 集成SkillManager数据
- ✅ 最小改动，最大复用

**总工作量估计：**
- Unity配置：30-40分钟
- 代码编写：40-50分钟
- 测试优化：20-30分钟
- **总计：约1.5-2小时**

---

**计划文档已完成！准备开始实施了吗？从哪个阶段开始？** 🚀
