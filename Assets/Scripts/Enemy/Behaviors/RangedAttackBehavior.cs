using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 远程攻击行为
/// 攻击范围投射到玩家附近位置，跟随敌人移动
/// </summary>
public class RangedAttackBehavior : BaseAttackBehavior
{
    // ⚠️ 注意：投射逻辑基于 XZ 平面（地面），但需要完整的 3D 世界坐标来放置 AttackRange
    private Vector3 projectedPosition; // 保存投射位置（世界坐标，Y 与敌人相同）
    private Vector3 originalLocalPosition; // 保存原始本地位置
    private ParabolicIndicator parabolicIndicator; // 抛物线指示器
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return BehaviorStatus.Failure;
        }
        
        // 检测玩家是否在检测范围内（用于调试，仅作参考）
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 保存原始本地位置（用于恢复）
        originalLocalPosition = attackRange.transform.localPosition;
        
        // 计算投射位置（XZ 平面）并映射到世界坐标（保持与敌人相同的 Y 高度）
        Vector2 projectedXZ = CalculateProjectionPositionXZ(enemyTransform.position, playerTransform.position, levelConfig.rangedConfig);
        projectedPosition = new Vector3(projectedXZ.x, enemyTransform.position.y, projectedXZ.y);

        // 直接使用世界坐标定位落点（不改变父子关系）
        attackRange.transform.position = projectedPosition;

        // 显示攻击预告（这会激活 GameObject 并设置方向）
        attackRange.ShowTelegraph(projectedPosition);
        
        // 显示抛物线指示器
        if (levelConfig.rangedConfig.showParabolicIndicator)
        {
            ShowParabolicIndicator(enemyTransform, attackRange.transform, levelConfig.rangedConfig);
        }
        
        runtimeState.currentAttackState = "Telegraph";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public override BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return BehaviorStatus.Failure;
        }
        
        // 使用预告阶段保存的朝向
        attackRange.ApplyTelegraphedDirection();
        
        // ✅ 新伤害系统：设置 CanAttack 状态
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        blackboard.Set("CanAttack", true);
        
        // 播放攻击特效
        PlayAttackEffect(attackEffect, enemyTransform.name);
        
        // ✅ 新伤害系统：主动检测范围内的玩家并发布碰撞事件
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                // ✅ 3D化：使用 3D Collider
                Collider targetCollider = target.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    // 使用 AttackRange 作为 source（Tag = EnemyAttackRange）
                    CollisionEvent evt = CollisionEvent.CreateFromTrigger(attackRange.gameObject, targetCollider);
                    GameEventBus.PublishCollision(evt);
                }
                else
                {
                    Debug.LogWarning($"RangedAttackBehavior: 目标 {target.name} 没有 Collider 组件！");
                }
            }
        }
        
        runtimeState.currentAttackState = "Attacking";
        runtimeState.lastAttackTime = Time.time;
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    public override BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        // ✅ 新伤害系统：清理 CanAttack 状态
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        blackboard.Set("CanAttack", false);
        
        if (attackRange == null)
        {
            return BehaviorStatus.Failure;
        }
        
        // 隐藏并清理抛物线指示器
        HideParabolicIndicator();
        
        // ✅ 简化方案：恢复原始本地位置
        attackRange.transform.localPosition = originalLocalPosition;
        
        // ✅ 统一显隐责任：在清理时隐藏攻击范围
        attackRange.HideTelegraph();
        
        runtimeState.currentAttackState = "";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 计算投射位置（XZ 平面上的 2D 坐标，x=x, y=z）
    /// </summary>
    private Vector2 CalculateProjectionPositionXZ(Vector3 enemyPosition3D, Vector3 playerPosition3D, RangedAttackConfig config)
    {
        // 将 3D 位置映射到 XZ 平面上的 2D 坐标
        Vector2 enemyPosition = new Vector2(enemyPosition3D.x, enemyPosition3D.z);
        Vector2 playerPosition = new Vector2(playerPosition3D.x, playerPosition3D.z);
        
        // 方向（敌人 -> 玩家）
        Vector2 directionToPlayer = (playerPosition - enemyPosition).normalized;
        
        // 期望的从敌人出发的投射距离：玩家距离 - 回缩量
        float rawDesiredDistance = Vector2.Distance(enemyPosition, playerPosition) - config.projectionDistance;
        // 夹到 [0, detectionRange]
        float clampedDistance = Mathf.Clamp(rawDesiredDistance, 0f, config.detectionRange);
        
        // 基础落点：从敌人位置沿方向前进 clampedDistance
        Vector2 basePosition = enemyPosition + directionToPlayer * clampedDistance;
        
        // 添加随机偏移
        if (config.useRandomOffset)
        {
            Vector2 randomOffset = Random.insideUnitCircle * config.randomOffsetRange;
            basePosition += randomOffset;
            
            // 偏移后再次限制到最大可投射圆内
            Vector2 fromEnemy = basePosition - enemyPosition;
            float afterOffsetDistance = fromEnemy.magnitude;
            if (afterOffsetDistance > config.detectionRange)
            {
                basePosition = enemyPosition + fromEnemy.normalized * config.detectionRange;
            }
        }
        
        return basePosition;
    }
    
    /// <summary>
    /// 显示抛物线指示器
    /// </summary>
    private void ShowParabolicIndicator(Transform enemyTransform, Transform attackRangeTransform, RangedAttackConfig config)
    {
        // 尝试从AttackRange获取抛物线指示器组件
        if (parabolicIndicator == null)
        {
            parabolicIndicator = attackRangeTransform.GetComponent<ParabolicIndicator>();
            
            if (parabolicIndicator == null)
            {
                Debug.LogWarning($"RangedAttackBehavior: AttackRange上未找到ParabolicIndicator组件！请在AttackRange预制体上添加ParabolicIndicator组件");
                return;
            }
            
            Debug.Log($"RangedAttackBehavior: 从AttackRange获取到抛物线指示器组件");
        }
        
        // 查找敌人的实际显示物体（Image 或 EnemyItem）
        Transform actualEnemyTransform = FindActualEnemyTransform(enemyTransform);
        
        // 设置起点和终点（两者都跟随各自的Transform实时更新）
        parabolicIndicator.SetPoints(actualEnemyTransform, attackRangeTransform);
        
        // 显示
        parabolicIndicator.Show();
        
        Debug.Log($"RangedAttackBehavior: 显示抛物线指示器 - 起点:{actualEnemyTransform.name} at {actualEnemyTransform.position}, 终点:{attackRangeTransform.position}");
    }
    
    /// <summary>
    /// 查找敌人的实际显示Transform（优先Image，其次EnemyItem）
    /// </summary>
    private Transform FindActualEnemyTransform(Transform enemyRoot)
    {
        // 优先查找 Image
        Transform image = enemyRoot.Find("EnemyItem/Image");
        if (image != null)
        {
            Debug.Log($"RangedAttackBehavior: 找到敌人Image: {image.name}");
            return image;
        }
        
        // 其次查找 EnemyItem
        Transform enemyItem = enemyRoot.Find("EnemyItem");
        if (enemyItem != null)
        {
            Debug.Log($"RangedAttackBehavior: 找到EnemyItem: {enemyItem.name}");
            return enemyItem;
        }
        
        // 如果都没找到，返回根Transform
        Debug.LogWarning($"RangedAttackBehavior: 未找到Image或EnemyItem，使用根Transform: {enemyRoot.name}");
        return enemyRoot;
    }
    
    /// <summary>
    /// 隐藏抛物线指示器
    /// </summary>
    private void HideParabolicIndicator()
    {
        if (parabolicIndicator != null)
        {
            parabolicIndicator.Hide();
            parabolicIndicator = null; // 清理引用，下次重新获取
            Debug.Log($"RangedAttackBehavior: 隐藏并清理抛物线指示器引用");
        }
    }
}
