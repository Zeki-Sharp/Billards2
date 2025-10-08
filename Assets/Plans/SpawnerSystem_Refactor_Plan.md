# 生成器系统重构计划（三层架构方案）

## 目标
将现有的敌人生成系统重构为统一的三层架构，实现敌人、道具、特效等对象的生成逻辑完全解耦和复用，为ItemSystem和未来扩展提供坚实的架构基础。

## 核心问题分析

### 现有EnemySpawner的职责混合
当前`EnemySpawner`同时承担了三个职责：
1. **配置管理** - 管理WaveConfig列表、波次索引、初始敌人配置
2. **生成决策** - 被EnemyController调用，决定生成时机
3. **生成执行** - 计算位置、实例化预制体、注册到Controller

### 问题表现
- ❌ WaveConfig与Spawner强耦合，无法独立管理关卡配置
- ❌ 生成触发方式单一（只支持主动调用），无法支持事件驱动
- ❌ 位置计算、实例化等逻辑无法被其他生成器复用
- ❌ 难以为道具系统实现不同的触发方式（死亡掉落、定时刷新等）

## 三层架构设计

### 架构概览

```
┌─────────────────────────────────────────────────┐
│           配置层 (Configuration Layer)           │
│  SpawnConfigProvider<T> - 数据提供者接口         │
│  ├── WaveConfigProvider (波次配置)              │
│  ├── DropTableProvider (掉落表配置)              │
│  └── SpawnPoolProvider (刷新池配置)              │
└─────────────────────────────────────────────────┘
                      ↓ 提供数据
┌─────────────────────────────────────────────────┐
│           决策层 (Strategy Layer)                │
│  SpawnTrigger<T> - 触发器基类                    │
│  ├── WaveSpawnTrigger (波次触发)                 │
│  ├── DeathDropTrigger (死亡掉落触发)             │
│  ├── TimedSpawnTrigger (定时刷新触发)            │
│  └── AreaEnterTrigger (区域触发)                 │
└─────────────────────────────────────────────────┘
                      ↓ 发起请求
┌─────────────────────────────────────────────────┐
│           执行层 (Execution Layer)               │
│  BaseSpawner<T> - 生成器基类                     │
│  ├── EnemySpawner (敌人生成器)                   │
│  ├── ItemSpawner (道具生成器)                    │
│  └── EffectSpawner (特效生成器)                  │
└─────────────────────────────────────────────────┘
```

### 层级职责定义

#### 配置层 (Configuration Layer)
**核心职责：数据管理与查询**

- 接口：`SpawnConfigProvider<T>`
- 职责：
  - 存储和管理配置数据（波次、掉落表、刷新池等）
  - 提供统一的查询接口
  - 支持动态配置调整（难度、活动加成等）
  - 执行概率计算和随机抽取逻辑
- 数据来源：ScriptableObject资源文件
- 独立性：完全独立，可单独测试

#### 决策层 (Strategy Layer)
**核心职责：触发逻辑与生成决策**

- 基类：`SpawnTrigger<T>`
- 职责：
  - 订阅游戏事件或接收外部调用
  - 查询配置层获取生成数据
  - 决定何时、何地、生成什么
  - 调用执行层完成实际生成
- 触发方式：
  - 主动调用（被其他系统触发）
  - 事件驱动（订阅GameEventBus）
  - 定时触发（协程或定时器）
  - 条件触发（进入区域、满足条件等）
- 依赖：配置层 + 执行层

#### 执行层 (Execution Layer)
**核心职责：对象实例化与生成**

- 基类：`BaseSpawner<T>`
- 职责：
  - 位置计算（矩形/圆形/自定义范围）
  - 位置验证（碰撞检测、有效性检查）
  - 对象实例化（Instantiate预制体）
  - 后处理（注册到管理器、播放特效等）
- 纯执行：不关心何时、为何生成，只负责如何生成
- 高复用：所有类型对象的生成逻辑基本相同

## 详细设计

### 1. 配置层设计

#### SpawnConfigProvider<T> (接口)
**作用：** 定义配置提供者的统一接口

**核心方法：**
- `GetSpawnData()` - 获取生成数据列表
- `ShouldSpawn()` - 判断是否应该生成
- `GetSpawnCount()` - 获取生成数量

#### WaveConfigProvider (波次配置提供者)
**作用：** 管理关卡波次配置

**数据结构：**
- 波次配置列表 (`List<WaveConfig>`)
- 当前波次索引
- 是否循环波次

**核心功能：**
- 获取当前波次的敌人配置
- 推进到下一波次
- 计算波次生成数量
- 重置波次索引

**ScriptableObject配置：**
- `LevelConfig.asset` - 存储整个关卡的波次序列
- 支持多关卡配置复用

#### DropTableProvider (掉落表提供者)
**作用：** 管理敌人掉落配置

**数据结构：**
- 敌人类型 → 掉落表映射
- 全局掉落率调整器

**核心功能：**
- 根据敌人类型查询掉落表
- 执行概率抽取（支持多物品）
- 应用全局掉落率调整（难度、活动、幸运值）
- 支持掉落数量限制

**ScriptableObject配置：**
- `DropTableConfig.asset` - 存储所有掉落表
- 每个敌人类型对应一个掉落表

**掉落表数据：**
```
DropTable
├── enemyType (敌人类型)
├── maxDropCount (最大掉落数量)
└── dropEntries (掉落条目列表)
    ├── itemConfig (道具配置)
    ├── dropChance (掉落概率 0-1)
    └── weight (权重，用于多物品抽取)
```

#### SpawnPoolProvider (刷新池提供者 - 未来扩展)
**作用：** 管理定时刷新点配置

**数据结构：**
- 刷新点位置列表
- 刷新间隔时间
- 刷新对象池

**核心功能：**
- 获取可用刷新点
- 标记刷新点占用
- 刷新冷却管理

---

### 2. 决策层设计

#### SpawnTrigger<T> (抽象基类)
**作用：** 定义触发器的统一接口和通用逻辑

**核心成员：**
- `configProvider` - 配置提供者引用
- `spawner` - 生成器引用
- `isActive` - 触发器激活状态

**抽象方法：**
- `SubscribeEvents()` - 订阅游戏事件
- `UnsubscribeEvents()` - 取消事件订阅

**通用方法：**
- `RequestSpawn(T data, Vector3 position)` - 请求生成对象
- `RequestSpawnBatch(List<T> data, List<Vector3> positions)` - 批量生成
- `SetActive(bool active)` - 启用/禁用触发器

#### WaveSpawnTrigger (波次触发器)
**作用：** 接收EnemyController调用，执行波次生成

**触发方式：** 主动调用（被EnemyController.ExecuteTelegraphPhase调用）

**核心流程：**
1. 接收`OnWaveStart()`调用
2. 查询`WaveConfigProvider`获取当前波次配置
3. 遍历波次中的敌人配置
4. 调用`EnemySpawner.Spawn()`生成每个敌人

**配置依赖：**
- `WaveConfigProvider` - 获取波次数据

**生成器依赖：**
- `EnemySpawner` - 执行敌人生成

**与现有系统集成：**
- 替代原有`EnemySpawner.GenerateEnemies()`调用
- `EnemyController.ExecuteTelegraphPhase()`改为调用`waveSpawnTrigger.OnWaveStart()`

#### DeathDropTrigger (死亡掉落触发器)
**作用：** 监听敌人死亡事件，执行道具掉落

**触发方式：** 事件驱动（订阅`GameEventBus.OnDeath`）

**核心流程：**
1. 订阅`GameEventBus.OnDeath`事件
2. 收到死亡事件，提取敌人数据和位置
3. 查询`DropTableProvider`获取掉落配置
4. 根据概率决定掉落哪些道具
5. 调用`ItemSpawner.Spawn()`生成道具

**配置依赖：**
- `DropTableProvider` - 获取掉落表

**生成器依赖：**
- `ItemSpawner` - 执行道具生成

**特殊逻辑：**
- 支持掉落位置偏移（避免重叠）
- 支持掉落数量限制
- 支持Boss特殊掉落

#### TimedSpawnTrigger (定时刷新触发器 - 未来扩展)
**作用：** 定时刷新场景中的对象

**触发方式：** 定时器驱动（协程）

**核心流程：**
1. 启动定时协程
2. 到达刷新时间
3. 查询`SpawnPoolProvider`获取刷新点
4. 调用Spawner生成对象

**应用场景：**
- 场景道具定时刷新
- 怪物刷新点
- 资源采集点重生

#### AreaEnterTrigger (区域触发器 - 未来扩展)
**作用：** 玩家进入区域时触发生成

**触发方式：** 碰撞检测（OnTriggerEnter）

**应用场景：**
- Boss战触发
- 埋伏战
- 场景事件触发

---

### 3. 执行层设计

#### BaseSpawner<T> (抽象基类)
**作用：** 定义生成器的通用逻辑

**通用功能：**
- 位置计算
  - 矩形范围随机
  - 圆形范围随机
  - 指定位置生成
  - 位置偏移
- 位置验证
  - 边界检测
  - 碰撞检测（避免生成在墙内）
  - 有效性检查
- 范围设置
  - `SetSpawnRange(矩形/圆形)`
  - 动态调整生成范围

**抽象方法：**
- `InstantiateObject(T data)` - 实例化对象（子类实现）
- `OnPostSpawn(GameObject obj)` - 生成后处理（可选重写）

**公共接口：**
- `Spawn(T data, Vector3? position)` - 生成单个对象
- `SpawnBatch(List<T> data, List<Vector3> positions)` - 批量生成

#### EnemySpawner : BaseSpawner<EnemyData>
**作用：** 生成敌人对象

**实现细节：**
- `InstantiateObject()`
  - 实例化敌人容器预制体
  - 获取Enemy组件
  - 设置EnemyData
- `OnPostSpawn()`
  - 注册到EnemyController（预告列表或激活列表）
  - （可选）播放生成特效

**与现有系统集成：**
- 保留原有的位置计算逻辑
- 保留原有的注册逻辑
- 移除波次管理相关代码

#### ItemSpawner : BaseSpawner<ItemConfig>
**作用：** 生成道具对象

**实现细节：**
- `InstantiateObject()`
  - 实例化道具预制体（从ItemConfig获取）
  - 获取ItemPickup组件
  - 设置itemConfig引用
- `OnPostSpawn()`
  - （可选）播放生成特效
  - （可选）添加悬浮动画

**特殊功能：**
- 支持道具堆叠生成（多个道具重叠时自动散开）
- 支持道具生存时间（N秒后自动销毁）

#### EffectSpawner : BaseSpawner<EffectConfig> (未来扩展)
**作用：** 生成特效对象

**应用场景：**
- 环境特效
- 战斗特效
- 场景装饰

---

## 改动范围

### 新增文件

#### 脚本文件
```
Assets/Scripts/SpawnSystem/
├── Core/
│   ├── BaseSpawner.cs                    # 生成器基类
│   ├── SpawnTrigger.cs                   # 触发器基类
│   └── SpawnConfigProvider.cs            # 配置提供者接口
│
├── ConfigProviders/
│   ├── WaveConfigProvider.cs             # 波次配置提供者
│   └── DropTableProvider.cs              # 掉落表提供者
│
├── Triggers/
│   ├── WaveSpawnTrigger.cs               # 波次触发器
│   └── DeathDropTrigger.cs               # 死亡掉落触发器
│
├── Spawners/
│   ├── EnemySpawner.cs (重构)            # 敌人生成器
│   └── ItemSpawner.cs                    # 道具生成器
│
└── README.md                              # 系统文档
```

#### 数据资源
```
Assets/Sources/Data/Spawn/
├── LevelConfigs/
│   ├── Level1_Config.asset               # 第1关配置
│   └── Level2_Config.asset               # 第2关配置
│
└── DropTables/
    ├── NormalEnemyDropTable.asset        # 普通敌人掉落表
    ├── EliteEnemyDropTable.asset         # 精英敌人掉落表
    └── BossDropTable.asset               # Boss掉落表
```

### 修改文件

#### EnemySpawner.cs (重大重构)
**改动内容：**
- 继承`BaseSpawner<EnemyData>`
- 移除波次管理相关代码（移至WaveConfigProvider）
- 移除`GenerateEnemies()`方法（由WaveSpawnTrigger调用Spawn）
- 保留位置计算和实例化逻辑
- 实现`InstantiateObject()`和`OnPostSpawn()`

**兼容性：**
- 保留原有的公共接口（SetSpawnRange等）
- 生成逻辑保持不变，只是调用方式改变

#### EnemyController.cs (小幅修改)
**改动内容：**
- 引用`WaveSpawnTrigger`替代`EnemySpawner`
- `ExecuteTelegraphPhase()`中调用`waveSpawnTrigger.OnWaveStart()`
- 其他逻辑保持不变

**改动范围：** 仅修改生成敌人的调用方式

#### WaveConfig.cs (数据结构调整)
**改动内容：**
- 保持现有数据结构不变
- 添加新的配置选项（可选）
  - 波次描述
  - 波次奖励配置

**兼容性：** 完全向后兼容

### 无需修改的文件
- `EnemyPhaseController.cs` - 不涉及生成逻辑
- `Enemy.cs` / `EnemyBehavior.cs` - 敌人实体逻辑不变
- `PlayerCore.cs` - 玩家逻辑不变
- `GameEventBus.cs` - 只订阅现有事件
- 所有SkillSystem文件 - 技能系统独立

---

## 分阶段实施计划

### 阶段0：准备工作（0.5天）
**目标：** 创建基础架构和接口定义

**任务清单：**
- [ ] 创建目录结构 `Assets/Scripts/SpawnSystem/`
- [ ] 定义`SpawnConfigProvider<T>`接口
- [ ] 定义`SpawnTrigger<T>`抽象基类
- [ ] 定义`BaseSpawner<T>`抽象基类
- [ ] 编写系统架构文档README.md

**验收标准：**
- 三层接口定义清晰，注释完整
- 编译通过，无错误

---

### 阶段1：提取配置层（1天）
**目标：** 将波次配置从EnemySpawner中分离

**任务清单：**
- [ ] 创建`WaveConfigProvider.cs`
- [ ] 将`WaveConfig`相关逻辑移入Provider
- [ ] 创建`LevelConfig` ScriptableObject
- [ ] 在Unity中创建测试关卡配置
- [ ] 编写配置层单元测试

**验收标准：**
- `WaveConfigProvider`可独立查询波次数据
- 可以在Inspector中配置关卡波次
- 单元测试通过

**风险点：**
- 无，纯新增代码

---

### 阶段2：重构EnemySpawner（1天）
**目标：** 将EnemySpawner改造为纯生成器

**任务清单：**
- [ ] 实现`BaseSpawner<T>`的通用逻辑（位置计算、验证）
- [ ] `EnemySpawner`继承`BaseSpawner<EnemyData>`
- [ ] 移除波次管理代码
- [ ] 实现`InstantiateObject()`方法
- [ ] 实现`OnPostSpawn()`方法（注册到Controller）
- [ ] 保留向后兼容的公共接口

**验收标准：**
- `EnemySpawner`编译通过
- 可以通过`Spawn(EnemyData, Vector3)`生成敌人
- 生成的敌人正常注册到EnemyController

**风险点：**
- ⚠️ 中等风险 - 改动现有核心代码
- **缓解措施**：
  - 先保留原有代码，新增新方法
  - 充分测试后再删除旧代码
  - 使用Git分支进行重构

---

### 阶段3：实现WaveSpawnTrigger（0.5天）
**目标：** 创建波次触发器，替代原有调用方式

**任务清单：**
- [ ] 创建`WaveSpawnTrigger.cs`
- [ ] 实现`OnWaveStart()`方法
- [ ] 集成`WaveConfigProvider`和`EnemySpawner`
- [ ] 修改`EnemyController.ExecuteTelegraphPhase()`调用

**验收标准：**
- 波次生成功能完全正常
- 敌人数量、类型与之前一致
- EnemyPhase流程正常运行

**风险点：**
- ⚠️ 中等风险 - 改变现有调用流程
- **缓解措施**：
  - 保留原有方法作为fallback
  - 添加Debug日志对比新旧流程

---

### 阶段4：测试与验证（0.5天）
**目标：** 确保敌人生成系统重构后功能完全正常

**测试项目：**
- [ ] 初始敌人生成正常
- [ ] 波次敌人生成正常
- [ ] 波次循环正常
- [ ] 敌人生成位置正确（矩形/圆形范围）
- [ ] 敌人正常注册到预告列表
- [ ] EnemyPhase流程完整（预告→生成→攻击→移动）
- [ ] 性能无明显下降

**验收标准：**
- 所有现有功能正常工作
- 无新增Bug
- 帧率稳定

**风险点：**
- 低风险 - 功能性测试

---

### 阶段5：实现道具生成系统（1天）
**目标：** 基于新架构实现ItemSystem的生成部分

**任务清单：**
- [ ] 创建`DropTableProvider.cs`
- [ ] 创建`DropTableConfig` ScriptableObject
- [ ] 配置测试掉落表（普通敌人、精英、Boss）
- [ ] 创建`ItemSpawner.cs`继承`BaseSpawner<ItemConfig>`
- [ ] 实现`InstantiateObject()`方法
- [ ] 创建`DeathDropTrigger.cs`
- [ ] 订阅`GameEventBus.OnDeath`事件
- [ ] 实现概率判断和生成逻辑
- [ ] **集成技能系统查询**
  - [ ] 在`DropTableProvider`中添加技能查询逻辑
  - [ ] 在`DeathDropTrigger`中检查玩家激活技能
  - [ ] 实现技能影响掉落率的功能（如双倍掉落）
  - [ ] 测试技能与掉落系统的集成

**验收标准：**
- 敌人死亡时有概率掉落道具
- 道具生成在死亡位置
- 掉落概率符合配置
- 支持多物品掉落
- 技能系统正确影响掉落（如双倍掉落技能）
- 技能与掉落系统集成无冲突

**风险点：**
- 低风险 - 纯新增功能，不影响现有系统

---

### 阶段6：集成ItemPickup和效果系统（0.5天）
**目标：** 完成道具拾取和效果应用

**任务清单：**
- [ ] 确保`ItemSpawner`生成的道具包含`ItemPickup`组件
- [ ] `ItemPickup`配置正确引用`ItemConfig`
- [ ] 测试道具拾取触发
- [ ] 测试道具效果应用（治疗、增益）
- [ ] 测试拾取特效和音效

**验收标准：**
- 玩家碰撞道具可拾取
- 道具效果正确生效
- 特效和音效正常播放

---

### 阶段7：优化与扩展（1天）
**目标：** 完善掉落系统和配置工具

**任务清单：**
- [ ] 实现全局掉落率调整（难度、活动加成）
- [ ] 实现道具位置偏移（避免重叠）
- [ ] 为不同敌人类型配置不同掉落表
- [ ] 添加Boss特殊掉落逻辑
- [ ] 优化掉落表编辑体验（Odin Inspector）
- [ ] 添加掉落调试工具（强制掉落、掉落日志）

**验收标准：**
- 掉落率可动态调整
- 多物品掉落不重叠
- Boss掉落奖励丰厚
- 配置界面友好

---

### 阶段8：文档与清理（0.5天）
**目标：** 完善文档和代码清理

**任务清单：**
- [ ] 编写完整的系统使用文档
- [ ] 添加代码注释和XML文档
- [ ] 清理测试代码和Debug日志
- [ ] 更新ItemSystem设计文档
- [ ] 录制系统使用演示视频（可选）

**验收标准：**
- 文档完整清晰
- 代码注释规范
- 无冗余代码

---

## Unity配置要求

### Scene配置

#### GameManagers场景
```
GameManagers
├── SpawnSystemManager (新增)
│   ├── WaveConfigProvider
│   ├── DropTableProvider
│   ├── WaveSpawnTrigger
│   │   ├── configProvider: WaveConfigProvider
│   │   └── spawner: EnemySpawner
│   └── DeathDropTrigger
│       ├── configProvider: DropTableProvider
│       └── spawner: ItemSpawner
│
├── EnemySpawner (重构后)
│   ├── spawnParent: Transform
│   ├── rangeConfig: (矩形/圆形配置)
│   └── (移除waveConfigs等字段)
│
└── ItemSpawner (新增)
    ├── spawnParent: Transform
    └── rangeConfig: (可选位置偏移)
```

### ScriptableObject配置

#### 关卡配置
```
Assets/Sources/Data/Spawn/LevelConfigs/
├── Level1_Config.asset
│   ├── levelName: "关卡1"
│   ├── waves: List<WaveConfig>
│   │   ├── Wave 0: [NormalEnemy x3]
│   │   ├── Wave 1: [NormalEnemy x5, EliteEnemy x1]
│   │   └── Wave 2: [NormalEnemy x3, Boss x1]
│   └── loopWaves: true
```

#### 掉落表配置
```
Assets/Sources/Data/Spawn/DropTables/
├── NormalEnemyDropTable.asset
│   ├── enemyType: Normal
│   ├── maxDropCount: 1
│   └── dropEntries:
│       ├── HealthPotion_Small (30%)
│       └── HealthPotion_Large (10%)
│
├── EliteEnemyDropTable.asset
│   ├── enemyType: Elite
│   ├── maxDropCount: 2
│   └── dropEntries:
│       ├── HealthPotion_Large (50%)
│       ├── DamageBoost (20%)
│       └── SpeedBoost (15%)
│
└── BossDropTable.asset
    ├── enemyType: Boss
    ├── maxDropCount: 5
    └── dropEntries:
        ├── HealthPotion_Large x2 (100%)
        ├── DamageBoost (80%)
        ├── SpeedBoost (80%)
        └── SpecialItem (50%)
```

### Inspector配置提示

#### WaveConfigProvider组件配置
- 拖入`LevelConfig.asset`资源
- 设置初始波次索引
- 勾选是否循环波次

#### DropTableProvider组件配置
- 添加敌人类型与掉落表映射
  - Key: EnemyType枚举
  - Value: DropTableConfig资源
- 设置全局掉落率倍数（测试时可调高）

#### WaveSpawnTrigger组件配置
- configProvider: 拖入WaveConfigProvider组件
- spawner: 拖入EnemySpawner组件

#### DeathDropTrigger组件配置
- configProvider: 拖入DropTableProvider组件
- spawner: 拖入ItemSpawner组件
- enableDropPositionOffset: true（避免道具重叠）
- dropOffsetRange: 0.5（偏移范围）

---

## 扩展规划

### 短期扩展（实施完成后1周内）
1. **道具磁吸效果**
   - 在ItemSpawner中添加磁吸逻辑
   - 道具自动飞向玩家

2. **道具存活时间**
   - ItemSpawner生成时启动定时器
   - N秒后自动销毁

3. **掉落率调试工具**
   - Inspector按钮：强制100%掉落
   - 掉落日志记录

### 中期扩展（1个月内）
1. **波次奖励系统**
   - 创建`WaveRewardProvider`
   - 创建`WaveRewardTrigger`
   - 订阅波次完成事件

2. **宝箱系统**
   - 创建`ChestLootProvider`
   - 创建`ChestOpenTrigger`
   - 与ItemSpawner集成

3. **定时刷新点**
   - 创建`SpawnPoolProvider`
   - 创建`TimedSpawnTrigger`
   - 支持场景道具定时刷新

### 长期扩展（未来版本）
1. **动态难度调整**
   - 根据玩家表现调整生成数量
   - 根据战斗时长调整掉落率

2. **区域触发生成**
   - 创建`AreaEnterTrigger`
   - 支持埋伏战、Boss战触发

3. **成就系统集成**
   - 特定敌人击杀掉落特殊道具
   - 连杀奖励提升掉落率

---

## 风险评估与缓解

### 高风险项
无

### 中风险项

#### 风险1：EnemySpawner重构破坏现有功能
**影响范围：** 敌人生成、波次管理

**缓解措施：**
- 使用Git功能分支开发，保留main分支稳定版本
- 先实现新方法，保留旧方法，测试通过后再删除
- 充分测试所有波次生成场景
- 回归测试EnemyPhase完整流程

#### 风险2：调用流程改变导致时序问题
**影响范围：** EnemyController与Spawner交互

**缓解措施：**
- 保持原有的调用时机不变
- 添加详细的Debug日志
- 对比新旧流程的执行顺序
- 单步调试验证

### 低风险项

#### 风险3：配置数据迁移
**影响范围：** 现有WaveConfig数据

**缓解措施：**
- 新配置结构向后兼容
- 提供数据迁移工具（可选）
- 保留原有配置文件作为参考

#### 风险4：性能影响
**影响范围：** 生成器调用链增加

**缓解措施：**
- 使用Profiler监控性能
- 优化频繁调用的路径
- 生成器采用对象池（如需要）

---

## 验收标准

### 功能完整性
- [ ] 敌人生成功能完全正常（初始、波次、循环）
- [ ] 道具掉落功能正常（概率、位置、效果）
- [ ] 所有现有功能无回退
- [ ] 新功能按设计实现

### 代码质量
- [ ] 三层架构清晰，职责分离明确
- [ ] 代码符合项目规范
- [ ] 注释完整，可读性强
- [ ] 无冗余代码，无硬编码

### 性能要求
- [ ] 帧率稳定（60fps）
- [ ] 生成大量对象无卡顿
- [ ] 内存占用合理
- [ ] GC频率低

### 扩展性
- [ ] 易于添加新的ConfigProvider
- [ ] 易于添加新的SpawnTrigger
- [ ] 易于添加新的Spawner类型
- [ ] 配置灵活，策划友好

### 文档完整性
- [ ] 系统架构文档完整
- [ ] 使用手册清晰
- [ ] 代码注释规范
- [ ] 配置说明详细

---

## 时间估算

### 总计：约 6-7 工作日

| 阶段 | 预计时间 | 累计时间 |
|------|---------|---------|
| 阶段0：准备工作 | 0.5天 | 0.5天 |
| 阶段1：提取配置层 | 1天 | 1.5天 |
| 阶段2：重构EnemySpawner | 1天 | 2.5天 |
| 阶段3：实现WaveSpawnTrigger | 0.5天 | 3天 |
| 阶段4：测试与验证 | 0.5天 | 3.5天 |
| 阶段5：实现道具生成系统 | 1天 | 4.5天 |
| 阶段6：集成ItemPickup | 0.5天 | 5天 |
| 阶段7：优化与扩展 | 1天 | 6天 |
| 阶段8：文档与清理 | 0.5天 | 6.5天 |
| **缓冲时间** | 0.5天 | **7天** |

---

## 总结

本重构计划将现有的敌人生成系统升级为统一的三层架构，实现了配置、决策、执行的完全解耦。新架构不仅解决了当前的职责混合问题，更为道具系统和未来扩展提供了坚实的基础。

### 核心价值
1. **统一架构** - 所有对象生成共享同一套框架
2. **高度解耦** - 配置、逻辑、执行完全分离
3. **极强扩展性** - 添加新功能只需实现新的Provider或Trigger
4. **易于测试** - 各层独立，单元测试友好
5. **策划友好** - 配置可视化，调整便捷

### 实施建议
- 严格按照阶段顺序实施，确保每个阶段验收通过后再进入下一阶段
- 充分利用Git分支管理，降低风险
- 及时编写文档，方便团队理解
- 预留缓冲时间应对意外情况

通过本次重构，项目架构将更加清晰，代码质量将显著提升，为后续开发奠定坚实基础。

