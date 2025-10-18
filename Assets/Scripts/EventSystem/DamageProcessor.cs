using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 伤害处理器 - 统一处理所有攻击伤害
/// 按优先级顺序调用各个伤害修改器，然后发布处理完成的伤害数据
/// </summary>
public class DamageProcessor : MonoBehaviour
{
    public static DamageProcessor Instance { get; private set; }
    
    #region 配置
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private bool showProcessingStats = false;
    
    #endregion
    
    #region 私有字段
    
    /// <summary>
    /// 注册的伤害修改器列表
    /// </summary>
    private List<IDamageModifier> damageModifiers = new List<IDamageModifier>();
    
    /// <summary>
    /// 处理统计信息
    /// </summary>
    private int totalProcessedCount = 0;
    private int modifiedCount = 0;
    
    #endregion
    
    #region Unity 生命周期
    
    void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            if (enableDebugLog)
                Debug.Log("[DamageProcessor] 单例创建成功");
        }
        else
        {
            Debug.LogWarning("[DamageProcessor] 检测到重复实例，销毁");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 延迟一帧查找，确保所有 Awake 都执行完毕
        StartCoroutine(DelayedRegistration());
        
        // 订阅攻击事件
        GameEventBus.OnAttack += ProcessAttackDamage;
    }
    
    /// <summary>
    /// 延迟注册伤害修改器
    /// </summary>
    System.Collections.IEnumerator DelayedRegistration()
    {
        yield return null; // 等待一帧，确保所有 Awake 都执行完毕
        
        // 自动注册场景中的伤害修改器
        RegisterDamageModifiers();
        
        // 总是显示初始化信息，帮助调试
        Debug.Log($"[DamageProcessor] 初始化完成，注册了 {damageModifiers.Count} 个伤害修改器");
        
        if (damageModifiers.Count == 0)
        {
            Debug.LogWarning("[DamageProcessor] 警告：没有找到任何伤害修改器！请确保 WeakPointManager 在场景中。");
        }
        else
        {
            Debug.Log($"[DamageProcessor] 已注册的修改器：{string.Join(", ", damageModifiers.Select(m => m.ModifierName))}");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅攻击事件
        GameEventBus.OnAttack -= ProcessAttackDamage;
    }
    
    #endregion
    
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
    /// 处理攻击伤害
    /// </summary>
    /// <param name="attackData">攻击数据</param>
    private void ProcessAttackDamage(AttackData attackData)
    {
        // 总是显示处理开始信息
        Debug.Log($"[DamageProcessor] 开始处理攻击伤害: {attackData.AttackType}, 原始伤害: {attackData.Damage}, 目标: {attackData.Target?.name}");
        
        if (damageModifiers.Count == 0)
        {
            Debug.LogWarning("[DamageProcessor] 没有注册的伤害修改器，直接发布原始伤害");
        }
        
        // 创建处理数据
        ProcessedDamageData processedData = new ProcessedDamageData(attackData);
        
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
                // 创建攻击数据的副本用于修改
                AttackData modifiedData = attackData;
                
                Debug.Log($"[DamageProcessor] 调用修改器: {modifier.ModifierName}");
                bool processed = modifier.ProcessDamage(ref modifiedData);
                
                if (processed)
                {
                    // 更新处理数据
                    processedData.AddModifier(modifier.ModifierName);
                    attackData = modifiedData; // 更新原始数据
                    
                    Debug.Log($"[DamageProcessor] {modifier.ModifierName} 修改伤害: {processedData.OriginalData.Damage} → {modifiedData.Damage}");
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
        
        // 更新最终伤害
        processedData.FinalDamage = attackData.Damage;
        
        // 更新统计
        totalProcessedCount++;
        if (processedData.WasModified)
        {
            modifiedCount++;
        }
        
        // 发布处理完成的伤害数据
        GameEventBus.PublishDamageProcessed(processedData);
        
        Debug.Log($"[DamageProcessor] 伤害处理完成: {processedData.GetDebugInfo()}");
        
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
