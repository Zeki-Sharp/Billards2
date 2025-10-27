using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能状态面板 - 显示所有已获得的技能
/// 
/// 【核心职责】：
/// - 显示玩家已获得的所有技能列表
/// - 技能去重（同一技能只显示最高等级）
/// - 按等级自动排序
/// - 动态生成技能项
/// </summary>
public class SkillStatusPanel : BasePanel
{
    [Header("UI元素")]
    [SerializeField] private RectTransform skillContainer;
    [SerializeField] private GameObject skillItemPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI skillCountText;
    
    
    // 当前显示的技能项列表
    private List<SkillItem> currentSkillItems = new List<SkillItem>();
    
    #region BasePanel生命周期
    
    /// <summary>
    /// 面板初始化
    /// </summary>
    public override void OnInit()
    {
        base.OnInit();
        
        // 设置面板类型
        panelType = UIPanelType.Popup;
        pauseGameOnShow = true; // 暂停游戏，方便查看技能
        
        // 设置关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        else
        {
            Debug.LogWarning("SkillStatusPanel: 关闭按钮未配置！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log("SkillStatusPanel: 初始化完成");
        }
    }
    
    /// <summary>
    /// 面板显示时调用
    /// </summary>
    public override void OnShow(UIPanelData data = null)
    {
        base.OnShow(data);
        
        // 刷新技能列表
        RefreshSkillList();
    }
    
    /// <summary>
    /// 面板隐藏时调用
    /// </summary>
    public override void OnHide()
    {
        base.OnHide();
        
        // 清理技能项
        ClearSkillItems();
    }
    
    #endregion
    
    #region 技能列表管理
    
    /// <summary>
    /// 刷新技能列表
    /// </summary>
    void RefreshSkillList()
    {
        // 清理旧的技能项
        ClearSkillItems();
        
        // 获取所有已激活的技能
        List<SkillInstance> allSkills = GetPlayerSkills();
        
        if (allSkills == null || allSkills.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log("SkillStatusPanel: 玩家还没有获得任何技能");
            }
            
            // 更新技能数量显示
            UpdateSkillCount(0);
            return;
        }
        
        // 技能去重（同一技能只保留最高等级）
        List<SkillInstance> uniqueSkills = GetUniqueSkills(allSkills);
        
        // 按等级排序（降序）
        List<SkillInstance> sortedSkills = uniqueSkills
            .OrderByDescending(s => s.currentLevel)
            .ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillStatusPanel: 显示 {sortedSkills.Count} 个技能（去重并排序后）");
        }
        
        // 动态生成技能项
        foreach (var skill in sortedSkills)
        {
            CreateSkillItem(skill);
        }
        
        // 更新技能数量显示
        UpdateSkillCount(sortedSkills.Count);
    }
    
    /// <summary>
    /// 获取玩家技能列表
    /// </summary>
    List<SkillInstance> GetPlayerSkills()
    {
        SkillManager skillManager = SkillManager.Instance;
        
        if (skillManager == null)
        {
            Debug.LogWarning("SkillStatusPanel: SkillManager.Instance 为空！");
            return new List<SkillInstance>();
        }
        
        return skillManager.GetAllActiveSkills();
    }
    
    /// <summary>
    /// 技能去重（同一技能只保留最高等级）
    /// </summary>
    List<SkillInstance> GetUniqueSkills(List<SkillInstance> allSkills)
    {
        // 按技能名称分组，每组只取最高等级的
        var uniqueSkills = allSkills
            .GroupBy(s => s.config.skillName)
            .Select(g => g.OrderByDescending(s => s.currentLevel).First())
            .ToList();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillStatusPanel: 去重前 {allSkills.Count()} 个，去重后 {uniqueSkills.Count} 个");
        }
        
        return uniqueSkills;
    }
    
    /// <summary>
    /// 创建单个技能项
    /// </summary>
    void CreateSkillItem(SkillInstance skillInstance)
    {
        if (skillItemPrefab == null)
        {
            Debug.LogError("SkillStatusPanel: SkillItem预制体未配置！");
            return;
        }
        
        if (skillContainer == null)
        {
            Debug.LogError("SkillStatusPanel: 技能容器未配置！");
            return;
        }
        
        // 实例化技能项
        GameObject itemObj = Instantiate(skillItemPrefab, skillContainer);
        SkillItem skillItem = itemObj.GetComponent<SkillItem>();
        
        if (skillItem == null)
        {
            Debug.LogError("SkillStatusPanel: SkillItem预制体缺少SkillItem组件！");
            Destroy(itemObj);
            return;
        }
        
        // 设置技能数据
        skillItem.SetSkillData(skillInstance);
        
        // 添加到列表
        currentSkillItems.Add(skillItem);
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillStatusPanel: 创建技能项 - {skillInstance.config.skillName} Lv.{skillInstance.currentLevel}");
        }
    }
    
    /// <summary>
    /// 清理所有技能项
    /// </summary>
    void ClearSkillItems()
    {
        foreach (var item in currentSkillItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        
        currentSkillItems.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillStatusPanel: 技能项已清理");
        }
    }
    
    /// <summary>
    /// 更新技能数量显示
    /// </summary>
    void UpdateSkillCount(int count)
    {
        if (skillCountText != null)
        {
            skillCountText.text = $"已获得{count}个技能";
        }
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    void OnCloseButtonClicked()
    {
        if (showDebugInfo)
        {
            Debug.Log("SkillStatusPanel: 点击关闭按钮");
        }
        
        // 通过UIController隐藏面板
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
        else
        {
            // 备用方案：直接隐藏
            OnHide();
        }
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("刷新技能列表")]
    void DebugRefreshSkillList()
    {
        RefreshSkillList();
    }
    
    [ContextMenu("显示面板状态")]
    void ShowPanelStatus()
    {
        Debug.Log($"SkillStatusPanel 状态:\n" +
                 $"Skill Container: {(skillContainer != null ? "已配置" : "未配置")}\n" +
                 $"Skill Item Prefab: {(skillItemPrefab != null ? "已配置" : "未配置")}\n" +
                 $"Close Button: {(closeButton != null ? "已配置" : "未配置")}\n" +
                 $"当前技能项数量: {currentSkillItems.Count}");
    }
    
    #endregion
}

