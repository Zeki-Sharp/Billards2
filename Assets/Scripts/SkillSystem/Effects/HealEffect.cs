using UnityEngine;

/// <summary>
/// 治疗效果 - 恢复玩家当前生命值
/// 用于治疗药水、回血技能等场景
/// </summary>
public class HealEffect : IEffect
{
    public string EffectName => "HealEffect";
    
    private float healAmount = 20f;
    private PlayerCore targetPlayer;
    
    /// <summary>
    /// 设置治疗量
    /// </summary>
    public void SetHealAmount(float amount)
    {
        healAmount = amount;
        Debug.Log($"[{EffectName}] 设置治疗量: {healAmount}");
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 查找目标玩家
        targetPlayer = Object.FindObjectOfType<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogError($"[{EffectName}] 未找到PlayerCore，无法应用治疗效果");
            return;
        }
        
        Debug.Log($"[{EffectName}] 初始化完成，目标玩家: {targetPlayer.name}");
    }
    
    /// <summary>
    /// 执行治疗效果
    /// </summary>
    public bool ExecuteEffect(object eventData)
    {
        if (targetPlayer == null)
        {
            Debug.LogError($"[{EffectName}] 目标玩家为空，无法执行治疗");
            return false;
        }
        
        // 记录治疗前的血量
        float beforeHealth = targetPlayer.GetCurrentHealth();
        float maxHealth = targetPlayer.GetMaxHealth();
        
        Debug.Log($"[{EffectName}] 🩹 开始执行治疗 - 治疗量: {healAmount}, 当前血量: {beforeHealth}/{maxHealth}");
        
        // 执行治疗
        targetPlayer.Heal(healAmount);
        
        // 记录治疗后的血量
        float afterHealth = targetPlayer.GetCurrentHealth();
        float actualHealed = afterHealth - beforeHealth;
        
        Debug.Log($"[{EffectName}] ✅ 治疗完成 - 治疗量: {healAmount}, " +
                  $"实际恢复: {actualHealed}, " +
                  $"血量变化: {beforeHealth}/{maxHealth} → {afterHealth}/{maxHealth}");
        
        // 触发治疗表现效果（可选）
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 触发治疗的表现效果
    /// </summary>
    private void TriggerVisualEffect()
    {
        // TODO: 可以在这里触发治疗特效
        // 例如：绿色光芒、回血数字等
        // GameEventBus.PublishEffectEvent("Heal", targetPlayer.transform.position, ...);
        
        Debug.Log($"[{EffectName}] 触发治疗表现效果 at {targetPlayer.transform.position}");
    }
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void Reset()
    {
        // 治疗是瞬时效果，无需重置
        // 如果未来需要支持"持续回血"，可以在这里处理
        Debug.Log($"[{EffectName}] 效果重置（治疗是瞬时效果，无需处理）");
    }
}

