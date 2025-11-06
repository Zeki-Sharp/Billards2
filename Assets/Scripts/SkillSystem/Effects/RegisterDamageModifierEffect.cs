using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 注册伤害修改器效果 - 用于状态条件增伤技能
/// 
/// 【核心职责】：
/// - 管理 StatusConditionalDamageModifier 的生命周期
/// - 技能激活时：创建并注册 Modifier 到 DamageProcessor
/// - 技能失效时：注销并销毁 Modifier
/// 
/// 【使用场景】：
/// - 被动技能（如点燃惩戒）
/// - 配合 AlwaysTrueTrigger 使用（技能激活时立即执行）
/// - 状态检测和伤害修改在 Modifier 中完成
/// 
/// 【配置说明】：
/// - targetStatusData：要检测的状态（拖拽 SO）
/// - increaseType：百分比或固定值
/// - damageMultiplier：百分比倍率（如 1.5 = +50%）
/// - fixedDamageBonus：固定值加成（如 +10）
/// 
/// 【技术说明】：
/// - 在玩家 GameObject 上创建 Modifier 组件
/// - 自动注册到 DamageProcessor
/// - 技能升级时会先移除旧的，再创建新的
/// </summary>
[System.Serializable]
public class RegisterDamageModifierEffect : IEffect
{
    public string EffectName => "RegisterDamageModifier";
    
    #region 配置参数
    
    [BoxGroup("状态检测配置")]
    [LabelText("目标状态")]
    [Tooltip("要检测的状态数据（拖拽 TurnBasedStatusData SO）")]
    [Required]
    public TurnBasedStatusData targetStatusData;
    
    [BoxGroup("伤害增加配置")]
    [LabelText("增伤模式")]
    [Tooltip("百分比：伤害 × 倍率，固定值：伤害 + 固定值")]
    public DamageIncreaseType increaseType = DamageIncreaseType.Percentage;
    
    [BoxGroup("伤害增加配置")]
    [LabelText("伤害倍率")]
    [Tooltip("百分比模式使用（如 1.5 = +50%伤害）")]
    [ShowIf("increaseType", DamageIncreaseType.Percentage)]
    public float damageMultiplier = 1.5f;
    
    [BoxGroup("伤害增加配置")]
    [LabelText("固定伤害加成")]
    [Tooltip("固定值模式使用（如 +10 点伤害）")]
    [ShowIf("increaseType", DamageIncreaseType.Fixed)]
    public float fixedDamageBonus = 10f;
    
    [BoxGroup("调试")]
    [LabelText("显示日志")]
    public bool showDebugLog = true;
    
    #endregion
    
    #region 运行时字段
    
    // 执行权限（由重置条件控制）
    private bool canExecute = true;
    public bool CanExecute => canExecute;
    
    // 目标角色ID（技能归属的角色）
    private string targetCharacterID;
    
    // 创建的 Modifier 实例引用
    private StatusConditionalDamageModifier modifierInstance;
    
    #endregion
    
    #region IEffect 接口实现
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 验证配置
        if (targetStatusData == null)
        {
            Debug.LogError($"[{EffectName}] targetStatusData 未配置！");
        }
        
        // 验证数值合理性
        if (increaseType == DamageIncreaseType.Percentage && damageMultiplier <= 0f)
        {
            Debug.LogWarning($"[{EffectName}] 百分比模式下 damageMultiplier 应该 > 0，当前值: {damageMultiplier}");
        }
        
        if (increaseType == DamageIncreaseType.Fixed && fixedDamageBonus <= 0f)
        {
            Debug.LogWarning($"[{EffectName}] 固定值模式下 fixedDamageBonus 应该 > 0，当前值: {fixedDamageBonus}");
        }
    }
    
    /// <summary>
    /// 设置是否允许执行（由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool value)
    {
        canExecute = value;
    }
    
    /// <summary>
    /// 设置效果的目标角色ID
    /// </summary>
    public void SetTarget(string characterID)
    {
        targetCharacterID = characterID;
    }
    
    /// <summary>
    /// 执行效果 - 创建并注册 Modifier
    /// </summary>
    public bool ExecuteEffect(SkillArgs args)
    {
        // 检查执行权限
        if (!canExecute)
        {
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] 执行权限被禁止，跳过执行");
            }
            return false;
        }
        
        // 检查配置
        if (targetStatusData == null)
        {
            Debug.LogError($"[{EffectName}] targetStatusData 未配置，无法创建 Modifier！");
            return false;
        }
        
        // 检查是否已经创建过（避免重复）
        if (modifierInstance != null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[{EffectName}] Modifier 已存在，跳过重复创建");
            }
            return false;
        }
        
        // 获取玩家对象
        GameObject playerObject = GetPlayerObject();
        if (playerObject == null)
        {
            Debug.LogError($"[{EffectName}] 无法找到玩家对象，无法创建 Modifier！");
            return false;
        }
        
        // 在玩家对象上创建 Modifier 组件
        modifierInstance = playerObject.AddComponent<StatusConditionalDamageModifier>();
        
        // 配置 Modifier 参数
        modifierInstance.Configure(
            targetStatusData,
            increaseType,
            damageMultiplier,
            fixedDamageBonus,
            targetCharacterID,
            showDebugLog
        );
        
        // 注册到 DamageProcessor
        if (DamageProcessor.Instance != null)
        {
            DamageProcessor.Instance.RegisterDamageModifier(modifierInstance);
            
            if (showDebugLog)
            {
                string increaseDesc = increaseType == DamageIncreaseType.Percentage 
                    ? $"×{damageMultiplier}" 
                    : $"+{fixedDamageBonus}";
                Debug.Log($"[{EffectName}] ✅ 注册伤害修改器成功 - 状态:{targetStatusData.displayName}, 增伤:{increaseDesc}");
            }
        }
        else
        {
            Debug.LogError($"[{EffectName}] DamageProcessor 不存在，无法注册 Modifier！");
            Object.Destroy(modifierInstance);
            modifierInstance = null;
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 移除效果 - 注销并销毁 Modifier
    /// </summary>
    public void RemoveEffect()
    {
        if (modifierInstance != null)
        {
            // 从 DamageProcessor 注销
            if (DamageProcessor.Instance != null)
            {
                DamageProcessor.Instance.UnregisterDamageModifier(modifierInstance);
            }
            
            // 销毁组件
            Object.Destroy(modifierInstance);
            modifierInstance = null;
            
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] ✅ 移除伤害修改器 - 状态:{targetStatusData?.displayName}");
            }
        }
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 获取玩家对象（根据角色ID）
    /// </summary>
    private GameObject GetPlayerObject()
    {
        // ✅ 多角色系统：通过 TeamData 查找对应的玩家球
        if (!string.IsNullOrEmpty(targetCharacterID))
        {
            var teamData = GameSession.Instance?.GetTeamData();
            if (teamData != null)
            {
                var character = teamData.GetCharacter(targetCharacterID);
                if (character != null && character.ballInstance != null)
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"[{EffectName}] 找到角色 {targetCharacterID} 的球实例: {character.ballInstance.name}");
                    }
                    return character.ballInstance;
                }
            }
        }
        
        // 兜底：查找场景中的第一个玩家（单角色模式）
        var player = Object.FindFirstObjectByType<PlayerBehavior>();
        if (player != null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning($"[{EffectName}] 未找到角色ID对应的玩家，使用场景中的第一个玩家: {player.gameObject.name}");
            }
            return player.gameObject;
        }
        
        return null;
    }
    
    #endregion
}

