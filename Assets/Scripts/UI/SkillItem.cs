using UnityEngine;
using TMPro;

/// <summary>
/// 技能项 - 显示单个技能的信息
/// 
/// 【核心职责】：
/// - 显示技能名称、等级、描述
/// - 可复用的UI组件
/// </summary>
public class SkillItem : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 设置技能数据
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <param name="level">技能等级</param>
    /// <param name="description">技能描述</param>
    public void SetSkillData(string skillName, int level, string description)
    {
        // 设置技能名称
        if (skillNameText != null)
        {
            skillNameText.text = skillName;
        }
        else
        {
            Debug.LogWarning("SkillItem: 技能名称文本未配置！");
        }
        
        // 设置技能等级
        if (skillLevelText != null)
        {
            skillLevelText.text = $"Lv.{level}";
        }
        else
        {
            Debug.LogWarning("SkillItem: 技能等级文本未配置！");
        }
        
        // 设置技能描述
        if (skillDescriptionText != null)
        {
            skillDescriptionText.text = description;
        }
        else
        {
            Debug.LogWarning("SkillItem: 技能描述文本未配置！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillItem: 设置技能数据 - {skillName} Lv.{level}");
        }
    }
    
    /// <summary>
    /// 从SkillInstance设置数据
    /// </summary>
    /// <param name="skillInstance">技能实例</param>
    public void SetSkillData(SkillInstance skillInstance)
    {
        if (skillInstance == null || skillInstance.config == null)
        {
            Debug.LogError("SkillItem: 技能实例或配置为空！");
            return;
        }
        
        SetSkillData(
            skillInstance.config.skillName,
            skillInstance.currentLevel,
            skillInstance.config.description
        );
    }
    
    /// <summary>
    /// 清除显示
    /// </summary>
    public void Clear()
    {
        if (skillNameText != null) skillNameText.text = "";
        if (skillLevelText != null) skillLevelText.text = "";
        if (skillDescriptionText != null) skillDescriptionText.text = "";
    }
}

