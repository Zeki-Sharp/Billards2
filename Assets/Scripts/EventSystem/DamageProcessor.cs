using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 伤害处理器 - 统一处理所有攻击伤害
/// 按优先级顺序调用各个伤害修改器，然后发布处理完成的伤害数据
/// 
/// 【执行顺序】：SYSTEM 层 (-50)
/// 【依赖】：无（自动注册场景中的 IDamageModifier）
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class DamageProcessor : SingletonManager<DamageProcessor>
{
    
    #region 配置
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private bool showProcessingStats = false;
    
    #endregion
    
    #region 私有字段
    
    private List<IDamageModifier> damageModifiers = new List<IDamageModifier>();
    private int totalProcessedCount = 0;
    private int modifiedCount = 0;
    
    #endregion
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;
    protected override bool EnableDebugLog => enableDebugLog;
    
    protected override void OnManagerCreated()
    {
        GameEventBus.OnAttack += ProcessAttackDamage;
    }
    
    protected override void OnManagerDestroyed()
    {
        GameEventBus.OnAttack -= ProcessAttackDamage;
    }
    
    #endregion
    
    void Start()
    {
        RegisterDamageModifiers();
        
        Debug.Log($"[DamageProcessor] 初始化完成（SYSTEM 层），注册了 {damageModifiers.Count} 个伤害修改器");
        
        if (damageModifiers.Count == 0)
        {
            Debug.LogWarning("[DamageProcessor] 警告：没有找到任何伤害修改器！请确保 WeakPointManager 在场景中。");
        }
        else
        {
            Debug.Log($"[DamageProcessor] 已注册的修改器：{string.Join(", ", damageModifiers.Select(m => m.ModifierName))}");
        }
    }
    
    #region 伤害修改器管理
    
    /// <summary>
    /// 自动注册场景中的伤害修改器
    /// </summary>
    private void RegisterDamageModifiers()
    {
        // 查找所有实现 IDamageModifier 的组件
        var modifiers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDamageModifier>();
        
        foreach (var modifier in modifiers)
        {
            RegisterDamageModifier(modifier);
        }
    }
    
    /// <summary>
    /// 注册伤害修改器
    /// </summary>
    /// <param name="modifier">伤害修改器</param>
    public void RegisterDamageModifier(IDamageModifier modifier)
    {
        if (modifier == null)
        {
            Debug.LogWarning("[DamageProcessor] 尝试注册空的伤害修改器");
            return;
        }
        
        if (!damageModifiers.Contains(modifier))
        {
            damageModifiers.Add(modifier);
            
            // 按优先级排序
            damageModifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            
            if (enableDebugLog)
            {
                Debug.Log($"[DamageProcessor] 注册伤害修改器: {modifier.ModifierName} (优先级: {modifier.Priority})");
            }
        }
    }
    
    /// <summary>
    /// 取消注册伤害修改器
    /// </summary>
    /// <param name="modifier">伤害修改器</param>
    public void UnregisterDamageModifier(IDamageModifier modifier)
    {
        if (modifier != null && damageModifiers.Contains(modifier))
        {
            damageModifiers.Remove(modifier);
            
            if (enableDebugLog)
            {
                Debug.Log($"[DamageProcessor] 取消注册伤害修改器: {modifier.ModifierName}");
            }
        }
    }
    
    #endregion
    
    #region 伤害处理
    
    /// <summary>
    /// 处理攻击伤害（公开接口，供 DamageSystem 调用）
    /// </summary>
    /// <param name="attackData">攻击数据（引用传递，会被修改）</param>
    public void ProcessDamage(ref AttackData attackData)
    {
        ProcessAttackDamage(ref attackData);
    }
    
    /// <summary>
    /// 处理攻击伤害（内部实现，支持引用传递）
    /// </summary>
    /// <param name="attackData">攻击数据（引用传递）</param>
    private void ProcessAttackDamage(ref AttackData attackData)
    {
        // 总是显示处理开始信息
        Debug.Log($"[DamageProcessor] 开始处理攻击伤害: {attackData.AttackType}, 原始伤害: {attackData.Damage}, 目标: {attackData.Target?.name}");
        
        // ✅ 从 PlayerStats 读取最终 Damage 属性（包含技能加成等持久修改器）
        if (attackData.Attacker != null && attackData.Attacker.CompareTag("Player"))
        {
            PlayerStats playerStats = attackData.Attacker.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                float baseDamageFromStats = playerStats.GetFinalStat("Damage");
                Debug.Log($"[DamageProcessor] ✅ 从 PlayerStats 读取 Damage: {attackData.Damage} → {baseDamageFromStats} (包含技能加成)");
                attackData.Damage = baseDamageFromStats;
            }
            else
            {
                Debug.LogWarning($"[DamageProcessor] 攻击者没有 PlayerStats 组件，使用原始伤害: {attackData.Damage}");
            }
        }
        
        if (damageModifiers.Count == 0)
        {
            Debug.LogWarning("[DamageProcessor] 没有注册的伤害修改器，直接使用当前伤害");
            return; // 提前返回，避免创建不必要的 ProcessedDamageData
        }
        
        // 按优先级顺序处理伤害修改
        foreach (var modifier in damageModifiers)
        {
            if (!modifier.IsEnabled)
            {
                Debug.Log($"[DamageProcessor] 跳过禁用的修改器: {modifier.ModifierName}");
                continue;
            }
            
            try
            {
                Debug.Log($"[DamageProcessor] 调用修改器: {modifier.ModifierName}");
                bool processed = modifier.ProcessDamage(ref attackData);
                
                if (processed)
                {
                    Debug.Log($"[DamageProcessor] {modifier.ModifierName} 修改伤害: 最终 {attackData.Damage}");
                }
                else
                {
                    Debug.Log($"[DamageProcessor] {modifier.ModifierName} 未处理此攻击");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DamageProcessor] 伤害修改器 {modifier.ModifierName} 处理时发生异常: {ex}");
            }
        }
        
        Debug.Log($"[DamageProcessor] 伤害处理完成，最终伤害: {attackData.Damage}");
    }
    
    /// <summary>
    /// 处理攻击伤害（旧接口，保持兼容）
    /// </summary>
    private void ProcessAttackDamage(AttackData attackData)
    {
        // 临时变量，用于引用传递
        AttackData mutableData = attackData;
        ProcessAttackDamage(ref mutableData);
        
        // 创建处理数据（保持旧流程兼容）
        ProcessedDamageData processedData = new ProcessedDamageData(attackData);
        processedData.FinalDamage = mutableData.Damage;
        
        // 更新统计
        totalProcessedCount++;
        if (processedData.WasModified)
        {
            modifiedCount++;
        }
        
        // 发布处理完成的伤害数据
        GameEventBus.PublishDamageProcessed(processedData);
        
        // 显示处理统计
        if (showProcessingStats)
        {
            ShowProcessingStats();
        }
    }
    
    #endregion
    
    #region 调试和统计
    
    /// <summary>
    /// 显示处理统计信息
    /// </summary>
    private void ShowProcessingStats()
    {
        float modificationRate = totalProcessedCount > 0 ? (float)modifiedCount / totalProcessedCount * 100f : 0f;
        
        Debug.Log($"[DamageProcessor] 处理统计 - 总计: {totalProcessedCount}, 修改: {modifiedCount}, 修改率: {modificationRate:F1}%");
    }
    
    /// <summary>
    /// 获取注册的修改器信息
    /// </summary>
    /// <returns>修改器信息字符串</returns>
    public string GetModifierInfo()
    {
        if (damageModifiers.Count == 0)
        {
            return "未注册任何伤害修改器";
        }
        
        string info = "已注册的伤害修改器:\n";
        foreach (var modifier in damageModifiers)
        {
            string status = modifier.IsEnabled ? "启用" : "禁用";
            info += $"  - {modifier.ModifierName} (优先级: {modifier.Priority}, 状态: {status})\n";
        }
        
        return info;
    }
    
    #endregion
}
