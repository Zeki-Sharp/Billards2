using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 远程攻击行为
/// 攻击范围投射到玩家附近位置，位置独立于敌人，即使敌人被撞击也不会改变攻击位置
/// </summary>
public class RangedAttackBehavior : BaseAttackBehavior
{
    private Vector2 projectedPosition; // 保存投射位置
    private Vector3 originalLocalPosition; // 保存原始本地位置
    private Transform originalParent; // 保存原始父物体
    private ParabolicIndicator parabolicIndicator; // 抛物线指示器
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return;
        }
        
        // 检测玩家是否在检测范围内
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        if (distanceToPlayer > levelConfig.rangedConfig.detectionRange)
        {
            Debug.Log($"RangedAttackBehavior: 玩家不在检测范围内 ({distanceToPlayer} > {levelConfig.rangedConfig.detectionRange})");
            return;
        }
        
        // 保存原始父子关系和本地位置
        originalParent = attackRange.transform.parent;
        originalLocalPosition = attackRange.transform.localPosition;
        
        Debug.Log($"RangedAttackBehavior: 保存原始状态 - 父物体: {(originalParent != null ? originalParent.name : "null")}，localPosition: {originalLocalPosition}");
        
        // 计算投射位置
        projectedPosition = CalculateProjectionPosition(enemyTransform.position, playerTransform.position, levelConfig.rangedConfig);
        
        // 解除父子关系，使用世界坐标
        attackRange.transform.SetParent(null);
        
        // 设置攻击范围到投射位置
        attackRange.transform.position = projectedPosition;
        
        // 显示攻击预告（这会激活 GameObject 并设置方向）
        attackRange.ShowTelegraph();
        
        // 显示抛物线指示器
        if (levelConfig.rangedConfig.showParabolicIndicator)
        {
            ShowParabolicIndicator(enemyTransform, attackRange.transform, levelConfig.rangedConfig);
        }
        
        Debug.Log($"RangedAttackBehavior: 显示远程攻击预告，投射位置: {projectedPosition}，AttackRange实际位置: {attackRange.transform.position}");
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public override void ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return;
        }
        
        Debug.Log($"RangedAttackBehavior: 攻击阶段开始 - AttackRange位置: {attackRange.transform.position}，父物体: {(attackRange.transform.parent != null ? attackRange.transform.parent.name : "null")}");
        
        // 使用预告阶段保存的朝向
        attackRange.ApplyTelegraphedDirection();
        
        Debug.Log($"RangedAttackBehavior: ApplyTelegraphedDirection后 - AttackRange位置: {attackRange.transform.position}");
        
        // 播放攻击特效
        PlayAttackEffect(attackEffect, enemyTransform.name);
        
        // 获取攻击范围内的目标
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                DealDamageToPlayer(target, levelConfig, enemyTransform);
            }
        }
    }
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    public override void CleanupAttack(Transform enemyTransform, AttackRange attackRange)
    {
        if (attackRange == null || originalParent == null)
        {
            return;
        }
        
        Debug.Log($"RangedAttackBehavior: 开始清理 - AttackRange位置: {attackRange.transform.position}");
        
        // 隐藏并清理抛物线指示器
        HideParabolicIndicator();
        
        // 恢复父子关系
        attackRange.transform.SetParent(originalParent);
        
        // 恢复原始本地位置
        attackRange.transform.localPosition = originalLocalPosition;
        
        Debug.Log($"RangedAttackBehavior: 清理完成 - 恢复到原始父物体: {originalParent.name}，localPosition: {originalLocalPosition}");
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
        
        Debug.Log($"RangedAttackBehavior: 计算投射位置 - 玩家: {playerPosition}, 投射: {basePosition}");
        
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
