# 三层属性系统说明文档

## 概述

本文档说明三层属性系统（Stats / Attributes / StatusEffects）的设计和使用方法。

---

## 三层架构

### 1. Stats 层 - 基础属性

**用途**: 管理基础数值属性（攻击力、速度、防御等）

**特点**:
- ✅ 只有数值，无上下限
- ✅ 支持 Modifier 修改
- ✅ 最终值 = (base + constant) * (1 + percent)

**典型应用**:
- 攻击力（Damage）
- 移动速度（MoveSpeed）
- 攻击范围（AreaRadius）

**核心类**:
- `StatData` - 配置数据
- `StatList` - 配置列表
- `RuntimeStatsManager` - 运行时管理

---

### 2. Attributes 层 - 动态资源

**用途**: 管理有上下限的动态资源（生命值、能量值等）

**特点**:
- ✅ 有 MinValue 和 MaxValue
- ✅ 提供 Ratio 计算（当前值/最大值）
- ✅ 自动 Clamp 到范围内
- ✅ 支持百分比初始化

**典型应用**:
- 生命值（Health）
- 能量值（Mana）
- 护盾值（Shield）

**核心类**:
- `AttributeData` - 配置数据
- `AttributeList` - 配置列表
- `RuntimeAttribute` - 单个资源运行时
- `RuntimeAttributes` - 资源管理器

---

### 3. StatusEffects 层 - 状态效果

**用途**: 管理临时状态效果（Buff/Debuff）

**特点**:
- ✅ 支持持续时间
- ✅ 支持堆叠
- ✅ OnStart/OnEnd/WhileActive 回调
- ✅ 可配置是否显示在UI

**典型应用**:
- 中毒效果（持续扣血）
- 加速效果（临时提升速度）
- 护盾效果（临时防御）
- 无敌状态

**核心类**:
- `StatusEffectData` - ScriptableObject 配置
- `RuntimeStatusEffect` - 单个效果运行时
- `RuntimeStatusEffects` - 效果管理器

---

## 使用示例

### Stats 层使用

```csharp
PlayerStatsManagerV2 statsManager = GetComponent<PlayerStatsManagerV2>();

// 添加固定值修改器
ModifierHandle handle = statsManager.AddConstant("Damage", 10f);

// 添加百分比修改器 (+50%)
ModifierHandle handle2 = statsManager.AddPercent("Damage", 0.5f);

// 获取最终值
float finalDamage = statsManager.FinalDamage;

// 移除修改器
statsManager.RemoveModifier("Damage", handle);
```

### Attributes 层使用

```csharp
// 获取当前生命值
float currentHealth = statsManager.CurrentHealth;

// 获取生命值百分比
float healthRatio = statsManager.HealthRatio;

// 扣血
statsManager.SubtractHealth(20f);

// 加血
statsManager.AddHealth(15f);

// 设置为满血
statsManager.SetHealth(statsManager.MaxHealth);
```

### StatusEffects 层使用

```csharp
// 添加状态效果
StatusEffectData poisonData = ...; // ScriptableObject
RuntimeStatusEffect poison = statsManager.AddStatusEffect(poisonData);

// 检查是否有某个状态
bool isPoisoned = statsManager.HasStatusEffect("Poison");

// 移除状态
statsManager.RemoveStatusEffectByID("Poison");

// 获取所有状态
var allEffects = statsManager.GetAllStatusEffects();
```

---

## 三层协同工作

### 示例：生命值系统

```
Stats 层:
  MaxHealth (Stat) = 100 + Modifiers
          ↓
Attributes 层:
  Health (Attribute) = CurrentHealth / MaxHealth
  - CurrentValue: 80
  - MaxValue: 从 Stats.MaxHealth 获取
  - Ratio: 80/100 = 0.8 (80%)
          ↓
StatusEffects 层:
  Poison (StatusEffect)
  - WhileActive: 每秒扣 5 点生命值
  - 影响 Attributes.Health.CurrentValue
```

### 示例：攻击力提升技能

```
技能触发
  ↓
Stats 层:
  添加 Damage Modifier (+50%)
  Damage = 10 * 1.5 = 15
  ↓
StatusEffects 层:
  添加"攻击力提升"状态（UI显示）
  持续时间: 10秒
  ↓
时间到期:
  移除 Modifier
  移除 StatusEffect
  Damage 恢复为 10
```

---

## 性能特点

### Stats 层
- ⚡ O(1) 最终值访问（缓存机制）
- ⚡ Modifier 是 struct（低GC）
- ⚡ ModifierList 缓存总值

### Attributes 层
- ⚡ 自动 Clamp（无需手动检查）
- ⚡ 事件通知（值变化时）
- ⚡ Ratio 计算（血条UI友好）

### StatusEffects 层
- ⚡ 自动过期检测
- ⚡ 堆叠管理
- ⚡ 生命周期回调

---

## 文件结构

```
AttributeSystem/Core/
├─ Stats/
│  ├─ StatData.cs
│  └─ StatList.cs
├─ Attributes/
│  ├─ AttributeData.cs
│  ├─ AttributeList.cs
│  ├─ RuntimeAttribute.cs
│  └─ RuntimeAttributes.cs
└─ StatusEffects/
   ├─ StatusEffectData.cs
   ├─ RuntimeStatusEffect.cs
   └─ RuntimeStatusEffects.cs
```

---

## 调试方法

### 查看完整三层信息

```csharp
Debug.Log(statsManager.GetDebugInfo());
```

输出：
```
=== 玩家属性系统（三层架构） ===

【1. Stats 层 - 基础属性】
[Damage] 基础: 10, 最终: 15 (+50%)
[MaxHealth] 基础: 100, 最终: 100

【2. Attributes 层 - 动态资源】
[Health] 80/100 (80%)

【3. StatusEffects 层 - 状态效果】
[Poison] 中毒 (剩余 3.2s/5s)
```

---

## 总结

**核心优势**:
1. ⭐ 职责清晰 - 三层各司其职
2. ⭐ 扩展性强 - 易于添加新类型
3. ⭐ 性能优秀 - 轻量级设计
4. ⭐ 易于调试 - 完整的调试信息

**设计原则**:
- Stats: 基础数值，可被修改
- Attributes: 动态资源，有上下限
- StatusEffects: 临时效果，有生命周期

