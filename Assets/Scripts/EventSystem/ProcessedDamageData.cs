using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 处理完成的伤害数据
/// 包含原始攻击数据和最终处理结果
/// </summary>
public struct ProcessedDamageData
{
    /// <summary>
    /// 原始攻击数据
    /// </summary>
    public AttackData OriginalData;
    
    /// <summary>
    /// 最终伤害值
    /// </summary>
    public float FinalDamage;
    
    /// <summary>
    /// 应用的修改器列表（按执行顺序）
    /// </summary>
    public List<string> AppliedModifiers;
    
    /// <summary>
    /// 处理时间戳
    /// </summary>
    public float ProcessingTime;
    
    /// <summary>
    /// 是否被修改过
    /// </summary>
    public bool WasModified;
    
    /// <summary>
    /// 伤害修改倍数
    /// </summary>
    public float DamageMultiplier;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="originalData">原始攻击数据</param>
    public ProcessedDamageData(AttackData originalData)
    {
        OriginalData = originalData;
        FinalDamage = originalData.Damage;
        AppliedModifiers = new List<string>();
        ProcessingTime = Time.time;
        WasModified = false;
        DamageMultiplier = 1.0f;
    }
    
    /// <summary>
    /// 添加修改器记录
    /// </summary>
    /// <param name="modifierName">修改器名称</param>
    /// <param name="damageMultiplier">伤害倍数</param>
    public void AddModifier(string modifierName, float damageMultiplier = 1.0f)
    {
        AppliedModifiers.Add(modifierName);
        WasModified = true;
        DamageMultiplier *= damageMultiplier;
        FinalDamage *= damageMultiplier;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试字符串</returns>
    public string GetDebugInfo()
    {
        string modifiers = AppliedModifiers.Count > 0 ? string.Join(", ", AppliedModifiers) : "无";
        return $"伤害: {OriginalData.Damage} → {FinalDamage} (倍数: {DamageMultiplier:F2}), 修改器: {modifiers}";
    }
}
