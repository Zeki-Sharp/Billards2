using UnityEngine;

/// <summary>
/// 伤害增加模式枚举
/// </summary>
public enum DamageIncreaseType
{
    /// <summary>百分比加成（伤害 × 倍率）</summary>
    Percentage,
    
    /// <summary>固定值加成（伤害 + 固定值）</summary>
    Fixed
}

/// <summary>
/// 状态条件伤害修改器 - 检测目标状态并增加伤害
/// 
/// 【核心职责】：
/// - 在伤害计算流程中检测目标是否有指定状态
/// - 如果目标有状态且激活，则增加伤害
/// - 支持百分比和固定值两种增伤模式
/// 
/// 【使用场景】：
/// - 技能系统通过 RegisterDamageModifierEffect 动态创建此组件
/// - 技能激活时注册到 DamageProcessor
/// - 技能失效时从 DamageProcessor 注销
/// 
/// 【设计特点】：
/// - 通用化：通过配置 targetStatusData 支持任意状态检测
/// - 可配置：支持百分比/固定值两种增伤模式
/// - 可叠加：多个状态增伤技能可乘法叠加
/// 
/// 【技术说明】：
/// - 由技能系统管理生命周期（创建/销毁）
/// - 在伤害系统中执行（正确的时机）
/// - 实现 IDamageModifier 接口，集成到 DamageProcessor
/// </summary>
public class StatusConditionalDamageModifier : MonoBehaviour, IDamageModifier
{
    // 技能归属角色ID（用于多角色过滤）
    private string ownerCharacterID;
    #region IDamageModifier 接口实现
    
    /// <summary>
    /// 修改器优先级 - 普通优先级，在弱点系统（High）之后执行
    /// </summary>
    public EventPriority Priority => EventPriority.Normal;
    
    /// <summary>
    /// 修改器名称（用于调试和日志）
    /// </summary>
    public string ModifierName => $"状态增伤-{(targetStatusData != null ? targetStatusData.displayName : "未配置")}";
    
    /// <summary>
    /// 是否启用此修改器
    /// </summary>
    public bool IsEnabled => enabled && targetStatusData != null;
    
    /// <summary>
    /// 处理伤害修改 - 检测目标状态并增加伤害
    /// </summary>
    /// <param name="attackData">攻击数据（可修改）</param>
    /// <returns>是否成功处理了伤害修改</returns>
    public bool ProcessDamage(ref AttackData attackData)
    {
        // 验证配置
        if (targetStatusData == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("[StatusConditionalDamageModifier] targetStatusData 未配置，跳过");
            }
            return false;
        }
        
        // 验证攻击数据
        if (attackData.Target == null)
        {
            return false;
        }
        
        // ✅ 多角色系统：只对归属角色的攻击生效
        if (!string.IsNullOrEmpty(ownerCharacterID))
        {
            if (!TriggerHelper.IsOwner(attackData.Attacker, ownerCharacterID))
            {
                return false;
            }
        }
        
        // 检查目标是否有指定状态
        if (!CheckTargetHasStatus(attackData.Target))
        {
            return false;
        }
        
        // 应用伤害增加
        float originalDamage = attackData.Damage;
        ApplyDamageIncrease(ref attackData.Damage);
        
        if (showDebugLog)
        {
            string increaseDesc = increaseType == DamageIncreaseType.Percentage 
                ? $"×{damageMultiplier}" 
                : $"+{fixedDamageBonus}";
            Debug.Log($"[{ModifierName}] 目标有 {targetStatusData.displayName} 状态，伤害提升: {originalDamage:F1} → {attackData.Damage:F1} ({increaseDesc})");
        }
        
        return true;
    }
    
    #endregion
    
    #region 配置字段
    
    [Header("状态检测配置")]
    [Tooltip("要检测的状态数据（拖拽 TurnBasedStatusData SO）")]
    [SerializeField] private TurnBasedStatusData targetStatusData;
    
    [Header("伤害增加配置")]
    [Tooltip("伤害增加模式：百分比 or 固定值")]
    [SerializeField] private DamageIncreaseType increaseType = DamageIncreaseType.Percentage;
    
    [Tooltip("伤害倍率（百分比模式，如 1.5 = +50%）")]
    [SerializeField] private float damageMultiplier = 1.5f;
    
    [Tooltip("固定伤害加成（固定值模式，如 +10）")]
    [SerializeField] private float fixedDamageBonus = 10f;
    
    [Header("调试")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLog = false;
    
    #endregion
    
    #region 公共配置方法（由技能效果调用）
    
    /// <summary>
    /// 设置状态检测目标
    /// </summary>
    public void SetTargetStatus(TurnBasedStatusData statusData)
    {
        targetStatusData = statusData;
    }
    
    /// <summary>
    /// 设置增伤模式
    /// </summary>
    public void SetIncreaseType(DamageIncreaseType type)
    {
        increaseType = type;
    }
    
    /// <summary>
    /// 设置伤害倍率（百分比模式）
    /// </summary>
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }
    
    /// <summary>
    /// 设置固定伤害加成（固定值模式）
    /// </summary>
    public void SetFixedDamageBonus(float bonus)
    {
        fixedDamageBonus = bonus;
    }
    
    /// <summary>
    /// 设置调试日志开关
    /// </summary>
    public void SetDebugLog(bool enable)
    {
        showDebugLog = enable;
    }
    
    /// <summary>
    /// 一次性配置所有参数（推荐使用）
    /// </summary>
    public void Configure(
        TurnBasedStatusData statusData, 
        DamageIncreaseType type, 
        float multiplier, 
        float fixedBonus,
        string ownerId = null,
        bool debugLog = false)
    {
        targetStatusData = statusData;
        increaseType = type;
        damageMultiplier = multiplier;
        fixedDamageBonus = fixedBonus;
        ownerCharacterID = ownerId;
        showDebugLog = debugLog;
        
        if (showDebugLog)
        {
            Debug.Log($"[StatusConditionalDamageModifier] 配置完成 - 状态:{statusData?.displayName}, 模式:{type}, 倍率:{multiplier}, 固定值:{fixedBonus}");
        }
    }
    
    /// <summary>
    /// 设置技能归属角色ID
    /// </summary>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
    }

    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 检查目标是否有指定的状态
    /// </summary>
    /// <param name="target">要检查的目标</param>
    /// <returns>是否有匹配且激活的状态</returns>
    private bool CheckTargetHasStatus(GameObject target)
    {
        if (target == null || targetStatusData == null)
        {
            return false;
        }
        
        // ✅ 修复：Enemy 层级问题
        // 伤害目标可能是 enemyItem（子对象），但状态挂在 Enemy 根物体上
        // 需要向上查找 IDamageable 来定位根物体
        
        GameObject rootObject = target;
        
        // 如果目标本身没有 IDamageable，尝试从父级查找
        var damageable = target.GetComponent<IDamageable>();
        if (damageable == null)
        {
            damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                rootObject = (damageable as MonoBehaviour)?.gameObject;
            }
        }
        
        if (rootObject == null)
        {
            rootObject = target; // 兜底：使用原始目标
        }
        
        // 获取根对象身上的所有状态组件
        var statusComponents = rootObject.GetComponents<TurnBasedStatusComponent>();
        
        if (statusComponents == null || statusComponents.Length == 0)
        {
            return false;
        }
        
        // 遍历检查是否有匹配的状态
        foreach (var component in statusComponents)
        {
            // 检查状态数据是否匹配
            if (component.StatusData == targetStatusData)
            {
                // 检查状态是否激活（剩余回合数 > 0）
                if (component.RemainingTurns > 0)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 应用伤害增加（根据配置的模式）
    /// </summary>
    /// <param name="damage">当前伤害值（引用传递，会被修改）</param>
    private void ApplyDamageIncrease(ref float damage)
    {
        if (increaseType == DamageIncreaseType.Percentage)
        {
            // 百分比模式：伤害 × 倍率
            damage *= damageMultiplier;
        }
        else // DamageIncreaseType.Fixed
        {
            // 固定值模式：伤害 + 固定值
            damage += fixedDamageBonus;
        }
    }
    
    #endregion
    
    #region Unity 生命周期
    
    private void OnValidate()
    {
        // Inspector 中验证配置
        if (targetStatusData == null)
        {
            Debug.LogWarning("StatusConditionalDamageModifier: 未设置 targetStatusData，请配置要检测的状态");
        }
        
        // 验证数值合理性
        if (increaseType == DamageIncreaseType.Percentage && damageMultiplier <= 0f)
        {
            Debug.LogWarning("StatusConditionalDamageModifier: 百分比模式下 damageMultiplier 应该 > 0");
        }
        
        if (increaseType == DamageIncreaseType.Fixed && fixedDamageBonus <= 0f)
        {
            Debug.LogWarning("StatusConditionalDamageModifier: 固定值模式下 fixedDamageBonus 应该 > 0");
        }
    }
    
    #endregion
}

