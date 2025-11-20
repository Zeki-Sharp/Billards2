using UnityEngine;

/// <summary>
/// 点燃状态 - 持续造成火焰伤害
/// 
/// 【效果说明】：
/// - 每个敌人回合结束时造成伤害
/// - 回合数累加，伤害保持第一次的值
/// - 回合数耗尽后自动移除
/// 
/// 【伤害类型】：
/// - 普通物理伤害（简化版）
/// - 通过 IDamageable.OnDamageReceived() 造成伤害
/// </summary>
public class BurningStatus : TurnBasedStatusComponent
{
    /// <summary>
    /// 每回合触发：造成火焰伤害
    /// </summary>
    protected override void OnTurnTrigger()
    {
        // 获取 IDamageable 组件（支持从父级查找）
        var damageable = GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Debug.LogWarning($"[点燃] {gameObject.name} 及其父级没有 IDamageable 组件，无法造成伤害");
            return;
        }
        
        // 检查是否可以受伤
        if (!damageable.CanTakeDamage())
        {
            return;
        }
        
        // 构造伤害事件
        // ✅ 3D适配：使用XZ平面投影和真实3D位置
        Vector3 pos3D = transform.position;
        
        DamageEvent damageEvent = new DamageEvent
        {
            Source = source,
            Target = gameObject,
            FinalDamage = damagePerTurn,
            Type = DamageType.Physical,  // ✅ 简化版：普通物理伤害
            TriggerType = DamageTriggerType.Interval,  // 持续伤害是间隔触发
            HitPosition = new Vector2(pos3D.x, pos3D.z),  // XZ平面投影（向后兼容）
            HitPosition3D = pos3D,  // ✅ 真实3D位置（用于特效定位）
            HitDirection = Vector2.zero,  // 持续伤害无方向
            VelocityAtHit = 0f,
            KnockbackForce = 0f,  // 持续伤害不击退
            StunDuration = 0f,
            CanBeBlocked = false,  // 持续伤害无法格挡
            RuleName = "BurningDamage",
            EventTime = Time.time
        };
        
        // 造成伤害
        damageable.OnDamageReceived(damageEvent);
        
        if (showDebugLog)
        {
            Debug.Log($"[点燃] 🔥 {gameObject.name} 受到点燃伤害：{damagePerTurn}，剩余{remainingTurns}回合");
        }
    }
    
    /// <summary>
    /// 状态首次施加时
    /// </summary>
    protected override void OnStatusApplied()
    {
        // 可选：添加特效或动画
    }
    
    /// <summary>
    /// 状态移除时
    /// </summary>
    protected override void OnStatusRemoved()
    {
        // 可选：清理特效或动画
    }
}

