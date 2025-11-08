using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 远程攻击行为
/// 攻击范围投射到玩家附近位置，跟随敌人移动
/// </summary>
public class RangedAttackBehavior : BaseAttackBehavior
{
    private Vector2 projectedPosition; // 保存投射位置
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
        
        // 检测玩家是否在检测范围内
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        if (distanceToPlayer > levelConfig.rangedConfig.detectionRange)
        {
            return BehaviorStatus.Failure; // 玩家不在范围，攻击失败
        }
        
        // 保存原始本地位置（用于恢复）
        originalLocalPosition = attackRange.transform.localPosition;
        
        // 计算投射位置（世界坐标）
        projectedPosition = CalculateProjectionPosition(enemyTransform.position, playerTransform.position, levelConfig.rangedConfig);
        
        // ✅ 简化方案：转换为相对于敌人的本地坐标偏移
        // 保持父子关系，跟随敌人移动
        Vector2 localOffset = projectedPosition - (Vector2)enemyTransform.position;
        attackRange.transform.localPosition = localOffset;
        
        // 显示攻击预告（这会激活 GameObject 并设置方向）
        attackRange.ShowTelegraph();
        
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
                // 使用 AttackRange 作为 source（Tag = EnemyAttackRange）
                CollisionEvent evt = CollisionEvent.CreateFromTrigger(attackRange.gameObject, target.GetComponent<Collider2D>());
                GameEventBus.PublishCollision(evt);
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
    /// 计算投射位置
    /// </summary>
    private Vector2 CalculateProjectionPosition(Vector2 enemyPosition, Vector2 playerPosition, RangedAttackConfig config)
    {
        // 基础位置：玩家位置向敌人方向偏移指定距离
        Vector2 directionToPlayer = (playerPosition - enemyPosition).normalized;
        Vector2 basePosition = playerPosition - directionToPlayer * config.projectionDistance;
        
        // 添加随机偏移
        if (config.useRandomOffset)
        {
            Vector2 randomOffset = Random.insideUnitCircle * config.randomOffsetRange;
            basePosition += randomOffset;
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
