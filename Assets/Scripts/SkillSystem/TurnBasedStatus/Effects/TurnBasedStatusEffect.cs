using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合制状态效果 - 对目标施加回合制状态
/// 
/// 【核心逻辑】：
/// - 从技能参数中获取目标
/// - 检查目标是否已有对应状态
/// - 如果没有：添加组件并初始化
/// - 如果有：叠加回合数
/// 
/// 【使用场景】：
/// - 配合 DamageTrigger 使用，在造成伤害时施加状态
/// - 可配置状态数据和状态类型
/// 
/// 【简化版】：
/// - 目前只支持 BurningStatus
/// - 后续可扩展为通用的状态施加器
/// </summary>
[System.Serializable]
public class TurnBasedStatusEffect : IEffect
{
    public string EffectName => "TurnBasedStatusEffect";
    
    [BoxGroup("状态配置")]
    [LabelText("状态数据")]
    [Tooltip("要施加的状态配置")]
    [Required]
    public TurnBasedStatusData statusData;
    
    [BoxGroup("调试")]
    [LabelText("显示日志")]
    public bool showDebugLog = true;
    
    // 执行权限（由重置条件控制）
    private bool canExecute = true;
    public bool CanExecute => canExecute;
    
    // 目标角色ID（保留但点燃不使用）
    private string targetCharacterID;
    
    #region IEffect 接口实现
    
    public void Initialize()
    {
        // 验证配置
        if (statusData == null)
        {
            Debug.LogError($"[{EffectName}] statusData 未配置！");
        }
    }
    
    public void SetCanExecute(bool value)
    {
        canExecute = value;
    }
    
    public void SetTarget(string characterID)
    {
        targetCharacterID = characterID;
    }
    
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
        if (statusData == null || !statusData.IsValid())
        {
            Debug.LogError($"[{EffectName}] 状态数据无效或未配置！");
            return false;
        }
        
        // 从 args 中获取目标
        GameObject target = null;
        GameObject sourceObj = null;
        
        // 尝试从 DamageEvent 获取目标
        if (args.TryGetEventData<DamageEvent>(out var damageEvent))
        {
            target = damageEvent.Target;
            sourceObj = damageEvent.Source;
            
            // ✅ 关键修复：确保目标有 IDamageable 组件
            // 如果 Target 是子对象（如 enemyItem），尝试从父级查找
            if (target != null && target.GetComponent<IDamageable>() == null)
            {
                var parentDamageable = target.GetComponentInParent<IDamageable>();
                if (parentDamageable != null)
                {
                    // 找到了父级的 IDamageable，将状态添加到父级对象
                    target = (parentDamageable as MonoBehaviour)?.gameObject;
                }
            }
        }
        
        if (target == null)
        {
            Debug.LogWarning($"[{EffectName}] 无法获取目标，状态效果无法施加");
            return false;
        }
        
        ApplyConfiguredStatus(target, sourceObj);
        
        return true;
    }
    
    public void RemoveEffect()
    {
        // 状态效果由组件自己管理生命周期，不需要手动移除
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 根据配置施加具体状态
    /// </summary>
    void ApplyConfiguredStatus(GameObject target, GameObject source)
    {
        var componentType = statusData.GetComponentType();

        if (componentType == null || !typeof(TurnBasedStatusComponent).IsAssignableFrom(componentType))
        {
            Debug.LogError($"[{EffectName}] ❌ 状态配置返回的组件类型无效: {componentType}");
            return;
        }

        var existingStatus = target.GetComponent(componentType) as TurnBasedStatusComponent;

        if (existingStatus == null)
        {
            var newStatusComponent = target.AddComponent(componentType) as TurnBasedStatusComponent;
            if (newStatusComponent == null)
            {
                Debug.LogError($"[{EffectName}] ❌ 无法在 {target.name} 上创建状态组件 {componentType.Name}");
                return;
            }

            newStatusComponent.Initialize(statusData, source, showDebugLog);

            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] ✅ 对 {target.name} 施加{statusData.displayName}");
            }
        }
        else
        {
            existingStatus.ReapplyStatus();

            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] ✅ 对 {target.name} 叠加{statusData.displayName}");
            }
        }
    }
    
    #endregion
}

