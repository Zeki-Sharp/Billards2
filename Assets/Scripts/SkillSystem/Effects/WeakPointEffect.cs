using UnityEngine;

/// <summary>
/// 弱点攻击效果 - 为所有敌人添加弱点标记
/// 类似英雄联盟剑姬的弱点机制
/// </summary>
public class WeakPointEffect : IEffect
{
    public string EffectName => "WeakPointEffect";
    
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
    
    // 配置参数（从 SkillEffectConfig 传入）
    private GameObject weakPointMarkerPrefab;
    private float radius;
    private float damageMultiplier;
    private bool refreshOnHit;
    
    // 运行时管理器
    private WeakPointManager manager;
    
    /// <summary>
    /// 设置弱点参数
    /// </summary>
    public void SetParameters(
        GameObject prefab, 
        float radius, 
        float damageMultiplier,
        bool refreshOnHit)
    {
        this.weakPointMarkerPrefab = prefab;
        this.radius = radius;
        this.damageMultiplier = damageMultiplier;
        this.refreshOnHit = refreshOnHit;
        
        Debug.Log($"[{EffectName}] 设置参数 - 半径: {radius}, 倍率: {damageMultiplier}x, 击中刷新: {refreshOnHit}");
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{EffectName}] 初始化弱点系统");
        
        // 验证预制体
        if (weakPointMarkerPrefab == null)
        {
            Debug.LogError($"[{EffectName}] 弱点标记预制体未设置！请在 SkillConfig 中配置 weakPointMarkerPrefab");
            return;
        }
        
        // 创建或获取管理器（单例模式）
        manager = WeakPointManager.GetOrCreateInstance();
        
        if (manager == null)
        {
            Debug.LogError($"[{EffectName}] 无法创建 WeakPointManager！");
            return;
        }
        
        // 配置管理器参数
        manager.Configure(
            weakPointMarkerPrefab,
            radius,
            damageMultiplier,
            refreshOnHit
        );
        
        // 启动管理器（会自动为现有敌人添加弱点）
        manager.Enable();
        
        Debug.Log($"[{EffectName}] ✅ 弱点系统初始化成功");
    }
    
    /// <summary>
    /// 执行效果（弱点效果是持续性的，不需要每次执行）
    /// </summary>
    public bool ExecuteEffect(object eventData)
    {
        // 检查执行权限（完全由重置条件控制）
        if (!canExecute)
        {
            Debug.Log($"[{EffectName}] 执行权限被禁止，跳过执行");
            return false;
        }
        
        // 弱点效果在 Initialize 时启动，持续到 Reset
        // 无需在此方法中处理
        
        // 执行成功后，禁止再次执行（由重置条件重新允许）
        canExecute = false;
        
        return true;
    }
    
    /// <summary>
    /// 重置效果（技能被移除时调用）
    /// </summary>
    public void RemoveEffect()
    {
        Debug.Log($"[{EffectName}] 清理弱点系统");
        
        // 禁用并清理管理器
        if (manager != null)
        {
            manager.Disable();
            Object.Destroy(manager.gameObject);
            manager = null;
        }
        
        Debug.Log($"[{EffectName}] ✅ 弱点系统已清理");
    }
}

