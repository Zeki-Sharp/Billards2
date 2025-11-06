# 状态条件伤害修改器 - 使用指南

> **创建时间**：2025年11月  
> **适用版本**：多角色系统

---

## 📋 功能说明

### 什么是状态条件伤害修改器？

一个通用的被动技能系统，允许配置"对具有特定状态的敌人造成额外伤害"的技能。

**典型技能示例：**
- **点燃惩戒**：对点燃的敌人 +50% 伤害
- **剧毒强化**：对中毒的敌人 +40% 伤害
- **减速打击**：对减速的敌人 +60% 伤害

---

## 🎯 核心组件

### 1. StatusConditionalDamageModifier（伤害修改器）
- **路径**：`Assets/Scripts/SkillSystem/DamageModifiers/`
- **职责**：在伤害计算时检测目标状态并增加伤害
- **自动管理**：由技能系统创建和销毁

### 2. RegisterDamageModifierEffect（技能效果）
- **路径**：`Assets/Scripts/SkillSystem/Effects/`
- **职责**：管理 Modifier 的生命周期

### 3. RegisterDamageModifierEffectConfig（效果配置）
- **路径**：`Assets/Scripts/SkillSystem/Configs/Polymorphic/`
- **职责**：在 SkillConfig 中配置效果参数

---

## 📝 Unity 配置步骤

### 步骤1：创建 SkillConfig SO

1. **创建文件：**
   - 在 Project 面板右键 → `Create → Game/Skill/Skill Config`
   - 命名：`Skill_BurningPunisher`（点燃惩戒）

2. **基础信息配置：**
   - `Skill Name`: "点燃惩戒"
   - `Description`: "对处于点燃状态的敌人造成额外 50% 伤害"
   - `Icon`: 拖拽技能图标 Sprite
   - `Skill Type`: Passive（被动技能）

---

### 步骤2：配置 Level 1

#### 2.1 触发器配置

- 展开 `Skill Levels → Element 0 (Level 1)`
- `Trigger Config` 字段点击下拉菜单
- 选择 `AlwaysTrueTriggerConfig`
- **说明**：被动技能，始终生效，不需要特定触发条件

#### 2.2 条件配置

- `Condition Config` 保持默认（AlwaysTrueCondition）
- **说明**：状态检测在 Modifier 中完成，这里不需要额外条件

#### 2.3 效果配置（核心）

- `Effect Config` 字段点击下拉菜单
- 选择 `RegisterDamageModifierEffectConfig`
- 配置参数：
  - **目标状态**：拖拽 `BurningStatusData` SO
  - **增伤模式**：选择 `Percentage`（百分比模式）
  - **伤害倍率**：输入 `1.5`（+50% 伤害）
  - **显示日志**：勾选（测试阶段）

#### 2.4 重置条件配置

- `Reset Condition Config` 选择 `NeverResetConditionConfig`
- **说明**：被动技能，不需要重置（始终生效）

#### 2.5 效果移除配置

- 不需要配置（伤害修改器不是持续效果）

---

### 步骤3：配置 Level 2 和 Level 3

1. **复制 Level 1：**
   - 展开 `Skill Levels`
   - 点击 `Element 0` 右键 → Duplicate
   - 重复两次，创建 Element 1 和 Element 2

2. **修改数值：**
   - **Level 2**：
     - `Effect Config → Damage Multiplier`: 改为 `1.75`（+75%）
   - **Level 3**：
     - `Effect Config → Damage Multiplier`: 改为 `2.0`（+100%）

3. **保存配置：**
   - `Ctrl + S` 保存 SO 文件

---

### 步骤4：添加到技能数据库（可选）

如果有 `SkillDatabase`：
1. 找到 `SkillDatabase` SO 文件
2. 在 `Available Skills` 列表中添加 `Skill_BurningPunisher`
3. 保存

---

## 🧪 测试步骤

### 测试1：技能激活

1. **进入游戏**
2. **获得技能**（通过技能选择系统）
3. **检查 Console 日志：**
   ```
   [RegisterDamageModifier] ✅ 注册伤害修改器成功 - 状态:点燃, 增伤:×1.5
   [DamageProcessor] 注册伤害修改器: 状态增伤-点燃 (优先级: Normal)
   ```
4. **检查玩家球的 Components：**
   - 应该有一个 `StatusConditionalDamageModifier` 组件

### 测试2：攻击点燃的敌人

1. **使用点燃技能**（如"引燃攻击"）
2. **攻击点燃的敌人**
3. **检查 Console 日志：**
   ```
   [DamageSystem] 碰撞事件 - Source: Player_1_撞击角色, Target: Enemy_XXX
   [DamageSystem] 基础伤害: 10.0
   [弱点系统] 未命中弱点
   [状态增伤-点燃] 目标有 点燃 状态，伤害提升: 10.0 → 15.0 (×1.5)
   ```
4. **观察伤害数字**：应该显示 15 而不是 10

### 测试3：攻击未点燃的敌人

1. **攻击没有点燃状态的敌人**
2. **检查 Console 日志：**
   ```
   [状态增伤-点燃] 目标 Enemy_XXX 没有 点燃 状态，跳过
   ```
3. **观察伤害数字**：应该显示正常伤害（10）

### 测试4：叠加效果

1. **攻击敌人弱点 + 敌人被点燃**
2. **检查伤害：**
   ```
   基础: 10 → 弱点×1.5 = 15 → 点燃×1.5 = 22.5
   ```
3. **最终伤害应该是 22.5**

---

## 🔧 配置示例

### 百分比模式配置（推荐）

```yaml
技能: 点燃惩戒
  └─ Level 1:
      ├─ Trigger: AlwaysTrueTrigger
      ├─ Condition: AlwaysTrueCondition
      ├─ Effect: RegisterDamageModifierEffectConfig
      │   ├─ targetStatusData: BurningStatusData
      │   ├─ increaseType: Percentage
      │   └─ damageMultiplier: 1.5 (+50%)
      ├─ Reset: NeverResetCondition
      └─ Removal: 无需配置
```

### 固定值模式配置

```yaml
技能: 点燃惩戒（固定值版）
  └─ Level 1:
      ├─ Effect: RegisterDamageModifierEffectConfig
      │   ├─ targetStatusData: BurningStatusData
      │   ├─ increaseType: Fixed
      │   └─ fixedDamageBonus: 10 (+10点)
      └─ 其他同上
```

---

## ⚠️ 注意事项

### 配置注意

1. **targetStatusData 必须配置**
   - 没有状态数据，Modifier 不会生效
   - 确保拖拽正确的 StatusData SO

2. **数值合理性**
   - 百分比模式：multiplier > 1.0（如 1.5 = +50%）
   - 固定值模式：fixedBonus > 0（如 +10）

3. **调试日志**
   - 测试阶段建议开启 `showDebugLog`
   - 发布版本可关闭以提升性能

### 技能叠加

- **多个状态增伤技能会叠加**（乘法）
- 示例：点燃惩戒(×1.5) + 中毒强化(×1.3) = ×1.95
- 示例：弱点(×1.5) + 点燃惩戒(×1.5) = ×2.25

### 性能考虑

- `GetComponents<TurnBasedStatusComponent>()` 在每次伤害时调用
- 如果敌人状态很多，可能有性能开销
- 目前简化实现，后续可优化

---

## 🐛 常见问题

### 问题1：技能获得后没有效果

**检查清单：**
- [ ] Console 是否有 "注册伤害修改器成功" 日志？
- [ ] 玩家球上是否有 `StatusConditionalDamageModifier` 组件？
- [ ] `targetStatusData` 是否正确配置？

### 问题2：对点燃敌人仍然是正常伤害

**检查清单：**
- [ ] Console 是否有 "目标有 XX 状态" 日志？
- [ ] 敌人是否真的被点燃？（检查 StatusBar UI）
- [ ] `damageMultiplier` 是否 > 1.0？

### 问题3：伤害数字不对

**检查清单：**
- [ ] 是否与弱点叠加了？（弱点 × 状态 = 更高伤害）
- [ ] 是否有多个状态增伤技能？（会乘法叠加）
- [ ] Console 日志显示的最终伤害是多少？

---

## 📚 相关文档

- [设计文档](../../../../Plans/StatusConditional_Damage_Modifier_Design.md)
- [回合制状态系统](../TurnBasedStatus/README_QuickStart.md)
- [伤害系统架构](../../../Core/Manager/DamageSystem.cs)

---

**最后更新**：2025年11月


