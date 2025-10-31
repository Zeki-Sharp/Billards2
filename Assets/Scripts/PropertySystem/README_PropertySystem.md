# Property 动态值系统使用指南

## 📖 概述

Property 系统允许配置中的数值参数使用**动态计算**而非硬编码的固定值。

---

## 🎯 核心概念

### 什么是 Property？

Property 是一个**值提供者（Value Provider）**，它在运行时动态计算并返回一个值。

**传统方式**（固定值）：
```csharp
public float healAmount = 20f; // ❌ 只能是固定 20
```

**Property 方式**（动态值）：
```csharp
[SerializeReference]
public PropertyGetFloat healAmount; // ✅ 可以是多种来源的值

// 在 Inspector 中选择：
// - ConstantFloat(20) → 固定 20
// - RandomFloat(10, 30) → 随机 10-30
// - AttributeRatioFloat("Health", 0.2) → 最大血量的 20%
```

---

## 📦 可用的 Property 类型

### 1. ConstantFloat - 固定值

**用途**：返回固定的常量值

**配置**：
- `value` - 固定值

**示例**：
```csharp
var prop = new ConstantFloat(20f);
float value = prop.Get(args); // 总是返回 20
```

**适用场景**：
- 固定伤害值
- 固定治疗量
- 固定持续时间

---

### 2. RandomFloat - 随机值

**用途**：返回指定范围内的随机值

**配置**：
- `minValue` - 最小值
- `maxValue` - 最大值

**示例**：
```csharp
var prop = new RandomFloat(10f, 30f);
float value = prop.Get(args); // 返回 10-30 之间的随机值
```

**适用场景**：
- 随机伤害（40-60）
- 随机治疗（10-20）
- 随机持续时间（5-10秒）

---

### 3. AttributeRatioFloat - 基于属性百分比

**用途**：基于目标的 Attribute 值计算

**配置**：
- `attributeID` - 属性ID（如 "Health"）
- `source` - 值来源（CurrentValue/MaxValue/Ratio）
- `ratio` - 百分比（0.2 = 20%）

**示例**：
```csharp
// 回复最大血量的 20%
var prop = new AttributeRatioFloat("Health", 0.2f, ValueSource.MaxValue);
float value = prop.Get(args); 
// 如果 MaxHealth = 100，返回 20

// 造成当前血量 50% 的伤害
var prop2 = new AttributeRatioFloat("Health", 0.5f, ValueSource.CurrentValue);
// 如果 CurrentHealth = 60，返回 30
```

**适用场景**：
- 基于血量百分比的治疗
- 基于当前血量的伤害
- 百分比消耗

---

### 4. StatBasedFloat - 基于属性值

**用途**：基于目标的 Stat 值计算

**配置**：
- `statID` - Stat ID（如 "Damage"、"MaxHealth"）
- `multiplier` - 乘数
- `additionalValue` - 附加值

**示例**：
```csharp
// 治疗量 = 攻击力的 50%
var prop = new StatBasedFloat("Damage", 0.5f);

// 伤害 = 最大血量的 10% + 5
var prop2 = new StatBasedFloat("MaxHealth", 0.1f, 5f);
```

**适用场景**：
- 基于攻击力的治疗
- 基于防御力的护盾
- 基于速度的持续时间

---

## 🔧 在技能效果中使用

### 示例 1：HealEffect 使用动态治疗量

**当前实现**（固定值）：
```csharp
public class HealEffect : IEffect
{
    private float healAmount = 20f; // ❌ 固定值
    
    public bool ExecuteEffect(SkillArgs args)
    {
        targetPlayer.Heal(healAmount); // 总是回复 20
    }
}
```

**改进后**（动态值）：
```csharp
public class HealEffect : IEffect
{
    [SerializeReference]
    private PropertyGetFloat healAmount; // ✅ 动态值
    
    public bool ExecuteEffect(SkillArgs args)
    {
        float amount = healAmount.Get(args); // 运行时计算
        targetPlayer.Heal(amount);
    }
}
```

**配置示例**：
```csharp
// 在 EffectConfig.cs 中
[SerializeReference]
public PropertyGetFloat healAmount = new ConstantFloat(20f); // 默认固定 20

// 在 Inspector 中可以切换为：
// - AttributeRatioFloat("Health", 0.2) → 回复 20% 最大血量
// - RandomFloat(15, 25) → 随机 15-25
// - StatBasedFloat("Damage", 0.5) → 攻击力的 50%
```

---

### 示例 2：StatModifierEffect 使用动态修改值

**当前实现**（固定值）：
```csharp
public float modifierValue = 2f; // ❌ 固定 +100%
```

**改进后**（动态值）：
```csharp
[SerializeReference]
public PropertyGetFloat modifierValue; // ✅ 可以基于击杀数增长

// 配置：每击杀一个敌人，+10% 攻击力
// FormulaFloat("kills * 0.1 + 1.0")
```

---

## 🎯 设计原则

### 1. 默认值兼容
```csharp
// 支持 null 或默认行为
if (healAmount == null)
    healAmount = new ConstantFloat(20f);
```

### 2. Args 上下文
```csharp
// Property 接收 SkillArgs，可以访问：
float Get(SkillArgs args)
{
    // args.Source - 技能施放者
    // args.Target - 技能目标
    // args.EventData - 事件数据
}
```

### 3. 安全返回
```csharp
// args 为 null 时返回合理的默认值
if (args == null) return defaultValue;
```

---

## 📋 扩展建议

### 未来可添加的 Property 类型

**FormulaFloat** - 基于公式：
```csharp
// 公式：kills * 5 + level * 2 + 10
formula = "kills * 5 + level * 2 + 10"
```

**CurveFloat** - 基于曲线：
```csharp
// 使用 AnimationCurve 映射值
curve = AnimationCurve
inputSource = "HealthRatio" // 血量百分比
// 血量 100% → 输出 10
// 血量 50% → 输出 20
// 血量 0% → 输出 30
```

**PropertyGetInt** - 整数版本：
```csharp
// 用于数量、层数等整数值
public abstract class PropertyGetInt { ... }
```

---

## ⚠️ 注意事项

### Unity 配置要求

**使用 SerializeReference**：
```csharp
[SerializeReference] // ✅ 必须！支持多态序列化
public PropertyGetFloat healAmount;
```

**不要用 public 字段**：
```csharp
public PropertyGetFloat healAmount; // ❌ Unity 不支持抽象类序列化
```

### Inspector 显示

需要安装 **Odin Inspector** 或编写自定义 PropertyDrawer 来：
- 在 Inspector 中选择 Property 类型
- 显示对应的配置字段

**如果没有 Odin**，需要手动在代码中赋值：
```csharp
healAmount = new ConstantFloat(20f);
```

---

## 🎉 收益总结

**配置灵活性**：
- ✅ 从固定值变为可配置的动态值
- ✅ 支持多种计算方式

**游戏设计**：
- ✅ 支持基于血量、攻击力等的数值设计
- ✅ 支持随机性
- ✅ 易于平衡调整

**代码质量**：
- ✅ 减少硬编码
- ✅ 易于扩展新的值类型
- ✅ 统一的值获取接口

---

**Property 系统是 GC2 设计思想的核心之一，极大提升了配置的灵活性！**

