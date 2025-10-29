# TimeMode 使用指南

## 📘 概述

TimeMode 是一个轻量级的时间抽象结构，用于统一管理游戏时间和真实时间（不受暂停影响的时间）。

---

## 🎯 核心概念

### 两种时间模式

#### GameTime（游戏时间）
- **受 `Time.timeScale` 影响**
- 暂停时 → `deltaTime = 0`
- 用于：游戏逻辑、角色移动、技能冷却

#### UnscaledTime（真实时间）
- **不受 `Time.timeScale` 影响**
- 暂停时 → `deltaTime` 仍然正常
- 用于：UI动画、暂停菜单、加载界面

---

## 💻 基础使用

### 1. 添加 TimeMode 字段

```csharp
public class MyComponent : MonoBehaviour
{
    // 在 Inspector 中可配置
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    
    void Update()
    {
        // 使用 TimeMode 的 DeltaTime
        float deltaTime = timeMode.DeltaTime;
        
        // 你的逻辑
        position += velocity * deltaTime;
    }
}
```

### 2. TimeMode 提供的属性

```csharp
// 时间增量（最常用）
float deltaTime = timeMode.DeltaTime;
// GameTime → Time.deltaTime
// UnscaledTime → Time.unscaledDeltaTime

// 当前时间
float currentTime = timeMode.Time;
// GameTime → Time.time
// UnscaledTime → Time.unscaledTime

// 固定更新时间增量
float fixedDelta = timeMode.FixedDeltaTime;
// GameTime → Time.fixedDeltaTime
// UnscaledTime → Time.fixedUnscaledDeltaTime

// 时间缩放
float scale = timeMode.TimeScale;
// GameTime → Time.timeScale
// UnscaledTime → 1.0f
```

---

## 📋 使用场景

### 场景1：角色移动（GameTime）

```csharp
public class CharacterMovement : MonoBehaviour
{
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    [SerializeField] private float moveSpeed = 5f;
    
    void Update()
    {
        float deltaTime = timeMode.DeltaTime;
        
        Vector3 movement = GetInput() * moveSpeed * deltaTime;
        transform.position += movement;
        
        // 暂停时：deltaTime = 0 → movement = 0 → 角色不移动 ✅
    }
}
```

### 场景2：蓄力系统（GameTime）

```csharp
public class ChargeSystem : MonoBehaviour
{
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    [SerializeField] private float chargeSpeed = 1f;
    
    private float chargeProgress = 0f;
    
    void Update()
    {
        if (isCharging)
        {
            float deltaTime = timeMode.DeltaTime;
            chargeProgress += chargeSpeed * deltaTime;
            
            // 暂停时：deltaTime = 0 → chargeProgress 不增长 ✅
        }
    }
}
```

### 场景3：技能冷却（GameTime）

```csharp
public class Skill : MonoBehaviour
{
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    [SerializeField] private float cooldownDuration = 5f;
    
    private float lastUseTime = 0f;
    
    void UseSkill()
    {
        lastUseTime = timeMode.Time;
    }
    
    bool IsOnCooldown()
    {
        float elapsedTime = timeMode.Time - lastUseTime;
        return elapsedTime < cooldownDuration;
        
        // 暂停时：timeMode.Time 不增长 → elapsedTime 不增长 → 冷却停止 ✅
    }
}
```

### 场景4：UI动画（UnscaledTime）

```csharp
public class UIFadeAnimation : MonoBehaviour
{
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.UnscaledTime);
    [SerializeField] private float fadeSpeed = 2f;
    
    private float alpha = 0f;
    
    void Update()
    {
        float deltaTime = timeMode.DeltaTime;
        alpha += fadeSpeed * deltaTime;
        
        // 暂停时：deltaTime 仍然正常 → alpha 继续增长 → UI继续动画 ✅
    }
}
```

### 场景5：暂停菜单动画（UnscaledTime）

```csharp
public class PauseMenuAnimation : MonoBehaviour
{
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.UnscaledTime);
    
    void OnEnable()
    {
        StartCoroutine(SlideIn());
    }
    
    IEnumerator SlideIn()
    {
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            // 使用 UnscaledTime 的 DeltaTime
            elapsed += timeMode.DeltaTime;
            
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            
            yield return null;
            
            // 暂停时：仍然播放动画 ✅
        }
    }
}
```

---

## ⚠️ 常见错误

### 错误1：仍然手动检查暂停

```csharp
// ❌ 错误做法
void Update()
{
    if (GameManager.Instance.IsGamePaused) return;
    
    float deltaTime = timeMode.DeltaTime;
    position += velocity * deltaTime;
}

// ✅ 正确做法（不需要检查）
void Update()
{
    float deltaTime = timeMode.DeltaTime;
    position += velocity * deltaTime;
    // 暂停时 deltaTime=0，自动停止
}
```

### 错误2：混用 Time.deltaTime 和 TimeMode.DeltaTime

```csharp
// ❌ 错误做法
void Update()
{
    float deltaTime = timeMode.DeltaTime;
    
    // 某些地方用了 Time.deltaTime，会导致暂停逻辑不一致
    chargeProgress += chargeSpeed * deltaTime;
    cooldown -= Time.deltaTime; // ← 错误！
}

// ✅ 正确做法（统一使用 TimeMode）
void Update()
{
    float deltaTime = timeMode.DeltaTime;
    
    chargeProgress += chargeSpeed * deltaTime;
    cooldown -= deltaTime; // ← 正确
}
```

### 错误3：协程中使用错误的 WaitForSeconds

```csharp
// ❌ 错误做法（GameTime 逻辑用了 WaitForSecondsRealtime）
IEnumerator AttackCooldown()
{
    // 这会导致暂停时冷却仍然继续！
    yield return new WaitForSecondsRealtime(cooldownTime);
}

// ✅ 正确做法（根据 TimeMode 选择）
IEnumerator AttackCooldown()
{
    if (timeMode.UpdateTime == TimeMode.UpdateMode.GameTime)
    {
        yield return new WaitForSeconds(cooldownTime); // 受暂停影响
    }
    else
    {
        yield return new WaitForSecondsRealtime(cooldownTime); // 不受暂停影响
    }
}
```

### 错误4：缓存 DeltaTime 在 Start/Awake

```csharp
// ❌ 错误做法
float cachedDeltaTime;

void Start()
{
    cachedDeltaTime = timeMode.DeltaTime; // 错误！每帧都不一样
}

void Update()
{
    position += velocity * cachedDeltaTime; // 使用过时的值
}

// ✅ 正确做法（每帧获取）
void Update()
{
    float deltaTime = timeMode.DeltaTime; // 每帧获取最新值
    position += velocity * deltaTime;
}
```

---

## 🎨 设计模式

### 模式1：可配置的时间模式

```csharp
public class ConfigurableComponent : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    [SerializeField] private bool pauseWithGame = true;
    
    void Update()
    {
        if (pauseWithGame)
        {
            // 受暂停影响
            float deltaTime = timeMode.DeltaTime;
            UpdateLogic(deltaTime);
        }
        else
        {
            // 不受暂停影响
            UpdateLogic(Time.unscaledDeltaTime);
        }
    }
}
```

### 模式2：时间模式继承

```csharp
// 基类提供 TimeMode
public abstract class GameComponent : MonoBehaviour
{
    [SerializeField] protected TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    
    protected virtual void Update()
    {
        float deltaTime = timeMode.DeltaTime;
        OnUpdate(deltaTime);
    }
    
    protected abstract void OnUpdate(float deltaTime);
}

// 子类只需实现逻辑
public class MyComponent : GameComponent
{
    protected override void OnUpdate(float deltaTime)
    {
        // 自动使用父类的 TimeMode
        position += velocity * deltaTime;
    }
}
```

---

## 🔧 调试技巧

### 技巧1：可视化时间模式

```csharp
void OnGUI()
{
    if (!Application.isPlaying) return;
    
    GUILayout.Label($"Time Mode: {timeMode.UpdateTime}");
    GUILayout.Label($"DeltaTime: {timeMode.DeltaTime:F4}");
    GUILayout.Label($"Time: {timeMode.Time:F2}");
    GUILayout.Label($"TimeScale: {timeMode.TimeScale:F2}");
}
```

### 技巧2：运行时切换时间模式

```csharp
[ContextMenu("切换到 GameTime")]
void SwitchToGameTime()
{
    timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
    Debug.Log("切换到 GameTime 模式");
}

[ContextMenu("切换到 UnscaledTime")]
void SwitchToUnscaledTime()
{
    timeMode = new TimeMode(TimeMode.UpdateMode.UnscaledTime);
    Debug.Log("切换到 UnscaledTime 模式");
}
```

### 技巧3：暂停测试

```csharp
[ContextMenu("测试暂停")]
void TestPause()
{
    StartCoroutine(PauseTest());
}

IEnumerator PauseTest()
{
    Debug.Log("开始测试");
    
    yield return new WaitForSeconds(1f);
    Debug.Log("1秒后");
    
    // 暂停
    Time.timeScale = 0f;
    Debug.Log("暂停");
    
    yield return new WaitForSecondsRealtime(2f);
    Debug.Log("2秒后（真实时间）");
    
    // 恢复
    Time.timeScale = 1f;
    Debug.Log("恢复");
}
```

---

## 📚 最佳实践

### 1. 默认使用 GameTime
除非明确需要不受暂停影响，否则都使用 GameTime：
```csharp
[SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.GameTime);
```

### 2. 在 Update 开头获取 DeltaTime
```csharp
void Update()
{
    float deltaTime = timeMode.DeltaTime;
    
    // 所有逻辑使用这个 deltaTime
    UpdateMovement(deltaTime);
    UpdateAnimation(deltaTime);
    UpdateEffects(deltaTime);
}
```

### 3. 一致性
同一个系统内保持一致的时间模式：
- 角色移动 → GameTime
- 角色动画 → GameTime
- 角色技能 → GameTime

### 4. 文档化
在复杂组件添加注释说明为什么选择某个时间模式：
```csharp
// UnscaledTime：确保加载界面动画在游戏暂停时仍然播放
[SerializeField] private TimeMode timeMode = new TimeMode(TimeMode.UpdateMode.UnscaledTime);
```

---

## 🎯 快速检查清单

重构组件时，按此清单检查：

- [ ] 添加 TimeMode 字段
- [ ] Update 中使用 `timeMode.DeltaTime`
- [ ] 移除手动的 `IsGamePaused` 检查
- [ ] 确认时间模式选择正确（GameTime/UnscaledTime）
- [ ] 检查协程中的等待语句
- [ ] 测试暂停/恢复行为
- [ ] 更新相关文档

---

## ❓ FAQ

### Q1: TimeMode 会影响性能吗？
**A**: 几乎不会。TimeMode 是结构体，访问属性是内联的，开销可忽略（< 0.1%）。

### Q2: 可以运行时切换 TimeMode 吗？
**A**: 可以，但不推荐。TimeMode 应该在设计时确定，运行时切换可能导致逻辑混乱。

### Q3: 第三方插件如何配合 TimeMode？
**A**: 
- DOTween: 使用 `.SetUpdate(true)` 表示 unscaled
- Animator: 设置 `updateMode = AnimatorUpdateMode.UnscaledTime`
- 协程: 使用 `WaitForSecondsRealtime`

### Q4: 如何处理多层时间缩放（如子弹时间）？
**A**: 可以参考 GC2 的 TimeManager，支持多个时间缩放层。当前简化版只支持 0/1。

### Q5: Update 中不使用 deltaTime 的逻辑怎么办？
**A**: 例如输入检测，可以在 InputHandler 中统一处理暂停检查，其他组件从 InputHandler 获取数据。

---

## 📖 参考资源

- **GameCreator 2**: `TimeMode.cs` 源码
- **Unity Manual**: [Time and Framerate Management](https://docs.unity3d.com/Manual/TimeFrameManagement.html)
- **项目文档**: `TimeMode_Refactoring_Plan.md`

---

**最后更新**: 2024年12月
**维护者**: 项目团队

