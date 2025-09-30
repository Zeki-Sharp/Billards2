using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 棘刺攻击行为
/// 攻击区域持续存在，玩家碰撞即受伤
/// </summary>
public class ThornAttackBehavior : BaseAttackBehavior
{
    private int currentRound = 0;
    private int lastActivateRound = -999;
    private bool isThornActive = false;
    private float lastDamageTime = 0f;
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, 
                                         EnemyData enemyData, AttackRange attackRange)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, attackRange))
            return;
        
        currentRound++;
        ThornAttackConfig config = enemyData.thornConfig;
        
        // 检查冷却
        int roundsSinceLastActivate = currentRound - lastActivateRound;
        bool isInCooldown = roundsSinceLastActivate <= config.cooldownRounds;
        
        // 获取 EnemyBehavior 组件
        EnemyBehavior enemyBehavior = enemyTransform.GetComponent<EnemyBehavior>();
        
        if (isInCooldown)
        {
            // 冷却中
            isThornActive = false;
            
            // 关闭陷阱模式
            if (enemyBehavior != null)
            {
                enemyBehavior.SetTrapMode(false);
            }
            
            UpdateVisual(attackRange, false, config);
            attackRange.gameObject.SetActive(config.showCooldownState);  // 根据配置决定是否显示冷却状态
            Debug.Log($"ThornAttackBehavior: 冷却中 ({roundsSinceLastActivate}/{config.cooldownRounds})");
            return;
        }
        
        // 激活棘刺
        lastActivateRound = currentRound;
        isThornActive = true;
        lastDamageTime = 0f;  // 重置伤害时间
        
        // 开启陷阱模式
        if (enemyBehavior != null)
        {
            enemyBehavior.SetTrapMode(true);
        }
        
        // 显示攻击范围（棘刺作为子物体，跟随敌人）
        attackRange.gameObject.SetActive(true);
        attackRange.ShowTelegraph();
        
        // 更新视觉为激活状态
        UpdateVisual(attackRange, true, config);
        
        Debug.Log($"ThornAttackBehavior: 棘刺激活 - 回合 {currentRound}");
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// 棘刺攻击通过持续碰撞检测造成伤害
    /// </summary>
    public override void ExecuteAttack(Transform enemyTransform, Transform playerTransform, 
                                      EnemyData enemyData, AttackRange attackRange, MMFeedbacks attackEffect)
    {
        if (!isThornActive) return;
        
        // 检测玩家是否在棘刺范围内
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                // 使用配置的伤害间隔控制伤害频率
                if (Time.time - lastDamageTime >= enemyData.thornConfig.damageInterval)
                {
                    lastDamageTime = Time.time;
                    DealDamageToPlayer(target, enemyData, enemyTransform);
                    
                    // 播放伤害特效（只在造成伤害时）
                    PlayAttackEffect(attackEffect, enemyTransform.name);
                    
                    Debug.Log($"ThornAttackBehavior: 棘刺造成伤害");
                }
            }
        }

    }
    
    /// <summary>
    /// 清理攻击状态
    /// 棘刺在移动阶段不清理，保持激活状态直到冷却
    /// </summary>
    public override void CleanupAttack(Transform enemyTransform, AttackRange attackRange)
    {
        // 棘刺在移动阶段不清理，保持激活状态
        Debug.Log($"ThornAttackBehavior: 移动阶段，棘刺保持激活");
    }
    
    /// <summary>
    /// 更新视觉状态
    /// </summary>
    private void UpdateVisual(AttackRange attackRange, bool isActive, ThornAttackConfig config)
    {
        if (!config.showCooldownState && !isActive) return;
        
        // 查找 Image 子物体
        Transform imageTransform = attackRange.transform.Find("Image");
        if (imageTransform == null) return;
        
        // 尝试 SpriteRenderer
        SpriteRenderer spriteRenderer = imageTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? config.activeColor : config.cooldownColor;
            Debug.Log($"ThornAttackBehavior: 更新视觉状态 - {(isActive ? "激活" : "冷却")}");
            return;
        }
        
        // 尝试 UnityEngine.UI.Image
        UnityEngine.UI.Image uiImage = imageTransform.GetComponent<UnityEngine.UI.Image>();
        if (uiImage != null)
        {
            uiImage.color = isActive ? config.activeColor : config.cooldownColor;
            Debug.Log($"ThornAttackBehavior: 更新视觉状态 - {(isActive ? "激活" : "冷却")}");
        }
    }
}

