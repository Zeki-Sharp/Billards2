# 生成系统"生成方式层"重构计划

## 背景

当前生成系统存在抽象层级问题：**"生成方式"（Spawn Strategy）没有被独立抽象出来**。

### 当前架构的核心问题

现有三层架构：
- **配置层（ConfigProvider）**：混杂了配置管理和生成逻辑
- **决策层（Trigger）**：决定何时生成
- **执行层（Spawner）**：执行具体生成

**问题具体表现：**
1. `WaveConfigProvider` 返回预先配置好的列表（列表生成）
2. `DropTableProvider` 内部做概率抽取后返回列表（概率生成）
3. 新需求"技能每回合生成2个固定道具"没有合适的抽象（固定配置生成）

**根本矛盾：** "如何决定生成什么" 这个职责被混在了ConfigProvider中，导致不同场景需要重复实现相似逻辑。

---

## 三个典型场景分析

### 场景1：敌人波次生成
- **触发时机**：敌人回合开始
- **生成方式**：按照预先配置的列表生成（列表生成）
- **配置特点**：每波次具体配置（Wave1: 敌人A x3, 敌人B x2）
- **生成范围**：世界坐标矩形范围

### 场景2：技能道具生成（新需求）
- **触发时机**：玩家回合开始
- **生成方式**：每次都生成固定种类和数量（固定配置生成）
- **配置特点**：笼统配置（每回合生成道具X x2）
- **生成范围**：世界坐标圆形范围
- **触发条件**：玩家携带该技能

### 场景3：击杀掉落
- **触发时机**：敌人死亡事件
- **生成方式**：根据概率表随机抽取（概率生成）
- **配置特点**：掉落表（道具A 30%, 道具B 10%）
- **生成范围**：相对于死亡位置的圆形偏移范围

---

## 解决方案：四层架构

### 新架构概览

```
Layer 1: Trigger（触发层）
    ↓ 决定"何时生成"
Layer 2: Spawn Strategy（生成方式层）★ 新增
    ↓ 决定"生成什么"
Layer 3: Range Config（范围配置层）
    ↓ 决定"在哪生成"
Layer 4: Spawner（执行层）
    ↓ 决定"如何生成"
```

### 核心改进：独立抽象"生成方式层"

**设计思路：**
- 将"决定生成什么内容"的逻辑从ConfigProvider中剥离
- 创建 `ISpawnStrategy<T>` 接口，定义统一的"获取生成列表"方法
- 实现三种具体策略：列表、固定配置、概率

**职责划分：**
- **ConfigProvider**：纯数据管理（存储列表、掉落表、道具池）
- **SpawnStrategy**：生成逻辑（如何从数据中决定生成什么）
- **Trigger**：时机和条件判断（何时、是否生成）
- **RangeConfig**：位置计算（在哪生成）
- **Spawner**：实例化执行（如何生成）

---

## 三种生成方式设计思路

### 1. 列表生成策略（ListSpawnStrategy）

**适用场景：** 敌人波次生成

**核心思路：**
- 提前配置好完整的生成列表
- 每次调用返回这个预设列表
- 适合需要精确控制每波次内容的场景

**配置示例：**
```
Wave1: [敌人A, 敌人A, 敌人A, 敌人B, 敌人B]
Wave2: [敌人C, 敌人C, 敌人C, 敌人C, 敌人C]
```

### 2. 固定配置生成策略（FixedSpawnStrategy）

**适用场景：** 技能道具生成

**核心思路：**
- 配置固定的"种类"和"数量"
- 每次调用生成N个相同的对象
- 适合重复性、固定规则的生成

**配置示例：**
```
道具种类: 治疗药水
生成数量: 2
→ 每次生成: [治疗药水, 治疗药水]
```

### 3. 概率生成策略（ProbabilisticSpawnStrategy）

**适用场景：** 击杀掉落

**核心思路：**
- 配置概率表（每个道具有独立概率）
- 每次调用时执行概率判定
- 支持条件判定（如技能影响）
- 适合随机性、不确定的生成

**配置示例：**
```
掉落表:
- 小血瓶: 30%
- 大血瓶: 10%
- 伤害增益: 15%
→ 每次掉落: 随机抽取（可能0-3个）
```

---

## 范围配置改进思路

### 当前问题

`SpawnRangeConfig` 目前只支持世界坐标，无法实现"相对于死亡位置生成"的需求。

### 解决思路：分离范围形状和坐标系统

**核心设计：**
1. `SpawnRangeConfig` 只描述形状和大小（矩形/圆形参数）
2. 提供 `GetRandomLocalOffset()` 方法，返回相对于原点的偏移
3. `Spawner` 负责坐标转换（原点 + 偏移 = 最终位置）

**两种使用方式：**

#### 世界坐标生成
```
origin = Vector3.zero
finalPosition = origin + rangeConfig.GetRandomLocalOffset()
→ 直接使用世界坐标范围
```

#### 相对坐标生成
```
origin = enemyDeathPosition
finalPosition = origin + rangeConfig.GetRandomLocalOffset()
→ 相对于死亡位置偏移
```

**优势：**
- `SpawnRangeConfig` 保持无状态，可序列化
- 清晰分离"范围形状"和"坐标系统"
- 灵活支持两种坐标模式

---

## 重构后的架构流程

### 场景1：敌人波次生成

```
回合开始事件
  ↓
WaveSpawnTrigger（检查是否应该生成敌人）
  ↓
ListSpawnStrategy（返回当前波次的敌人列表）
  ↓
WorldSpaceRangeConfig（计算世界坐标位置）
  ↓
EnemySpawner（实例化敌人）
```

### 场景2：技能道具生成

```
玩家回合开始 + 技能激活
  ↓
SkillItemSpawnTrigger（检查技能是否激活）
  ↓
FixedSpawnStrategy（返回固定的2个道具）
  ↓
WorldSpaceRangeConfig（计算世界坐标位置）
  ↓
ItemSpawner（实例化道具）
```

### 场景3：击杀掉落

```
敌人死亡事件
  ↓
DeathDropTrigger（检查是否应该掉落）
  ↓
ProbabilisticSpawnStrategy（概率抽取掉落道具）
  ↓
RelativeSpaceRangeConfig（计算相对死亡位置的偏移）
  ↓
ItemSpawner（实例化道具）
```

---

## 架构优势

### 1. 职责清晰
- **ConfigProvider**：只管数据存储，不管生成逻辑
- **SpawnStrategy**：只管"生成什么"，不管"何时生成"和"如何生成"
- **Trigger**：只管触发时机和条件判断
- **RangeConfig**：只管位置计算
- **Spawner**：只管实例化

### 2. 高度复用
- 同一个 `ItemSpawner` 可以被多个场景复用
- 同一个 `SpawnRangeConfig` 可以在不同策略中使用
- 策略可以自由组合

### 3. 易于扩展
需要新的生成方式？实现新的 `ISpawnStrategy`
需要新的触发时机？实现新的 `Trigger`
需要新的对象类型？实现新的 `Spawner`

### 4. 配置灵活
- 策划可以独立配置掉落表、波次列表、道具池
- 程序可以灵活组合不同的策略和触发器
- 支持运行时动态调整

---

## 实施方向

### 第一步：定义生成方式接口
- 创建 `ISpawnStrategy<T>` 接口
- 定义 `GetSpawnList()` 方法

### 第二步：实现三种策略
- `ListSpawnStrategy`（复用现有的列表配置）
- `FixedSpawnStrategy`（新增，支持技能道具）
- `ProbabilisticSpawnStrategy`（重构现有的掉落逻辑）

### 第三步：调整范围配置
- `SpawnRangeConfig.GetRandomPosition()` → `GetRandomLocalOffset()`
- `BaseSpawner.CalculateSpawnPosition()` 支持传入origin参数
- 区分世界坐标和相对坐标使用场景

### 第四步：重构现有Trigger
- `WaveSpawnTrigger` 使用 `ListSpawnStrategy`
- `DeathDropTrigger` 使用 `ProbabilisticSpawnStrategy`

### 第五步：实现技能道具生成
- 创建 `SkillItemSpawnTrigger`
- 使用 `FixedSpawnStrategy`
- 集成技能系统的激活检查

---

## 配置示例对比

### 重构前：混乱的职责

```
WaveConfigProvider:
- 存储波次列表
- 提供当前波次数据

DropTableProvider:
- 存储掉落表
- 执行概率抽取 ← 生成逻辑混在配置层
- 提供掉落结果

技能道具生成：？（没有合适的抽象）
```

### 重构后：清晰的分层

```
ConfigProvider（纯数据）:
- WaveConfigProvider: 存储波次列表
- DropTableProvider: 存储掉落表
- ItemPoolProvider: 存储道具池

SpawnStrategy（生成逻辑）:
- ListSpawnStrategy: 返回预设列表
- FixedSpawnStrategy: 生成固定数量
- ProbabilisticSpawnStrategy: 概率抽取

Trigger（触发时机）:
- WaveSpawnTrigger: 回合开始
- SkillItemSpawnTrigger: 技能激活
- DeathDropTrigger: 死亡事件
```

---

## 总结

### 核心改进
1. **独立抽象"生成方式"层**：将"决定生成什么"从配置层剥离
2. **三种生成策略**：列表、固定配置、概率，覆盖所有场景
3. **范围配置改进**：支持世界坐标和相对坐标
4. **职责清晰**：每一层只做一件事

### 架构演进

```
Before:
Trigger → ConfigProvider（配置+逻辑） → Spawner

After:
Trigger → SpawnStrategy（生成逻辑） → RangeConfig（位置） → Spawner
              ↑
         ConfigProvider（纯配置）
```

### 价值
- ✅ 技能道具生成有了合适的抽象
- ✅ 代码复用性大幅提升
- ✅ 扩展新生成方式无需改动现有代码
- ✅ 配置和逻辑完全解耦
- ✅ 易于测试和维护

---

## 实施建议

1. **渐进式重构**：先实现接口和策略，再逐步迁移现有代码
2. **保持向后兼容**：重构期间保留旧接口，测试通过后再删除
3. **充分测试**：每个策略独立测试，确保生成逻辑正确
4. **文档先行**：先完善接口文档，团队理解一致后再实施

通过这次重构，生成系统将达到真正的"高内聚、低耦合"，为后续功能开发提供坚实基础。

