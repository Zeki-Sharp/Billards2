# 配置系统多态化重构计划

## 概述
将当前基于 enum + switch 的配置系统重构为 GC2 风格的多态配置系统

## 优化目标

### 1. TriggerConfig 多态化

**当前设计：**
```csharp
[Serializable]
public class TriggerConfig
{
    public TriggerType triggerType;
    public string targetTag;           // Collision 专用
    public string killTargetTag;       // Kill 专用
    public DataExtractorType dataExtractorType; // DataSource 专用
}
```

**目标设计：**
```csharp
[Serializable]
public class TriggerConfig
{
    [SerializeReference]
    [TypeSelector]  // Odin Inspector 特性
    private ITrigger trigger;
    
    public ITrigger CreateTrigger() => trigger;
}

// 基类（接口改为抽象类，方便序列化）
[Serializable]
public abstract class TriggerBase : ITrigger
{
    [HideInInspector] public bool isEnabled = true;
}

// 具体触发器
[Serializable]
[AddTypeMenu("碰撞触发器")]
public class CollisionTrigger : TriggerBase
{
    [LabelText("目标标签")]
    public string targetTag = "Enemy";
}

[Serializable]
[AddTypeMenu("击杀触发器")]
public class KillTrigger : TriggerBase
{
    [LabelText("击杀目标")]
    public string killTargetTag = "Enemy";
}
```

### 2. ConditionConfig 多态化

**当前设计：**
```csharp
[Serializable]
public class SingleConditionConfig
{
    public ConditionType conditionType;
    public int requiredCount;        // Count 专用
    public float timeWindow;         // TimeWindow 专用
    public ComparisonType comparisonType; // ValueComparison 专用
    // ... 更多参数
}
```

**目标设计：**
```csharp
[Serializable]
public class ConditionConfig
{
    [SerializeReference]
    [TypeSelector]
    private ICondition condition;
    
    public ICondition CreateCondition() => condition;
}

[Serializable]
public abstract class ConditionBase : ICondition { }

[Serializable]
[AddTypeMenu("计数条件")]
public class CountCondition : ConditionBase
{
    [LabelText("需要计数")]
    public int requiredCount = 2;
}

[Serializable]
[AddTypeMenu("值比较条件")]
public class ValueComparisonCondition : ConditionBase
{
    [LabelText("比较类型")]
    public ComparisonType comparisonType;
    
    [LabelText("目标值")]
    public float targetValue;
    
    [LabelText("数据提取器")]
    public DataExtractorType dataExtractorType;
}
```

### 3. EffectConfig 多态化

**目标设计：**
```csharp
[Serializable]
public class EffectConfig
{
    [SerializeReference]
    [TypeSelector]
    private IEffect effect;
    
    public IEffect CreateEffect() => effect;
}

[Serializable]
public abstract class EffectBase : IEffect { }

[Serializable]
[AddTypeMenu("属性修改效果")]
public class StatModifierEffect : EffectBase
{
    [LabelText("目标属性")]
    public StatType targetStat;
    
    [LabelText("修改值")]
    public float modifierValue;
}

[Serializable]
[AddTypeMenu("弱点标记效果")]
public class WeakPointEffect : EffectBase
{
    [LabelText("弱点半径")]
    public float radius = 0.5f;
    
    [LabelText("伤害倍率")]
    public float damageMultiplier = 1.5f;
}
```

## 优势

### ✅ Inspector 体验提升
- 只显示选中类型的参数
- 使用 TypeSelector 下拉菜单选择类型
- 无冗余参数，清晰直观

### ✅ 代码扩展性
- 新增类型只需新建一个类
- 无需修改 switch 语句
- 遵循开闭原则

### ✅ 类型安全
- 编译时类型检查
- 避免参数混淆
- 更好的 IDE 支持

## 实施步骤

1. **阶段1：创建抽象基类**
   - TriggerBase
   - ConditionBase
   - EffectBase

2. **阶段2：迁移具体实现**
   - 将现有 Trigger 类改为继承 TriggerBase
   - 将现有 Condition 类改为继承 ConditionBase
   - 将现有 Effect 类改为继承 EffectBase

3. **阶段3：更新配置类**
   - 添加 [SerializeReference]
   - 添加 [TypeSelector]
   - 移除 enum 字段

4. **阶段4：数据迁移**
   - 编写编辑器工具转换现有配置
   - 测试所有技能配置

## 注意事项

⚠️ **SerializeReference 限制：**
- Unity 2019.3+ 支持
- 必须是类（class），不能是结构体
- 必须标记 [Serializable]

⚠️ **Odin Inspector 配合：**
- 使用 TypeSelector 特性
- 使用 AddTypeMenu 定义菜单路径
- 保持良好的类型组织

## 参考 GC2 实现

```csharp
// GC2 的基类设计
namespace GameCreator.Runtime.Common
{
    [Serializable]
    public abstract class TPolymorphicItem<TType> : IPolymorphicItem
    {
        [SerializeField] [HideInInspector]
        private bool m_Breakpoint = false;
        
        [SerializeField] [HideInInspector]
        private bool m_IsEnabled = true;
        
        public Type BaseType => typeof(TType);
        public Type FullType => this.GetType();
        public bool IsEnabled => this.m_IsEnabled;
    }
}
```

