using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 棘刺攻击行为
/// 攻击区域持续存在，玩家碰撞即受伤
/// </summary>
public class ThornAttackBehavior : BaseAttackBehavior
{
    private int currentRound = 0;
    private int lastActivateRound = 0;  // 初始化为0，让第一次Telegraph进入冷却，下一次即可激活
    private bool isThornActive = false;
    private float lastDamageTime = 0f;
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, 
                                         EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
            return BehaviorStatus.Failure;
        
        // 回合计数从0开始，对应 Telegraph 调用次数
        currentRound++;
        ThornAttackConfig config = levelConfig.thornConfig;
        
        // 检查冷却
        int roundsSinceLastActivate = currentRound - lastActivateRound;
        bool isInCooldown = roundsSinceLastActivate <= config.cooldownRounds;
        
        // 获取 EnemyBehavior 组件和 Blackboard
        EnemyBehavior enemyBehavior = enemyTransform.GetComponent<EnemyBehavior>();
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        
        // 确保攻击范围始终显示
        attackRange.ShowTelegraph();
        
        if (isInCooldown)
        {
            // 冷却中
            isThornActive = false;
            
            // ✅ 新伤害系统：清除 IsTrap 状态
            blackboard.Set("IsTrap", false);
            
            // 关闭陷阱模式（保留，用于其他逻辑）
            if (enemyBehavior != null)
            {
                enemyBehavior.SetTrapMode(false);
            }
            
            // 更新视觉为冷却状态（灰色）
            UpdateVisual(attackRange, false, config);
            
            // 禁用碰撞体（冷却时玩家可以攻击敌人）
            SetColliderEnabled(attackRange, false);
            
            runtimeState.currentAttackState = "Cooldown";
            return BehaviorStatus.Success; // 冷却也是正常状态
        }
        
        // 激活棘刺
        lastActivateRound = currentRound;
        isThornActive = true;
        lastDamageTime = 0f;  // 重置伤害时间
        
        // ✅ 新伤害系统：设置 IsTrap 状态
        blackboard.Set("IsTrap", true);
        
        // 开启陷阱模式（保留，用于其他逻辑）
        if (enemyBehavior != null)
        {
            enemyBehavior.SetTrapMode(true);
        }
        
        // 更新视觉为激活状态（红色）
        UpdateVisual(attackRange, true, config);
        
        // 启用碰撞体（激活时触发陷阱伤害）
        SetColliderEnabled(attackRange, true);
        
        runtimeState.currentAttackState = "Active";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// 棘刺攻击通过持续碰撞检测造成伤害
    /// </summary>
    public override BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, 
                                      EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect, EnemyRuntimeState runtimeState)
    {
        if (!isThornActive)
            return BehaviorStatus.Success; // 未激活时跳过攻击，仍视为成功
        
        // 检测玩家是否在棘刺范围内
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                // 使用配置的伤害间隔控制伤害频率
                if (Time.time - lastDamageTime >= levelConfig.thornConfig.damageInterval)
                {
                    lastDamageTime = Time.time;
                    
                    // ✅ 3D化：使用 3D Collider
                    Collider targetCollider = target.GetComponent<Collider>();
                    if (targetCollider != null)
                    {
                        // ✅ 新伤害系统：发布碰撞事件
                        CollisionEvent evt = CollisionEvent.CreateFromTrigger(attackRange.gameObject, targetCollider);
                        GameEventBus.PublishCollision(evt);
                    }
                    else
                    {
                        Debug.LogWarning($"ThornAttackBehavior: 目标 {target.name} 没有 Collider 组件！");
                    }
                    
                    // 播放伤害特效（只在造成伤害时）
                    PlayAttackEffect(attackEffect, enemyTransform.name);
                }
            }
        }
        
        runtimeState.currentAttackState = "Attacking";
        runtimeState.lastAttackTime = Time.time;
        
        // 攻击阶段结束后关闭棘刺（下一阶段不再携带激活状态移动）
        ThornAttackConfig config = levelConfig.thornConfig;
        EnemyBehavior enemyBehavior = enemyTransform.GetComponent<EnemyBehavior>();
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        
        isThornActive = false;
        SetColliderEnabled(attackRange, false);
        UpdateVisual(attackRange, false, config);
        
        if (enemyBehavior != null)
        {
            enemyBehavior.SetTrapMode(false);
        }
        blackboard.Set("IsTrap", false);
        
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 清理攻击状态
    /// 注意：IsTrap 状态不在这里清除，而是在下次 Telegraph 阶段根据冷却状态更新
    /// 这样可以保证玩家回合移动时撞到激活的刺仍会受伤
    /// </summary>
    public override BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        // ✅ 修复：不在这里清除 IsTrap 状态
        // IsTrap 状态会在下次 Telegraph 阶段根据冷却情况更新
        // 这样玩家回合撞到激活的刺时，状态仍然有效
        
        runtimeState.currentAttackState = "";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 更新视觉状态
    /// </summary>
    private void UpdateVisual(AttackRange attackRange, bool isActive, ThornAttackConfig config)
    {
        // 查找 Image 子物体
        Transform imageTransform = attackRange.transform.Find("Image");
        if (imageTransform == null) return;
        
        // 尝试 SpriteRenderer
        SpriteRenderer spriteRenderer = imageTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? config.activeColor : config.cooldownColor;
            Debug.Log($"ThornAttackBehavior: 更新视觉状态 - {(isActive ? "激活(红色)" : "冷却(灰色)")}");
            return;
        }
        
        // 尝试 UnityEngine.UI.Image
        UnityEngine.UI.Image uiImage = imageTransform.GetComponent<UnityEngine.UI.Image>();
        if (uiImage != null)
        {
            uiImage.color = isActive ? config.activeColor : config.cooldownColor;
            Debug.Log($"ThornAttackBehavior: 更新视觉状态 - {(isActive ? "激活(红色)" : "冷却(灰色)")}");
        }
    }
    
    /// <summary>
    /// 控制碰撞体启用/禁用
    /// </summary>
    private void SetColliderEnabled(AttackRange attackRange, bool enabled)
    {
        // 查找 Image 子物体（碰撞体在这里）
        Transform imageTransform = attackRange.transform.Find("Image");
        if (imageTransform == null)
        {
            Debug.LogWarning($"ThornAttackBehavior: 未找到 Image 子物体，无法控制碰撞体");
            return;
        }
        
        // 获取所有碰撞体组件
        Collider2D[] colliders = imageTransform.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = enabled;
        }
        
        Debug.Log($"ThornAttackBehavior: 碰撞体 {(enabled ? "启用" : "禁用")}");
    }
}

