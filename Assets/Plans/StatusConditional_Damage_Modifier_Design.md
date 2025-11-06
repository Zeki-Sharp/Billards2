# 通用状态条件伤害修改器设计方案

> **创建时间**：2025年11月  
> **状态**：设计阶段  
> **优先级**：⭐⭐ 中优先级

---

## 📋 背景

### 需求来源
玩家提出需求：希望实现一个技能，"如果撞击到带有点燃状态的敌人，伤害增加"。

### 当前问题
初始设计方案存在通用性不足的问题：
- ❌ 硬编码检测特定状态类型（如 `BurningStatus`）
- ❌ 每增加一种状态条件（中毒、减速、虚弱等）需要写新的 Modifier
- ❌ 无法灵活配置检测多种状态的组合条件
- ❌ 不符合"可配置、可扩展"的技能系统设计原则

### 改进目标
设计一个**通用的状态条件伤害修改器系统**，满足以下要求：
- ✅ 可配置检测任意单一状态类型（点燃、中毒、减速、虚弱...）
- ✅ 一套代码支持所有单状态检测需求
- ✅ 可通过 SO 配置，无需修改代码
- ✅ 支持技能升级和参数调整
- ⏸️ 多状态组合逻辑（暂不实现，预留扩展性）

---

## 🎯 核心设计理念

### 设计原则
1. **数据驱动**：通过 ScriptableObject 配置，而非硬编码
2. **高复用性**：一个 Modifier 类支持所有状态检测场景
3. **灵活组合**：支持 AND/OR 逻辑组合多种状态
4. **松耦合**：Modifier 不依赖具体的状态类型，只依赖状态系统接口

### 架构层次
```
技能配置层 (SkillConfig SO)
    ↓ 配置参数
状态检测修改器 (StatusConditionalDamageModifier)
    ↓ 查询状态
状态系统 (TurnBasedStatusComponent)
    ↓ 应用增伤
伤害处理器 (DamageProcessor)
```

---

## 🔍 方案对比

### 方案A：基于状态数据 (StatusData) 检测 ⭐⭐⭐
**核心思路**：通过配置状态 SO 引用，检测目标是否有对应的状态组件

**优点**：
- ✅ 完全数据驱动，Inspector 可配置
- ✅ 类型安全（拖拽 SO 引用）
- ✅ 支持跨场景复用配置
- ✅ 可视化配置，策划友好

**缺点**：
- ⚠️ 需要在 Modifier 中维护状态 SO 列表

**推荐度**：⭐⭐⭐（最推荐）

---

### 方案B：基于状态类型 (Type) 检测 ⭐⭐
**核心思路**：通过类型名称（字符串）或枚举，检测目标是否有对应类型的组件

**优点**：
- ✅ 实现简单
- ✅ 不依赖 SO 引用

**缺点**：
- ❌ 类型名称字符串容易出错（拼写错误）
- ❌ 重构时难以维护（改类名需要改配置）
- ❌ 不支持运行时动态添加新状态类型

**推荐度**：⭐⭐

---

### 方案C：基于状态标签 (Tag) 系统 ⭐
**核心思路**：为每个状态配置标签（如 "Debuff", "DoT"），Modifier 检测标签

**优点**：
- ✅ 可以一次检测多种状态（如"所有减益效果"）
- ✅ 语义化更好

**缺点**：
- ❌ 需要扩展现有状态系统（添加标签字段）
- ❌ 增加系统复杂度
- ❌ 不适合当前项目阶段

**推荐度**：⭐（暂不推荐，后期可考虑）

---

## 🏆 推荐方案详细设计

### 方案A：基于 StatusData 检测（简化版）

#### 核心组件

**1. StatusConditionalDamageModifier**
- **职责**：检测目标是否有指定状态并修改伤害
- **输入参数**：
  - 要检测的状态（`TurnBasedStatusData`，单个）
  - 伤害增加类型（`DamageIncreaseType`：Percentage / Fixed）
  - 伤害倍率（`float`，百分比模式，如 1.5 = +50%）
  - 固定伤害值（`float`，固定值模式，如 +10）
- **输出**：修改后的伤害值

**2. RegisterDamageModifierEffect**
- **职责**：作为技能效果，管理 Modifier 的生命周期
- **功能**：
  - 技能激活时：创建并注册 Modifier
  - 技能失效时：注销并销毁 Modifier

**3. RegisterDamageModifierEffect（技能效果）**
- **职责**：作为技能配置的一个效果组件
- **配置参数**：
  - `targetStatusData`：要检测的状态（拖拽 SO）
  - `increaseType`：伤害增加类型（Percentage / Fixed）
  - `damageMultiplier`：伤害倍率（百分比模式，如 1.5）
  - `fixedDamageBonus`：固定伤害加成（固定值模式，如 10）
  - `showDebugLog`：是否显示日志

**4. SkillConfig 配置**
- **配置项**：
  - 触发器：AlwaysTrueTrigger（被动技能，始终生效）
  - 效果列表：包含 RegisterDamageModifierEffect
  - 参数：通过 Effect 配置状态数据和增伤参数
  
**配置关系：**
```
SkillConfig SO (点燃惩戒 - 百分比模式)
  ├─ Trigger: AlwaysTrueTrigger
  └─ Effects:
      └─ RegisterDamageModifierEffect
          ├─ targetStatusData → BurningStatusData (SO引用)
          ├─ increaseType → Percentage
          └─ damageMultiplier → 1.5 (+50%)

SkillConfig SO (点燃惩戒 - 固定值模式)
  ├─ Trigger: AlwaysTrueTrigger
  └─ Effects:
      └─ RegisterDamageModifierEffect
          ├─ targetStatusData → BurningStatusData (SO引用)
          ├─ increaseType → Fixed
          └─ fixedDamageBonus → 10 (+10点)
```

#### 状态检测逻辑（简化版）

**单状态检测**
- 检测目标是否有**指定的单一状态**
- 如果有且状态激活 → 触发增伤
- 如果没有或状态未激活 → 不触发
- 用例：对"点燃"的敌人增伤

**未来扩展**（暂不实现）
- 多状态 OR 检测（Any 模式）
- 多状态 AND 检测（All 模式）
- 状态层数检测
- 状态剩余时间检测

#### 执行流程

**技能激活时：**
```
玩家选择技能
  ↓
SkillManager 激活技能
  ↓
ExecuteEffect(RegisterDamageModifierEffect)
  ↓
创建 StatusConditionalDamageModifier 组件
  ↓
设置检测参数（状态数据、倍率）
  ↓
注册到 DamageProcessor
```

**伤害计算时：**
```
玩家撞击敌人
  ↓
DamageSystem 计算基础伤害
  ↓
DamageProcessor 调用所有 Modifier
  ↓
StatusConditionalDamageModifier:
  - 获取目标的所有状态组件
  - 遍历检查是否有匹配的状态
  - 比较 component.StatusData == 配置的状态数据
  - 检查 component.RemainingTurns > 0
  - 如果匹配 → 伤害 × 倍率
  ↓
最终伤害应用到敌人
```

---

## 🔧 技术要点

### 状态匹配机制

**依赖现有接口：**
- `TurnBasedStatusComponent.StatusData`：获取状态配置 SO
- `TurnBasedStatusComponent.RemainingTurns`：获取剩余回合数
  - `RemainingTurns > 0` 表示状态激活

**匹配逻辑（简化版）：**
```
获取目标的所有 TurnBasedStatusComponent
  ↓
遍历每个组件：
  - 比较 component.StatusData 是否等于配置的 StatusData
  - 检查 component.RemainingTurns > 0
  ↓
如果找到匹配且激活的状态 → 返回 true
否则 → 返回 false
```

### 优先级设计

**Modifier 执行顺序：**
1. 弱点系统（Priority = 10）
2. 状态条件增伤（Priority = 20）
   - 如果有多个状态增伤 Modifier，按注册顺序执行
   - 所有 Modifier 使用相同优先级（20），允许叠加
3. 暴击系统（Priority = 30，如果未来实现）

**原因**：
- 确保状态增伤在基础修改器之后执行
- 相同优先级的 Modifier 按注册顺序依次叠加
- 乘法叠加机制：每个 Modifier 都在前一个结果上计算

**叠加计算示例：**
```
基础伤害: 10点
  ↓ WeakPointModifier (Priority 10)
  → × 1.5 = 15点
  ↓ StatusModifier_Burning (Priority 20)
  → × 1.5 = 22.5点
  ↓ StatusModifier_Poison (Priority 20)
  → × 1.3 = 29.25点
  ↓ 最终伤害
```

### 配置灵活性

**支持的配置场景（简化版）：**
- 单状态检测：配置一个状态（如"点燃"）
- 未配置：如果状态数据为 null，跳过检测

**未来可扩展（暂不实现）：**
- 多状态 OR：配置多个状态列表 + Any 模式（如"点燃或中毒"）
- 多状态 AND：配置多个状态列表 + All 模式（如"点燃且减速"）

---

## 📊 使用示例和技能配置

### 示例1：点燃惩戒（初版实现）

#### 需求描述
对点燃目标造成 +50% 伤害（被动技能）

#### SkillConfig 配置（SO 文件）

**基础信息：**
```yaml
技能ID: skill_burning_punisher
技能名称: 点燃惩戒
技能类型: 被动技能（Passive）
描述: 对处于点燃状态的敌人造成额外 50% 伤害
图标: [点燃惩戒图标.png]
```

**Level 1 配置（百分比模式）：**
```yaml
等级: 1
触发器类型: AlwaysTrueTrigger
  - 说明: 被动技能，始终生效，不需要特定触发条件
  
效果列表:
  - 效果类型: RegisterDamageModifierEffect
    参数:
      - targetStatusData: BurningStatusData（拖拽 SO 引用）
      - increaseType: Percentage
      - damageMultiplier: 1.5（+50% 伤害）
      - showDebugLog: true（测试时开启）
```

**Level 2 配置：**
```yaml
等级: 2
触发器类型: AlwaysTrueTrigger
效果列表:
  - 效果类型: RegisterDamageModifierEffect
    参数:
      - damageMultiplier: 1.75（+75% 伤害）
      # 其他参数继承 Level 1
```

**Level 3 配置：**
```yaml
等级: 3
触发器类型: AlwaysTrueTrigger
效果列表:
  - 效果类型: RegisterDamageModifierEffect
    参数:
      - damageMultiplier: 2.0（+100% 伤害）
      # 其他参数继承 Level 1
```

**固定值模式示例（可选配置）：**
```yaml
等级: 1
效果列表:
  - 效果类型: RegisterDamageModifierEffect
    参数:
      - targetStatusData: BurningStatusData
      - increaseType: Fixed
      - fixedDamageBonus: 10（+10点固定伤害）
```

#### Unity Inspector 配置步骤

1. **创建 SkillConfig SO：**
   - 右键 → Create → Game/Skill/Skill Config
   - 命名：`Skill_BurningPunisher`

2. **配置基础信息：**
   - Skill Name: "点燃惩戒"
   - Description: "对处于点燃状态的敌人造成额外50%伤害"
   - Icon: 拖拽图标 Sprite

3. **配置 Level 1：**
   - Trigger Config: 选择 `AlwaysTrueTriggerConfig`
   - Effects (列表):
     - Element 0:
       - Effect Type: `RegisterDamageModifierEffect`
       - Target Status Data: 拖拽 `BurningStatusData` SO
       - Increase Type: 选择 `Percentage`（或 `Fixed`）
       - Damage Multiplier: `1.5`（如果是百分比模式）
       - Fixed Damage Bonus: `10`（如果是固定值模式）

4. **配置 Level 2 和 3：**
   - 复制 Level 1 配置
   - 只修改伤害数值：
     - Level 2: Damage Multiplier → `1.75` 或 Fixed Damage Bonus → `15`
     - Level 3: Damage Multiplier → `2.0` 或 Fixed Damage Bonus → `20`

#### 技能获取流程

**玩家获得技能时：**
```
玩家升级/选择技能
  ↓
SkillManager.AddSkillToCharacter(characterID, skillConfig)
  ↓
技能系统创建 SkillInstance
  ↓
自动执行 AlwaysTrueTrigger（被动技能立即激活）
  ↓
ExecuteEffect(RegisterDamageModifierEffect)
  ↓
在玩家 GameObject 上添加 StatusConditionalDamageModifier 组件
  ↓
设置参数：targetStatus = BurningStatusData, multiplier = 1.5
  ↓
注册到 DamageProcessor（优先级 20）
  ↓
✅ 技能激活完成，玩家现在对点燃敌人有额外伤害
```

**技能升级时：**
```
玩家升级技能到 Level 2
  ↓
SkillInstance 切换到 Level 2 配置
  ↓
RemoveEffect()：移除旧的 Modifier
  ↓
ExecuteEffect()：重新注册新的 Modifier（倍率 1.75）
  ↓
✅ 升级完成
```

#### 实际战斗效果

**场景1：攻击点燃的敌人（百分比模式）**
```
玩家球撞击敌人
  ↓
DamageSystem 计算基础伤害: 10点
  ↓
DamageProcessor 调用 Modifier:
  1. WeakPointModifier: 未命中弱点，跳过
  2. StatusConditionalDamageModifier:
     - 攻击时检测敌人有 BurningStatus ✅
     - 增加类型: Percentage
     - 伤害 × 1.5 = 15点
  ↓
最终伤害: 15点
  ↓
Console 日志: "[状态惩戒] 目标被点燃，伤害提升: 10.0 → 15.0 (×1.5)"
```

**场景1-B：攻击点燃的敌人（固定值模式）**
```
玩家球撞击敌人
  ↓
DamageSystem 计算基础伤害: 10点
  ↓
DamageProcessor 调用 Modifier:
  1. WeakPointModifier: 未命中弱点，跳过
  2. StatusConditionalDamageModifier:
     - 攻击时检测敌人有 BurningStatus ✅
     - 增加类型: Fixed
     - 伤害 + 10 = 20点
  ↓
最终伤害: 20点
  ↓
Console 日志: "[状态惩戒] 目标被点燃，伤害提升: 10.0 → 20.0 (+10)"
```

**场景2：攻击未点燃的敌人**
```
玩家球撞击敌人
  ↓
DamageSystem 计算基础伤害: 10点
  ↓
DamageProcessor 调用 Modifier:
  1. WeakPointModifier: 未命中弱点，跳过
  2. StatusConditionalDamageModifier:
     - 检测敌人无 BurningStatus ❌
     - 跳过
  ↓
最终伤害: 10点（无加成）
```

**场景3：命中弱点 + 点燃状态（双重叠加）**
```
玩家球撞击敌人的弱点区域
  ↓
DamageSystem 计算基础伤害: 10点
  ↓
DamageProcessor 调用 Modifier（按优先级顺序）:
  1. WeakPointModifier (Priority 10): 命中弱点 ✅
     - 伤害 × 1.5 = 15点
  2. StatusConditionalDamageModifier (Priority 20): 敌人被点燃 ✅
     - 伤害 × 1.5 = 22.5点
  ↓
最终伤害: 22.5点（乘法叠加：1.5 × 1.5 = 2.25×）
  ↓
Console 日志:
  "[弱点系统] 命中弱点！伤害提升: 10.0 → 15.0 (×1.5)"
  "[状态惩戒] 目标被点燃，伤害提升: 15.0 → 22.5 (×1.5)"
```

**场景4：多技能叠加（点燃惩戒 + 中毒强化）**
```
玩家拥有两个技能：
  - 点燃惩戒：对点燃目标 ×1.5
  - 中毒强化：对中毒目标 ×1.3
  
玩家攻击一个同时被点燃和中毒的敌人
  ↓
DamageSystem 计算基础伤害: 10点
  ↓
DamageProcessor 调用 Modifier:
  1. StatusConditionalDamageModifier (点燃):
     - 检测到点燃状态 ✅
     - 伤害 × 1.5 = 15点
  2. StatusConditionalDamageModifier (中毒):
     - 检测到中毒状态 ✅
     - 伤害 × 1.3 = 19.5点
  ↓
最终伤害: 19.5点（乘法叠加：1.5 × 1.3 = 1.95×）
  ↓
Console 日志:
  "[状态惩戒-点燃] 目标被点燃，伤害提升: 10.0 → 15.0 (×1.5)"
  "[状态惩戒-中毒] 目标被中毒，伤害提升: 15.0 → 19.5 (×1.3)"
```

---

### 示例2：剧毒强化（未来扩展）
**需求**：对中毒目标造成 +40% 伤害

**配置：**
- 技能名称：剧毒强化
- 检测状态：PoisonStatusData
- 伤害倍率：1.4

**说明**：使用相同的 Modifier 类，只需配置不同的状态数据即可

---

### 示例3：减速打击（未来扩展）
**需求**：对减速目标造成 +60% 伤害

**配置：**
- 技能名称：减速打击
- 检测状态：SlowStatusData
- 伤害倍率：1.6

---

## 🎨 扩展性考虑

### 未来可扩展的功能

**1. 状态层数检测**
- 当前：只检测"有无"状态
- 扩展：检测状态层数，层数越高增伤越高
- 示例："点燃层数 ≥ 3 时额外增伤 50%"

**2. 状态剩余时间检测**
- 当前：不考虑状态剩余回合数
- 扩展：根据剩余回合调整倍率
- 示例："点燃剩余 ≤ 1 回合时伤害翻倍（斩杀效果）"

**3. 复合条件表达式**
- 当前：只支持 Any / All
- 扩展：支持复杂表达式
- 示例："(点燃 OR 中毒) AND 减速"

**4. 多段式伤害倍率**
- 当前：固定倍率
- 扩展：根据条件数量递增
- 示例：
  - 1 个状态：1.3×
  - 2 个状态：1.6×
  - 3 个状态：2.0×

**5. 特殊效果触发**
- 当前：只增加伤害
- 扩展：触发额外效果
- 示例："对点燃目标攻击时，有 20% 概率引发爆炸"

---

## ⚠️ 注意事项

### 性能考虑
- `GetComponents<TurnBasedStatusComponent>()` 在每次伤害计算时调用
- 如果状态组件很多，可能有性能开销
- **优化方案**：状态系统可考虑维护一个状态映射表（空间换时间）

### 兼容性
- 需要确保 `TurnBasedStatusComponent` 有 `StatusData` 和 `IsActive` 属性
- 如果当前实现缺少这些接口，需要先补充

### 调试支持
- Modifier 应提供详细的调试日志
- 显示检测到的状态列表
- 显示匹配结果和伤害变化

---

## 📝 实施计划

### 阶段1：核心实现（预计 1.5-2 小时）

**1. 创建伤害修改器（Assets/Scripts/SkillSystem/DamageModifiers/）**
- 文件：`StatusConditionalDamageModifier.cs`
- 实现 `IDamageModifier` 接口
- 枚举：
  - `DamageIncreaseType { Percentage, Fixed }`：增伤模式
- 字段：
  - `TurnBasedStatusData targetStatusData`（要检测的状态）
  - `DamageIncreaseType increaseType`（增伤模式）
  - `float damageMultiplier`（百分比倍率，如 1.5）
  - `float fixedDamageBonus`（固定值加成，如 10）
  - `bool showDebugLog`（调试开关）
- 方法：
  - `ModifyDamage(ref AttackData attackData)`：核心检测和增伤逻辑
  - `CheckTargetHasStatus(GameObject target)`：状态检测辅助方法
  - `ApplyDamageIncrease(ref float damage)`：应用增伤（根据模式）

**2. 创建技能效果（Assets/Scripts/SkillSystem/Effects/）**
- 文件：`RegisterDamageModifierEffect.cs`
- 实现 `IEffect` 接口
- 字段（可配置）：
  - `TurnBasedStatusData targetStatusData`（要检测的状态）
  - `DamageIncreaseType increaseType`（增伤模式）
  - `float damageMultiplier`（百分比倍率）
  - `float fixedDamageBonus`（固定值加成）
  - `bool showDebugLog`
- 方法：
  - `ExecuteEffect(SkillArgs args)`：创建并注册 Modifier，传递配置参数
  - `RemoveEffect()`：注销并销毁 Modifier
  - `Initialize()`：验证配置（检查状态数据和数值有效性）

**3. 集成到现有系统**
- 确保 `DamageProcessor` 支持动态注册/注销
- 验证 Priority 优先级正确（20）

### 阶段2：技能配置（预计 1 小时）

**1. 创建 SkillConfig SO**
- 路径：`Assets/Data/Skills/Passive/`
- 文件名：`Skill_BurningPunisher.asset`
- 配置 3 个等级的参数

**2. 配置技能数据库**
- 将技能添加到 `SkillDatabase`
- 确保技能可以在技能选择界面出现

**3. 关联状态数据**
- 确保 `BurningStatusData` SO 存在
- 路径：`Assets/Data/StatusEffects/BurningStatusData.asset`

### 阶段3：测试验证（预计 1-1.5 小时）

**1. 单元测试（手动）**
- ✅ 玩家获得技能后，Modifier 正确注册
- ✅ 对点燃敌人增伤 50%
- ✅ 对非点燃敌人不增伤
- ✅ 技能升级后倍率正确变化
- ✅ 技能失效后 Modifier 正确移除

**2. 集成测试**
- ✅ 与弱点系统的叠加效果（1.5 × 1.5 = 2.25×）
- ✅ 多个状态增伤技能的叠加（点燃 × 中毒）
- ✅ 与其他伤害修改器的兼容性
- ✅ 多个玩家角色同时拥有技能时的独立性

**3. 边界测试**
- ✅ 敌人状态刚失效时不触发增伤
- ✅ 敌人有多层点燃时正常触发
- ✅ 配置错误（statusData 为 null）时不崩溃

### 阶段4：优化和文档（预计 30 分钟）

**1. 性能优化**
- 检查 `GetComponents` 调用频率
- 考虑缓存机制（如果性能有问题）

**2. 调试优化**
- 添加详细的调试日志（可开关）
- 在伤害数字上显示特殊颜色（可选）

**3. 文档编写**
- 创建使用说明 README
- 记录配置模板
- 列出已知限制

---

## ❓ 待确认问题

### 已确认的设计决策：

1. ✅ **伤害增加方式**
   - **支持两种模式**：
     - **百分比模式**：伤害 × 倍率（如 1.5 = +50%）
     - **固定值模式**：伤害 + 固定值（如 +10）
   - 通过 `increaseType` 枚举选择模式
   - 不同技能可选择不同模式

2. ✅ **技能升级方向**
   - **只调整伤害数值**
   - Level 1 → 2 → 3：倍率或固定值递增
   - 不增加额外功能（保持简单）

3. ✅ **触发反馈**
   - **暂不做额外 UI 表现**
   - 只在 Console 输出调试日志（开发阶段）
   - 后续可根据需要添加视觉/音效反馈

4. ✅ **状态失效时机**
   - **攻击时判断状态**
   - 只检查攻击触发瞬间目标是否具有状态
   - 不需要预判或提前检测

5. ✅ **叠加规则**
   - **弱点 + 状态增伤**：✅ 允许叠加
   - **多个状态增伤技能**：✅ 允许叠加（乘法叠加）
   - 示例：弱点(×1.5) + 点燃增伤(×1.5) = 最终 ×2.25
   - 示例：点燃增伤(×1.5) + 中毒增伤(×1.3) = 最终 ×1.95

### 待确认的设计问题：

1. **初版使用哪种模式？**
   - 百分比模式（推荐，数值平衡更直观）
   - 固定值模式
   - 两种都配置示例技能

---

## 📦 依赖和前置条件

### 现有系统依赖
1. **技能系统**
   - `SkillManager`：管理技能实例和激活
   - `SkillConfig`：技能配置 SO
   - `IEffect`：技能效果接口
   - `AlwaysTrueTrigger`：被动技能触发器

2. **状态系统**
   - `TurnBasedStatusComponent`：状态基类
   - `TurnBasedStatusData`：状态配置 SO
   - `BurningStatus`：点燃状态实现

3. **伤害系统**
   - `DamageProcessor`：伤害修改器管理
   - `IDamageModifier`：伤害修改器接口
   - `AttackData`：伤害数据结构

### 必需的 ScriptableObject
- `BurningStatusData.asset`：点燃状态配置
- `Skill_BurningPunisher.asset`：点燃惩戒技能配置

### 必需的代码接口
- `TurnBasedStatusComponent.StatusData`（Property）
- `TurnBasedStatusComponent.RemainingTurns`（Property）
  - 通过 `RemainingTurns > 0` 判断状态是否激活

---

## 🔗 相关文档

- [回合制状态系统设计](./TurnBased_Status_System_Design.md)
- [伤害系统架构](../Scripts/Core/Manager/DamageSystem.cs)
- [技能系统框架](../Scripts/SkillSystem/README.md)
- [技能配置示例](../Scripts/SkillSystem/SkillConfig.cs)

---

**最后更新**：2025年11月  
**负责人**：AI Assistant  
**审核状态**：待用户确认


