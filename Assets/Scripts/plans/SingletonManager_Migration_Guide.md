# SingletonManager 迁移指南

## 概述

本文档说明如何将现有的 Manager 类迁移到新的 `SingletonManager<T>` 基类。

---

## 迁移步骤

### 步骤 1: 修改类声明

**迁移前：**
```csharp
public class MyManager : MonoBehaviour
{
    public static MyManager Instance { get; private set; }
}
```

**迁移后：**
```csharp
public class MyManager : SingletonManager<MyManager>
{
    // 移除 Instance 属性（由基类提供）
}
```

---

### 步骤 2: 移除重复的单例代码

**迁移前：**
```csharp
void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 初始化逻辑
        SubscribeToEvents();
    }
    else
    {
        Destroy(gameObject);
        return;
    }
}
```

**迁移后：**
```csharp
protected override void OnManagerCreated()
{
    // 只保留初始化逻辑
    SubscribeToEvents();
}

// 如果需要保留 Awake 做其他事情
protected override void Awake()
{
    base.Awake();  // 必须先调用基类
    // 其他非单例相关的逻辑
}
```

---

### 步骤 3: 移动清理代码

**迁移前：**
```csharp
void OnDestroy()
{
    // 清理代码
    UnsubscribeFromEvents();
}
```

**迁移后：**
```csharp
protected override void OnManagerDestroyed()
{
    // 清理代码
    UnsubscribeFromEvents();
}

// 如果需要保留 OnDestroy 做其他事情
protected override void OnDestroy()
{
    base.OnDestroy();  // 必须先调用基类
    // 其他清理逻辑
}
```

---

### 步骤 4: 配置选项（可选）

如果需要自定义行为，可以重写配置属性：

```csharp
// 如果不需要跨场景保留
protected override bool PersistAcrossScenes => false;

// 如果需要启用调试日志
protected override bool EnableDebugLog => true;
```

---

## 完整迁移示例

### 迁移前：SkillManager（部分代码）

```csharp
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }
    
    [Header("调试设置")]
    public bool enableDebugLog = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            GameEventBus.OnGameRestart += ResetState;
            
            if (enableDebugLog)
            {
                Debug.Log("[SkillManager] 单例初始化完成");
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.Log("[SkillManager] 检测到重复实例，销毁");
            }
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        skillStateManager = FindFirstObjectByType<SkillStateManager>();
        ReinitializeSkillInstances();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        GameEventBus.OnGameRestart -= ResetState;
        UnsubscribeFromEvents();
    }
}
```

### 迁移后：SkillManager

```csharp
public class SkillManager : SingletonManager<SkillManager>
{
    [Header("调试设置")]
    public bool enableDebugLog = true;
    
    // 配置调试日志
    protected override bool EnableDebugLog => enableDebugLog;
    
    // Manager 创建时调用
    protected override void OnManagerCreated()
    {
        // 订阅游戏重启事件
        GameEventBus.OnGameRestart += ResetState;
        
        if (enableDebugLog)
        {
            Debug.Log("[SkillManager] 单例初始化完成");
        }
    }
    
    void Start()
    {
        // Start 逻辑保持不变
        skillStateManager = FindFirstObjectByType<SkillStateManager>();
        ReinitializeSkillInstances();
        SubscribeToEvents();
    }
    
    // Manager 销毁时调用
    protected override void OnManagerDestroyed()
    {
        // 取消订阅
        GameEventBus.OnGameRestart -= ResetState;
        UnsubscribeFromEvents();
    }
}
```

---

## 对比变化

### 移除的代码（✂️ 删除）
- ✂️ `public static XXXManager Instance { get; private set; }`
- ✂️ `Awake()` 中的 `if (Instance == null)` 检查
- ✂️ `Instance = this;`
- ✂️ `DontDestroyOnLoad(gameObject);`
- ✂️ `Destroy(gameObject);` 处理重复实例

### 新增的代码（➕ 添加）
- ➕ 继承 `SingletonManager<T>`
- ➕ `OnManagerCreated()` 方法（初始化）
- ➕ `OnManagerDestroyed()` 方法（清理，可选）
- ➕ `EnableDebugLog` 属性（可选）
- ➕ `PersistAcrossScenes` 属性（可选）

### 代码减少量
- 平均每个 Manager 减少 **15-20 行**重复代码
- 消除 **100%** 的单例模板代码

---

## 特殊情况处理

### 情况 1: Manager 不需要跨场景保留

```csharp
public class LevelManager : SingletonManager<LevelManager>
{
    // 设置为不跨场景保留
    protected override bool PersistAcrossScenes => false;
    
    protected override void OnManagerCreated()
    {
        // 初始化逻辑
    }
}
```

### 情况 2: 需要在 Awake 中做额外处理

```csharp
public class GameManager : SingletonManager<GameManager>
{
    protected override void Awake()
    {
        base.Awake();  // ⚠️ 必须先调用基类
        
        // 额外的 Awake 逻辑（非单例相关）
        Physics2D.gravity = Vector2.zero;
    }
    
    protected override void OnManagerCreated()
    {
        // 单例初始化逻辑
    }
}
```

### 情况 3: Manager 实现了接口

```csharp
// 完全兼容接口实现
public class WeakPointManager : SingletonManager<WeakPointManager>, IDamageModifier
{
    // 接口实现
    public EventPriority Priority => EventPriority.High;
    public string ModifierName => "弱点判定";
    public bool IsEnabled => isEnabled;
    
    public bool ProcessDamage(ref AttackData attackData)
    {
        // 实现逻辑
    }
    
    protected override void OnManagerCreated()
    {
        // 初始化逻辑
    }
}
```

### 情况 4: 使用 GetOrCreateInstance 的 Manager

**迁移前：**
```csharp
public static WeakPointManager GetOrCreateInstance()
{
    if (Instance != null)
        return Instance;
    
    GameObject managerObj = new GameObject("WeakPointManager");
    return managerObj.AddComponent<WeakPointManager>();
}
```

**迁移后：**
```csharp
// 直接使用基类提供的方法
// WeakPointManager.GetOrCreateInstance();
// 无需自己实现
```

---

## 迁移检查清单

在迁移每个 Manager 时，请确保：

- [ ] 类声明修改为 `: SingletonManager<T>`
- [ ] 移除 `Instance` 静态属性
- [ ] 移除 `Awake()` 中的单例代码
- [ ] 初始化逻辑移到 `OnManagerCreated()`
- [ ] 清理逻辑移到 `OnManagerDestroyed()`
- [ ] 如果需要，重写 `EnableDebugLog` 属性
- [ ] 如果需要，重写 `PersistAcrossScenes` 属性
- [ ] 测试 Manager 功能正常
- [ ] 测试场景切换正常
- [ ] 测试重复实例处理正常

---

## 迁移顺序建议

### 第一批：简单 Manager（测试迁移）
1. DamageTextManager
2. HoleManager
3. WallManager

### 第二批：中等复杂度 Manager
4. EffectManager
5. WeakPointManager
6. SkillStateManager

### 第三批：核心 Manager
7. SkillManager
8. GameManager
9. LevelManager
10. EnemyController

---

## 注意事项

### ⚠️ 必须注意

1. **调用顺序**
   - 如果重写 `Awake()`，**必须先调用** `base.Awake()`
   - 如果重写 `OnDestroy()`，**必须先调用** `base.OnDestroy()`

2. **Instance 访问**
   - 迁移后，`Instance` 仍然可以正常访问
   - `HasInstance` 可以安全检查实例是否存在

3. **应用退出**
   - 基类自动处理应用退出情况
   - 不需要手动检查 `isApplicationQuitting`

### ✅ 兼容性

- ✅ 完全向后兼容（外部代码无需修改）
- ✅ 支持接口实现
- ✅ 支持虚方法重写
- ✅ 支持多态

---

## 验证测试

迁移完成后，请进行以下测试：

1. **基本功能测试**
   - Manager 正常初始化
   - Manager 功能正常工作

2. **单例测试**
   - 只有一个实例存在
   - 重复实例被正确销毁

3. **场景切换测试**
   - 跨场景 Manager 保留
   - 非跨场景 Manager 销毁

4. **应用退出测试**
   - 退出时无错误日志
   - 清理逻辑正常执行

---

## 收益总结

### 代码质量
- ✅ 消除 15-20 行/每个 Manager 的重复代码
- ✅ 统一的生命周期管理
- ✅ 更清晰的代码结构

### 开发效率
- ✅ 新 Manager 只需继承基类
- ✅ 无需重复编写单例逻辑
- ✅ 减少出错可能

### 可维护性
- ✅ 统一的调试日志
- ✅ 统一的配置选项
- ✅ 更容易理解和修改

---

## 帮助和支持

如果在迁移过程中遇到问题：

1. 检查是否调用了 `base.Awake()` / `base.OnDestroy()`
2. 检查是否正确实现了 `OnManagerCreated()`
3. 查看控制台日志（启用 EnableDebugLog）
4. 参考本文档的完整示例

祝迁移顺利！🎯

