using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能状态管理器 - 跟踪当前激活的技能
/// 用于掉落系统等需要知道技能状态的场景
/// </summary>
public class SkillStateManager : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    /// <summary>
    /// 当前激活的技能名称集合
    /// </summary>
    private HashSet<string> activeSkills = new HashSet<string>();
    
    /// <summary>
    /// 获取当前激活的技能名称集合
    /// </summary>
    public HashSet<string> GetActiveSkills()
    {
        return new HashSet<string>(activeSkills);
    }
    
    /// <summary>
    /// 检查指定技能是否激活
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <returns>是否激活</returns>
    public bool IsSkillActive(string skillName)
    {
        return activeSkills.Contains(skillName);
    }
    
    /// <summary>
    /// 添加激活的技能
    /// </summary>
    /// <param name="skillName">技能名称</param>
    public void AddActiveSkill(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            Debug.LogWarning("[SkillStateManager] 尝试添加空的技能名称");
            return;
        }
        
        activeSkills.Add(skillName);
        
        if (enableDebugLog)
        {
            Debug.Log($"[SkillStateManager] ✅ 技能激活: {skillName} (总计: {activeSkills.Count})");
        }
        
        // 发布技能激活事件
        GameEventBus.PublishSkillActivated(skillName);
    }
    
    /// <summary>
    /// 移除激活的技能
    /// </summary>
    /// <param name="skillName">技能名称</param>
    public void RemoveActiveSkill(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            Debug.LogWarning("[SkillStateManager] 尝试移除空的技能名称");
            return;
        }
        
        if (activeSkills.Remove(skillName))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SkillStateManager] ❌ 技能失效: {skillName} (剩余: {activeSkills.Count})");
            }
            
            // 发布技能失效事件
            GameEventBus.PublishSkillDeactivated(skillName);
        }
    }
    
    /// <summary>
    /// 清空所有激活的技能
    /// </summary>
    public void ClearActiveSkills()
    {
        var skillsToRemove = new List<string>(activeSkills);
        foreach (var skillName in skillsToRemove)
        {
            RemoveActiveSkill(skillName);
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[SkillStateManager] 🧹 清空所有激活技能");
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        if (activeSkills.Count == 0)
        {
            return "当前无激活技能";
        }
        
        return $"激活技能 ({activeSkills.Count}): {string.Join(", ", activeSkills)}";
    }
    
    /// <summary>
    /// 在Inspector中显示当前状态
    /// </summary>
    [ContextMenu("显示当前技能状态")]
    public void ShowCurrentState()
    {
        Debug.Log($"[SkillStateManager] {GetDebugInfo()}");
    }
}
