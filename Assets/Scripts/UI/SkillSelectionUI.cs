using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 技能选择界面 - 使用动态布局
/// 
/// 【核心职责】：
/// - 动态生成技能按钮（使用 Prefab + Layout Group）
/// - 显示技能名称、描述、分配角色
/// - 处理技能选择点击事件
/// - 与 SkillSelectionManager 交互
/// </summary>
public class SkillSelectionUI : BasePanel
{
    [Header("技能按钮配置")]
    [Tooltip("技能按钮预制体（GameObject，包含 SkillButtonPrefab 组件）")]
    [SerializeField] private GameObject skillButtonPrefab; // 技能按钮预制体
    
    [Tooltip("技能列表容器（用于 Layout Group 布局）")]
    [SerializeField] private Transform skillListContainer; // 技能列表容器
    
    // 组件引用
    private SkillSelectionManager skillSelectionManager;
    
    // 当前显示的技能列表
    private List<SkillConfig> currentSkills = new List<SkillConfig>();
    
    // 当前实例化的按钮列表
    private List<SkillButtonPrefab> instantiatedButtons = new List<SkillButtonPrefab>();
    
    #region BasePanel生命周期
    
    /// <summary>
    /// 面板初始化
    /// </summary>
    public override void OnInit()
    {
        base.OnInit(); // BasePanel会调用SetVisible(false)隐藏面板
        
        // 设置面板类型
        panelType = UIPanelType.FullScreen;
        pauseGameOnShow = true; // 技能选择时暂停游戏
        
        InitializeUI();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: OnInit完成，面板已隐藏");
        }
    }
    
    /// <summary>
    /// 面板显示时调用（由 UIController.ShowPanel 触发）
    /// </summary>
    public override void OnShow(UIPanelData data = null)
    {
        base.OnShow(data);
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 面板显示，游戏已暂停（由 UIController 处理）");
        }
    }
    
    /// <summary>
    /// 面板隐藏时调用（由 UIController.HidePanel 触发）
    /// </summary>
    public override void OnHide()
    {
        base.OnHide();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 面板隐藏，游戏已恢复（由 UIController 处理）");
        }
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    #endregion
    
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
        
        // 订阅事件
        SubscribeToEvents();
        
        // 验证配置
        ValidateConfiguration();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionUI: 初始化完成");
        }
    }
    
    /// <summary>
    /// 验证配置
    /// </summary>
    void ValidateConfiguration()
    {
        if (skillButtonPrefab == null)
        {
            Debug.LogError("SkillSelectionUI: 技能按钮预制体未配置！请在 Inspector 中分配 SkillButtonPrefab。");
        }
        
        if (skillListContainer == null)
        {
            Debug.LogError("SkillSelectionUI: 技能列表容器未配置！请在 Inspector 中分配用于 Layout Group 的 Transform。");
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
    /// 技能选择开始事件处理
    /// </summary>
    /// <param name="availableSkills">可选择的技能列表</param>
    void OnSkillSelectionStarted(List<SkillConfig> availableSkills)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[SkillSelectionUI] 收到技能选择开始事件，技能数量: {availableSkills.Count}");
        }
        
        // ✅ 确保 SkillSelectionManager 引用存在
        if (skillSelectionManager == null)
        {
            skillSelectionManager = SkillSelectionManager.Instance;
            if (skillSelectionManager == null)
            {
                Debug.LogError("[SkillSelectionUI] 无法获取 SkillSelectionManager.Instance，技能选项将缺少角色分配信息！");
            }
        }
        
        // 准备技能数据
        currentSkills.Clear();
        currentSkills.AddRange(availableSkills);
        
        // 更新技能显示
        UpdateSkillDisplay();
        
        // 通过 UIController 统一显示（会自动暂停游戏）
        if (UIController.Instance != null)
        {
            UIController.Instance.ShowPanel(this);
        }
        else
        {
            Debug.LogError("[SkillSelectionUI] UIController.Instance 为空！");
        }
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
        
        // 清理技能数据和UI
        currentSkills.Clear();
        ClearSkillButtons();
        
        // 通过 UIController 统一隐藏（会自动恢复游戏）
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
        else
        {
            Debug.LogError("SkillSelectionUI: UIController.Instance 为空！");
        }
    }
    
    /// <summary>
    /// ✅ 使用 Prefab + Layout Group 动态生成技能按钮
    /// </summary>
    void UpdateSkillDisplay()
    {
        // 清理现有按钮
        ClearSkillButtons();
        
        // 验证配置
        if (skillButtonPrefab == null || skillListContainer == null)
        {
            Debug.LogError("SkillSelectionUI: 技能按钮预制体或容器未配置，无法更新显示！");
            return;
        }
        
        // 动态生成技能按钮
        for (int i = 0; i < currentSkills.Count; i++)
        {
            SkillConfig skill = currentSkills[i];
            if (skill == null)
                continue;
            
            // 获取技能选项（包含角色分配信息）
            SkillSelectionOption option = null;
            if (skillSelectionManager != null)
            {
                option = skillSelectionManager.GetSkillOption(i);
                
                if (option == null && showDebugInfo)
                {
                    Debug.LogWarning($"[SkillSelectionUI] GetSkillOption({i}) 返回 null！");
                }
            }
            else
            {
                Debug.LogError($"[SkillSelectionUI] skillSelectionManager 为 null！");
            }
            
            // 实例化按钮
            GameObject buttonObj = Instantiate(skillButtonPrefab, skillListContainer);
            if (buttonObj != null)
            {
                // 获取 SkillButtonPrefab 组件
                SkillButtonPrefab buttonInstance = buttonObj.GetComponent<SkillButtonPrefab>();
                if (buttonInstance != null)
                {
                    // 初始化按钮
                    buttonInstance.Initialize(skill, i, option, OnSkillButtonClicked);
                    instantiatedButtons.Add(buttonInstance);
                }
                else
                {
                    Debug.LogError($"[SkillSelectionUI] 实例化的按钮对象缺少 SkillButtonPrefab 组件！");
                    Destroy(buttonObj);
                }
            }
            else
            {
                Debug.LogError($"[SkillSelectionUI] 无法实例化技能按钮 {i + 1}！");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionUI: 更新技能显示完成，共生成 {instantiatedButtons.Count} 个按钮");
        }
    }
    
    /// <summary>
    /// 清理所有技能按钮
    /// </summary>
    void ClearSkillButtons()
    {
        foreach (var button in instantiatedButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        
        instantiatedButtons.Clear();
    }
    
    /// <summary>
    /// 技能按钮点击事件处理
    /// </summary>
    /// <param name="skillIndex">技能索引</param>
    void OnSkillButtonClicked(int skillIndex)
    {
        if (!IsVisible || skillIndex < 0 || skillIndex >= currentSkills.Count || currentSkills[skillIndex] == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"SkillSelectionUI: 无效的技能索引 {skillIndex}，当前技能数量: {currentSkills.Count}");
            }
            return;
        }
        
        SkillConfig selectedSkill = currentSkills[skillIndex];
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionUI: 选择技能 - {selectedSkill.skillName} (索引: {skillIndex})");
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
        
        // 准备技能数据
        currentSkills.Clear();
        currentSkills.AddRange(testSkills);
        UpdateSkillDisplay();
        
        // 通过 UIController 显示
        if (UIController.Instance != null)
        {
            UIController.Instance.ShowPanel(this);
        }
    }
    
    [ContextMenu("隐藏技能选择")]
    void TestHideSkillSelection()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.HidePanel(this);
        }
    }
    
    [ContextMenu("显示UI状态")]
    void ShowUIStatus()
    {
        Debug.Log($"SkillSelectionUI 状态:\n" +
                  $"UI激活: {IsVisible}\n" +
                  $"当前技能数量: {currentSkills.Count}\n" +
                  $"SkillSelectionManager: {(skillSelectionManager != null ? "已连接" : "未连接")}");
    }
    
    #endregion
}
