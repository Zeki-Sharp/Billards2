using UnityEngine;

/// <summary>
/// 数值调整效果 - 技能系统第一阶段最小验证
/// 修改玩家的某个属性（如攻击力+50%）
/// 通过 GameEventBus.PublishEffectEvent 触发表现
/// </summary>
public class StatModifierEffect : IEffect
{
    public string EffectName => "StatModifierEffect";
    
    private string targetStat = "damage"; // 默认修改攻击力
    private float modifierValue = 1.5f;   // 默认+50%
    private float originalValue = 0f;     // 原始值
    private bool isApplied = false;       // 是否已应用
    private PlayerCore targetPlayer;      // 目标玩家
    
    /// <summary>
    /// 设置修改参数
    /// </summary>
    /// <param name="stat">要修改的属性名</param>
    /// <param name="modifier">修改值（倍数）</param>
    public void SetModifier(string stat, float modifier)
    {
        targetStat = stat;
        modifierValue = modifier;
        Debug.Log($"[{EffectName}] 设置修改参数: {targetStat} * {modifierValue}");
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 查找目标玩家
        targetPlayer = Object.FindObjectByType<PlayerCore>();
        if (targetPlayer == null)
        {
            Debug.LogError($"[{EffectName}] 未找到PlayerCore，无法应用效果");
            return;
        }
        
        Debug.Log($"[{EffectName}] 初始化完成，目标玩家: {targetPlayer.name}");
    }
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        if (targetPlayer == null)
        {
            Debug.LogError($"[{EffectName}] 目标玩家为空，无法执行效果");
            return false;
        }
        
        if (isApplied)
        {
            Debug.Log($"[{EffectName}] 效果已应用，跳过重复执行");
            return true;
        }
        
        // 获取玩家数据
        var playerData = targetPlayer.playerData;
        if (playerData == null)
        {
            Debug.LogError($"[{EffectName}] 玩家数据为空，无法执行效果");
            return false;
        }
        
        // 根据属性名修改对应数值
        switch (targetStat.ToLower())
        {
            case "damage":
                originalValue = playerData.damage;
                playerData.damage *= modifierValue;
                Debug.Log($"[{EffectName}] 攻击力修改: {originalValue} -> {playerData.damage} (x{modifierValue})");
                break;
                
            case "maxhealth":
                originalValue = playerData.maxHealth;
                playerData.maxHealth *= modifierValue;
                Debug.Log($"[{EffectName}] 最大血量修改: {originalValue} -> {playerData.maxHealth} (x{modifierValue})");
                break;
                
            case "micromovespeed":
                originalValue = playerData.microMoveSpeed;
                playerData.microMoveSpeed *= modifierValue;
                Debug.Log($"[{EffectName}] 微调移动速度修改: {originalValue} -> {playerData.microMoveSpeed} (x{modifierValue})");
                break;
                
            default:
                Debug.LogWarning($"[{EffectName}] 不支持的属性: {targetStat}");
                return false;
        }
        
        isApplied = true;
        
        // 触发表现效果（通过现有事件系统）
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 触发表现效果
    /// </summary>
    private void TriggerVisualEffect()
    {
        // 触发攻击力提升的表现特效
        // 使用现有的特效类型，比如 "Hit" 或自定义的升级特效
        // GameEventBus.PublishEffectEvent(
        //     "Hit",  // 使用现有的特效类型，或者可以扩展 EffectManager 支持新的特效类型
        //     targetPlayer.transform.position, 
        //     Vector3.up, 
        //     targetPlayer.gameObject, 
        //     "Player"
        // );
        
        Debug.Log($"[{EffectName}] 触发表现效果: 攻击力提升特效 at {targetPlayer.transform.position}");
        
        // TODO: 后续可以在 EffectManager 中添加专门的技能特效类型
        // 如: "SkillUpgrade", "StatBoost" 等，用于技能相关的表现效果
    }
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void Reset()
    {
        // 恢复原始值
        if (isApplied && targetPlayer != null && targetPlayer.playerData != null)
        {
            switch (targetStat.ToLower())
            {
                case "damage":
                    targetPlayer.playerData.damage = originalValue;
                    break;
                case "maxhealth":
                    targetPlayer.playerData.maxHealth = originalValue;
                    break;
                case "micromovespeed":
                    targetPlayer.playerData.microMoveSpeed = originalValue;
                    break;
            }
            
            Debug.Log($"[{EffectName}] 恢复原始值: {targetStat} = {originalValue}");
        }
        
        isApplied = false;
        originalValue = 0f;
        Debug.Log($"[{EffectName}] 效果重置完成");
    }
}
