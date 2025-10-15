using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 玩家属性管理器 - 管理所有活跃的属性修饰器
/// 负责计算属性的最终值，处理修饰器的生命周期
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    [Header("基础数据")]
    public PlayerData playerData;
    
    [Header("调试设置")]
    public bool enableDebugLog = true;
    
    // 修饰器管理
    private List<StatModifier> activeModifiers = new List<StatModifier>();
    
    // 缓存最终值，避免重复计算
    private Dictionary<string, float> cachedFinalValues = new Dictionary<string, float>();
    private bool cacheDirty = true;
    
    #region Unity生命周期
    
    void Start()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerStatsManager: PlayerData 未设置！");
        }
        
        // 订阅球停止运动事件（通过GameEventBus）
        GameEventBus.OnBallStopped += HandleBallStopped;
        
        // 订阅游戏流程状态变化事件
        GameEventBus.OnGameFlowStateChanged += HandleGameFlowStateChanged;
        
        // 订阅血量变化事件，用于检查移除条件
        GameEventBus.OnHealthChanged += HandleHealthChanged;
        
        if (enableDebugLog)
        {
            Debug.Log("PlayerStatsManager: 初始化完成");
        }
    }
    
    void Update()
    {
        UpdateModifiers();
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        GameEventBus.OnBallStopped -= HandleBallStopped;
        GameEventBus.OnGameFlowStateChanged -= HandleGameFlowStateChanged;
        GameEventBus.OnHealthChanged -= HandleHealthChanged;
    }
    
    #endregion
    
    #region 修饰器管理
    
    /// <summary>
    /// 应用修饰器
    /// </summary>
    public void ApplyModifier(StatModifier modifier)
    {
        if (modifier == null)
        {
            Debug.LogError("PlayerStatsManager: 尝试应用空的修饰器");
            return;
        }
        
        activeModifiers.Add(modifier);
        cacheDirty = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"PlayerStatsManager: 应用修饰器 - {modifier.GetDebugInfo()}");
        }
        
        // 触发属性变化事件
        OnStatChanged(modifier.targetStat);
    }
    
    /// <summary>
    /// 移除修饰器
    /// </summary>
    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        
        bool removed = activeModifiers.Remove(modifier);
        if (removed)
        {
            cacheDirty = true;
            
            if (enableDebugLog)
            {
                Debug.Log($"PlayerStatsManager: 移除修饰器 - {modifier.GetDebugInfo()}");
            }
            
            // 触发属性变化事件
            OnStatChanged(modifier.targetStat);
        }
    }
    
    /// <summary>
    /// 移除指定来源的所有修饰器
    /// </summary>
    public void RemoveModifiersBySource(object source)
    {
        var modifiersToRemove = activeModifiers.Where(m => m.source == source).ToList();
        
        foreach (var modifier in modifiersToRemove)
        {
            RemoveModifier(modifier);
        }
        
        if (enableDebugLog && modifiersToRemove.Count > 0)
        {
            Debug.Log($"PlayerStatsManager: 移除来源 {source?.GetType().Name} 的 {modifiersToRemove.Count} 个修饰器");
        }
    }
    
    /// <summary>
    /// 更新修饰器状态
    /// </summary>
    private void UpdateModifiers()
    {
        // 更新有时间限制的修饰器
        var modifiersToRemove = new List<StatModifier>();
        
        foreach (var modifier in activeModifiers)
        {
            modifier.UpdateTime(Time.deltaTime);
            
            // 只检查时间到期，不检查基于事件的移除条件
            if (modifier.IsTimeExpired())
            {
                modifiersToRemove.Add(modifier);
            }
        }
        
        // 移除过期的修饰器
        foreach (var modifier in modifiersToRemove)
        {
            RemoveModifier(modifier);
        }
    }
    
    /// <summary>
    /// 检查事件相关的修饰器移除
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void CheckEventBasedRemoval(object eventData)
    {
        var modifiersToRemove = new List<StatModifier>();
        
        foreach (var modifier in activeModifiers)
        {
            // 只对相关的事件类型进行移除检查
            if (IsEventRelevantForModifier(eventData, modifier) && modifier.ShouldBeRemoved(eventData))
            {
                modifiersToRemove.Add(modifier);
            }
        }
        
        // 移除满足条件的修饰器
        foreach (var modifier in modifiersToRemove)
        {
            RemoveModifier(modifier);
        }
    }
    
    /// <summary>
    /// 检查事件是否与修饰器相关
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <param name="modifier">修饰器</param>
    /// <returns>是否相关</returns>
    private bool IsEventRelevantForModifier(object eventData, StatModifier modifier)
    {
        // 如果修饰器有移除条件，检查事件类型是否相关
        if (modifier.effectRemovalCondition != null)
        {
            // 根据移除条件类型判断事件相关性
            if (modifier.effectRemovalCondition is DurationEffectRemovalCondition)
            {
                // DurationEffectRemovalCondition 对所有事件都相关（用于时间检查）
                return true;
            }
            else if (modifier.effectRemovalCondition is OnPhaseEndedEffectRemovalCondition)
            {
                // OnPhaseEndedEffectRemovalCondition 只对游戏流程事件有效
                return eventData is GameFlowStateChangedData;
            }
            else if (modifier.effectRemovalCondition is OnConditionMetEffectRemovalCondition)
            {
                // OnConditionMetEffectRemovalCondition 对所有事件都相关
                return true;
            }
            else if (modifier.effectRemovalCondition is InverseConditionCheckEffectRemovalCondition)
            {
                // InverseConditionCheckEffectRemovalCondition 对所有事件都相关（用于反向检查）
                return true;
            }
            // 其他移除条件类型...
        }
        
        // 默认情况下，对所有事件进行移除检查
        return true;
    }
    
    #endregion
    
    #region 最终值计算
    
    
    /// <summary>
    /// 获取最终最大血量
    /// </summary>
    public float FinalMaxHealth
    {
        get { return GetFinalStat("MaxHealth"); }
    }
    
    /// <summary>
    /// 获取最终微调移动速度
    /// </summary>
    public float FinalMicroMoveSpeed
    {
        get { return GetFinalStat("MicroMoveSpeed"); }
    }
    
    /// <summary>
    /// 获取最终攻击力
    /// </summary>
    public float FinalDamage
    {
        get { return GetFinalStat("Damage"); }
    }
    
    /// <summary>
    /// 获取指定属性的最终值
    /// </summary>
    public float GetFinalStat(string statName)
    {
        // 检查缓存
        if (!cacheDirty && cachedFinalValues.TryGetValue(statName, out float cachedValue))
        {
            return cachedValue;
        }
        
        // 计算最终值
        float finalValue = CalculateFinalStat(statName);
        
        // 更新缓存
        if (!cacheDirty)
        {
            cachedFinalValues[statName] = finalValue;
        }
        
        return finalValue;
    }
    
    /// <summary>
    /// 计算指定属性的最终值
    /// </summary>
    private float CalculateFinalStat(string statName)
    {
        // 获取基础值
        float baseValue = GetBaseStat(statName);
        if (baseValue == 0f) return 0f;
        
        // 获取相关修饰器
        var relevantModifiers = activeModifiers.Where(m => m.targetStat == statName).ToList();
        
        float addedValue = 0f;
        float percentAdded = 0f;
        float percentMultiplied = 1f;
        
        foreach (var modifier in relevantModifiers)
        {
            switch (modifier.type)
            {
                case StatModifierType.Add:
                    addedValue += modifier.value;
                    break;
                case StatModifierType.PercentAdd:
                    percentAdded += modifier.value;
                    break;
                case StatModifierType.PercentMult:
                    percentMultiplied *= modifier.value;
                    break;
            }
        }
        
        // 计算最终值：基础值 + 固定值 → 乘以百分比增加 → 乘以百分比乘数
        float finalValue = (baseValue + addedValue) * (1f + percentAdded) * percentMultiplied;
        
        return finalValue;
    }
    
    /// <summary>
    /// 获取基础属性值
    /// </summary>
    public float GetBaseStat(string statName)
    {
        if (playerData == null) return 0f;
        
        switch (statName)
        {
            case "MaxHealth":
                return playerData.baseMaxHealth;
            case "MicroMoveSpeed":
                return playerData.baseMicroMoveSpeed;
            case "Damage":
                // 从 PlayerAttackManager 获取当前攻击模式的攻击力
                PlayerAttackManager attackManager = GetComponent<PlayerAttackManager>();
                return attackManager?.GetBaseAttackDamage() ?? 0f;
            default:
                Debug.LogWarning($"PlayerStatsManager: 未知的基础属性: {statName}");
                return 0f;
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理球停止运动事件
    /// </summary>
    private void HandleBallStopped(BallPhysics ballPhysics)
    {
        // 检查基于事件的移除条件
        CheckEventBasedRemoval(ballPhysics);
    }
    
    /// <summary>
    /// 处理游戏流程状态变化事件
    /// </summary>
    private void HandleGameFlowStateChanged(GameFlowState gameFlowState)
    {
        // 创建游戏流程状态变化数据
        var gameFlowData = new GameFlowStateChangedData { NewState = gameFlowState };
        // 检查基于事件的移除条件
        CheckEventBasedRemoval(gameFlowData);
    }
    
    /// <summary>
    /// 处理血量变化事件
    /// </summary>
    private void HandleHealthChanged(HealthStateData healthData)
    {
        // 检查基于血量变化的移除条件
        CheckEventBasedRemoval(healthData);
    }
    
    /// <summary>
    /// 属性变化时的回调
    /// </summary>
    private void OnStatChanged(string statName)
    {
        cacheDirty = true;
        
        if (enableDebugLog)
        {
            float finalValue = GetFinalStat(statName);
            Debug.Log($"PlayerStatsManager: {statName} 变化，最终值: {finalValue}");
        }
    }
    
    #endregion
    
    #region 调试和监控
    
    /// <summary>
    /// 获取所有活跃修饰器的调试信息
    /// </summary>
    public string GetActiveModifiersDebugInfo()
    {
        if (activeModifiers.Count == 0)
        {
            return "当前没有活跃的修饰器";
        }
        
        var info = $"活跃修饰器数量: {activeModifiers.Count}\n";
        foreach (var modifier in activeModifiers)
        {
            info += $"- {modifier.GetDebugInfo()}\n";
        }
        
        return info;
    }
    
    /// <summary>
    /// 获取所有属性的最终值调试信息
    /// </summary>
    public string GetFinalStatsDebugInfo()
    {
        return $"最终属性值:\n" +
               $"- 攻击力: {FinalDamage}\n" +
               $"- 最大血量: {FinalMaxHealth}\n" +
               $"- 微调移动速度: {FinalMicroMoveSpeed}";
    }
    
    #endregion
}
