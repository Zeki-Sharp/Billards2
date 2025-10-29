# 执行顺序与初始化时机优化计划

## 📋 文档信息
- **创建日期**: 2024年12月
- **版本**: 1.0
- **状态**: 规划阶段
- **优先级**: ⭐⭐⭐⭐ (高)
- **难度**: 中等
- **目标**: 借鉴 GC2 的 `DefaultExecutionOrder` 设计，消除延迟初始化协程，提升系统可靠性

---

## 🎯 优化目标

### 核心问题
1. **延迟初始化不优雅**
   - 使用协程等待（`yield return null` 或 `WaitForSeconds`）
   - 不确定性：等待时间是否足够？
   - 难以调试：异步执行，问题难以定位 expansions
   - 性能浪费：可能等待时间超过必要

2. **OnManagerCreated 空实现**
   - 多个 Manager 的 `OnManagerCreated()` 只有注释
   - 真正的初始化逻辑在 `Start()` 或协程中
   - 违背了基类设计的初衷

3. **依赖关系混乱**
   - Manager 之间依赖顺序不明确
   - 使用 `FindFirstObjectByType` 可能找不到实例
   - 缺乏明确的初始化顺序保证

---

## 📊 当前问题统计

### 使用延迟初始化的 Manager

| Manager | 延迟方式 | 等待时间 | 依赖对象 |
|---------|---------|---------|---------|
| `PlayerPhaseController` | `yield return null` | 1帧 | `PlayerStateMachine` |
| `GameFlowController` | `WaitForSeconds(0.1f)` | 0.1秒 | `PlayerPhaseController`, `EnemyPhaseController` |
| `TimeManager` | `Start()` 中查找 | - | `GameFlowController`, `PlayerStateMachine` |
| `GameManager` | `Start()` 中初始化 | - | - |

### OnManagerCreated 空实现统计

```csharp
// ❌ 当前模式
protected override void OnManagerCreated()
{
    // PlayerPhaseController 初始化逻辑在延迟协程中  ← 空实现 + 注释
}

void Start()
{
    StartCoroutine(DelayedInitialization());  // ← 真正的初始化
}
```

**空实现列表**：
- `PlayerPhaseController` - 延迟协程初始化
- `GameFlowController` - 延迟协程初始化（0.1秒）
- `EnemyPhaseController` - Start 中初始化
- `GameManager` - Start 中初始化
- `TimeManager` - Start 中初始化

---

## 🔍 GC2 的解决方案分析

### 方案1：DefaultExecutionOrder ⭐⭐⭐⭐⭐ (最推荐)

**GC2 的实现**：
```csharp
// 定义执行顺序常量
public class ApplicationManager : Singleton<ApplicationManager>
{
    public const int EXECUTION_ORDER_DEFAULT = 0;
    public const int EXECUTION_ORDER_DEFAULT_LATER = EXECUTION_ORDER_DEFAULT + 1;
    public const int EXECUTION_ORDER_DEFAULT_EARLIER = EXECUTION_ORDER_DEFAULT - 1;
    
    public const int EXECUTION_ORDER_FIRST = EXECUTION_ORDER_DEFAULT - 50;
    public const int EXECUTION_ORDER_LAST = EXECUTION_ORDER_DEFAULT + 50;
}

// 使用示例
[DefaultExecutionOrder(ApplicationManager.EXECUTION_ORDER_FIRST)]
public class FoundationManager : Singleton<FoundationManager>
{
    protected override void OnCreate()
    {
        // 最先执行，可以安全初始化
    }
}

[DefaultExecutionOrder(ApplicationManager.EXECUTION_ORDER_DEFAULT)]
public class NormalManager : Singleton<NormalManager>
{
    protected override void OnCreate()
    {
        // FoundationManager 已初始化，可以安全访问
        var foundation = FoundationManager.Instance;
    }
}
```

**优势**：
- ✅ **确定性**：Unity 保证执行顺序
- ✅ **同步执行**：无异步问题
- ✅ **性能最优**：无协程开销
- ✅ **易于理解**：执行顺序清晰可见
- ✅ **编译时检查**：顺序在编译时确定

---

### 方案2：RuntimeInitializeOnLoadMethod

**GC2 的实现**：
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void OnSubsystemsInit()
{
    Instance.WakeUp();  // 强制初始化
}
```

**初始化时机选项**：
- `SubsystemRegistration` - 子系统注册时（最早）
- `BeforeSceneLoad` - 场景加载前
- `AfterSceneLoad` - 场景加载后（默认 Start 之前）
- `AfterAssembliesLoaded` - 程序集加载后
- `BeforeSplashScreen` - 启动画面前
- `AfterSplashScreen` - 启动画面后

**优势**：
- ✅ **时机精确**：多种初始化时机可选
- ✅ **无需等待**：Unity 保证时机
- ✅ **集中管理**：初始化逻辑清晰

**劣势**：
- ❌ 需要 `static` 方法，无法直接访问实例成员
- ❌ 只能用于强制初始化，不适用于依赖其他 Manager 的场景

---

### 方案3：事件驱动初始化

**GC2 的实现**：
```csharp
public class ManagerA : Singleton<ManagerA>
{
    protected override void OnCreate()
    {
        ManagerEvents.OnManagerAReady?.Invoke(this);
    }
}

public class ManagerB : Singleton<ManagerB>
{
    protected override void OnCreate()
    {
        ManagerEvents.OnManagerAReady += OnManagerAReady;
    }
    
    private void OnManagerAReady(ManagerA manager)
    {
        // 依赖就绪，开始初始化
    }
}
```

**优势**：
- ✅ **松耦合**：事件驱动
- ✅ **灵活性**：依赖关系清晰
- ✅ **可扩展**：易于添加新依赖

**劣势**：
- ❌ 复杂度较高
- ❌ 需要额外的事件系统
- ❌ 调试难度稍高

---

## 💡 推荐方案：DefaultExecutionOrder + 分层体系

### 执行顺序分层设计

```csharp
/// <summary>
/// Manager 执行顺序常量 - 参考 GC2 的设计
/// </summary>
public static class ManagerExecutionOrder
{
    // 基础层（最先执行）
    public const int CORE = -100;              // GameManager (物理设置等)
    
    // 系统层
    public const int SYSTEM = -50;             // SkillManager, EffectManager, WeakPointManager
    
    // 关卡层
    public const int LEVEL = -30;              // LevelManager, SkillSelectionManager
    
    // 控制层（依赖系统层）
    public const int CONTROLLER = 0;           // GameFlowController, PlayerPhaseController, EnemyPhaseController
    
    // 工具层（依赖控制层）
    public const int UTILITY = 10;             // TimeManager, DamageTextManager
    
    // UI 层（最后，依赖所有系统）
    public const int UI = 50;                  // UIController
    
    // 组件层（不是 Manager，但要依赖 Manager）
    public const int COMPONENT = 100;          // Player, Enemy, PlayerStateMachine
}
```

### 依赖关系图

```
CORE (-100)
  └── GameManager
      └── 设置全局物理参数

SYSTEM (-50)
  ├── SkillManager
  ├── EffectManager
  ├── WeakPointManager
  └── TurnPenaltyManager

LEVEL (-30)
  ├── LevelManager (依赖 System)
  └── SkillSelectionManager (依赖 System)

CONTROLLER (0)
  ├── GameFlowController (依赖 System)
  ├── PlayerPhaseController (依赖 System)
  └── EnemyPhaseController (依赖 System)

UTILITY (10)
  └── TimeManager (依赖 Controller)

UI (50)
  └── UIController (依赖所有)

COMPONENT (100)
  ├── PlayerStateMachine (依赖 Controller)
  ├── Player (依赖 Controller, System)
  └── Enemy (依赖 Controller, System)
```

---

## 📝 迁移步骤

### 阶段1：定义执行顺序常量

**目标**：创建执行顺序常量文件

**文件**：`Assets/Scripts/Core/Manager/ManagerExecutionOrder.cs`

```csharp
/// <summary>
/// Manager 执行顺序常量
/// Unity 的 DefaultExecutionOrder 特性使用这些常量来确保初始化顺序
/// 
/// 【使用规则】：
///#define EXECUTION_ORDER_BEFORE(managerOrder) managerOrder - 1
///#define EXECUTION_ORDER_AFTER(managerOrder) managerOrder + 1
/// 
/// 【示例】：
/// [DefaultExecutionOrder(ManagerExecutionOrder.CORE)]
/// public class GameManager : SingletonManager<GameManager> { }
/// </summary>
public static class ManagerExecutionOrder
{
    // 基础层（最先执行，-100）
    public const int CORE = -100;
    
    // 系统层（-50）
    public const int SYSTEM = -50;
    
    // 关卡层（-30）
    public const int LEVEL = -30;
    
    // 控制层（0）
    public const int CONTROLLER = 0;
    
    // 工具层（10）
    public const int UTILITY = 10;
    
    // UI 层（50）
    public const int UI = 50;
    
    // 组件层（100，非 Manager）
    public const int COMPONENT = 100;
    
    // 辅助方法：在某个顺序之前
    public static int Before(int order) => order - 1;
    
    // 辅助方法：在某个顺序之后
    public static int After(int order) => order + 1;
}
```

---

### 阶段2：逐个迁移 Manager

#### 2.1 GameManager (CORE, -100)

**当前问题**：
- `OnManagerCreated()` 空实现
- 初始化逻辑在 `Start()` 中

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.CORE)]
public class GameManager : SingletonManager<GameManager>
{
    protected override void Awake()
    {
        base.Awake(); // 必须先调用基类
        // 设置全局物理参数（在 Awake 中即可，因为执行顺序最早）
        Physics2D.gravity = Vector2.zero;
        if (EnableDebugLog)
        {
            Debug.Log("GameManager: 已禁用全局重力");
        }
    }
    
    protected override void OnManagerCreated()
    {
        // ✅ 现在可以在这里初始化，因为执行顺序最早
        InitializeGameState();
    }
    
    // 移除 Start() 中的初始化逻辑
}
```

**检查点**：
- ✅ `Physics2D.gravity` 在 `Awake` 中设置
- ✅ `InitializeGameState()` 移到 `OnManagerCreated`
- ✅ 移除 `Start()` 中的重复初始化

---

#### 2.2 System 层 Manager (SYSTEM, -50)

**包括**：
- `SkillManager`
- `EffectManager`
- `WeakPointManager`
- `TurnPenaltyManager`

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class SkillManager : SingletonManager<SkillManager>
{
    protected override void OnManagerCreated()
    {
        // ✅ GameManager 已初始化，可以直接初始化
        // 移除延迟协程
        InitializeSkillManager();
    }
    
    // 移除 Start() 和 DelayedInitialization()
}
```

**检查点**：
- ✅ 移除所有延迟初始化协程
- ✅ 逻辑移到 `OnManagerCreated`
- ✅ 确保不依赖 CONTROLLER 层

---

#### 2.3 Level 层 Manager (LEVEL, -30)

**包括**：
- `LevelManager`
- `SkillSelectionManager`

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.LEVEL)]
public class LevelManager : SingletonManager<LevelManager>
{
    protected override void OnManagerCreated()
    {
        // ✅ System 层已初始化，可以访问 SkillManager 等
        // 订阅事件（不依赖其他 Manager 的实例化）
        GameEventBus.OnGameRestart += ResetState;
        
        // 场景相关的初始化在 Start() 中处理（因为需要场景加载）
        // 但 Manager 本身的初始化可以在这里完成
    }
    
    void Start()
    {
        // 场景加载后初始化（需要场景对象）
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadCurrentSceneLevel();
    }
}
```

**检查点**：
- ✅ Manager 初始化移到 `OnManagerCreated`
- ✅ 场景相关初始化保留在 `Start()`

---

#### 2.4 Controller 层 (CONTROLLER, 0)

**包括**：
- `GameFlowController`
- `PlayerPhaseController`
- `EnemyPhaseController`

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class GameFlowController : SingletonManager<GameFlowController>
{
    protected override void OnManagerCreated()
    {
        // ✅ System 和 Level 层已初始化
        // ✅ 可以直接访问其他 Controller 的 Instance
        playerPhaseController = PlayerPhaseController.Instance;
        enemyPhaseController = EnemyPhaseController.Instance;
        
        // 验证依赖
        if (playerPhaseController == null || enemyPhaseController == null)
        {
            Debug.LogError("GameFlowController: 依赖的 Controller 未找到！");
            return;
        }
        
        // 直接初始化，无需延迟
        InitializeControllers();
    }
    
    // ❌ 移除 Start() 和 DelayedInitialization()
}
```

**PlayerPhaseController 迁移**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerPhaseController : SingletonManager<PlayerPhaseController>
{
    protected override void OnManagerCreated()
    {
        // ⚠️ 注意：PlayerStateMachine 是 COMPONENT 层，还未初始化
        // 但可以通过 FindFirstObjectByType 查找（对象已存在，只是未初始化）
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        
        if (playerStateMachine == null)
        {
            Debug.LogError("PlayerPhaseController: PlayerStateMachine 未找到！");
            return;
        }
        
        // 初始化 Controller（不调用 PlayerStateMachine 的方法）
        InitializeController();
    }
    
    // ❌ 移除 Start() 和 DelayedInitialization()
    
    // ⚠️ 注意：如果需要在 PlayerStateMachine 初始化后调用，可以使用事件
}
```

**检查点**：
- ✅ 移除所有延迟初始化协程
- ✅ 在 `OnManagerCreated` 中获取依赖
- ✅ 验证依赖是否存在（null 检查）
- ⚠️ 处理跨层依赖（Controller → Component）

---

#### 2.5 Utility 层 (UTILITY, 10)

**包括**：
- `TimeManager`

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.UTILITY)]
public class TimeManager : SingletonManager<TimeManager>
{
    protected override void OnManagerCreated()
    {
        // ✅ Controller 层已初始化，可以安全访问
        gameFlowController = GameFlowController.Instance;
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        
        // 验证依赖
        if (gameFlowController == null)
        {
            Debug.LogError("TimeManager: GameFlowController 未找到！");
        }
        
        if (playerStateMachine == null)
        {
            Debug.LogError("TimeManager: PlayerStateMachine 未找到！");
        }
        
        // 订阅事件
        SubscribeToEvents();
    }
    
    // ❌ 移除 Start() 中的查找逻辑
}
```

**检查点**：
- ✅ 依赖查找移到 `OnManagerCreated`
- ✅ 移除 `Start()` 中的初始化

---

#### 2.6 UI 层 (UI, 50)

**包括**：
- `UIController`

**迁移方案**：
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.UI)]
public class UIController : SingletonManager<UIController>
{
    protected override void OnManagerCreated()
    {
        // ✅ 所有 Manager 都已初始化，可以安全访问
        // ✅ UI 初始化逻辑可以在这里完成
        InitializeUI();
        SubscribeToEvents();
    }
    
    // ❌ 移除 Start() 中的初始化
}
```

**检查点**：
- ✅ 所有 UI 初始化移到 `OnManagerCreated`
- ✅ 确保依赖的所有 Manager 都已就绪

---

#### 2.7 Component 层（非 Manager）

**包括**：
- `PlayerStateMachine`
- `Player`
- `Enemy`

**方案**：为关键组件添加执行顺序

```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.COMPONENT)]
public class PlayerStateMachine : MonoBehaviour
{
    void Awake()
    {
        // ✅ 所有 Manager 都已初始化，可以安全访问
        // ✅ 这里可以获取 Manager 引用
    }
}
```

---

### 阶段3：处理跨层依赖

**问题**：Controller 层需要访问 Component 层（如 `PlayerStateMachine`）

**解决方案1：延迟初始化回调**
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerPhaseController : SingletonManager<PlayerPhaseController>
{
    protected override void OnManagerCreated()
    {
        // 查找对象（对象已存在，但可能未初始化）
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        
        if (playerStateMachine != null)
        {
            // 订阅初始化完成事件
            PlayerStateMachine.OnInitialized += OnPlayerStateMachineReady;
        }
    }
    
    private void OnPlayerStateMachineReady()
    {
        // PlayerStateMachine 初始化完成，开始使用
        StartPlayerPhase();
    }
}
```

**解决方案2：保留 Start() 中的场景相关初始化**
```csharp
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerPhaseController : SingletonManager<PlayerPhaseController>
{
    protected override void OnManagerCreated()
    {
        // Manager 相关的初始化（订阅事件等）
        GameEventBus.OnGameRestart += ResetState;
    }
    
    void Start()
    {
        // 场景对象相关的初始化（需要等待场景加载）
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        InitializeController();
        
        // 但不再需要延迟协程，因为执行顺序已保证
    }
}
```

**推荐**：方案2更简单，因为 `Start()` 在 `Awake()` 之后执行，此时所有 Manager 的 `Awake()` 都已执行完成。

---

## ✅ 迁移检查清单

### 每个 Manager 迁移后检查

- [ ] 添加 `[DefaultExecutionOrder(...)]` 特性
- [ ] 移除延迟初始化协程（`DelayedInitialization`）
- [ ] 移除 `Start()` 中的初始化逻辑（除非是场景相关的）
- [ ] 将所有初始化逻辑移到 `OnManagerCreated()`
- [ ] 验证依赖是否存在（null 检查）
- [ ] 添加错误日志（如果依赖未找到）
- [ ] 测试功能是否正常

### 迁移顺序建议

1. ✅ **GameManager** (CORE) - 最先，无依赖
2. ✅ **System 层** (SYSTEM) - 只依赖 CORE
3. ✅ **Level 层** (LEVEL) - 依赖 SYSTEM
4. ✅ **Controller 层** (CONTROLLER) - 依赖 SYSTEM 和 LEVEL
5. ✅ **Utility 层** (UTILITY) - 依赖 CONTROLLER
6. ✅ **UI 层** (UI) - 依赖所有
7. ✅ **Component 层** (COMPONENT) - 依赖所有 Manager

---

## 🧪 测试计划

### 单元测试（手动）

1. **初始化顺序测试**
   - [ ] 确认所有 Manager 按正确顺序初始化
   - [ ] 确认无空引用异常
   - [ ] 确认无初始化时序问题

2. **功能回归测试**
   - [ ] GameManager 物理设置正确
   - [ ] 游戏流程正常（PlayerPhase → EnemyPhase）
   - [ ] 技能系统正常
   - [ ] UI 显示正常
   - [ ] 时间管理正常

3. **场景切换测试**
   - [ ] 场景切换后 Manager 正常
   - [ ] 跨场景 Manager 正常保留
   - [ ] 新场景的 Component 能正确访问 Manager

### 调试工具

**添加执行顺序可视化工具**：
```csharp
#if UNITY_EDITOR
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void LogExecutionOrder()
{
    Debug.Log($"[执行顺序] GameManager: {GetExecutionOrder(typeof(GameManager))}");
    Debug.Log($"[执行顺序] SkillManager: {GetExecutionOrder(typeof(SkillManager))}");
    // ... 记录所有 Manager
}
#endif
```

---

## 📊 预期收益

### 代码质量提升

- ✅ **消除延迟协程**：减少异步代码，降低复杂度
- ✅ **明确的执行顺序**：通过特性声明，一目了然
- ✅ **更可靠的初始化**：Unity 保证顺序，无需猜测
- ✅ **更好的调试体验**：同步执行，问题易定位

### 性能提升

- ✅ **减少协程开销**：无 `yield` 和 `WaitForSeconds`
- ✅ **更快的启动**：无需等待固定时间
- ✅ **内存优化**：无协程对象创建

### 可维护性提升

- ✅ **代码更清晰**：初始化逻辑集中在 `OnManagerCreated`
- ✅ **依赖关系明确**：通过执行顺序体现
- ✅ **易于扩展**：新 Manager 只需选择合适的执行顺序

---

## ⚠️ 注意事项

### 1. 跨层依赖处理

**问题**：Controller 层需要访问 Component 层

**解决**：
- 场景对象在 `Start()` 中查找（此时所有 Manager 已初始化）
- 或使用事件驱动初始化

### 2. 场景加载时机

**问题**：场景对象在场景加载后才存在

**解决**：
- Manager 初始化在 `OnManagerCreated`（场景无关）
- 场景对象查找在 `Start()`（场景加载后）

### 3. DontDestroyOnLoad 处理

**问题**：跨场景 Manager 在场景切换时的初始化

**解决**：
- `OnManagerCreated` 只在首次创建时调用
- 场景相关初始化在 `Start()` 中，每次场景加载都会调用

### 4. 向后兼容

**问题**：迁移过程中可能出现空引用

**解决**：
- 添加充分的 null 检查
- 添加错误日志
- 分阶段迁移，充分测试

---

## 📚 参考资料

### Unity 文档
- [DefaultExecutionOrder Attribute](https://docs.unity3d.com/ScriptReference/DefaultExecutionOrder.html)
- [Script Execution Order](https://docs.unity3d.com/Manual/class-MonoManager.html#Execution%20Order)
- [RuntimeInitializeOnLoadMethod Attribute](https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html)

### GC2 实现参考
- `Assets/Plugins/GameCreator/Packages/Core/Runtime/Common/Managers/ApplicationManager.cs`
- GC2 的执行顺序常量定义
- GC2 Manager 的使用示例

---

## 🎯 总结

通过引入 `DefaultExecutionOrder` 特性，我们可以：

1. ✅ **消除延迟初始化协程** - 更可靠的初始化
2. ✅ **明确执行顺序** - 通过特性声明，一目了然
3. ✅ **提升代码质量** - 更清晰、更易维护
4. ✅ **改善性能** - 减少协程开销
5. ✅ **提升调试体验** - 同步执行，问题易定位

**下一步行动**：
1. 创建 `ManagerExecutionOrder` 常量文件
2. 按顺序逐个迁移 Manager
3. 充分测试每个 Manager
4. 记录遇到的问题和解决方案

---

**文档状态**: 📝 规划完成，等待实施

