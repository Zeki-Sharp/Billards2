using UnityEngine;

/// <summary>
/// 治疗效果 - 恢复玩家当前生命值
/// 用于治疗药水、回血技能等场景
/// </summary>
public class HealEffect : IEffect
{
    public string EffectName => "HealEffect";
    
    // 瞬时效果，总是可以执行
    public bool CanExecute => true;
    
    /// <summary>
    /// 设置是否允许执行（空实现，瞬时效果总是可以执行）
    /// </summary>
    public void SetCanExecute(bool canExecute)
    {
        // 瞬时效果不需要控制执行权限
    }
    
    private float healAmount = 20f;
    private PlayerCore targetPlayer;
    
    /// <summary>
    /// 设置治疗量
    /// </summary>
    public void SetHealAmount(float amount)
    {
        healAmount = amount;
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 延迟初始化：不在初始化时查找玩家，而是在执行时动态查找
    }
    
    /// <summary>
    /// 执行治疗效果
    /// </summary>
    public bool ExecuteEffect(object eventData)
    {
        // 动态查找目标玩家
        if (!GetTargetPlayer())
        {
            Debug.LogError($"[{EffectName}] 无法找到目标玩家，无法执行治疗");
            return false;
        }
        
        // 执行治疗
        targetPlayer.Heal(healAmount);
        Debug.Log($"[技能] 治疗效果触发 +{healAmount}");
        
        // 触发治疗表现效果（可选）
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 触发治疗的表现效果
    /// </summary>
    private void TriggerVisualEffect()
    {
        // 可以在这里触发治疗特效（例如：绿色光芒、回血数字等）
    }
    
    /// <summary>
    /// 动态获取目标玩家
    /// </summary>
    private bool GetTargetPlayer()
    {
        if (targetPlayer == null)
        {
            targetPlayer = Object.FindFirstObjectByType<PlayerCore>();
            if (targetPlayer == null)
            {
                Debug.LogWarning($"[{EffectName}] 未找到PlayerCore");
                return false;
            }
        }
        
        // 检查玩家是否就绪
        if (!IsPlayerReady(targetPlayer))
        {
            targetPlayer = null;
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查玩家是否就绪
    /// </summary>
    private bool IsPlayerReady(PlayerCore player)
    {
        return player != null && player.enabled && player.gameObject.activeInHierarchy;
    }
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void Reset()
    {
        // 治疗是瞬时效果，无需重置
    }
}

