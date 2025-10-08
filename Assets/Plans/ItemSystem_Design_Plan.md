# 道具系统设计计划

## 目标
创建一个模块化、可扩展的道具系统，支持场景中道具的掉落、拾取和效果应用，并与现有的技能系统无缝衔接。

## 系统概述

### 核心功能
1. **道具数据配置** - 通过ScriptableObject配置道具属性
2. **场景道具实体** - 场景中可见、可拾取的道具对象
3. **道具生成系统** - 敌人死亡、波次奖励等触发道具掉落
4. **效果应用系统** - 拾取道具后应用治疗、增益等效果
5. **技能系统集成** - 道具可临时激活技能或修改属性

### 设计原则
- **数据驱动** - 使用ScriptableObject配置，避免硬编码
- **事件解耦** - 通过GameEventBus通信，降低组件耦合度
- **组件化设计** - 职责明确，符合单一职责原则
- **易于扩展** - 支持新增道具类型和效果
- **最小侵入** - 不修改现有核心系统（PlayerCore、EnemyBehavior等）

## 系统架构

### 核心组件（基于SpawnerSystem三层架构）
1. **ItemConfig** - 道具配置ScriptableObject（数据层）
2. **ItemPickup** - 场景中的道具实体组件（表现层）
3. **ItemSpawner** - 道具生成器（执行层）
4. **DeathDropTrigger** - 死亡掉落触发器（决策层）
5. **DropTableProvider** - 掉落表配置提供者（配置层）

### 三层架构集成

#### 配置层 (Configuration Layer)
- **DropTableProvider**: 管理敌人掉落配置，支持概率计算和权重抽取
- **ItemConfig**: 道具基础配置，引用SkillConfig定义效果

#### 决策层 (Strategy Layer)  
- **DeathDropTrigger**: 监听敌人死亡事件，决定掉落时机和内容

#### 执行层 (Execution Layer)
- **ItemSpawner**: 继承BaseSpawner<ItemConfig>，负责道具实例化和位置计算
- **ItemPickup**: 处理碰撞检测、拾取反馈、触发技能

### 职责分离
- **ItemConfig**: 引用SkillConfig，定义道具对应的技能和基础属性
- **DropTableProvider**: 管理掉落表配置，执行概率计算
- **DeathDropTrigger**: 监听死亡事件，查询掉落表，调用ItemSpawner
- **ItemSpawner**: 计算位置、实例化道具、后处理
- **ItemPickup**: 处理碰撞检测、拾取反馈、触发技能
- **SkillSystem**: 完全复用现有的Effect系统处理所有道具效果

### 与现有系统的集成点

| 现有系统 | 集成方式 | 用途 |
|---------|---------|------|
| **SpawnerSystem** | 使用三层架构 | 道具生成完全复用SpawnerSystem |
| **GameEventBus** | 订阅`OnDeath`事件 | 监听敌人死亡触发掉落 |
| **GameEventBus** | 发布`OnItemPickedUp`事件 | 通知UI、音效系统 |
| **PlayerCore** | 调用`Heal()`方法 | 应用治疗效果 |
| **PlayerStatsManager** | 添加`StatModifier` | 应用属性增益效果 |
| **SkillManager** | 临时添加技能实例 | 应用所有道具效果（治疗、增益等） |
| **SkillSystem.IEffect** | 复用Effect实现 | 执行具体效果（HealEffect、StatModifierEffect） |
| **EffectManager** | 播放拾取特效 | 视觉反馈 |

## 详细设计

### 1. ItemConfig (道具配置SO) - 简化设计

**职责：**
- 定义道具的基本信息（名称、描述、图标）
- 引用对应的SkillConfig（效果完全由技能系统处理）
- 配置掉落相关设置

**关键配置：**
```csharp
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Game/Item Config")]
public class ItemConfig : ScriptableObject
{
    [BoxGroup("基本信息")]
    public string itemName;
    public string description;
    public Sprite icon;
    
    [BoxGroup("效果配置")]
    [Tooltip("拾取后触发的技能（治疗、增益等效果由技能的Effect处理）")]
    public SkillConfig itemSkill; // 引用技能配置
    
    [BoxGroup("效果配置")]
    [Tooltip("是否为一次性效果（true=立即执行后移除技能）")]
    public bool isInstantEffect = true;
    
    [BoxGroup("掉落配置")]
    public float dropChance = 0.3f;
    public GameObject itemPrefab; // 场景中的道具预制体
    
    [BoxGroup("视觉配置")]
    public GameObject pickupEffect; // 拾取特效
    public AudioClip pickupSound; // 拾取音效
}
```

**设计优势：**
- ✅ **极简设计** - 只引用SkillConfig，不重复定义效果参数
- ✅ **完全复用** - 所有效果由SkillSystem的Effect处理
- ✅ **支持复杂效果** - 技能的Trigger、Condition、RemovalCondition全部可用
- ✅ **易于配置** - 一个技能可被多个道具复用

**存储位置：** `Assets/Sources/Data/Item/`

**示例配置：**

**治疗道具：**
1. 创建技能：`Skill_InstantHeal_20.asset`
   - Effect类型：Heal
   - 治疗量：20
   - 移除条件：立即移除（NeverRemove）
2. 创建道具：`HealthPotion_Small.asset`
   - 引用技能：`Skill_InstantHeal_20`
   - isInstantEffect：true

**增益道具：**
1. 创建技能：`Skill_DamageBoost_30s.asset`
   - Effect类型：StatModifier（Damage +50%）
   - 移除条件：持续30秒
2. 创建道具：`DamageBoostPotion.asset`
   - 引用技能：`Skill_DamageBoost_30s`
   - isInstantEffect：false（技能会持续存在）

### 2. ItemPickup (场景道具实体) - 简化设计

**职责：**
- 在场景中显示道具
- 检测玩家碰撞触发拾取
- 通过SkillManager激活技能（效果由技能系统处理）
- 播放拾取反馈（特效、音效）
- 销毁自身

**核心功能：**
```csharp
public class ItemPickup : MonoBehaviour
{
    public ItemConfig itemConfig;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. 激活道具对应的技能
            SkillManager skillManager = FindObjectOfType<SkillManager>();
            skillManager.AddTemporarySkill(itemConfig.itemSkill);
            
            // 2. 如果是一次性效果，立即执行并移除
            if (itemConfig.isInstantEffect)
            {
                // Effect会在ExecuteEffect后自动生效
                // 不需要单独处理
            }
            
            // 3. 发布拾取事件
            GameEventBus.PublishItemPickedUp(itemConfig);
            
            // 4. 播放特效和销毁
            PlayPickupEffect();
            Destroy(gameObject);
        }
    }
}
```

**设计优势：**
- ✅ **极简实现** - 只负责触发技能，不处理具体效果逻辑
- ✅ **统一流程** - 所有道具效果都通过SkillManager
- ✅ **自动支持** - 技能的RemovalCondition自动处理持续时间

**预制体结构：**
```
ItemPickup_Prefab
├── SpriteRenderer - 道具图标显示
├── CircleCollider2D - 触发器（Is Trigger = true）
├── ItemPickup (脚本)
└── (可选) 悬浮/旋转动画
```

**注意事项：**
- 使用Trigger碰撞，不影响物理系统
- 拾取后立即销毁，不使用对象池（简化初期实现）
- 可选添加磁吸效果（未来扩展）

### 3. DropTableProvider (掉落表配置提供者)

**职责：**
- 管理敌人类型与掉落表的映射关系
- 执行概率计算和权重抽取
- 支持全局掉落率调整（难度、活动加成）
- 支持掉落数量限制

**数据结构：**
```csharp
[CreateAssetMenu(fileName = "DropTableConfig", menuName = "Game/Drop Table Config")]
public class DropTableConfig : ScriptableObject
{
    [LabelText("敌人类型")]
    public EnemyType enemyType;
    
    [LabelText("最大掉落数量")]
    [MinValue(1)]
    public int maxDropCount = 1;
    
    [LabelText("掉落条目")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<ItemDropEntry> dropEntries = new List<ItemDropEntry>();
}

[System.Serializable]
public class ItemDropEntry
{
    [LabelText("道具配置")]
    public ItemConfig itemConfig;
    
    [LabelText("掉落概率")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;
    
    [LabelText("权重")]
    [MinValue(1)]
    public int weight = 1;
}
```

**核心功能：**
- 根据敌人类型查询对应的掉落表
- 执行概率抽取（支持多物品掉落）
- 应用全局掉落率调整
- 支持掉落数量限制

### 4. DeathDropTrigger (死亡掉落触发器)

**职责：**
- 订阅敌人死亡事件
- 查询DropTableProvider获取掉落配置
- 根据概率决定掉落哪些道具
- 调用ItemSpawner执行生成

**实现要点：**
```csharp
public class DeathDropTrigger : SpawnTrigger<ItemConfig>
{
    [Header("配置")]
    public DropTableProvider dropTableProvider;
    public ItemSpawner itemSpawner;
    
    [Header("掉落设置")]
    public bool enableDropPositionOffset = true;
    public float dropOffsetRange = 0.5f;
    
    protected override void SubscribeEvents()
    {
        GameEventBus.OnDeath += OnEnemyDeath;
    }
    
    protected override void UnsubscribeEvents()
    {
        GameEventBus.OnDeath -= OnEnemyDeath;
    }
    
    private void OnEnemyDeath(DeathData deathData)
    {
        // 1. 验证是否为敌人
        if (!IsEnemy(deathData.target)) return;
        
        // 2. 查询掉落表
        var dropTable = dropTableProvider.GetDropTable(deathData.enemyType);
        if (dropTable == null) return;
        
        // 3. 执行概率抽取
        var itemsToDrop = dropTableProvider.GetItemsToDrop(dropTable);
        if (itemsToDrop.Count == 0) return;
        
        // 4. 计算掉落位置（支持偏移避免重叠）
        var positions = CalculateDropPositions(deathData.position, itemsToDrop.Count);
        
        // 5. 调用ItemSpawner生成道具
        RequestSpawnBatch(itemsToDrop, positions);
    }
}
```

**配置方式：**
- 在SpawnSystemManager中配置DeathDropTrigger
- 拖入DropTableProvider和ItemSpawner引用
- 配置掉落位置偏移等参数

### 5. ItemSpawner (道具生成器)

**职责：**
- 继承BaseSpawner<ItemConfig>
- 计算生成位置（矩形/圆形范围）
- 实例化道具预制体
- 设置ItemPickup组件引用

**实现要点：**
```csharp
public class ItemSpawner : BaseSpawner<ItemConfig>
{
    [Header("生成设置")]
    public Transform spawnParent;
    public SpawnRangeConfig rangeConfig;
    
    protected override GameObject InstantiateObject(ItemConfig itemConfig)
    {
        // 1. 实例化道具预制体
        var prefab = itemConfig.itemPrefab;
        var instance = Instantiate(prefab, spawnParent);
        
        // 2. 设置ItemPickup组件
        var itemPickup = instance.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemConfig(itemConfig);
        }
        
        return instance;
    }
    
    protected override void OnPostSpawn(GameObject obj)
    {
        // 可选：播放生成特效
        // 可选：添加悬浮动画
        // 可选：启动存活时间定时器
    }
}
```

**特殊功能：**
- 支持道具堆叠生成（多个道具重叠时自动散开）
- 支持道具生存时间（N秒后自动销毁）
- 支持磁吸效果（道具自动飞向玩家）

### 5. SkillSystem扩展 - 新增HealEffect和ItemSpawnEffect

**职责：**
- 扩展SkillSystem支持治疗效果和道具生成
- 复用现有的Effect系统架构

**需要新增的Effect：**

#### HealEffect（治疗效果）✅ 已完成
**实现状态：** 已实现并集成到技能系统中
- 文件位置：`Assets/Scripts/SkillSystem/Effects/HealEffect.cs`
- 功能完整：支持治疗量设置、血量恢复、UI更新、事件触发
- 已集成到`EffectConfig.CreateEffect()`方法中
- 支持详细的调试日志和错误处理

#### ItemSpawnEffect（道具生成效果）
```csharp
/// <summary>
/// 道具生成效果 - 在指定位置生成道具
/// </summary>
public class ItemSpawnEffect : IEffect
{
    public string EffectName => "ItemSpawnEffect";
    
    private ItemConfig itemToSpawn;
    private ItemSpawner itemSpawner;
    private Vector3 spawnPosition;
    
    public void SetItemToSpawn(ItemConfig item, ItemSpawner spawner, Vector3 position)
    {
        itemToSpawn = item;
        itemSpawner = spawner;
        spawnPosition = position;
    }
    
    public bool ExecuteEffect(object eventData)
    {
        if (itemToSpawn != null && itemSpawner != null)
        {
            itemSpawner.Spawn(itemToSpawn, spawnPosition);
            Debug.Log($"[ItemSpawnEffect] 生成道具: {itemToSpawn.itemName}");
            return true;
        }
        return false;
    }
    
    public void Reset()
    {
        // 道具生成是瞬时效果，无需重置
    }
}
```

**更新SkillEffectConfig：**
```csharp
public enum SkillEffectType 
{ 
    StatModifier,  // 现有 - 属性修改
    Heal,          // ✅ 已完成 - 治疗效果
    ItemSpawn,     // 新增 - 道具生成
}

// 在SkillEffectConfig中添加配置
[ShowIf("effectType", SkillEffectType.Heal)]
public float healAmount = 20f;  // ✅ 已实现

[ShowIf("effectType", SkillEffectType.ItemSpawn)]
public ItemConfig itemToSpawn;  // 待实现

[ShowIf("effectType", SkillEffectType.ItemSpawn)]
public Vector3 spawnPosition = Vector3.zero;  // 待实现
```

**设计优势：**
- ✅ **统一架构** - 治疗效果和道具生成都走Effect系统
- ✅ **完全复用** - 无需ItemEffectApplier
- ✅ **易于扩展** - 未来可添加Shield、Damage等Effect
- ✅ **灵活触发** - 支持任意技能触发条件

## 技能系统集成方案（统一方案）

### **唯一方案：物品 = 一次性/临时技能**

**核心思想：**
- 所有道具都引用SkillConfig
- 拾取道具 = 激活对应技能
- 效果完全由技能的Effect系统处理

**架构优势：**
- ✅ **完全统一** - 无需区分"道具效果"和"技能效果"
- ✅ **零重复代码** - 不需要ItemEffectApplier
- ✅ **自动继承** - 技能的Trigger、Condition、RemovalCondition全部可用
- ✅ **极简实现** - ItemPickup只需调用SkillManager

**实现方式：**

**1. 扩展SkillManager支持临时技能：**
```csharp
// 在SkillManager中添加
public void AddTemporarySkill(SkillConfig skillConfig)
{
    var instance = skillConfig.CreateSkillInstance();
    
    // 如果是一次性技能，立即执行Effect
    if (skillConfig.isInstantSkill)
    {
        instance.effect.ExecuteEffect(null);
        // 不添加到管理列表，执行完就结束
    }
    else
    {
        // 持续性技能，添加到管理列表
        tempSkillInstances.Add(instance);
        SubscribeSkillEvents(instance);
    }
}
```

**2. 扩展SkillConfig支持一次性技能：**
```csharp
// 在SkillConfig中添加
[BoxGroup("技能属性")]
[Tooltip("是否为一次性技能（立即执行Effect后不保留）")]
public bool isInstantSkill = false;
```

**3. 扩展SkillSystem支持Heal和ItemSpawn效果：**
- 创建`HealEffect.cs`和`ItemSpawnEffect.cs`实现`IEffect`接口
- 在`SkillEffectConfig`中添加Heal和ItemSpawn类型
- 在`EffectConfig.CreateEffect()`中支持创建这些Effect

**配置示例：**

**治疗药水：**
1. 创建技能：`Assets/Sources/Data/Skill/Item_HealSmall.asset`
   - skillName: "小型治疗"
   - isInstantSkill: true（一次性）
   - effectType: Heal
   - healAmount: 20
   - triggerConfig: AlwaysTrigger（立即触发）
   - conditionConfig: AlwaysTrue（无条件）
   - removalCondition: NeverRemove（立即执行无需移除）

2. 创建道具：`Assets/Sources/Data/Item/HealthPotion_Small.asset`
   - itemName: "小型治疗药水"
   - itemSkill: 引用上述技能

**伤害增益药水：**
1. 创建技能：`Assets/Sources/Data/Skill/Item_DamageBoost30s.asset`
   - skillName: "伤害增益（30秒）"
   - isInstantSkill: false（持续性）
   - effectType: StatModifier
   - targetStat: "Damage"
   - modifierValue: 1.5（+50%）
   - removalCondition: Duration（30秒后移除）

2. 创建道具：`Assets/Sources/Data/Item/DamageBoostPotion.asset`
   - itemName: "伤害增益药水"
   - itemSkill: 引用上述技能

**撞墙生成道具技能：**
1. 创建技能：`Assets/Sources/Data/Skill/WallCollision_ItemSpawn.asset`
   - skillName: "撞墙生成道具"
   - isInstantSkill: true（一次性）
   - effectType: ItemSpawn
   - itemToSpawn: HealthPotion_Small
   - spawnPosition: 撞墙位置
   - triggerConfig: CollisionTrigger（碰撞触发，设置tag为"Wall"）
   - conditionConfig: AlwaysTrue（无条件）
   - removalCondition: NeverRemove（立即执行无需移除）

**方案总结：**
- 🎯 **一个道具 = 一个技能配置**
- 🎯 **治疗/增益/道具生成全部用Effect实现**
- 🎯 **ItemPickup极简：只调用SkillManager.AddTemporarySkill()**
- 🎯 **支持任意触发条件：碰撞、区域、血量等**
- 🎯 **无需ItemEffectApplier、无需单独的效果处理逻辑**

## 事件系统集成

### 新增事件定义

**在GameEventBus中添加：**
```csharp
// 道具拾取事件
public static event Action<ItemPickupData> OnItemPickedUp;
public static void PublishItemPickedUp(ItemConfig item, Vector3 position)
{
    OnItemPickedUp?.Invoke(new ItemPickupData { Item = item, Position = position });
}
```

**事件数据结构：**
```csharp
public struct ItemPickupData
{
    public ItemConfig Item;
    public Vector3 Position;
    public GameObject Picker; // 拾取者
}
```

**事件流程：**
```
敌人死亡 → GameEventBus.OnDeath
    ↓
EnemyDropSpawner (监听) → 生成道具
    ↓
玩家碰撞道具 → ItemPickup.OnTriggerEnter2D
    ↓
应用效果 → ItemEffectApplier.ApplyEffect
    ↓
发布事件 → GameEventBus.OnItemPickedUp
    ↓
UI更新、音效播放（其他系统监听）
```

## 目录结构规划

### 脚本文件
```
Assets/Scripts/
├── SpawnSystem/                   # 复用SpawnerSystem三层架构
│   ├── ConfigProviders/
│   │   └── DropTableProvider.cs   # 掉落表配置提供者
│   ├── Triggers/
│   │   └── DeathDropTrigger.cs    # 死亡掉落触发器
│   └── Spawners/
│       └── ItemSpawner.cs         # 道具生成器
│
├── ItemSystem/
│   ├── ItemConfig.cs              # 道具配置SO（引用SkillConfig）
│   ├── ItemPickup.cs              # 场景道具实体（触发技能）
│   └── README.md                  # 系统文档
│
└── SkillSystem/Effects/           # 扩展Effect系统
    └── HealEffect.cs              # 新增 - 治疗效果
```

### 数据资源
```
Assets/Sources/Data/
├── Item/
│   ├── Consumables/
│   │   ├── HealthPotion_Small.asset
│   │   ├── HealthPotion_Large.asset
│   │   └── ManaPotion.asset
│   └── Buffs/
│       ├── DamageBoostPotion.asset
│       └── SpeedBoostPotion.asset
│
└── Spawn/DropTables/              # 掉落表配置
    ├── NormalEnemyDropTable.asset
    ├── EliteEnemyDropTable.asset
    └── BossDropTable.asset
```

### 预制体
```
Assets/Prefabs/
└── Items/
    ├── HealthPotion.prefab        # 治疗药水
    ├── DamageBoost.prefab         # 伤害增益
    └── Effects/
        └── ItemPickupEffect.prefab # 拾取特效
```

## Unity配置要求

### 1. Layer和Tag设置

**新增Tag：**
- `Item` - 标记道具对象（可选）

**物理层设置：**
- 确保道具的Collider能与Player的Collider交互
- 在`Physics2D Settings`中检查碰撞矩阵

### 2. 道具预制体配置

**必需组件：**
1. **SpriteRenderer** - 显示道具图标
2. **CircleCollider2D** - 触发器碰撞检测
   - Is Trigger = true
   - 半径根据图标大小调整
3. **ItemPickup脚本** - 拾取逻辑
4. **Rigidbody2D**（可选）- 如需重力或物理效果
   - Body Type = Kinematic
   - Gravity Scale = 0

**可选组件：**
- 旋转动画（Animator或脚本实现）
- 悬浮动画（上下浮动效果）
- 光晕特效（提高可见度）

### 3. 场景配置

**SpawnSystemManager配置：**
1. 在GameManagers场景中添加SpawnSystemManager
2. 配置DeathDropTrigger：
   - configProvider: 拖入DropTableProvider组件
   - spawner: 拖入ItemSpawner组件
   - enableDropPositionOffset: true
   - dropOffsetRange: 0.5

**DropTableProvider配置：**
1. 创建DropTableConfig资源文件
2. 配置敌人类型与掉落表映射：
   - NormalEnemy → NormalEnemyDropTable.asset
   - EliteEnemy → EliteEnemyDropTable.asset  
   - Boss → BossDropTable.asset

**ItemSpawner配置：**
1. 设置spawnParent: 创建Items空物体
2. 配置rangeConfig: 设置生成范围（矩形/圆形）

**示例掉落表配置：**
- 普通敌人：小型治疗药水30%，大型治疗药水10%
- 精英敌人：大型治疗药水50%，伤害增益20%，速度增益15%
- Boss：大型治疗药水x2(100%)，伤害增益80%，速度增益80%，特殊道具50%

### 4. 特效配置（可选）

**拾取特效：**
- 使用EffectManager播放拾取特效
- 配置粒子系统（光芒、星星等）
- 添加音效反馈

## 开发步骤

### ✅ 阶段1：基础数据层
1. 创建`ItemConfig.cs` ScriptableObject
2. 定义枚举类型（ItemType、ItemEffectType）
3. 创建示例道具配置（治疗药水）
4. 在Inspector中验证配置

**验收标准：**
- 可以在Unity中创建ItemConfig资源
- 可以引用SkillConfig资源
- Odin Inspector显示正常
- 配置简洁明了（只引用技能，不重复定义效果参数）

### ✅ 阶段2：场景道具实体
1. 创建`ItemPickup.cs`脚本
2. 实现碰撞检测和拾取逻辑
3. 创建道具预制体（包含图标、碰撞器、脚本）
4. 在场景中手动放置道具测试拾取

**验收标准：**
- 玩家靠近道具可触发拾取
- 拾取后道具消失
- 技能正确添加到SkillManager
- 效果正确生效（治疗、增益等）

### ✅ 阶段3：扩展SkillSystem支持治疗
1. 创建`HealEffect.cs`实现`IEffect`接口
2. 在`SkillEffectConfig`中添加Heal类型
3. 扩展`SkillConfig`添加`isInstantSkill`字段
4. 扩展`SkillManager`添加`AddTemporarySkill`方法
5. 测试治疗技能和增益技能

**验收标准：**
- HealEffect正确恢复生命值
- 一次性技能立即执行后不残留
- 持续性技能按RemovalCondition移除
- 技能系统扩展不影响现有技能

### ✅ 阶段4：SpawnerSystem集成
1. 创建`DropTableProvider.cs`（配置层）
2. 创建`DeathDropTrigger.cs`（决策层）
3. 创建`ItemSpawner.cs`继承`BaseSpawner<ItemConfig>`（执行层）
4. 配置SpawnSystemManager
5. 测试敌人死亡掉落

**验收标准：**
- 敌人死亡时有概率掉落道具
- 道具生成在敌人死亡位置
- 掉落概率符合配置
- 完全复用SpawnerSystem三层架构

### ✅ 阶段5：技能系统集成
1. 扩展SkillManager支持临时技能
2. 创建buff技能配置（伤害提升等）
3. 创建buff道具配置（引用技能）
4. 测试buff道具效果

**验收标准：**
- 拾取buff道具后技能生效
- 技能效果按配置执行
- 技能正确移除（时间或条件）

### ✅ 阶段6：事件和反馈
1. 在GameEventBus中添加道具事件
2. 实现拾取特效播放
3. 添加拾取音效
4. 添加UI提示（可选）

**验收标准：**
- 拾取时播放特效和音效
- 事件正确发布和监听
- UI提示显示正确（如有）

### ✅ 阶段7：测试和优化
1. 性能测试（大量道具同时存在）
2. 边界情况测试（多道具重叠、快速拾取）
3. 平衡性调整（掉落率、效果数值）
4. 代码优化和清理
5. 编写README文档

**验收标准：**
- 系统稳定无bug
- 性能满足要求
- 代码符合规范
- 文档完整清晰

## 扩展规划

### 短期扩展（1-2周内）
1. **道具磁吸效果** - 道具自动飞向玩家
2. **道具存活时间** - N秒后自动消失
3. **稀有度系统** - 不同颜色表示稀有度
4. **掉落表扩展** - 为不同敌人配置不同掉落
5. **技能驱动生成** - 支持碰撞、区域、血量等触发条件生成道具

### 中期扩展（1个月内）
1. **背包系统** - 存储拾取的道具
2. **主动使用** - 按键使用背包中的道具
3. **道具堆叠** - 同类道具数量累加
4. **波次奖励** - 完成波次掉落奖励道具
5. **复杂触发条件** - 连击、时间窗口、组合条件等

### 长期扩展（未来版本）
1. **装备系统** - 永久性道具
2. **装备强化** - 道具升级系统
3. **合成系统** - 多个道具合成新道具
4. **商店系统** - 购买道具
5. **道具组合** - 特定道具组合产生额外效果
6. **动态生成规则** - 根据游戏状态动态调整生成条件

## 性能考虑

### 优化措施
1. **对象池**（未来）- 频繁生成时使用对象池
2. **事件优化** - 避免频繁订阅/取消订阅
3. **碰撞优化** - 使用合适的Collider大小
4. **资源管理** - 及时释放不用的道具实例

### 性能目标
- 场景中同时存在50+道具不卡顿
- 拾取响应时间<50ms
- 生成道具无明显性能峰值

## 测试计划

### 功能测试
- [ ] 道具配置创建和编辑
- [ ] 道具拾取触发
- [ ] 治疗效果应用
- [ ] 属性增益效果
- [ ] 技能buff效果
- [ ] 敌人死亡掉落
- [ ] 掉落概率验证
- [ ] 特效和音效播放
- [ ] 技能驱动道具生成（碰撞、区域、血量等触发条件）

### 边界测试
- [ ] 多个道具重叠拾取
- [ ] 快速连续拾取
- [ ] 满血时拾取治疗道具
- [ ] 同时拾取多种buff道具
- [ ] 道具生成在墙外
- [ ] 极端掉落率（0%、100%）

### 性能测试
- [ ] 100个道具同时存在
- [ ] 连续10秒生成道具
- [ ] 内存占用监控
- [ ] GC压力测试

### 集成测试
- [ ] 与技能系统联动
- [ ] 与PlayerCore交互
- [ ] 与GameEventBus通信
- [ ] 与EffectManager配合

## 验收标准

### 功能完整性
- 所有计划的道具类型可配置
- 拾取逻辑正常工作
- 效果应用准确无误
- 生成系统稳定运行

### 代码质量
- 符合项目编码规范
- 注释清晰完整
- 无冗余代码
- 易于理解和维护

### 性能要求
- 帧率稳定（60fps）
- 无明显卡顿
- 内存占用合理
- GC频率低

### 扩展性
- 易于添加新道具类型
- 易于添加新效果
- 易于添加新生成器
- 配置灵活方便

## 风险评估

### 低风险
- 数据层设计（参考现有SO设计）
- 拾取逻辑实现（标准Unity碰撞）
- 治疗效果应用（直接调用现有接口）

### 中风险
- 技能系统集成（需要扩展SkillManager）
- 复杂效果实现（多重效果、条件效果）
- 性能优化（大量道具时）

### 缓解措施
1. **分阶段实施** - 先实现简单功能再扩展
2. **充分测试** - 每个阶段完成后测试
3. **保持解耦** - 避免对现有系统的侵入性修改
4. **预留接口** - 为未来扩展留出空间

## 总结

道具系统的设计完全基于SpawnerSystem三层架构，实现了配置、决策、执行的完全解耦。系统与技能系统、PlayerCore、EffectManager等现有模块无缝集成，最小化对核心代码的修改。

### 核心优势
1. **完全复用SpawnerSystem** - 道具生成使用统一的三层架构，无需重复实现
2. **高度解耦** - 配置层、决策层、执行层职责清晰，易于测试和维护
3. **技能系统集成** - 道具效果完全由SkillSystem处理，支持复杂效果
4. **易于扩展** - 添加新道具类型只需配置，添加新生成方式只需实现新的Trigger

### 架构价值
- **统一性** - 敌人生成、道具生成、特效生成使用同一套框架
- **可维护性** - 三层架构清晰，修改影响范围可控
- **可测试性** - 各层独立，单元测试友好
- **策划友好** - 配置可视化，调整便捷

通过分阶段实施，先实现基础功能（治疗道具、敌人掉落），再逐步扩展（buff道具、技能集成），最后优化和完善（特效、平衡性），确保开发过程稳健可控。

系统设计充分考虑了可扩展性，为未来的背包系统、装备系统、商店系统等留出了扩展空间，符合游戏长期发展需求。

