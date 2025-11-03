using UnityEngine;

/// <summary>
/// 治疗效果 - 恢复玩家当前生命值
/// 用于治疗药水、回血技能等场景
/// </summary>
public class HealEffect : IEffect
{
    public string EffectName => "HealEffect";
    
    private bool canExecute = true; // 是否允许执行（完全由重置条件控制）
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    public bool CanExecute => canExecute;
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool value)
    {
        canExecute = value;
        Debug.Log($"[{EffectName}] 设置执行权限: {value}");
    }
    
    // ✅ 使用 PropertyGetFloat 替代固定值
    private PropertyGetFloat healAmount;
    private PlayerBehavior targetPlayer;
    
    // ✅ 多角色系统：目标角色ID
    private string targetCharacterID;
    
    /// <summary>
    /// 设置治疗量 Property
    /// </summary>
    public void SetHealAmount(PropertyGetFloat property)
    {
        healAmount = property;
    }
    
    /// <summary>
    /// ✅ 多角色系统：设置效果的目标角色ID
    /// </summary>
    /// <param name="characterID">目标角色ID</param>
    public void SetTarget(string characterID)
    {
        targetCharacterID = characterID;
        
        // 清除缓存的目标，强制重新查找
        targetPlayer = null;
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 延迟初始化：不在初始化时查找玩家，而是在执行时动态查找
        
        // ✅ 如果没有设置 Property，使用默认固定值
        if (healAmount == null)
        {
            healAmount = new ConstantFloat(20f);
        }
    }
    
    /// <summary>
    /// 执行治疗效果
    /// </summary>
    public bool ExecuteEffect(SkillArgs args)
    {
        // 检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            Debug.Log($"[{EffectName}] 执行权限被禁止，跳过执行");
            return false;
        }
        
        // 动态查找目标玩家
        if (!GetTargetPlayer())
        {
            Debug.LogError($"[{EffectName}] 无法找到目标玩家，无法执行治疗");
            return false;
        }
        
        // ✅ 动态获取治疗量
        float amount = healAmount.Get(args);
        
        // 执行治疗
        targetPlayer.Heal(amount);
        Debug.Log($"[技能] 治疗效果触发 +{amount:F1} HP");
        
        // 触发治疗表现效果（可选）
        TriggerVisualEffect();
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        canExecute = false;
        Debug.Log($"[{EffectName}] 执行成功，禁止再次执行，等待重置条件");
        
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
    /// ✅ 多角色系统改造：动态获取目标角色
    /// </summary>
    private bool GetTargetPlayer()
    {
        if (targetPlayer == null)
        {
            // ✅ 多角色系统：根据 targetCharacterID 查找对应角色
            if (!string.IsNullOrEmpty(targetCharacterID))
            {
                var teamData = GameSession.Instance?.GetTeamData();
                if (teamData != null)
                {
                    var character = teamData.characters.Find(c => c.characterID == targetCharacterID);
                    if (character != null && character.ballInstance != null)
                    {
                        targetPlayer = character.ballInstance.GetComponent<PlayerBehavior>();
                        
                        if (targetPlayer == null)
                        {
                            Debug.LogError($"[{EffectName}] 角色 {targetCharacterID} 的球体没有 PlayerBehavior 组件");
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{EffectName}] 找不到角色 {targetCharacterID} 或其球体实例");
                        return false;
                    }
                }
                else
                {
                    Debug.LogWarning($"[{EffectName}] TeamData 为空，无法查找目标角色");
                    return false;
                }
            }
            else
            {
                // 向后兼容：如果没有指定角色ID，使用全局查找
                Debug.LogWarning($"[{EffectName}] 未指定目标角色ID，使用全局查找（不推荐）");
                targetPlayer = Object.FindFirstObjectByType<PlayerBehavior>();
                
                if (targetPlayer == null)
                {
                    Debug.LogWarning($"[{EffectName}] 未找到PlayerCore");
                    return false;
                }
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
    private bool IsPlayerReady(PlayerBehavior player)
    {
        return player != null && player.enabled && player.gameObject.activeInHierarchy;
    }
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void RemoveEffect()
    {
        // 治疗是瞬时效果，无需重置
    }
}

