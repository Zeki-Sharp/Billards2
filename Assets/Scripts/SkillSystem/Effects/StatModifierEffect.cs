using UnityEngine;

/// <summary>
/// 数值调整效果 - 单模式 + 叠加控制
/// 使用修饰器系统修改玩家的某个属性（如攻击力+50%）
/// 支持临时效果和基于条件的移除
/// </summary>
public class StatModifierEffect : IEffect
{
    public string EffectName => "StatModifierEffect";
    
    private string targetStat = "Damage"; // 默认修改攻击力（使用新命名）
    // ✅ 使用 PropertyGetFloat 替代固定值
    private PropertyGetFloat modifierValue;
    private StatModifierType modifierType = StatModifierType.PercentMult; // 默认百分比乘算
    private bool canExecute = true;       // 是否允许执行（完全由重置条件控制）
    private PlayerBehavior targetPlayer;      // 目标玩家
    private PlayerStats statsManager; // ✅ 属性管理器（轻量级系统）
    
    // ✅ 多角色系统：目标角色ID
    private string targetCharacterID;
    
    // 叠加控制字段
    private bool allowStacking = true;    // 是否允许叠加
    private bool hasTriggered = false;    // 是否已经触发（用于非叠加效果）
    
    // 修改器管理字段（新系统）
    private System.Collections.Generic.List<ModifierHandle> appliedHandles = new System.Collections.Generic.List<ModifierHandle>(); // ✅ 使用轻量级句柄
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    public bool CanExecute => canExecute;
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    public void SetCanExecute(bool canExecute)
    {
        this.canExecute = canExecute;
    }
    
    /// <summary>
    /// 设置是否允许叠加
    /// </summary>
    /// <param name="allowStacking">是否允许叠加</param>
    public void SetAllowStacking(bool allowStacking)
    {
        this.allowStacking = allowStacking;
        Debug.Log($"[{EffectName}] 设置允许叠加: {allowStacking}");
    }
    
    /// <summary>
    /// 获取是否允许叠加
    /// </summary>
    /// <returns>是否允许叠加</returns>
    public bool GetAllowStacking()
    {
        return allowStacking;
    }
    
    /// <summary>
    /// 设置修改参数（Property 版本）
    /// </summary>
    /// <param name="stat">要修改的属性名</param>
    /// <param name="modifier">修改值 Property</param>
    /// <param name="type">修改器类型</param>
    public void SetModifier(string stat, PropertyGetFloat modifier, StatModifierType type)
    {
        targetStat = stat;
        modifierValue = modifier;
        modifierType = type;
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
        statsManager = null;
    }
    
    /// <summary>
    /// 初始化（确保有默认 Property）
    /// </summary>
    public void Initialize()
    {
        // ✅ 如果没有设置 Property，使用默认固定值
        if (modifierValue == null)
        {
            modifierValue = new ConstantFloat(1.5f);
        }
    }
    
    /// <summary>
    /// 设置效果移除条件（新接口）
    /// </summary>
    /// <param name="condition">效果移除条件</param>
    public void SetEffectRemovalCondition(IEffectRemovalCondition condition)
    {
        effectRemovalCondition = condition;
    }
    
    private IEffectRemovalCondition effectRemovalCondition; // 新的效果移除条件
    
    /// <summary>
    /// 清理已失效的修改器（新系统：简化版）
    /// </summary>
    private void CleanupInvalidModifiers()
    {
        // ✅ 清理本地列表中的无效句柄
        appliedHandles.RemoveAll(h => h == null);
    }
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(SkillArgs args)
    {
        // 只检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            return false;
        }
        
        // 叠加控制检查：如果不允许叠加且已经触发，则跳过执行
        if (!allowStacking && hasTriggered)
        {
            Debug.Log($"[{EffectName}] 不允许叠加且已触发，跳过执行");
            return false;
        }
        
        // 动态查找目标玩家
        if (!GetTargetPlayer())
        {
            Debug.LogError($"[{EffectName}] 无法找到目标玩家，无法执行效果");
            return false;
        }
        
        // 清理已失效的修改器
        CleanupInvalidModifiers();
        
        // ✅ 统一执行效果逻辑（所有属性都使用 PlayerStats）
        bool result = ExecuteStatModification(args);
        
        // 更新触发状态
        if (result && !allowStacking)
        {
            hasTriggered = true;
            Debug.Log($"[{EffectName}] 标记已触发，后续不允许叠加");
        }
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        if (result)
        {
            canExecute = false;
        }
        
        return result;
    }
    
    /// <summary>
    /// 执行属性修改 - ✅ 统一使用 PlayerStats 系统（包括 Damage）
    /// </summary>
    /// <returns>是否执行成功</returns>
    private bool ExecuteStatModification(SkillArgs args)
    {
        if (statsManager == null)
        {
            Debug.LogError($"[{EffectName}] 属性管理器为空，无法执行效果");
            return false;
        }
        
        // ✅ 动态获取修改值
        float value = modifierValue.Get(args);
        
        ModifierHandle handle = null;
        
        // ✅ 根据修改器类型选择合适的方法
        bool isPercent = (modifierType == StatModifierType.PercentAdd || modifierType == StatModifierType.PercentMult);
        
        // 如果有移除条件，使用带条件的方法
        if (effectRemovalCondition != null)
        {
            handle = statsManager.AddConditionalModifier(
                targetStat,
                value,
                isPercent,
                effectRemovalCondition,
                this
            );
        }
        else
        {
            // 永久修改器
            if (isPercent)
            {
                handle = statsManager.AddPercent(targetStat, value, this);
            }
            else
            {
                handle = statsManager.AddConstant(targetStat, value, this);
            }
        }
        
        // 保存句柄
        if (handle != null)
        {
            appliedHandles.Add(handle);
        }
        
        Debug.Log($"[{EffectName}] ✅ 创建新的属性修改器: {targetStat} {(isPercent ? $"+{value * 100}%" : $"+{value}")}, 当前句柄数量: {appliedHandles.Count}");
        
        // 触发表现效果
        TriggerVisualEffect();
        
        return true;
    }
    
    /// <summary>
    /// 触发表现效果
    /// </summary>
    private void TriggerVisualEffect()
    {
        // 触发攻击力提升的表现特效
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
            }
            
            if (targetPlayer != null)
            {
                // 查找属性管理器
                statsManager = targetPlayer.GetComponent<PlayerStats>();
                if (statsManager == null)
                {
                    Debug.LogError($"[{EffectName}] 未找到PlayerStatsManagerV2，无法应用效果");
                    targetPlayer = null;
                    return false;
                }
                
                Debug.Log($"[{EffectName}] 动态找到目标玩家: {targetPlayer.name}");
            }
            else
            {
                Debug.LogWarning($"[{EffectName}] 未找到PlayerCore，可能玩家还未初始化");
                return false;
            }
        }
        
        // 检查玩家是否就绪
        if (!IsPlayerReady(targetPlayer))
        {
            Debug.LogWarning($"[{EffectName}] 玩家未就绪，重置引用并重试");
            targetPlayer = null;
            statsManager = null;
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
    /// 移除效果（删除所有修改器，重置 hasTriggered 标志）- ✅ 使用新系统
    /// 注意：不重置 canExecute，因为它完全由重置条件控制
    /// </summary>
    public void RemoveEffect()
    {
        // ✅ 移除所有属性修改器（包括 Damage，统一使用 PlayerStats）
        if (statsManager != null)
        {
            foreach (var handle in appliedHandles)
            {
                if (handle != null)
                {
                    statsManager.RemoveModifier(targetStat, handle);
                }
            }
        }
        
        // 清空句柄列表
        appliedHandles.Clear();
        
        // 重置触发状态（效果被移除时重置，允许重新触发）
        hasTriggered = false;
        
        // 注意：不重置 canExecute，因为它完全由重置条件控制
    }
}