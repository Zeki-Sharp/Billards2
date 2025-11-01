# 伤害系统架构重构方案

> **目的**：设计规则驱动的松耦合伤害架构，支持复杂伤害场景
>
> **关联文档**：`GC2_Behavior_VS_Current_Architecture_Analysis.md`（行为系统主文档）

---

## 一、当前问题诊断

### 1.1 核心问题

| 问题 | 表现 | 影响 |
|------|------|------|
| **伤害条件分散** | 碰撞、停止、技能各处判断 | 难以维护、逻辑重复 |
| **状态检查硬编码** | `if (isDashing && tag == "Player")` | 扩展困难、条件纠缠 |
| **逻辑与触发混合** | 碰撞代码包含伤害计算 | 职责不清、耦合严重 |
| **缺少事件抽象** | 直接调用 `TakeDamage()` | 无法拦截、难以扩展 |

### 1.2 典型场景问题

**场景 1：敌人冲刺撞玩家**
```
当前实现问题：
- 需要在 EnemyBehavior 添加 isDashing 标志
- 需要在 OnCollisionEnter2D 检查 isDashing
- 需要在冲刺行为设置/清理 isDashing
- 伤害计算分散在碰撞代码中

结果：状态检查分散、伤害逻辑耦合、无法复用
```

**场景 2：敌人撞墙受伤**
```
当前实现问题：
- Wall 组件需要实现伤害逻辑
- Wall 需要获取敌人速度计算伤害
- 玩家撞墙不受伤，规则硬编码

结果：墙壁职责不清、速度获取困难、规则不统一
```

---

## 二、理想架构设计

### 2.1 核心原则

1. **规则驱动**：伤害条件通过配置定义
2. **事件驱动**：伤害通过事件传递
3. **职责分离**：伤害计算、判断、应用分离
4. **可配置性**：ScriptableObject 配置复杂规则

### 2.2 架构分层

```
[配置层] DamageRuleConfig (ScriptableObject)
    ↓ 定义规则
[判断层] DamageSystem (Manager)
    ↓ 规则检查、基础伤害计算
[修改层] DamageProcessor (Manager)
    ↓ 伤害修改器链（弱点、暴击、技能加成）
[事件层] DamageEvent
    ↓ 传递最终伤害
[接收层] IDamageable (Interface)
    ↓ 接收伤害、应用效果
[反馈层] DamageText, VFX, SFX
```

**职责划分**：

| 层级 | 职责 | 示例 |
|------|------|------|
| **配置层** | 定义伤害规则 | "冲刺状态撞玩家 = 基础伤害 10" |
| **判断层** | 决定是否造成伤害 | 规则匹配、状态检查 |
| **修改层** | 修改伤害数值 | 弱点 ×2、暴击 ×1.5、护盾 -50% |
| **事件层** | 传递伤害信息 | DamageEvent(source, target, finalDamage) |
| **接收层** | 应用伤害 | currentHealth -= finalDamage |

---

## 三、核心组件设计

### 3.1 DamageRuleConfig（规则配置）

**ScriptableObject 定义**：
```
DamageRuleConfig {
    // 触发条件
    TriggerType: Collision/Stopped/Interval
    SourceTag: "Enemy"
    TargetTag: "Player"
    
    // 状态要求（可选）
    RequireSourceState: "IsDashing"
    
    // 速度要求（可选）
    MinVelocity: 0
    VelocityMultiplier: 0.5
    
    // 伤害计算
    BaseDamage: 10
    DamageMultiplier: 2.0
    
    // 目标过滤
    AffectPlayer: true
    AffectEnemy: false
    
    // 附加效果
    KnockbackForce: 5.0
    SelfDamage: false  // 是否对自己造成伤害
}
```

**多规则组合示例**：
```
EntityDamageProfile {
    Rules: [
        // 冲刺撞击玩家
        {
            TriggerType: Collision,
            TargetTag: "Player",
            RequireSourceState: "IsDashing",
            BaseDamage: 15,
            DamageMultiplier: 2.0
        },
        
        // 撞墙受伤
        {
            TriggerType: Collision,
            TargetTag: "Wall",
            BaseDamage: 5,
            VelocityMultiplier: 1.0,
            SelfDamage: true  // 伤害目标是自己
        }
    ]
}
```

---

### 3.2 DamageEvent（伤害事件）

```
DamageEvent {
    GameObject Source       // 伤害来源
    GameObject Target       // 伤害目标
    float FinalDamage       // 最终伤害
    
    DamageType Type         // Physical/Magical/True
    DamageTrigger Trigger   // Collision/Stopped/Skill
    
    Vector2 HitPosition     // 碰撞位置
    Vector2 HitDirection    // 碰撞方向
    float VelocityAtHit     // 碰撞时速度
    
    float KnockbackForce    // 击退力度
    float StunDuration      // 眩晕时长
    
    DamageRuleConfig Rule   // 触发的规则
}
```

---

### 3.3 DamageSystem（系统管理器）

**职责**：
1. 注册实体和其伤害规则
2. 监听触发事件（碰撞、停止等）
3. 检查规则条件
4. 计算伤害值
5. 发布 DamageEvent

**核心流程**：
```
1. 注册阶段
RegisterEntity(entity, damageProfile)

2. 触发阶段
OnCollisionEvent(evt) {
    profile = GetDamageProfile(evt.source)
    
    foreach (rule in profile.Rules) {
        if (!CheckRule(rule, evt.source, evt.target)) continue;
        damage = CalculateDamage(rule, evt.source, evt.target)
        PublishDamageEvent(evt.source, evt.target, damage, rule)
    }
}

3. 规则检查
CheckRule(rule, source, target) {
    // 检查标签、状态、速度
    if (!target.CompareTag(rule.TargetTag)) return false;
    if (rule.RequireSourceState != "" && 
        !GetBlackboard(source).Get<bool>(rule.RequireSourceState)) return false;
    return true;
}

4. 伤害计算
CalculateDamage(rule, source, target) {
    damage = rule.BaseDamage * rule.DamageMultiplier;
    if (rule.VelocityMultiplier > 0) {
        damage += GetVelocity(source) * rule.VelocityMultiplier;
    }
    return damage;
}
```

---

### 3.4 DamageProcessor（修改器层）

**职责**：伤害修改器链管理（保留现有实现）

**核心接口**：
```csharp
public interface IDamageModifier {
    string ModifierName { get; }
    int Priority { get; }        // 优先级（越小越先执行）
    bool IsEnabled { get; }
    bool ProcessDamage(ref AttackData attackData);
}
```

**工作流程**：
```csharp
DamageSystem.ProcessDamage(rule, source, target) {
    // 1. 计算基础伤害
    float baseDamage = rule.BaseDamage * rule.DamageMultiplier;
    
    // 2. 创建攻击数据
    var attackData = new AttackData {
        Attacker = source,
        Target = target,
        Damage = baseDamage,
        AttackType = AttackType.Collision
    };
    
    // 3. 调用修改器链（弱点、暴击、技能加成等）
    DamageProcessor.Instance.ProcessDamage(ref attackData);
    
    // 4. 发布最终伤害
    PublishDamageEvent(attackData);
}
```

**常见修改器**：
- WeakPointModifier：弱点伤害 ×2
- CriticalHitModifier：暴击 ×1.5
- ShieldModifier：护盾吸收 50%
- SkillBonusModifier：技能加成（从 PlayerStats 读取）

**优势**：
- ✅ 职责分离：规则判断 vs 数值修改
- ✅ 易于扩展：新增修改器不影响 DamageSystem
- ✅ 优先级控制：确保执行顺序正确

---

### 3.5 IDamageable（受伤接口）

```csharp
public interface IDamageable {
    void OnDamageReceived(DamageEvent damageEvent);
    bool CanTakeDamage();
    float GetCurrentHealth();
}

// 实现示例
PlayerBehavior : IDamageable {
    void OnEnable() {
        GameEventBus.Subscribe<DamageEvent>(OnDamageReceived);
    }
    
    void OnDamageReceived(DamageEvent evt) {
        if (evt.Target != gameObject) return;
        if (!CanTakeDamage()) return;  // 无敌帧、护盾
        
        currentHealth -= evt.FinalDamage;
        
        // 附加效果
        if (evt.KnockbackForce > 0) {
            ApplyKnockback(evt.HitDirection, evt.KnockbackForce);
        }
        
        // 反馈
        ShowDamageText(evt.FinalDamage);
        PlayHitEffect();
    }
}
```

---

## 四、碰撞伤害处理设计（关键）

### 4.1 核心问题

**Unity 物理特性**：
- 两个对象碰撞时，双方的 `OnCollisionEnter2D` 都会触发
- 如果双方都发布事件，可能导致重复伤害计算

**常见误区**：
```csharp
// ❌ 错误：需要去重缓存、每帧清理
DamageSystem {
    HashSet<int> processedCollisions;  // 维护缓存
    void LateUpdate() { processedCollisions.Clear(); }  // 每帧清理
}
```

---

### 4.2 推荐方案：规则层自然过滤 ⭐

**设计思路**（参考 GC2 Status-based 控制）：
- 允许双方发布事件（简化发布逻辑）
- 通过**规则配置**决定伤害方向，而不是运行时去重
- 无需缓存、无需每帧清理

**核心原理**：
```
敌人撞玩家：
├─ 敌人发布事件 (source=敌人, target=玩家)
│  └─ 规则匹配 ✓ → 玩家受伤
│
└─ 玩家发布事件 (source=玩家, target=敌人)
   └─ 规则不匹配 ✗ → 跳过（玩家无攻击规则）
```

---

### 4.3 实现设计

**简化的事件发布**：
```csharp
// 所有对象统一发布（无需 collisionID）
OnCollisionEnter2D(collision) {
    GameEventBus.Publish(new CollisionEvent {
        source = gameObject,
        target = collision.gameObject,
        velocity = rb2d.velocity.magnitude
    });
}
```

**规则层过滤**：
```csharp
DamageSystem.OnCollisionEvent(evt) {
    // 获取 source 的伤害规则
    profile = GetDamageProfile(evt.source);
    if (profile == null) return;  // 无攻击能力，直接跳过
    
    // 遍历规则，只有匹配的才执行
    foreach (rule in profile.Rules) {
        // 规则自然过滤
        if (rule.TargetTag != evt.target.tag) continue;
        if (rule.RequireState != "" && !CheckState(evt.source, rule.RequireState)) continue;
        
        // 计算并发布伤害
        CalculateAndPublishDamage(rule, evt);
    }
}
```

**规则配置示例**：
```csharp
// 敌人配置
EnemyDamageProfile {
    Rules: [
        { TargetTag: "Player", Damage: 10 }  // 只能伤害玩家
    ]
}

// 玩家配置（普通状态：无规则）
PlayerDamageProfile {
    Rules: []  // 默认无攻击能力
}

// 玩家配置（反伤状态）
PlayerDamageProfile_WithReflect {
    Rules: [
        { 
            TargetTag: "Enemy", 
            RequireState: "HasReflectShield",  // 需要反伤状态
            Damage: 5 
        }
    ]
}
```

---

### 4.4 流程示例

**场景 1：普通碰撞**
```
敌人撞玩家：
1. 敌人发布事件 → 规则匹配 (Enemy→Player) → 玩家受伤 ✓
2. 玩家发布事件 → 无规则 → 跳过 ✗

结果：只有玩家受伤
```

**场景 2：反伤技能**
```
敌人撞玩家（玩家开启反伤）：
1. 敌人发布事件 → 规则匹配 (Enemy→Player) → 玩家受伤 ✓
2. 玩家发布事件 → 规则匹配 (Player→Enemy, 需要反伤状态) → 敌人受伤 ✓

结果：双向伤害（正常+反伤）
```

---

### 4.5 优势总结

| 对比项 | 去重缓存方案 | 规则过滤方案 ⭐ |
|--------|--------------|----------------|
| **性能开销** | 每帧清理 HashSet | 无额外开销 |
| **内存占用** | 需要缓存 | 无需缓存 |
| **实现复杂度** | 中（需要维护状态） | 低（规则匹配） |
| **灵活性** | 中 | 高（配置驱动） |
| **GC2 对齐** | ✗ | ✓（Status-based） |

**核心优势**：
- ✅ **零缓存开销**：无需 HashSet，无需每帧清理
- ✅ **规则驱动**：伤害方向由配置决定
- ✅ **符合 GC2 理念**：通过状态/规则控制，而不是运行时缓存
- ✅ **灵活性高**：支持动态切换攻击能力（如反伤技能）
- ✅ **发布简单**：所有对象统一逻辑

---

## 五、场景实现方案

### 5.1 场景：敌人冲刺撞玩家

**配置**：
```
EnemyDamageProfile {
    Rules: [
        {
            TriggerType: Collision,
            TargetTag: "Player",
            RequireSourceState: "IsDashing",
            BaseDamage: 15,
            DamageMultiplier: 2.0,
            KnockbackForce: 10.0
        }
    ]
}
```

**流程**：
```
1. 冲刺行为设置状态
   blackboard.Set("IsDashing", true)

2. 碰撞发布事件
   GameEventBus.Publish(new CollisionEvent { source, target })

3. DamageSystem 检查规则并计算基础伤害
   - 查找 "IsDashing + Player" 规则
   - 检查 Blackboard 状态
   - 计算基础伤害：15 × 2.0 = 30

4. DamageProcessor 应用修改器
   - 读取 PlayerStats 技能加成（如 +5）
   - 检查弱点（无）
   - 最终伤害：30 + 5 = 35

5. 玩家接收伤害
   OnDamageReceived(evt) {
       currentHealth -= 35;
       ApplyKnockback();
   }
```

**优势**：
- ✅ 规则判断与数值修改分离
- ✅ 支持技能加成等动态修改
- ✅ 碰撞代码只发布事件
- ✅ 玩家统一接收最终伤害

---

### 5.2 场景：敌人撞墙受伤

**配置**：
```
EnemyDamageProfile {
    Rules: [
        {
            TriggerType: Collision,
            TargetTag: "Wall",
            BaseDamage: 5,
            VelocityMultiplier: 1.0,
            SelfDamage: true,        // 关键：伤害自己
            MinVelocity: 3.0
        }
    ]
}
```

**流程**：
```
1. 碰撞发布事件（携带速度）
   GameEventBus.Publish(new CollisionEvent { 
       source, target, velocity 
   })

2. DamageSystem 计算基础伤害
   - 找到 "Wall" 规则
   - 检查速度（< 3.0 则不触发）
   - 基础伤害：5 + 速度 × 1.0
   - 注意：SelfDamage=true，目标是 source 自己

3. DamageProcessor 应用修改器
   - 无特殊修改器
   - 最终伤害 = 基础伤害

4. 敌人接收伤害
   EnemyBehavior.OnDamageReceived(evt) {
       currentHealth -= evt.FinalDamage;
   }
```

**关键点**：
- **墙壁极简**：只需 Collider2D + Tag，无任何伤害逻辑
- **速度伤害**：速度越快伤害越高
- **SelfDamage**：伤害目标是碰撞者自己

---

## 六、与当前系统整合

### 6.1 实体注册
```csharp
// 玩家注册
Player.Start() {
    damageProfile = playerData.damageProfile;
    DamageSystem.Instance.RegisterEntity(gameObject, damageProfile);
    GameEventBus.Subscribe<DamageEvent>(OnDamageReceived);
}

// 敌人注册
EnemyBehavior.Start() {
    damageProfile = enemyData.GetLevelConfig().damageProfile;
    DamageSystem.Instance.RegisterEntity(gameObject, damageProfile);
    GameEventBus.Subscribe<DamageEvent>(OnDamageReceived);
}
```

### 6.2 碰撞检测重构
```csharp
// 统一碰撞事件发布
OnCollisionEnter2D(collision) {
    GameEventBus.Publish(new CollisionEvent {
        source = gameObject,
        target = collision.gameObject,
        contactPoint = collision.contacts[0].point,
        velocity = rb2d.velocity.magnitude
    });
}

// 移除硬编码的伤害判断
// ❌ 删除：if (isAttacking) { target.TakeDamage(); }
```

### 6.3 游戏流程控制
```csharp
// 暂停时禁用伤害
GameFlowManager.OnGamePause() {
    DamageSystem.Instance.SetEnabled(false);
}

GameFlowManager.OnGameResume() {
    DamageSystem.Instance.SetEnabled(true);
}
```

---

## 七、概念澄清与执行顺序

### 7.1 "触发器"概念澄清 ⭐ 重要

**TriggerType** 是 DamageRuleConfig 的配置字段，不是独立系统：
```csharp
DamageRuleConfig {
    TriggerType: Collision,  // 配置字段：何时触发规则
    TargetTag: "Player",
    Damage: 10
}
```

**已移除的错误概念**：
- ❌ "Trigger-based 攻击系统"：不需要
- ❌ "AttackSystem"：不需要独立存在
- ✅ DamageSystem **已经包含**了触发逻辑

---

### 7.2 重构执行顺序

**推荐**：**先伤害系统，后行为系统**

**理由**：
- 伤害系统独立性强，只需 Blackboard 最小集
- 解决燃眉之急（冲撞、撞墙）
- 为行为系统提供基础设施
- 风险低、工作量小

**依赖关系**：
```
Blackboard（基础设施）→ 被伤害系统和行为系统共同使用
无循环依赖，单向依赖安全
```

---

## 八、优化建议（按优先级）

### ⭐⭐⭐ 高优先级（第 1-2 周）

#### Phase 0：Blackboard 基础设施（1 天）
- [ ] 实现 Blackboard 类（Get/Set/TryGet）
- [ ] MonoBehaviour 扩展方法
- [ ] 单元测试

#### Phase 1：伤害系统核心（9 天）

**Day 2-3：伤害系统核心**
- [ ] 定义 DamageEvent、DamageRuleConfig、CollisionEvent
- [ ] 实现 DamageSystem 骨架

**Day 4-5：规则系统**
- [ ] 规则匹配逻辑（规则层过滤，无需缓存）
- [ ] 基础伤害计算
- [ ] 集成 DamageProcessor

**Day 6-7：碰撞重构**
- [ ] 统一发布 CollisionEvent
- [ ] 移除硬编码伤害判断

**Day 8-9：场景实现**
- [ ] 冲刺撞击场景（配置规则 + Blackboard 查询）
- [ ] 撞墙受伤场景（速度伤害）
- [ ] 测试和修复

**Day 10：缓冲时间**

**验收标准**：
- ✅ 规则自然过滤，无重复伤害
- ✅ DamageProcessor 修改器正常工作
- ✅ 冲撞、撞墙场景完整可用

---

### ⭐⭐ 中优先级（第 3-4 周，可选）

**说明**：伤害系统重构完成后，转入行为系统重构（见主文档）

#### 后续扩展（可选）：
- 规则配置资源创建
- Stopped/Interval 触发类型
- 多规则组合优化

---

### ⭐ 低优先级（可选）

#### 7. 附加效果系统（3-5 天）
- 击退（Knockback）
- 眩晕（Stun）
- 持续伤害（DOT）

#### 8. 伤害修正系统（3-5 天）
- 护盾系统
- 减伤系统
- 免疫系统
- 伤害反射

#### 9. 伤害反馈增强（2-3 天）
- 伤害数字优化
- 击中特效
- 屏幕震动

#### 10. 统计和调试（2-3 天）
- 伤害统计（总伤害、DPS）
- 伤害日志系统
- 规则匹配可视化

---

## 十、后续行为系统重构

**转入**：伤害系统完成后，转入行为系统重构（Week 3-4）

**任务**：
- RuntimeState 提取
- BehaviorStatus 统一返回值
- 复合行为重构（IntervalMovement、Flee）

**详见**：`GC2_Behavior_VS_Current_Architecture_Analysis.md` 行为系统主文档

---

## 十一、架构优势总结

### 8.1 解决的问题

| 问题 | 当前 | 新架构 |
|------|------|--------|
| **条件纠缠** | if-else 嵌套 | 规则配置 + 修改器链 |
| **逻辑分散** | 碰撞/攻击/墙壁都有伤害逻辑 | 判断层 + 修改层分离 |
| **扩展困难** | 新增状态需修改多处 | 添加规则或修改器 |
| **墙壁复杂** | 墙壁需要伤害逻辑 | 墙壁只需标签 |
| **无法拦截** | 直接调用 TakeDamage | 事件驱动，修改器可拦截 |

### 8.2 核心优势

- ✅ **配置驱动**：规则配置 + 修改器扩展
- ✅ **职责分离**：判断层、修改层、接收层独立
- ✅ **易于扩展**：新规则、新修改器、新触发类型
- ✅ **保留现有**：DamageProcessor 无需重写
- ✅ **易调试**：规则过滤 → 修改器链 → 最终伤害

---

## 十二、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 学习成本 | 中 | 提供示例配置、文档 |
| 重构工作量 | 高 | 渐进式迁移，保留旧代码 |
| 调试更抽象 | 中 | 伤害日志、事件追踪 |
| 性能影响 | 低 | 规则缓存、事件池 |

---

## 十三、与其他系统的关系

### 11.1 与 DamageProcessor 的整合

**已整合**：DamageProcessor 作为修改器层，处理弱点、暴击、技能加成

### 11.2 与现有 IAttackBehavior 的关系

**重构后**：
- ❌ **IAttackBehavior 策略模式不再需要**
- ✅ **DamageSystem 替代攻击触发逻辑**
- ✅ **行为系统只负责设置状态，不触发伤害**

**旧代码**：
```
EnemyBehavior.ExecuteAttackPhase() {
    attackBehavior.ExecuteAttack();  // 触发伤害
}
```

**新代码**：
```
// 行为系统只设置状态
DashBehavior.Execute() {
    blackboard.Set("IsDashing", true);  // 仅设置状态
    ExecuteMovement();  // 执行移动
}

// 伤害系统自动处理
OnCollisionEnter2D() {
    PublishCollision();  // 发布碰撞
}

DamageSystem.OnCollision() {
    // 检查规则（自动触发伤害）
}
```

### 11.3 与行为系统重构的顺序

**推荐顺序**：**先伤害系统，后行为系统**

**理由**：
- ✅ 伤害系统独立性强，只需 Blackboard 最小集
- ✅ 解决冲撞、撞墙等燃眉之急
- ✅ 为行为系统提供基础设施（Blackboard）
- ✅ 风险更低

**详见**：`Refactoring_Execution_Order_Analysis.md` 重构顺序分析

---

**文档版本**：v3.0  
**创建日期**：2025-11-01  
**维护者**：AI Assistant  
**变更记录**：
- v3.0: 整合 DamageProcessor，规则过滤碰撞去重，澄清概念
- v2.0: 精简内容，去除重复场景，添加优先级明确的实施建议
- v1.0: 初始版本
