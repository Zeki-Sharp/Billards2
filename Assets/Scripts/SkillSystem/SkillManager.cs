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
    
    /// <summary>
    /// 技能状态管理器引用
    /// </summary>
    private SkillStateManager skillStateManager;
    
    // 技能实例管理
    private Dictionary<string, SkillInstance> skillInstances = new Dictionary<string, SkillInstance>();
    
    /// <summary>
    /// Spawn类型技能名称列表（用于掉落系统）
    /// </summary>
    private HashSet<string> spawnSkillNames = new HashSet<string>();
    
    #region Unity生命周期
    
    void Start()
    {
        // 查找技能状态管理器
        skillStateManager = FindFirstObjectByType<SkillStateManager>();
        if (skillStateManager == null)
        {
            Debug.LogWarning("[SkillManager] 未找到SkillStateManager，技能状态跟踪功能将不可用");
        }
        
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
                
                // 检查是否为Spawn类型技能
                if (skillConfig.effectConfig?.effectType == SkillEffectType.Spawn)
                {
                    spawnSkillNames.Add(skillConfig.skillName);
                    if (enableDebugLog)
                    {
                        Debug.Log($"SkillManager: 注册Spawn技能 - {skillConfig.skillName}");
                    }
                }
                
                // 通知技能状态管理器技能已激活
                skillStateManager?.AddActiveSkill(skillConfig.skillName);
                
                if (enableDebugLog)
                {
                    Debug.Log($"SkillManager: 初始化技能 - {skillConfig.skillName}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"SkillManager: 初始化完成，共加载 {skillInstances.Count} 个技能");
            foreach (var skillName in skillInstances.Keys)
            {
                Debug.Log($"SkillManager: 已加载技能 - {skillName}");
            }
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnAttack += HandleAttackEvent;
        GameEventBus.OnDeath += HandleDeathEvent;
        //GameEventBus.OnChargingStarted += OnNewShotStarted;
        GameEventBus.OnHealthChanged += HandleHealthChangedEvent;
        GameEventBus.OnGameFlowStateChanged += HandleGameFlowStateChanged;
        GameEventBus.OnBallStopped += HandleBallStoppedEvent;
        
        if (enableDebugLog)
        {
            Debug.Log("SkillManager: 已订阅攻击事件、死亡事件、发射开始事件、生命值变化事件、游戏流程状态变化事件和球停止事件");
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnAttack -= HandleAttackEvent;
        GameEventBus.OnDeath -= HandleDeathEvent;
        //；GameEventBus.OnChargingStarted -= OnNewShotStarted;
        GameEventBus.OnHealthChanged -= HandleHealthChangedEvent;
        GameEventBus.OnGameFlowStateChanged -= HandleGameFlowStateChanged;
        GameEventBus.OnBallStopped -= HandleBallStoppedEvent;
        
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
        
        // 只处理攻击相关的技能
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(attackData, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(attackData);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理死亡事件
    /// </summary>
    void HandleDeathEvent(DeathData deathData)
    {
        // 只处理死亡相关的技能
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(deathData, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(deathData);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理生命值变化事件
    /// </summary>
    void HandleHealthChangedEvent(HealthStateData healthData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到生命值变化事件: {healthData.CurrentHealth}/{healthData.MaxHealth} ({healthData.HealthPercentage:P1})");
        }
        
        // 只处理血量相关的技能
        foreach (var skillInstance in skillInstances.Values)
        {
            if (IsEventRelevantForSkill(healthData, skillInstance))
            {
                bool processed = skillInstance.ProcessEvent(healthData);
                if (processed && enableDebugLog)
                {
                    Debug.Log($"[SkillManager] 技能 {skillInstance.config.skillName} 被触发");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理游戏流程状态变化事件
    /// </summary>
    void HandleGameFlowStateChanged(GameFlowState newState)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 游戏流程状态变化: {newState}");
        }
        
        // 通知所有技能实例处理回合结束事件
        foreach (var skillInstance in skillInstances.Values)
        {
            skillInstance.HandlePhaseEndEvent(newState);
        }
    }
    
    /// <summary>
    /// 处理球停止事件 - 用于触发 MovingEnd 技能
    /// </summary>
    void HandleBallStoppedEvent(BallPhysics ballPhysics)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] 收到球停止事件: {ballPhysics.gameObject.name}");
        }
        
        // 只处理玩家球的停止事件
        if (ballPhysics.gameObject.CompareTag("Player"))
        {
            // 处理所有 MovingEnd 类型的技能
            foreach (var skillInstance in skillInstances.Values)
            {
                if (IsEventRelevantForSkill(ballPhysics, skillInstance))
                {
                    bool processed = skillInstance.ProcessEvent(ballPhysics);
                    if (processed && enableDebugLog)
                    {
                        Debug.Log($"[SkillManager] MovingEnd 技能 {skillInstance.config.skillName} 被触发");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 检查事件是否与技能相关
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="skillInstance">技能实例</param>
    /// <returns>是否相关</returns>
    private bool IsEventRelevantForSkill(object eventData, SkillInstance skillInstance)
    {
        // 根据触发器的类型判断事件相关性
        if (skillInstance.trigger is DataSourceTrigger dataSourceTrigger)
        {
            // DataSourceTrigger 根据配置的数据提取器类型判断
            switch (skillInstance.config.triggerConfig.dataExtractorType)
            {
                case DataExtractorType.Health:
                    return eventData is HealthStateData;
                case DataExtractorType.Attack:
                    return eventData is AttackData;
                case DataExtractorType.Defense:
                    return eventData is AttackData; // 防御通常与攻击事件相关
                case DataExtractorType.Speed:
                    return false; // 速度变化事件暂未实现
                case DataExtractorType.Mana:
                    return false; // 法力变化事件暂未实现
                default:
                    return false;
            }
        }
        else if (skillInstance.trigger is CollisionTrigger)
        {
            // CollisionTrigger 只对攻击事件有效
            return eventData is AttackData;
        }
        else if (skillInstance.trigger is KillTrigger)
        {
            // KillTrigger 只对死亡事件有效
            return eventData is DeathData;
        }
        else if (skillInstance.trigger is MovingEndTrigger)
        {
            // MovingEndTrigger 只对球停止事件有效
            return eventData is BallPhysics;
        }
        
        // 默认情况下，不处理任何事件
        return false;
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
            
            // 通知技能状态管理器技能已激活
            skillStateManager?.AddActiveSkill(skillConfig.skillName);
            
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
            
            // 通知技能状态管理器技能已失效
            skillStateManager?.RemoveActiveSkill(skillName);
            
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
    
    /// <summary>
    /// 获取所有Spawn类型技能名称（用于掉落系统）
    /// </summary>
    public HashSet<string> GetSpawnSkillNames()
    {
        return new HashSet<string>(spawnSkillNames);
    }
    
    /// <summary>
    /// 检查指定技能是否为Spawn类型
    /// </summary>
    public bool IsSpawnSkill(string skillName)
    {
        return spawnSkillNames.Contains(skillName);
    }
    
    /// <summary>
    /// 检查是否有指定类型的激活技能
    /// </summary>
    /// <param name="effectType">效果类型</param>
    /// <returns>是否有该类型的技能</returns>
    public bool HasActiveSkillOfType(SkillEffectType effectType)
    {
        foreach (var skillInstance in skillInstances.Values)
        {
            if (skillInstance.config.effectConfig?.effectType == effectType)
            {
                return true;
            }
        }
        return false;
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
