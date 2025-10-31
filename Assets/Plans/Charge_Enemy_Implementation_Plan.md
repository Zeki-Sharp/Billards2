# 冲刺型敌人实现计划

## 📋 需求
实现一个向玩家方向冲刺的敌人：
- 预告阶段：显示冲刺方向
- 移动阶段：按预告方向冲刺
- 碰撞玩家：造成伤害 + 施加推力（**只在冲刺过程中**）

---

## 🎯 核心设计

### **关键架构决策**

1. **独立实现 ChargeBehavior**（不继承 FollowPlayerBehavior）
   - 原因：未来可能需要随机方向、预设方向等
   - 扩展性：不受跟随逻辑限制

2. **碰撞处理委托给行为系统**（不在 EnemyBehavior 中处理）
   - 原因：避免 EnemyBehavior 臃肿，每个新敌人都要修改
   - 原则：符合开闭原则和单一职责

3. **新增 ChargeAttackBehavior**（Attack 阶段为空）
   - 原因：阶段顺序 Attack → Move，但冲刺在 Move 阶段才碰撞
   - 实现：Attack 阶段不造成伤害，Move 阶段碰撞时处理

---

## 🏗️ 系统架构

### **行为系统扩展**

```
IAttackBehavior (接口)
    ├─ HandleCollision()  // 新增：处理碰撞
    ├─ ExecuteTelegraph()
    ├─ ExecuteAttack()
    └─ CleanupAttack()

BaseAttackBehavior (基类)
    └─ HandleCollision() → return false  // 默认不处理

MeleeAttackBehavior
    └─ (使用默认实现，不处理碰撞)

ChargeAttackBehavior (新增)
    └─ HandleCollision() → 覆盖，处理冲刺碰撞
```

### **职责划分**

```
EnemyBehavior:
  - OnCollisionEnter2D() → 转发给 attackBehavior.HandleCollision()
  - 保持简洁，不包含特殊逻辑

ChargeAttackBehavior:
  - HandleCollision() → 判断是否冲刺中，处理碰撞伤害
  - 包含冲刺特有的碰撞逻辑
```

---

## 📝 实现步骤

### 阶段一：扩展行为系统接口

#### 1. 修改 IAttackBehavior.cs
```csharp
public interface IAttackBehavior
{
    void ExecuteTelegraph(...);
    void ExecuteAttack(...);
    void CleanupAttack(...);
    
    // 新增：处理碰撞
    bool HandleCollision(Collision2D collision, EnemyData enemyData, 
                        Transform enemyTransform);
}
```

#### 2. 修改 BaseAttackBehavior.cs
```csharp
public abstract class BaseAttackBehavior : IAttackBehavior
{
    // ... 现有方法 ...
    
    /// <summary>
    /// 处理碰撞（默认不处理）
    /// </summary>
    public virtual bool HandleCollision(Collision2D collision, 
                                       EnemyData enemyData, 
                                       Transform enemyTransform)
    {
        return false;  // 默认不处理碰撞
    }
    
    /// <summary>
    /// 对玩家施加击退推力（工具方法）
    /// </summary>
    protected void ApplyKnockback(GameObject playerObject, 
                                  Vector2 knockbackDirection, 
                                  float knockbackForce)
    {
        Rigidbody2D playerRb = playerObject.GetComponent<Rigidbody2D>()
            ?? playerObject.GetComponentInParent<Rigidbody2D>()
            ?? playerObject.GetComponentInChildren<Rigidbody2D>();
        
        if (playerRb != null)
        {
            playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
    }
}
```

---

### 阶段二：冲刺移动行为

#### 3. MovementConfig.cs
```csharp
public enum MovementType { FollowPlayer, Flee, IntervalMovement, Charge }

[System.Serializable]
public class ChargeMovementConfig
{
    public float chargeSpeed = 10f;
    public float chargeDistance = 5f;
    public float knockbackForce = 15f;
}
```

#### 4. ChargeBehavior.cs（新建，~50行）
```csharp
public class ChargeBehavior : BaseMovementBehavior
{
    private Vector2 lockedDirection = Vector2.right;
    private bool isDirectionLocked = false;
    
    // 锁定方向（预告阶段调用）
    public void LockChargeDirection(Transform enemy, Transform player)
    {
        lockedDirection = (player.position - enemy.position).normalized;
        isDirectionLocked = true;
    }
    
    // 计算目标位置（移动阶段调用）
    public override Vector2 ExecuteMovement(Transform enemy, Transform player, EnemyData data)
    {
        cachedEnemyData = data;
        if (!ValidateMovementParams(enemy, player, data))
            return enemy.position;
        
        Vector2 direction = isDirectionLocked ? lockedDirection : Vector2.right;
        float distance = data.chargeConfig.chargeDistance;
        currentDirection = direction;
        SetMoving(true);
        
        return CalculateTargetPosition(enemy.position, direction, distance);
    }
    
    // 返回冲刺速度
    public override float GetCurrentMoveSpeed()
    {
        return cachedEnemyData?.chargeConfig.chargeSpeed ?? 10f;
    }
}
```

---

### 阶段三：冲刺攻击行为

#### 5. 创建 ChargeAttackBehavior.cs（新建，~70行）

```csharp
using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 冲刺攻击行为
/// Attack 阶段标记开始冲刺，Move 阶段碰撞时造成伤害
/// </summary>
public class ChargeAttackBehavior : BaseAttackBehavior
{
    private bool isCharging = false;  // 是否处于冲刺攻击状态
    
    /// <summary>
    /// 预告阶段：显示冲刺轨迹
    /// </summary>
    public override void ExecuteTelegraph(Transform enemy, Transform player, 
                                         EnemyData data, AttackRange range)
    {
        if (!ValidateAttackParams(enemy, player, data, range))
            return;
        
        range.ShowTelegraph();
        Debug.Log("ChargeAttackBehavior: 显示冲刺轨迹");
    }
    
    /// <summary>
    /// 攻击阶段：标记进入冲刺状态（不造成伤害）
    /// </summary>
    public override void ExecuteAttack(Transform enemy, Transform player, 
                                      EnemyData data, AttackRange range, 
                                      MMFeedbacks effect)
    {
        isCharging = true;  // 标记开始冲刺
        Debug.Log("ChargeAttackBehavior: 进入冲刺攻击状态");
    }
    
    /// <summary>
    /// 处理碰撞：冲刺过程中碰到玩家造成伤害
    /// </summary>
    public override bool HandleCollision(Collision2D collision, 
                                        EnemyData enemyData, 
                                        Transform enemyTransform)
    {
        // 只在冲刺状态下处理碰撞
        if (!isCharging)
            return false;
        
        // 检查是否碰到玩家
        if (!collision.gameObject.CompareTag("Player"))
            return false;
        
        PlayerCore playerCore = collision.gameObject.GetComponentInChildren<PlayerCore>();
        if (playerCore == null)
            return false;
        
        // 1. 造成伤害
        float damage = enemyData.damage;
        playerCore.TakeDamageIgnorePhase(damage);
        
        // 2. 施加推力
        Vector2 knockbackDir = (collision.transform.position - enemyTransform.position).normalized;
        ApplyKnockback(collision.gameObject, knockbackDir, enemyData.chargeConfig.knockbackForce);
        
        // 3. 发布攻击事件（触发特效）
        Vector3 hitPos = collision.contacts[0].point;
        enemyTransform.gameObject.PublishAttack("Charge", hitPos, collision.gameObject, damage);
        
        Debug.Log($"ChargeAttackBehavior: 冲刺碰撞造成 {damage} 点伤害");
        return true;
    }
    
    /// <summary>
    /// 清理：结束冲刺状态
    /// </summary>
    public override void CleanupAttack(Transform enemy, AttackRange range)
    {
        isCharging = false;
        Debug.Log("ChargeAttackBehavior: 结束冲刺状态");
    }
}
```

---

### 阶段四：集成到现有系统

#### 6. AttackType 枚举（在定义攻击类型的地方）
```csharp
public enum AttackType
{
    Melee,
    Ranged,
    Thorn,
    Charge  // 新增
}
```

#### 7. BehaviorFactory.cs
```csharp
// 移动行为工厂
case MovementType.Charge:
    return new ChargeBehavior();

// 攻击行为工厂
case AttackType.Charge:
    return new ChargeAttackBehavior();
```

#### 8. EnemyData.cs
```csharp
[BoxGroup("AI配置")]
[ShowIf("movementType", MovementType.Charge)]
public ChargeMovementConfig chargeConfig = new ChargeMovementConfig();
```

#### 9. EnemyBehavior.cs - 两处修改

**修改1：预告阶段锁定方向**
```csharp
public void ExecuteTelegraphPhase()
{
    // ... 现有代码 ...
    
    // 冲刺敌人：锁定方向
    if (enemyData?.movementType == MovementType.Charge && 
        movementBehavior is ChargeBehavior charge)
    {
        charge.LockChargeDirection(transform, player);
    }
}
```

**修改2：转发碰撞处理（保持简洁！）**
```csharp
/// <summary>
/// 碰撞处理：委托给攻击行为
/// </summary>
void OnCollisionEnter2D(Collision2D collision)
{
    // 简单转发，保持 EnemyBehavior 职责单一
    if (attackBehavior != null)
    {
        bool handled = attackBehavior.HandleCollision(collision, enemyData, transform);
        
        if (handled && showDebugInfo)
        {
            Debug.Log($"EnemyBehavior {name}: 碰撞由攻击行为处理");
        }
    }
}
```

---

### 阶段五：Unity 配置

1. **创建 EnemyData**
   - enemyName: "冲刺敌人"
   - movementType: Charge
   - attackType: Charge
   - chargeSpeed: 10, chargeDistance: 5, knockbackForce: 15

2. **配置预制体**
   - 复制现有敌人预制体
   - AttackRange 设置为长条形（表示冲刺轨迹）
   - 确保有 Collider2D（非 Trigger）和 Rigidbody2D

---

## 🎯 系统交互流程

```
【回合 N - 预告】
Telegraph 阶段：
  ├─ ChargeBehavior.LockChargeDirection()  // 锁定方向
  └─ ChargeAttackBehavior.ExecuteTelegraph()  // 显示轨迹

【回合 N+1 - 攻击】
Attack 阶段：
  └─ ChargeAttackBehavior.ExecuteAttack()  // isCharging = true

【回合 N+1 - 移动】
Move 阶段：
  ├─ ChargeBehavior.ExecuteMovement()  // 计算目标位置
  ├─ MoveToTarget() 协程执行冲刺  // 实际移动
  └─ 碰撞发生 →
      └─ EnemyBehavior.OnCollisionEnter2D()
          └─ attackBehavior.HandleCollision()  // 委托处理
              └─ ChargeAttackBehavior.HandleCollision()  // 造成伤害
```

---

## ✅ 设计优势

### **1. 职责清晰**
```
EnemyBehavior: 只转发事件，不包含特殊逻辑
ChargeBehavior: 处理冲刺移动
ChargeAttackBehavior: 处理冲刺攻击和碰撞
```

### **2. 符合开闭原则**
```
添加新敌人（如滚石）：
  ✅ 创建 RollBehavior
  ✅ 创建 RollAttackBehavior 并覆盖 HandleCollision
  ❌ 不需要修改 EnemyBehavior
```

### **3. 扩展性强**
```csharp
// 未来：随机方向冲刺
public void LockRandomDirection()
{
    float angle = Random.Range(0f, 360f);
    lockedDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
}
```

---

## ⚠️ 关键要点

### **为什么不在 EnemyBehavior 中处理碰撞？**
- ❌ 每个新敌人都要在 EnemyBehavior 加 if 判断
- ❌ 违反开闭原则（对修改关闭）
- ❌ EnemyBehavior 越来越臃肿

### **为什么需要 isCharging 状态？**
- ✅ 确保只有冲刺过程中的碰撞才造成伤害
- ✅ Attack 阶段标记开始，Move 阶段处理，CleanupAttack 阶段清理
- ✅ 防止非冲刺时的碰撞误触发

### **注意事项**
1. 玩家必须有 Rigidbody2D（推力才能生效）
2. 敌人必须有 Collider2D（非 Trigger）
3. attackType 和 movementType 都要设为 Charge
4. 冲刺速度建议 10+（普通移动 2-3）

---

## 📋 快速检查清单

**接口修改**（2个文件）：
- [ ] IAttackBehavior.cs - 添加 HandleCollision 方法
- [ ] BaseAttackBehavior.cs - 提供默认实现和工具方法

**新增文件**（2个）：
- [ ] ChargeBehavior.cs - 冲刺移动行为
- [ ] ChargeAttackBehavior.cs - 冲刺攻击行为

**代码修改**（4个文件）：
- [ ] MovementConfig.cs - 添加枚举和配置
- [ ] AttackType 枚举 - 添加 Charge
- [ ] BehaviorFactory.cs - 添加工厂逻辑
- [ ] EnemyData.cs - 添加配置字段

**最小修改**（1个文件）：
- [ ] EnemyBehavior.cs - 只添加预告处理和碰撞转发（保持简洁）

**Unity 配置**：
- [ ] 创建 ChargeEnemyData（movementType + attackType 都是 Charge）
- [ ] 配置预制体（长条形 AttackRange + 碰撞体）

**测试验证**：
- [ ] 预告阶段显示冲刺方向
- [ ] 冲刺过程中碰撞造成伤害+推力
- [ ] 非冲刺时碰撞不造成伤害
- [ ] EnemyBehavior 代码简洁，无特殊判断

---

**实现估时**：约 1-1.5 小时  
**核心原则**：职责清晰，符合开闭原则，保持扩展性

---

*文档结束*
