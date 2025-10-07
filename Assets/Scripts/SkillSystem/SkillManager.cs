using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能管理器 - 管理所有技能配置和运行时状态
/// 替代 TestSkillChain，提供可配置的技能系统
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("技能配置")]
    [Tooltip("激活的技能配置列表")]
    public List<SkillConfig> activeSkills = new List<SkillConfig>();
    
    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool enableDebugLog = true;
    
    // 技能实例管理
    private Dictionary<string, SkillInstance> skillInstances = new Dictionary<string, SkillInstance>();
    
    #region Unity生命周期
    
    void Start()
    {
        InitializeSkills();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化技能实例
    /// </summary>
    void InitializeSkills()
    {
        skillInstances.Clear();
        
        foreach (var skillConfig in activeSkills)
        {
            if (skillConfig == null)
            {
                Debug.LogWarning("SkillManager: 发现空的技能配置，跳过");
                continue;
            }
            
            if (!skillConfig.IsValid())
            {
                Debug.LogError($"SkillManager: 技能配置无效: {skillConfig.skillName}");
                continue;
            }
            
            var skillInstance = skillConfig.CreateSkillInstance();
            if (skillInstance != null)
            {
                skillInstances[skillConfig.skillName] = skillInstance;
                
                if (enableDebugLog)
                {
                    Debug.Log($"SkillManager: 初始化技能 - {skillConfig.skillName}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SkillManager: 初始化完成，共加载 {skillInstances.Count} 个技能");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnAttack += HandleAttackEvent;
        GameEventBus.OnChargingStarted += OnNewShotStarted;
        
        if (enableDebugLog)
        {
            Debug.Log("SkillManager: 已订阅攻击事件和发射开始事件");
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnAttack -= HandleAttackEvent;
        GameEventBus.OnChargingStarted -= OnNewShotStarted;
        
        if (enableDebugLog)
        {
            Debug.Log("SkillManager: 已取消事件订阅");
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理攻击事件
    /// </summary>
    void HandleAttackEvent(AttackData attackData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到攻击事件: {attackData.AttackType} at {attackData.Position}");
        }
        
        // 处理所有技能
        foreach (var skillInstance in skillInstances.Values)
        {
            bool processed = skillInstance.ProcessEvent(attackData);
            if (processed && enableDebugLog)
            {
                Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
            }
        }
    }
    
    /// <summary>
    /// 处理新发射开始事件 - 重置技能状态
    /// </summary>
    void OnNewShotStarted()
    {
        if (enableDebugLog)
        {
            Debug.Log("[SkillManager] 检测到新发射开始，重置技能状态");
        }
        
        // 重置所有技能状态
        foreach (var skillInstance in skillInstances.Values)
        {
            skillInstance.Reset();
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[SkillManager] 技能状态重置完成，可以重新触发");
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 添加技能配置
    /// </summary>
    public void AddSkill(SkillConfig skillConfig)
    {
        if (skillConfig == null || !skillConfig.IsValid())
        {
            Debug.LogError("SkillManager: 无法添加无效的技能配置");
            return;
        }
        
        if (skillInstances.ContainsKey(skillConfig.skillName))
        {
            Debug.LogWarning($"SkillManager: 技能 {skillConfig.skillName} 已存在，跳过添加");
            return;
        }
        
        activeSkills.Add(skillConfig);
        var skillInstance = skillConfig.CreateSkillInstance();
        if (skillInstance != null)
        {
            skillInstances[skillConfig.skillName] = skillInstance;
            
            if (enableDebugLog)
            {
                Debug.Log($"SkillManager: 添加技能 - {skillConfig.skillName}");
            }
        }
    }
    
    /// <summary>
    /// 移除技能配置
    /// </summary>
    public void RemoveSkill(string skillName)
    {
        if (skillInstances.ContainsKey(skillName))
        {
            skillInstances.Remove(skillName);
            activeSkills.RemoveAll(config => config.skillName == skillName);
            
            if (enableDebugLog)
            {
                Debug.Log($"SkillManager: 移除技能 - {skillName}");
            }
        }
    }
    
    /// <summary>
    /// 重新加载所有技能
    /// </summary>
    [ContextMenu("重新加载技能")]
    public void ReloadSkills()
    {
        InitializeSkills();
    }
    
    /// <summary>
    /// 获取技能实例
    /// </summary>
    public SkillInstance GetSkillInstance(string skillName)
    {
        skillInstances.TryGetValue(skillName, out SkillInstance skillInstance);
        return skillInstance;
    }
    
    /// <summary>
    /// 获取所有技能名称
    /// </summary>
    public List<string> GetAllSkillNames()
    {
        return new List<string>(skillInstances.Keys);
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 显示技能状态信息
    /// </summary>
    [ContextMenu("显示技能状态")]
    public void ShowSkillStatus()
    {
        Debug.Log("=== 技能状态信息 ===");
        foreach (var skillInstance in skillInstances.Values)
        {
            Debug.Log($"技能: {skillInstance.config.skillName}");
            Debug.Log($"- 配置: {skillInstance.config.GetDebugInfo()}");
        }
        Debug.Log("===================");
    }
    
    #endregion
}
