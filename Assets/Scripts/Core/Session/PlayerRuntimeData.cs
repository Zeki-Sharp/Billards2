using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家运行时数据快照
/// 用于跨场景保存和恢复玩家的运行时状态
/// </summary>
[System.Serializable]
public class PlayerRuntimeData
{
    #region Attributes 层数据
    
    /// <summary>
    /// 属性当前值快照（如：血量、能量等）
    /// Key: 属性ID (如 "Health")
    /// Value: 当前值
    /// </summary>
    public Dictionary<string, float> attributeCurrentValues = new Dictionary<string, float>();
    
    #endregion
    
    #region Stats 层数据
    
    /// <summary>
    /// 激活的修改器快照
    /// 存储所有需要跨场景保留的修改器
    /// </summary>
    public List<ModifierSnapshot> activeModifiers = new List<ModifierSnapshot>();
    
    #endregion
    
    #region StatusEffects 层数据
    
    /// <summary>
    /// 激活的状态效果快照
    /// 存储所有需要跨场景保留的状态效果
    /// </summary>
    public List<StatusEffectSnapshot> activeStatusEffects = new List<StatusEffectSnapshot>();
    
    #endregion
    
    #region 多角色队伍数据
    
    /// <summary>
    /// 队伍数据（多角色控制系统）
    /// 管理3个角色的运行时状态
    /// </summary>
    public TeamData teamData = null;
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 重置所有数据
    /// </summary>
    public void Clear()
    {
        attributeCurrentValues.Clear();
        activeModifiers.Clear();
        activeStatusEffects.Clear();
        
        // 清除队伍数据
        if (teamData != null)
        {
            teamData.Clear();
            teamData = null;
        }
    }
    
    /// <summary>
    /// 检查是否有数据
    /// </summary>
    public bool HasData()
    {
        return attributeCurrentValues.Count > 0 || 
               activeModifiers.Count > 0 || 
               activeStatusEffects.Count > 0 ||
               (teamData != null && teamData.IsValid());
    }
    
    /// <summary>
    /// 检查是否有队伍数据
    /// </summary>
    public bool HasTeamData()
    {
        return teamData != null && teamData.IsValid();
    }
    
    /// <summary>
    /// 获取队伍数据（如果不存在则创建）
    /// </summary>
    public TeamData GetOrCreateTeamData()
    {
        if (teamData == null)
        {
            teamData = new TeamData();
        }
        return teamData;
    }
    
    #endregion
}

/// <summary>
/// 修改器快照
/// </summary>
[System.Serializable]
public struct ModifierSnapshot
{
    public string statID;           // 属性ID
    public float value;             // 修改值
    public bool isPercentage;       // 是否是百分比
    public string sourceID;         // 来源ID（用于重建）
}

/// <summary>
/// 状态效果快照
/// </summary>
[System.Serializable]
public struct StatusEffectSnapshot
{
    public string effectID;         // 效果ID
    public float remainingDuration; // 剩余持续时间
    public int stackCount;          // 堆叠层数
    public string sourceID;         // 来源ID
}

