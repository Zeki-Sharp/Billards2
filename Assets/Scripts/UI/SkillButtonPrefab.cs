using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 技能选择按钮 - 单个技能按钮的UI组件
/// 
/// 【核心职责】：
/// - 显示技能名称、描述、分配角色
/// - 处理按钮点击事件
/// - 可复用的UI组件
/// </summary>
public class SkillButtonPrefab : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] private Button skillButton;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TextMeshProUGUI characterAssignmentText; // 分配角色文本
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 当前技能数据
    private SkillConfig currentSkill;
    private int skillIndex;
    private System.Action<int> onButtonClicked;
    
    /// <summary>
    /// 初始化技能按钮
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <param name="index">技能索引</param>
    /// <param name="option">技能选项（包含角色分配信息）</param>
    /// <param name="onClick">点击回调</param>
    public void Initialize(SkillConfig skill, int index, SkillSelectionOption option, System.Action<int> onClick)
    {
        currentSkill = skill;
        skillIndex = index;
        onButtonClicked = onClick;
        
        // 设置按钮点击事件
        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(() => onButtonClicked?.Invoke(skillIndex));
        }
        else
        {
            Debug.LogWarning("[SkillButtonPrefab] skillButton 为 null！");
        }
        
        // 更新UI显示
        UpdateDisplay(skill, option);
        
        if (showDebugInfo)
        {
            Debug.Log($"[SkillButtonPrefab] 初始化完成 - {skill?.skillName} (索引: {index})");
        }
    }
    
    /// <summary>
    /// 更新技能显示
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <param name="option">技能选项（包含角色分配信息）</param>
    void UpdateDisplay(SkillConfig skill, SkillSelectionOption option)
    {
        if (skill == null)
        {
            Debug.LogError("[SkillButtonPrefab] UpdateDisplay: skill 为 null！");
            Clear();
            return;
        }
        
        // 更新技能名称
        if (skillNameText != null)
        {
            string skillName = skill.skillName;
            
            // 添加等级标识（如果有多个等级）
            if (option != null)
            {
                int maxLevel = skill.GetMaxLevel();
                if (maxLevel > 1)
                {
                    skillName = $"{skillName} lv.{option.targetLevel}";
                }
            }
            
            skillNameText.text = skillName;
        }
        else
        {
            Debug.LogWarning("[SkillButtonPrefab] skillNameText 为 null！");
        }
        
        // 更新技能描述
        if (skillDescriptionText != null)
        {
            string description = "";
            
            if (option != null)
            {
                description = skill.GetDynamicDescription(option.targetLevel);
            }
            
            if (string.IsNullOrEmpty(description))
            {
                description = skill.GetDynamicDescription(1);
            }
            
            if (string.IsNullOrEmpty(description))
            {
                description = skill.description;
            }
            
            if (string.IsNullOrEmpty(description))
            {
                description = "暂无描述";
            }
            
            skillDescriptionText.text = description;
        }
        else
        {
            Debug.LogWarning("[SkillButtonPrefab] skillDescriptionText 为 null！");
        }
        
        // 更新角色分配文本
        if (characterAssignmentText != null)
        {
            if (option != null && !string.IsNullOrEmpty(option.characterName))
            {
                string assignmentText = $"{option.characterName}技能";
                characterAssignmentText.text = assignmentText;
            }
            else
            {
                characterAssignmentText.text = "";
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[SkillButtonPrefab] option 为 null 或 characterName 为空，清空角色分配文本");
                }
            }
        }
        else
        {
            Debug.LogError("[SkillButtonPrefab] characterAssignmentText 为 null！请检查 Inspector 配置！");
        }
    }
    
    /// <summary>
    /// 清除显示
    /// </summary>
    public void Clear()
    {
        if (skillNameText != null) skillNameText.text = "";
        if (skillDescriptionText != null) skillDescriptionText.text = "";
        if (characterAssignmentText != null) characterAssignmentText.text = "";
    }
    
    /// <summary>
    /// 设置按钮是否可交互
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (skillButton != null)
        {
            skillButton.interactable = interactable;
        }
    }
}

