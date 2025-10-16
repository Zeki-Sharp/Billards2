# 弱点攻击技能实现方案

## 一、功能概述

### 1.1 核心功能
实现类似英雄联盟剑姬的弱点击破机制：
- 玩家携带"弱点攻击"技能时，所有敌人身上出现可视化的弱点标记
- 攻击命中弱点位置时，造成额外伤害（如 1.5 倍）
- 弱点位置每回合刷新，或击破后立即刷新
- 弱点始终在敌人身上，跟随敌人移动

### 1.2 设计目标
- ✅ 无需修改现有核心代码，通过扩展实现
- ✅ 完全配置化，参数可在 Inspector 调整
- ✅ 基于事件系统，解耦合
- ✅ 易于扩展（支持多种弱点模式）

---

## 二、核心架构设计

### 2.1 系统组件

```
弱点攻击技能系统
├─ 配置层（SkillEffectConfig）
│  └─ 弱点参数配置（预制体、模式、倍率等）
│
├─ 效果层（WeakPointEffect）
│  └─ 实现 IEffect 接口，集成到技能系统
│
├─ 管理层（WeakPointManager）
│  ├─ 单例 MonoBehaviour
│  ├─ 管理所有敌人的弱点数据
│  ├─ 订阅游戏事件（攻击、死亡、阶段变化）
│  └─ 负责弱点生成、刷新、判定、清理
│
└─ 表现层（WeakPointMarker）
   ├─ 弱点标记预制体
   ├─ 可视化显示（UI/Sprite）
   └─ 动画效果（出现、刷新、命中）
```

### 2.2 数据流向

```
【配置阶段】
SkillConfig Asset → SkillEffectConfig → WeakPointEffect → WeakPointManager

【运行时】
技能激活 → 创建管理器 → 扫描敌人 → 实例化标记 → 订阅事件

【战斗过程】
攻击发生 → 事件拦截 → 弱点判定 → 修改伤害 → 应用到敌人
```

---

## 三、弱点生成机制

### 3.1 弱点位置策略（固定4方向）

**采用方案：固定4方向** ⭐

```
敌人周围固定4个位置：
       上 (0°)
        ↑
        |
左 (270°) ← 敌人 → 右 (90°)
        |
        ↓
      下 (180°)

规则：
- 每次随机激活其中1个方向
- 方向索引：0=上, 1=右, 2=下, 3=左
- 预测性强，玩家容易理解和瞄准
- 适合快节奏台球游戏
- 视觉清晰，易于识别

优势：
✅ 实现简单，易于调试
✅ 玩家学习成本低
✅ 适配所有形状的敌人
✅ 后期可扩展到8方向或其他模式
```

### 3.2 位置计算公式

**局部坐标系统**：
- 弱点标记作为敌人的子物体
- 使用相对于敌人的局部坐标
- 敌人移动时，弱点自动跟随

**4方向计算公式**：
```
方向索引映射角度：
  direction = 0 → angle = 0°   (上)
  direction = 1 → angle = 90°  (右)
  direction = 2 → angle = 180° (下)
  direction = 3 → angle = 270° (左)

局部坐标计算：
  localX = cos(angle × π/180) × radius
  localY = sin(angle × π/180) × radius
  localPosition = (localX, localY)

世界坐标转换：
  worldPosition = enemy.transform.TransformPoint(localPosition)

示例（假设半径 = 0.8）：
  方向0(上)   → (0, 0.8)
  方向1(右)   → (0.8, 0)
  方向2(下)   → (0, -0.8)
  方向3(左)   → (-0.8, 0)
```

---

## 四、刷新机制设计

### 4.1 刷新时机

#### 时机1：初始生成
- 敌人出现时，立即生成弱点
- 随机选择一个初始方向

#### 时机2：回合刷新
- 监听：`GameEventBus.OnGameFlowStateChanged`
- 触发：玩家回合开始时
- 行为：所有存活敌人的弱点重新随机位置

#### 时机3：击破刷新（类剑姬）
- 监听：弱点命中事件
- 触发：弱点判定成功时
- 行为：该敌人弱点立即换到其他位置
- 限制：避免刷新到当前位置

#### 时机4：超时刷新（可选）
- 计时器：每个敌人独立计时
- 触发：弱点存在超过N秒未击中
- 行为：自动换位置

### 4.2 刷新策略

**避免重复算法（4方向）**：
```
1. 获取当前弱点方向（0-3）
2. 生成其他3个方向的列表
   例：当前是1(右) → 候选列表 [0, 2, 3]
3. 从候选列表中随机选择一个
4. 更新弱点数据和标记位置

伪代码：
  current = weakPoint.currentDirection  // 假设是1
  candidates = [0, 2, 3]  // 排除当前方向1
  newDirection = Random.Choice(candidates)  // 随机选择
  weakPoint.currentDirection = newDirection
```

**多敌人协调**：
- 每个敌人独立管理弱点
- 刷新时机可统一（回合刷新）或独立（击破刷新）
- 使用字典存储：`Dictionary<Enemy, WeakPointData>`

---

## 五、判定系统设计

### 5.1 判定流程

```
1. 玩家攻击发出
   ↓
2. PlayerAttackManager 发布 AttackData
   ↓
3. GameEventBus.OnAttack 事件触发
   ↓
4. WeakPointManager 订阅者收到事件
   ↓
5. 检查攻击目标是否是敌人
   ↓
6. 检查该敌人是否有弱点数据
   ↓
7. 计算碰撞点与弱点位置的距离
   ↓
8. 如果距离 ≤ 判定半径 → 弱点命中
   ↓
9. 修改 AttackData.Damage *= 倍率
   ↓
10. EnemyBehavior 收到修改后的伤害值
```

### 5.2 判定算法（角度扇区判定）⭐

**核心原理：将敌人周围划分为4个扇区**

```
        扇区0 (上)
      (-45° ~ 45°)
           ↑
    ┌──────┼──────┐
    │      │      │
扇3 │      ●      │ 扇1
(左)│    敌人     │(右)
    │      │      │
    └──────┼──────┘
           ↓
        扇区2 (下)
     (135° ~ 225°)

扇区划分：
- 扇区0(上)：  -45° ~  45°  (弱点方向0)
- 扇区1(右)：   45° ~ 135°  (弱点方向1)
- 扇区2(下)： 135° ~ 225°  (弱点方向2)
- 扇区3(左)： 225° ~ 315°  (弱点方向3)
```

**判定步骤**：
```
步骤1：计算碰撞点相对于敌人中心的角度
  toHit = (碰撞点 - 敌人位置).normalized
  hitAngle = Atan2(toHit.y, toHit.x) × 180/π
  // 得到 -180° ~ 180° 的角度

步骤2：归一化角度到 0-360 范围
  if (hitAngle < 0) hitAngle += 360

步骤3：判断碰撞角度落在哪个扇区
  sectorIndex = Round(hitAngle / 90) % 4

步骤4：比较扇区索引与弱点方向索引
  isWeakPointHit = (sectorIndex == weakPointDirection)

示例：
  弱点在右边(方向1)
  碰撞角度 = 85°
  扇区索引 = Round(85/90) % 4 = 1
  1 == 1 → 命中！✅

  碰撞角度 = 270°（从左边撞击）
  扇区索引 = Round(270/90) % 4 = 3
  3 ≠ 1 → 未命中❌
```

**伪代码实现**：
```csharp
bool IsWeakPointHit(Enemy enemy, Vector3 hitPosition) {
    // 1. 计算碰撞方向
    Vector2 toHit = (hitPosition - enemy.position).normalized;
    float hitAngle = Mathf.Atan2(toHit.y, toHit.x) * Mathf.Rad2Deg;
    
    // 2. 归一化到 0-360
    if (hitAngle < 0) hitAngle += 360f;
    
    // 3. 计算扇区索引（4个扇区，每个90度）
    // 添加45度偏移，使0度对应上方
    float adjustedAngle = (hitAngle + 45f) % 360f;
    int sectorIndex = Mathf.FloorToInt(adjustedAngle / 90f);
    
    // 4. 比较扇区与弱点方向
    WeakPointData data = weakPoints[enemy];
    return sectorIndex == data.currentDirection;
}
```

**优势**：
✅ 符合剑姬机制：必须从正确方向攻击  
✅ 清晰的扇区划分，容易理解  
✅ 避免误判：从左边撞不会命中右边弱点  
✅ 4方向完美映射到4个90度扇区

### 5.3 伤害修改机制

**关键点：事件订阅顺序**
- WeakPointManager 在技能系统订阅 OnAttack
- 由于 C# 事件按订阅顺序执行
- WeakPointManager 修改 AttackData 后
- EnemyBehavior 收到的就是修改后的伤害值

**伤害计算**：
```
原始伤害 = PlayerStatsManager.FinalDamage
弱点伤害 = 原始伤害 × 伤害倍率

示例：
  基础伤害 = 10
  倍率 = 1.5
  弱点伤害 = 10 × 1.5 = 15
```

---

## 六、配置方案

### 6.1 技能配置文件

**位置**：`Assets/Resources/Data/Skill/弱点攻击.asset`

**结构**（SkillConfig）：
```
【基本信息】
- 技能名称：弱点攻击
- 技能描述：敌人身上出现弱点，命中弱点造成 150% 伤害
- 技能标签：common（通用技能）

【触发器配置】
- 触发类型：AlwaysTrue（被动技能，始终生效）

【条件配置】
- 条件类型：AlwaysTrue（无条件）

【效果配置】
- 效果类型：WeakPoint ⭐（新增类型）
- 弱点标记预制体：[拖入 WeakPointMarker.prefab] ⭐
- 判定半径：0.5（单位）
- 伤害倍率：1.5（150%）
- 击中后刷新：true（命中弱点后立即刷新）

注：固定使用4方向模式，每回合开始自动刷新

【重置条件配置】
- 重置类型：Never（持续到战斗结束）

```

### 6.2 预制体配置

**弱点标记预制体**：`Assets/Prefabs/UI/WeakPointMarker.prefab`

**层级结构**：
```
WeakPointMarker (GameObject)
├─ Canvas (World Space)
│  ├─ RenderMode: WorldSpace
│  ├─ ScaleFactor: 0.01
│  └─ Camera: Main Camera
│
└─ Image (标记图标)
   ├─ Sprite: 红色圆圈或箭头
   ├─ Color: Red (1, 0, 0, 0.8)
   ├─ Size: 0.5 × 0.5
   │
   ├─ Animator (可选)
   │  ├─ 脉冲动画（缩放 0.9-1.1）
   │  ├─ 出现动画（淡入+缩放）
   │  ├─ 刷新动画（旋转+闪烁）
   │  └─ 命中动画（爆炸+淡出）
   │
   └─ ParticleSystem (可选)
      └─ 发光粒子效果
```

**脚本组件**：`WeakPointMarker.cs`
```
功能：
- Initialize(Transform enemy, Vector2 offset)：初始化位置
- UpdatePosition(Vector2 newOffset)：刷新位置
- OnHit()：播放命中特效
- Hide()：隐藏并销毁
```

---

## 七、实现步骤

### 阶段1：扩展效果类型
**文件修改**：
1. `SkillEffectConfig.cs`
   - 添加 `WeakPoint` 到 `SkillEffectType` 枚举
   - 添加弱点相关配置字段（预制体、半径、倍率等）
   - 在 `CreateEffect()` 中添加 `WeakPoint` 分支

**配置项（固定4方向版本）**：
```
- weakPointMarkerPrefab: GameObject（预制体引用）⭐
- weakPointRadius: float（判定半径，默认 0.5）
- damageMultiplier: float（伤害倍率，默认 1.5）
- refreshOnHit: bool（击中后是否刷新，默认 true）

注：弱点模式固定为4方向，暂不需要 mode 字段
```

### 阶段2：创建弱点效果类
**新增文件**：`Assets/Scripts/SkillSystem/Effects/WeakPointEffect.cs`

**职责**：
- 实现 `IEffect` 接口
- `Initialize()`：创建 WeakPointManager 单例并配置
- `ExecuteEffect()`：返回 true（持续效果）
- `Reset()`：清理管理器和所有标记

**关键方法**：
```
SetParameters()：接收配置参数
  ↓
Initialize()：创建并配置管理器
  ↓
Manager.Enable()：启动弱点系统
```

### 阶段3：实现弱点管理器
**新增文件**：`Assets/Scripts/SkillSystem/WeakPointManager.cs`

**职责**：
- MonoBehaviour 单例模式
- 管理所有敌人的弱点数据
- 订阅游戏事件（攻击、死亡、阶段变化）
- 负责弱点生成、判定、刷新、清理

**核心数据结构（4方向版本）**：
```
Dictionary<Enemy, WeakPointData> weakPoints

WeakPointData:
- currentDirection: int（当前方向：0=上, 1=右, 2=下, 3=左）
- markerObject: GameObject（标记实例引用）
```

**关键方法（4方向版本）**：
```
Enable()：启动系统
  ├─ InitializeExistingEnemies()：为现有敌人添加弱点
  └─ SubscribeToEvents()：订阅事件

AddWeakPointToEnemy(Enemy)：为单个敌人添加弱点
  ├─ 生成随机方向（0-3）
  ├─ 计算4方向局部坐标
  ├─ 实例化标记预制体
  └─ 设置为敌人子物体

OnAttackEvent(AttackData)：攻击事件处理
  ├─ 计算碰撞方向角度
  ├─ 判定碰撞扇区是否匹配弱点方向（角度判定）⭐
  ├─ 如果命中：修改伤害值（× 倍率）
  └─ 触发命中反馈和刷新

RefreshWeakPoint(Enemy)：刷新弱点位置
  ├─ 从其他3个方向中随机选择（避免重复）
  ├─ 计算新方向的局部坐标
  └─ 更新标记位置

Calculate4DirectionPosition(int direction)：计算4方向坐标
  ├─ 方向转角度（direction × 90°）
  └─ 极坐标转直角坐标

Disable()：关闭系统
  ├─ CleanupAllWeakPoints()：清理所有标记
  └─ UnsubscribeFromEvents()：取消订阅
```

### 阶段4：创建弱点标记预制体
**制作步骤**：
1. 创建空 GameObject：`WeakPointMarker`
2. 添加 Canvas 组件：
   - RenderMode = World Space
   - WorldCamera = Main Camera
3. 添加 Image 子物体：
   - 导入弱点图标 Sprite（红色圆圈/十字准星）
   - 设置大小和颜色
4. 添加动画控制器（可选）：
   - 空闲动画：脉冲缩放
   - 出现动画：淡入
   - 刷新动画：旋转闪烁
   - 命中动画：爆炸淡出
5. 添加 `WeakPointMarker.cs` 脚本组件
6. 保存为预制体

### 阶段5：创建技能配置资产
**操作步骤**：
1. 在 Unity Editor 中：
   - 右键 → Create → Game → Skill Config
2. 配置参数：
   - 技能名称：弱点攻击
   - 触发器：AlwaysTrue
   - 条件：AlwaysTrue
   - 效果类型：选择 `WeakPoint`
3. 配置弱点参数：
   - 拖入 `WeakPointMarker.prefab`
   - 设置模式、半径、倍率等
4. 保存到：`Assets/Resources/Data/Skill/弱点攻击.asset`

### 阶段6：测试验证
**测试用例**：
1. 技能选择测试：
   - 关卡完成后出现技能选项
   - 选择"弱点攻击"技能
   - 验证技能添加到 SkillManager

2. 弱点生成测试：
   - 技能激活后，场景中所有敌人身上出现标记
   - 标记位置在敌人周围（4个方向之一）
   - 标记跟随敌人移动

3. 伤害判定测试（角度扇区）：
   - 弱点在上方，从上方攻击：命中 ✅（伤害 × 1.5）
   - 弱点在上方，从右方攻击：未命中 ❌（正常伤害）
   - 弱点在右方，从右方攻击：命中 ✅
   - 弱点在右方，从下方攻击：未命中 ❌
   - 控制台输出判定日志（碰撞角度、扇区索引、弱点方向）

4. 刷新机制测试：
   - 回合开始时所有弱点刷新
   - 击中弱点后该敌人弱点立即刷新
   - 新位置不同于旧位置

5. 清理测试：
   - 敌人死亡后标记自动销毁
   - 移除技能后所有标记清理
   - 无内存泄漏

---

## 八、扩展功能（后期）

### 8.1 多种弱点模式（未来扩展）
**当前版本**：
- ✅ 固定4方向（已实现）

**可扩展选项**：
- ⏳ 固定8方向（增加难度）
- ⏳ 扇区随机（在4个区域内随机偏移）
- ⏳ 完全随机（360度任意角度）

**实现方式（未来）**：
- 添加 `WeakPointMode` 枚举字段
- 修改 `GenerateRandomDirection()` 支持不同模式
- 在 SkillEffectConfig 中暴露模式选择

### 8.2 反馈增强
**视觉反馈**：
- 命中时相机震动
- 特殊粒子效果（破碎、闪光）
- 伤害数字显示为金色

**音效反馈**：
- 命中时播放暴击音效
- 刷新时播放切换音效

**UI反馈**：
- 屏幕边缘提示"弱点命中！"
- 连续命中计数器

### 8.3 技能升级系统
**升级选项**：
- Lv1：基础弱点（1.5倍伤害）
- Lv2：弱点判定半径 +30%
- Lv3：命中弱点恢复1点生命
- Lv4：伤害倍率提升至 2.0 倍

### 8.4 特殊弱点类型
**变体设计**：
- 冰冻弱点：命中后敌人减速
- 爆炸弱点：命中后范围伤害
- 连锁弱点：命中后传导到附近敌人
- 时限弱点：3秒内必须击破，否则敌人回血

---

## 九、技术要点总结

### 9.1 关键设计原则
✅ **单一职责**：每个组件职责明确
- WeakPointEffect：技能系统接口
- WeakPointManager：核心逻辑管理
- WeakPointMarker：表现层显示

✅ **事件驱动**：解耦合，易扩展
- 通过 GameEventBus 通信
- 不直接修改现有代码

✅ **配置优先**：参数可视化
- Inspector 直接配置
- 无需修改代码即可调整

✅ **生命周期管理**：自动化
- 技能激活时自动创建
- 敌人生成时自动添加
- 技能移除时自动清理

### 9.2 潜在问题及解决方案

**问题1：事件订阅顺序不确定**
- 解决：AttackData 是引用类型，修改会传递给后续订阅者
- 验证：添加日志确认伤害值正确传递

**问题2：敌人死亡后标记未清理**
- 解决：订阅 OnDeath 事件，及时移除字典条目
- 验证：检查字典大小和内存使用

**问题3：新敌人生成时未自动添加弱点**
- 解决方案A：定期扫描（Update 中检查新敌人）
- 解决方案B：添加 OnEnemySpawned 事件（推荐）

**问题4：跨场景技能保持**
- 解决：WeakPointManager 使用 DontDestroyOnLoad
- 验证：场景切换后技能仍然生效

### 9.3 性能优化建议
- 使用对象池管理标记预制体
- 弱点判定使用平方距离（避免开方）
- 批量刷新时使用协程分帧处理
- 字典查询优化（避免频繁遍历）

---

## 十、开发清单

### 10.1 文件清单
**新增文件**：
- [ ] `Assets/Scripts/SkillSystem/Effects/WeakPointEffect.cs`
- [ ] `Assets/Scripts/SkillSystem/WeakPointManager.cs`
- [ ] `Assets/Scripts/SkillSystem/WeakPointMarker.cs`
- [ ] `Assets/Prefabs/UI/WeakPointMarker.prefab`
- [ ] `Assets/Resources/Data/Skill/弱点攻击.asset`
- [ ] `Assets/Plans/WeakPoint_Skill_Design_Plan.md`（本文档）

**修改文件**：
- [ ] `Assets/Scripts/SkillSystem/Configs/EffectConfig.cs`
  - 添加 `WeakPoint` 枚举值
  - 添加弱点配置字段
  - 添加 CreateEffect 分支

### 10.2 开发进度
**Phase 1: 基础功能（核心）** ⭐
- [ ] 扩展 SkillEffectType 枚举（添加 WeakPoint）
- [ ] 在 SkillEffectConfig 添加弱点配置字段
- [ ] 实现 WeakPointEffect 类（IEffect 接口）
- [ ] 实现 WeakPointManager 核心逻辑（4方向）
  - [ ] 敌人扫描和弱点生成
  - [ ] 4方向位置计算
  - [ ] 事件订阅和处理
- [ ] 实现 WeakPointMarker 组件脚本
- [ ] 创建弱点标记预制体（Canvas + Image）
- [ ] 实现距离判定和伤害修改逻辑
- [ ] 创建技能配置资产（弱点攻击.asset）

**Phase 2: 刷新机制**
- [ ] 实现回合刷新
- [ ] 实现击破刷新
- [ ] 避免位置重复算法
- [ ] 订阅敌人死亡事件清理

**Phase 3: 视觉优化**
- [ ] 标记动画（脉冲、出现、刷新）
- [ ] 命中特效（闪光、破碎）
- [ ] 弱点标记 UI 美化

**Phase 4: 测试验证**
- [ ] 单元测试（判定算法）
- [ ] 集成测试（完整流程）
- [ ] 性能测试（大量敌人）
- [ ] Bug 修复和优化

**Phase 5: 扩展功能（可选）**
- [ ] 多种弱点模式
- [ ] 音效和相机震动
- [ ] 伤害数字颜色
- [ ] UI 提示信息

---

## 十一、参考资料

### 11.1 相关系统
- 技能系统架构：`Assets/Plans/Skill_System_Architecture_Plan.md`
- 事件总线设计：`Assets/Plans/GameEventBus_Architecture_Separation_Plan.md`
- 效果配置说明：`Assets/Scripts/SkillSystem/Configs/EffectConfig.cs`

### 11.2 类似实现
- 英雄联盟：剑姬弱点机制
- 暗黑3：精英怪弱点球系统
- 怪物猎人：部位破坏系统

### 11.3 设计参考
- 固定方向策略：降低预测难度，提升可玩性
- 事件驱动架构：保持代码解耦，易于维护
- 配置化设计：快速迭代，平衡调整方便

---

## 十二、版本历史

### v1.0（当前目标）⭐
- ✅ 固定4方向弱点模式
- ✅ 基础距离判定
- ✅ 伤害倍率修改（1.5倍）
- ✅ 回合刷新和击破刷新
- ✅ 简单弱点标记UI
- ✅ 自动敌人管理（生成/死亡/清理）

### v1.1（后期优化）
- ⏳ 视觉增强（动画、粒子特效）
- ⏳ 音效反馈（命中、刷新音效）
- ⏳ 性能优化（对象池、批处理）

### v1.2（扩展功能）
- ⏳ 多种弱点模式（8向、扇区、随机）
- ⏳ 技能升级系统
- ⏳ 弱点连击奖励

### v2.0（未来展望）
- 特殊弱点类型（冰冻、爆炸、连锁）
- 技能升级系统
- 弱点连击奖励
- Boss 专属弱点机制

---

**文档作者**：AI Assistant  
**创建日期**：2025-10-16  
**最后更新**：2025-10-16  
**状态**：设计阶段

