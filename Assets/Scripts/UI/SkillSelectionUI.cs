using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 技能选择界面 - 简化版
/// 
/// 【核心职责】：
/// - 显示3个技能按钮
/// - 显示3个技能名称
/// - 处理技能选择点击事件
/// - 与 SkillSelectionManager 交互
/// </summary>
public class SkillSelectionUI : MonoBehaviour
{
    [Header("UI面板")]
    [SerializeField] private GameObject skillSelectionPanel; // 技能选择面板
    
    [Header("技能按钮")]
    [SerializeField] private Button skillButton1; // 技能按钮1
    [SerializeField] private Button skillButton2; // 技能按钮2
    [SerializeField] private Button skillButton3; // 技能按钮3
    
    [Header("技能名称")]
    [SerializeField] private TextMeshProUGUI skillName1; // 技能名称1
    [SerializeField] private TextMeshProUGUI skillName2; // 技能名称2
    [SerializeField] private TextMeshProUGUI skillName3; // 技能名称3
    
    [Header("技能描述")]
    [SerializeField] private TextMeshProUGUI skillDescription1; // 技能描述1
    [SerializeField] private TextMeshProUGUI skillDescription2; // 技能描述2
    [SerializeField] private TextMeshProUGUI skillDescription3; // 技能描述3
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private SkillSelectionManager skillSelectionManager;
    
    // 当前显示的技能列表
    private List<SkillConfig> currentSkills = new List<SkillConfig>();
    
    // UI状态
    private bool isUIActive = false;
    
    void Start()
    {
        InitializeUI();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    void InitializeUI()
    {
        // 获取组件引用 - 使用单例
        skillSelectionManager = SkillSelectionManager.Instance;
        
        if (skillSelectionManager == null)
        {
            Debug.LogWarning("SkillSelectionUI: SkillSelectionManager.Instance 为空，将在需要时重新查找");
        }
        
        // 设置初始状态
        HideUI();
        
        // 订阅事件
        SubscribeToEvents();
        
        // 设置按钮事件
        SetupButtonEvents();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 初始化完成");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnSkillSelectionStarted += OnSkillSelectionStarted;
        GameEventBus.OnSkillSelectionCompleted += OnSkillSelectionCompleted;
    }
    
    /// <summary>
    /// 取消订阅事件
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnSkillSelectionStarted -= OnSkillSelectionStarted;
        GameEventBus.OnSkillSelectionCompleted -= OnSkillSelectionCompleted;
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtonEvents()
    {
        skillButton1?.onClick.AddListener(() => OnSkillButtonClicked(0));
        skillButton2?.onClick.AddListener(() => OnSkillButtonClicked(1));
        skillButton3?.onClick.AddListener(() => OnSkillButtonClicked(2));
    }
    
    /// <summary>
    /// 技能选择开始事件处理
    /// </summary>
    /// <param name="availableSkills">可选择的技能列表</param>
    void OnSkillSelectionStarted(List<SkillConfig> availableSkills)
    {
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionUI: 收到技能选择开始事件，技能数量: {availableSkills.Count}");
        }
        
        ShowSkillSelection(availableSkills);
    }
    
    /// <summary>
    /// 技能选择完成事件处理
    /// </summary>
    void OnSkillSelectionCompleted()
    {
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 收到技能选择完成事件");
        }
        
        HideUI();
    }
    
    /// <summary>
    /// 显示技能选择界面
    /// </summary>
    /// <param name="availableSkills">可选择的技能列表</param>
    void ShowSkillSelection(List<SkillConfig> availableSkills)
    {
        currentSkills.Clear();
        currentSkills.AddRange(availableSkills);
        isUIActive = true;
        
        // 显示UI面板
        skillSelectionPanel?.SetActive(true);
        
        // 更新技能显示
        UpdateSkillDisplay();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionUI: 显示技能选择界面，技能数量: {availableSkills.Count}");
        }
    }
    
    /// <summary>
    /// 隐藏UI界面
    /// </summary>
    void HideUI()
    {
        isUIActive = false;
        currentSkills.Clear();
        
        // 隐藏UI面板
        skillSelectionPanel?.SetActive(false);
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 隐藏技能选择界面");
        }
    }
    
    /// <summary>
    /// 更新技能显示
    /// </summary>
    void UpdateSkillDisplay()
    {
        // 更新技能1
        if (currentSkills.Count > 0 && currentSkills[0] != null)
        {
            skillButton1?.gameObject.SetActive(true);
            skillName1?.SetText(GetSkillNameWithLevel(currentSkills[0], 0));
            skillDescription1?.SetText(GetSkillDescription(currentSkills[0], 0)); // 传入索引
        }
        else
        {
            skillButton1?.gameObject.SetActive(false);
            skillName1?.SetText("");
            skillDescription1?.SetText("");
        }
        
        // 更新技能2
        if (currentSkills.Count > 1 && currentSkills[1] != null)
        {
            skillButton2?.gameObject.SetActive(true);
            skillName2?.SetText(GetSkillNameWithLevel(currentSkills[1], 1));
            skillDescription2?.SetText(GetSkillDescription(currentSkills[1], 1)); // 传入索引
        }
        else
        {
            skillButton2?.gameObject.SetActive(false);
            skillName2?.SetText("");
            skillDescription2?.SetText("");
        }
        
        // 更新技能3
        if (currentSkills.Count > 2 && currentSkills[2] != null)
        {
            skillButton3?.gameObject.SetActive(true);
            skillName3?.SetText(GetSkillNameWithLevel(currentSkills[2], 2));
            skillDescription3?.SetText(GetSkillDescription(currentSkills[2], 2)); // 传入索引
        }
        else
        {
            skillButton3?.gameObject.SetActive(false);
            skillName3?.SetText("");
            skillDescription3?.SetText("");
        }
    }
    
    /// <summary>
    /// 获取带等级标识的技能名称
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <param name="skillIndex">技能在选择列表中的索引</param>
    /// <returns>技能名称（如果有多个等级则带等级标识）</returns>
    string GetSkillNameWithLevel(SkillConfig skill, int skillIndex)
    {
        if (skill == null)
            return "";
        
        string skillName = skill.skillName;
        
        // 检查技能是否有多个等级
        int maxLevel = skill.GetMaxLevel();
        if (maxLevel > 1)
        {
            // 从 SkillSelectionManager 获取目标等级
            if (skillSelectionManager != null)
            {
                var option = skillSelectionManager.GetSkillOption(skillIndex);
                if (option != null)
                {
                    // 在技能名称后添加等级标识
                    skillName = $"{skillName} lv.{option.targetLevel}";
                }
            }
        }
        
        return skillName;
    }
    
    /// <summary>
    /// 获取技能描述文本
    /// </summary>
    /// <param name="skill">技能配置</param>
    /// <param name="skillIndex">技能在选择列表中的索引</param>
    /// <returns>技能描述文本</returns>
    string GetSkillDescription(SkillConfig skill, int skillIndex)
    {
        if (skill == null)
            return "";
        
        // 从 SkillSelectionManager 获取技能选项（包含目标等级）
        if (skillSelectionManager != null)
        {
            var option = skillSelectionManager.GetSkillOption(skillIndex);
            if (option != null)
            {
                // 使用目标等级生成描述
                string dynamicDescription = skill.GetDynamicDescription(option.targetLevel);
                if (!string.IsNullOrEmpty(dynamicDescription))
                {
                    return dynamicDescription;
                }
            }
        }
        
        // 回退到默认描述（等级1）
        string fallbackDescription = skill.GetDynamicDescription(1);
        if (!string.IsNullOrEmpty(fallbackDescription))
        {
            return fallbackDescription;
        }
        
        // 最后回退到原始描述
        if (!string.IsNullOrEmpty(skill.description))
        {
            return skill.description;
        }
        
        return "暂无描述";
    }
    
    /// <summary>
    /// 技能按钮点击事件处理
    /// </summary>
    /// <param name="skillIndex">技能索引</param>
    void OnSkillButtonClicked(int skillIndex)
    {
        if (!isUIActive || skillIndex >= currentSkills.Count || currentSkills[skillIndex] == null)
            return;
        
        SkillConfig selectedSkill = currentSkills[skillIndex];
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionUI: 选择技能 - {selectedSkill.skillName}");
        }
        
        // 通知 SkillSelectionManager - 使用单例，双重保险
        if (skillSelectionManager == null)
        {
            skillSelectionManager = SkillSelectionManager.Instance;
        }
        
        if (skillSelectionManager != null)
        {
            skillSelectionManager.OnSkillSelected(selectedSkill);
        }
        else
        {
            Debug.LogError("SkillSelectionUI: SkillSelectionManager 未找到！Instance也为null！");
        }
    }
    
    #region 调试方法
    
    [ContextMenu("测试显示技能选择")]
    void TestShowSkillSelection()
    {
        // 创建测试技能列表
        List<SkillConfig> testSkills = new List<SkillConfig>();
        
        if (skillSelectionManager != null)
        {
            var availableSkills = skillSelectionManager.GetCurrentSelection();
            if (availableSkills.Count > 0)
            {
                testSkills.AddRange(availableSkills);
            }
        }
        
        if (testSkills.Count == 0)
        {
            Debug.LogWarning("SkillSelectionUI: 没有可用的测试技能");
            return;
        }
        
        ShowSkillSelection(testSkills);
    }
    
    [ContextMenu("隐藏技能选择")]
    void TestHideSkillSelection()
    {
        HideUI();
    }
    
    [ContextMenu("显示UI状态")]
    void ShowUIStatus()
    {
        Debug.Log($"SkillSelectionUI 状态:\n" +
                  $"UI激活: {isUIActive}\n" +
                  $"当前技能数量: {currentSkills.Count}\n" +
                  $"SkillSelectionManager: {(skillSelectionManager != null ? "已连接" : "未连接")}");
    }
    
    #endregion
}
