# UI架构重构计划

## 项目概述

将当前分散的UI管理模式重构为统一的中央管理架构，建立可扩展的UI系统框架，为后续不断增加的UI界面提供良好的基础。

## 背景和问题分析

### 当前架构现状

**UIController.cs**：
- 职责：仅持有GameUIPanel引用
- 生命周期：随场景创建和销毁
- 管理能力：无实际管理功能

**各UI面板现状**：
- SkillSelectionUI：自己订阅事件，自己控制显示/隐藏
- VictoryPanel：自己订阅事件，自己控制显示/隐藏，自己暂停游戏
- GameOverPanel：自己订阅事件，自己控制显示/隐藏，自己暂停游戏
- GameUIPanel：只管理血条显示

### 存在的问题

1. **职责混乱**
   - Panel既负责UI显示，又负责业务逻辑判断
   - Panel自己决定何时显示/隐藏
   - Panel自己控制游戏暂停/恢复

2. **缺乏统一管理**
   - 没有中央控制器协调各面板
   - 无法处理面板互斥（例如同时只显示一个弹窗）
   - 无法统一处理面板层级和优先级

3. **扩展性差**
   - 新增面板需要重复编写相同逻辑
   - 难以实现统一的UI动画、音效等
   - 难以实现UI历史栈（返回上一个面板）

4. **维护困难**
   - 面板代码重复度高
   - 统一修改需要改动多个文件
   - 调试困难（不知道哪个面板在控制游戏状态）

## 重构目标

### 核心目标

1. **建立中央管理系统**：UIController作为唯一的UI管理入口
2. **职责分离**：Panel只负责UI显示，逻辑由UIController处理
3. **统一生命周期**：所有UI通过UIController统一管理生命周期
4. **提高可扩展性**：新增面板只需注册到UIController
5. **保持事件驱动**：保留GameEventBus机制，增加管理层

### 设计原则

1. **单一职责原则**：每个组件只负责一件事
2. **开闭原则**：对扩展开放，对修改关闭
3. **最小改动原则**：尽量保留现有可用代码
4. **渐进式重构**：不影响现有功能的前提下逐步优化

## 目标架构设计

### 架构层次

```
UIController (中央管理层)
    ├── 单例模式，DontDestroyOnLoad
    ├── 持有所有UI面板引用
    ├── 统一管理面板显示/隐藏
    ├── 统一处理游戏暂停/恢复
    └── 提供面板注册和查询机制
    
BasePanel (面板基类)
    ├── 定义面板通用行为
    ├── 标准化生命周期方法
    ├── 提供Show/Hide/Update接口
    └── 统一动画、音效处理
    
具体面板 (继承BasePanel)
    ├── 只负责UI元素的显示和更新
    ├── 接收UIController传递的数据
    ├── 通过UIController显示/隐藏
    └── UI交互通过UIController回调
```

### 核心组件职责

#### UIController 职责

**初始化管理**：
- 单例实例化
- DontDestroyOnLoad设置
- 面板注册和初始化
- EventSystem管理

**面板管理**：
- 显示面板（ShowPanel）
- 隐藏面板（HidePanel）
- 切换面板（SwitchPanel）
- 隐藏所有面板（HideAllPanels）

**状态管理**：
- 当前显示的面板跟踪
- 面板显示历史栈（可选）
- 游戏暂停/恢复状态控制

**面板互斥**：
- 弹窗类面板互斥（同时只显示一个）
- 游戏内UI和弹窗分离管理
- 面板层级和优先级处理

**事件协调**：
- 订阅GameEventBus事件
- 根据事件决定显示哪个面板
- 传递数据给面板

#### BasePanel 职责

**生命周期管理**：
- OnInit：面板初始化（只调用一次）
- OnShow：面板显示时调用
- OnHide：面板隐藏时调用
- OnUpdate：更新面板数据

**状态管理**：
- 面板是否已初始化
- 面板当前显示状态
- 面板类型标识（游戏内UI/弹窗等）

**通用功能**：
- 显示/隐藏动画接口
- 音效播放接口
- 按钮事件统一处理

#### 具体面板职责

**数据接收**：
- 提供公共方法接收UIController传递的数据
- 不自己获取数据，由UIController提供

**UI更新**：
- 根据接收的数据更新UI元素
- 处理用户交互（按钮点击等）

**回调通知**：
- 通过事件或回调通知UIController用户操作
- 不直接处理业务逻辑

## 面板分类系统

### 面板类型定义

**游戏内UI（HUD）**：
- 特点：始终显示，不互斥，不暂停游戏
- 例如：GameUIPanel（血条）、技能CD显示等

**弹窗UI（Popup）**：
- 特点：弹出显示，互斥，通常暂停游戏
- 例如：VictoryPanel、GameOverPanel

**全屏UI（FullScreen）**：
- 特点：全屏显示，互斥，通常暂停游戏
- 例如：SkillSelectionUI、设置界面、商店界面

**提示UI（Tips）**：
- 特点：短暂显示，不互斥，不暂停游戏
- 例如：伤害数字、提示文本等

### 面板管理规则

**互斥规则**：
- Popup类面板同时只能显示一个
- FullScreen类面板同时只能显示一个
- HUD类面板不互斥
- Tips类面板不互斥

**暂停规则**：
- Popup和FullScreen默认暂停游戏
- HUD和Tips不暂停游戏
- 可通过参数覆盖默认行为

**层级规则**：
- Tips > Popup > FullScreen > HUD
- 通过Canvas Sort Order实现

## 数据流向设计

### 显示面板流程

```
游戏事件发生
    ↓
GameEventBus触发事件
    ↓
UIController监听到事件
    ↓
UIController准备面板数据
    ↓
UIController调用ShowPanel(panel, data)
    ↓
UIController检查面板互斥规则
    ↓
UIController隐藏冲突面板（如果有）
    ↓
UIController设置游戏暂停（如果需要）
    ↓
UIController调用panel.OnShow(data)
    ↓
Panel更新UI显示
```

### 隐藏面板流程

```
用户点击按钮（如Restart）
    ↓
Panel的按钮回调触发
    ↓
Panel通知UIController（通过事件或回调）
    ↓
UIController处理业务逻辑（如清理数据、切换场景）
    ↓
UIController调用HidePanel(panel)
    ↓
UIController恢复游戏状态（如果之前暂停了）
    ↓
UIController调用panel.OnHide()
    ↓
Panel执行隐藏动画（如果有）
```

## 实施计划

### 第一阶段：创建基础架构

**目标**：建立BasePanel和升级UIController

**任务**：
1. 创建BasePanel抽象类
   - 定义面板生命周期接口
   - 定义面板类型枚举
   - 实现通用Show/Hide逻辑

2. 重构UIController
   - 添加单例模式
   - 添加DontDestroyOnLoad
   - 添加面板注册机制
   - 添加面板管理方法
   - 订阅必要的GameEventBus事件

3. 创建配置文件或枚举
   - 定义面板类型（PanelType）
   - 定义面板互斥规则

**输出**：
- BasePanel.cs
- UIController.cs（重构版）
- UIPanelType.cs（枚举定义）

### 第二阶段：重构现有面板

**目标**：让现有面板继承BasePanel并适配新架构

**任务**：
1. 重构VictoryPanel
   - 继承BasePanel
   - 移除事件订阅逻辑
   - 实现OnShow/OnHide方法
   - 保留UI更新逻辑

2. 重构GameOverPanel
   - 同VictoryPanel

3. 适配SkillSelectionUI
   - 评估是否需要继承BasePanel
   - 调整显示逻辑走UIController

4. 保持GameUIPanel
   - 暂不修改，作为HUD类面板独立运行
   - 后续可选择性继承BasePanel

**输出**：
- VictoryPanel.cs（重构版）
- GameOverPanel.cs（重构版）
- SkillSelectionUI.cs（可选调整）

### 第三阶段：Unity配置和集成

**目标**：在Unity中配置新架构

**配置方案**：采用混合方案（预放置 + 动态加载）

- **预放置面板**（在CharacterSelection场景，跨场景保留）
  - GameUIPanel：游戏内血条，高频使用
  - SkillSelectionUI：技能选择界面，中高频使用
  
- **动态加载面板**（放在Resources/UI/目录，按需加载）
  - VictoryPanel：胜利界面，低频使用
  - GameOverPanel：失败界面，低频使用
  - 未来新增UI默认动态加载

- **配置结构**：
  ```
  CharacterSelection场景
  └── UIRoot (DontDestroyOnLoad)
      ├── UIController
      ├── HUD Canvas → GameUIPanel
      ├── FullScreen Canvas → SkillSelectionUI
      └── Popup Canvas (空，用于动态加载)
  
  Resources/UI/Popups/
  ├── VictoryPanel.prefab
  └── GameOverPanel.prefab
  ```

**任务**：
1. 创建UI根对象
   - 在CharacterSelection场景创建UIRoot对象
   - 添加UIController组件
   - 设置Canvas和EventSystem

2. 组织UI层级
   - HUD Canvas（Sort Order: 0）
   - FullScreen Canvas（Sort Order: 100）
   - Popup Canvas（Sort Order: 200）

3. 配置预放置面板
   - 将GameUIPanel拖到HUD Canvas下
   - 将SkillSelectionUI拖到FullScreen Canvas下
   - 配置UIController的面板引用
   - 设置面板初始状态

4. 准备动态加载面板
   - 创建Resources/UI/Popups目录
   - 将VictoryPanel和GameOverPanel预制体放入
   - 在UIController中配置加载路径

5. 场景配置
   - 确保UIRoot在第一个场景中
   - 确认DontDestroyOnLoad正常工作

**输出**：
- 配置好的CharacterSelection场景
- 正确的UI层级结构

### 第四阶段：测试和优化

**目标**：验证架构可用性并优化

**任务**：
1. 功能测试
   - 测试胜利界面显示
   - 测试失败界面显示
   - 测试重启功能
   - 测试面板互斥

2. 跨场景测试
   - 测试场景切换后UI是否保持
   - 测试数据统计准确性

3. 性能测试
   - 检查是否有内存泄漏
   - 检查事件订阅是否正确取消

4. 代码优化
   - 移除重复代码
   - 优化面板切换性能
   - 添加必要的错误处理

**输出**：
- 测试报告
- 优化后的代码

## 扩展规划

### 新增面板流程

**步骤**：
1. 创建面板脚本继承BasePanel
2. 实现OnShow/OnHide/OnUpdate方法
3. 在Unity中创建面板预制体
4. 在UIController中注册面板引用
5. 在需要的地方调用UIController.ShowPanel()

**示例场景**：
- 暂停菜单：PausePanel（Popup类型）
- 设置界面：SettingsPanel（FullScreen类型）
- 商店界面：ShopPanel（FullScreen类型）
- 提示文本：ToastPanel（Tips类型）

### 高级功能扩展

**面板动画系统**：
- 在BasePanel中定义动画接口
- 统一管理淡入淡出、缩放等效果

**面板历史栈**：
- UIController维护面板显示历史
- 支持返回上一个面板功能

**面板预加载**：
- 常用面板在启动时预加载
- 减少运行时实例化开销

**面板池化**：
- 频繁显示/隐藏的面板使用对象池
- 提高性能

## 技术约束和注意事项

### 兼容性考虑

1. **与现有系统集成**
   - 保持GameEventBus事件系统不变
   - UIController作为事件的统一接收者
   - 不破坏现有Manager的工作方式

2. **向后兼容**
   - 允许Panel仍可以订阅事件（兼容期）
   - 逐步迁移，不强制一次性完成

3. **性能考虑**
   - DontDestroyOnLoad对象不要过多
   - 及时清理不用的UI资源
   - 注意Canvas合批优化

### 潜在问题和解决方案

**问题1：EventSystem冲突**
- 现象：场景切换时出现多个EventSystem
- 解决：UIController统一管理EventSystem，确保只有一个

**问题2：Canvas层级混乱**
- 现象：面板显示顺序不正确
- 解决：明确定义Canvas Sort Order规则

**问题3：面板引用丢失**
- 现象：场景切换后UIController的面板引用为空
- 解决：使用Resources.Load或Addressables动态加载

**问题4：内存泄漏**
- 现象：面板没有正确销毁，事件没有取消订阅
- 解决：在BasePanel中统一管理生命周期和事件订阅

## 成功标准

### 功能完整性

- [ ] UIController作为单例正常工作
- [ ] 所有面板通过UIController显示/隐藏
- [ ] 面板互斥规则正确执行
- [ ] 游戏暂停/恢复正常工作
- [ ] 跨场景UI保持正常

### 代码质量

- [ ] 代码结构清晰，职责分离
- [ ] 没有代码重复
- [ ] 完整的错误处理
- [ ] 完整的XML注释

### 扩展性

- [ ] 新增面板只需3步（创建、继承、注册）
- [ ] 面板类型易于扩展
- [ ] 支持自定义面板行为

### 性能

- [ ] 无内存泄漏
- [ ] 面板切换流畅（无卡顿）
- [ ] 事件订阅正确管理

## 风险评估

### 高风险

**风险**：大规模重构可能导致现有功能失效
**应对**：
- 渐进式重构，保留向后兼容
- 每个阶段充分测试
- 保留Git提交历史，可随时回滚

### 中风险

**风险**：UIController职责过重，成为God Object
**应对**：
- 明确定义职责边界
- 考虑将部分功能拆分到Helper类
- 定期Review代码结构

### 低风险

**风险**：面板数量增多后管理复杂度增加
**应对**：
- 使用字典或配置文件管理面板
- 考虑引入面板配置表
- 考虑使用UI框架（如FairyGUI、UGUI框架等）

## 总结

本重构计划旨在建立一个**可扩展、易维护、职责清晰**的UI架构系统。通过引入UIController中央管理器和BasePanel基类，将UI系统从"分散管理"升级为"统一管理"，为后续不断增加的UI需求提供坚实的基础。

重构将分四个阶段渐进式进行，每个阶段都有明确的目标和输出，确保在不影响现有功能的前提下完成升级。

重构完成后，新增UI界面将变得非常简单：**创建 → 继承 → 注册 → 使用**，大大提高开发效率。

