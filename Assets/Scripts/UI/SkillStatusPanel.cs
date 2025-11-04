using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能状态面板 - 多角色技能展示系统
/// 
/// 【核心职责】：
/// - 多标签页展示：技能总览 + 3个角色独立页面
/// - 智能跳转：打开面板时自动跳转到当前选中角色的页面
/// - 技能分组：总览页面按角色分组显示技能
/// - 视觉反馈：选中角色技能高亮，死亡角色技能置灰
/// - 技能去重：同一技能只显示最高等级
/// - 按等级排序
/// </summary>
public class SkillStatusPanel : BasePanel
{
    #region UI元素配置
    
    [Header("标签按钮")]
    [SerializeField] private Button overviewTabButton;        // 技能总览标签
    [SerializeField] private Button character1TabButton;      // 1号位角色标签
    [SerializeField] private Button character2TabButton;      // 2号位角色标签
    [SerializeField] private Button character3TabButton;      // 3号位角色标签
    
    [Header("标签文本")]
    [SerializeField] private TextMeshProUGUI overviewTabText;
    [SerializeField] private TextMeshProUGUI character1TabText;
    [SerializeField] private TextMeshProUGUI character2TabText;
    [SerializeField] private TextMeshProUGUI character3TabText;
    
    [Header("内容区域")]
    [SerializeField] private RectTransform skillContainer;
    [SerializeField] private GameObject skillItemPrefab;
    [SerializeField] private GameObject characterContainerPrefab;  // ✅ 角色技能容器预制体（带 CharacterSkillContainer 脚本，包含 Header/SkillList/NoSkillHint）
    
    [Header("其他UI")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI skillCountText;
    
    
    [Header("标签高亮颜色")]
    [SerializeField] private Color normalTabColor = Color.white;
    [SerializeField] private Color selectedTabColor = Color.yellow;
    [SerializeField] private Color highlightSkillColor = new Color(1f, 1f, 0.5f, 1f);  // 高亮技能背景色
    [SerializeField] private Color deadSkillColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // 死亡角色技能置灰色
    
    #endregion
    
    #region 运行时数据
    
    // 当前显示的技能项列表
    private List<SkillItem> currentSkillItems = new List<SkillItem>();
    
    // 当前显示模式
    private enum DisplayMode
    {
        Overview,       // 总览（所有角色的技能分组显示）
        Character1,     // 1号位角色
        Character2,     // 2号位角色
        Character3      // 3号位角色
    }
    private DisplayMode currentMode = DisplayMode.Overview;
    
    // 队伍数据
    private TeamData teamData;
    
    // 当前选中角色ID（用于高亮）
    private string selectedCharacterID;
    
    // ✅ 三个角色的技能容器实例（从预制体实例化，带 CharacterSkillContainer 脚本）
    private CharacterSkillContainer characterContainer1;
    private CharacterSkillContainer characterContainer2;
    private CharacterSkillContainer characterContainer3;
    
    #endregion
    
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
        
        // 设置标签按钮事件
        if (overviewTabButton != null)
            overviewTabButton.onClick.AddListener(() => SwitchToTab(DisplayMode.Overview));
        if (character1TabButton != null)
            character1TabButton.onClick.AddListener(() => SwitchToTab(DisplayMode.Character1));
        if (character2TabButton != null)
            character2TabButton.onClick.AddListener(() => SwitchToTab(DisplayMode.Character2));
        if (character3TabButton != null)
            character3TabButton.onClick.AddListener(() => SwitchToTab(DisplayMode.Character3));
        
        // ✅ 实例化3个角色技能容器
        CreateCharacterContainers();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillStatusPanel: 初始化完成");
        }
    }
    
    /// <summary>
    /// ✅ 创建3个角色技能容器（从预制体实例化）
    /// </summary>
    void CreateCharacterContainers()
    {
        if (characterContainerPrefab == null)
        {
            Debug.LogError("SkillStatusPanel: characterContainerPrefab 未配置！");
            return;
        }
        
        if (skillContainer == null)
        {
            Debug.LogError("SkillStatusPanel: skillContainer 未配置！");
            return;
        }
        
        // 实例化3个容器
        GameObject container1Obj = Instantiate(characterContainerPrefab, skillContainer);
        GameObject container2Obj = Instantiate(characterContainerPrefab, skillContainer);
        GameObject container3Obj = Instantiate(characterContainerPrefab, skillContainer);
        
        // 获取 CharacterSkillContainer 组件
        characterContainer1 = container1Obj.GetComponent<CharacterSkillContainer>();
        characterContainer2 = container2Obj.GetComponent<CharacterSkillContainer>();
        characterContainer3 = container3Obj.GetComponent<CharacterSkillContainer>();
        
        // 设置名称（便于调试）
        if (characterContainer1 != null) characterContainer1.gameObject.name = "CharacterContainer_0";
        if (characterContainer2 != null) characterContainer2.gameObject.name = "CharacterContainer_1";
        if (characterContainer3 != null) characterContainer3.gameObject.name = "CharacterContainer_2";
        
        // 验证
        if (characterContainer1 == null || characterContainer2 == null || characterContainer3 == null)
        {
            Debug.LogError("SkillStatusPanel: characterContainerPrefab 缺少 CharacterSkillContainer 组件！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log("SkillStatusPanel: 3个角色技能容器已创建");
        }
    }
    
    /// <summary>
    /// 面板显示时调用
    /// </summary>
    public override void OnShow(UIPanelData data = null)
    {
        base.OnShow(data);
        
        // 获取队伍数据
        LoadTeamData();
        
        // 更新角色标签文本
        UpdateCharacterTabTexts();
        
        // 智能跳转：如果有选中角色，跳转到该角色页面，否则显示总览
        DetermineInitialTab();
        
        // 刷新技能列表
        RefreshSkillList();
    }
    
    /// <summary>
    /// 面板隐藏时调用
    /// </summary>
    public override void OnHide()
    {
        base.OnHide();
        
        // ✅ 容器已创建好，技能项会在下次 OnShow 时自动刷新
        // 不需要手动清理
    }
    
    #endregion
    
    #region 数据加载
    
    /// <summary>
    /// 加载队伍数据
    /// </summary>
    void LoadTeamData()
    {
        var session = GameSession.GetOrCreateInstance();
        if (session != null && session.HasTeamData())
        {
            teamData = session.GetTeamData();
            
            // 获取当前选中角色ID
            if (CharacterSelectionController.Instance != null)
            {
                selectedCharacterID = CharacterSelectionController.Instance.SelectedCharacterID;
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"SkillStatusPanel: 加载队伍数据成功，当前选中角色：{selectedCharacterID ?? "无"}");
            }
        }
        else
        {
            Debug.LogWarning("SkillStatusPanel: 无法获取队伍数据！");
        }
    }
    
    /// <summary>
    /// 更新角色标签文本
    /// </summary>
    void UpdateCharacterTabTexts()
    {
        if (teamData == null || teamData.characters.Count < 3)
        {
            Debug.LogWarning("SkillStatusPanel: 队伍数据不完整！");
            return;
        }
        
        // 更新总览标签
        if (overviewTabText != null)
        {
            overviewTabText.text = "技能总览";
        }
        
        // 更新各角色标签（显示角色名称 + 存活状态）
        UpdateCharacterTabText(character1TabText, teamData.characters[0]);
        UpdateCharacterTabText(character2TabText, teamData.characters[1]);
        UpdateCharacterTabText(character3TabText, teamData.characters[2]);
        
        // ✅ 更新容器标题（通过 CharacterSkillContainer 组件）
        UpdateCharacterContainer(characterContainer1, teamData.characters[0]);
        UpdateCharacterContainer(characterContainer2, teamData.characters[1]);
        UpdateCharacterContainer(characterContainer3, teamData.characters[2]);
    }
    
    /// <summary>
    /// ✅ 更新角色容器信息
    /// </summary>
    void UpdateCharacterContainer(CharacterSkillContainer container, CharacterInstance character)
    {
        if (container == null || character == null) return;
        
        string characterName = character.characterData != null ? character.characterData.info.name : "未知角色";
        container.SetCharacterInfo(character.characterID, characterName, character.isAlive);
    }
    
    /// <summary>
    /// ✅ 更新单个角色标签文本（只显示角色名称）
    /// </summary>
    void UpdateCharacterTabText(TextMeshProUGUI tabText, CharacterInstance character)
    {
        if (tabText == null || character == null) return;
        
        string characterName = character.characterData != null ? character.characterData.info.name : "未知角色";
        string statusSuffix = character.isAlive ? "" : " (已死亡)";
        
        // ✅ 只显示角色名称，不显示"X号位"
        tabText.text = $"{characterName}{statusSuffix}";
    }
    
    /// <summary>
    /// 智能跳转：确定初始显示的标签页
    /// </summary>
    void DetermineInitialTab()
    {
        DisplayMode targetMode = DisplayMode.Overview; // 默认总览
        
        // 如果有选中角色，跳转到该角色页面
        if (!string.IsNullOrEmpty(selectedCharacterID) && teamData != null)
        {
            for (int i = 0; i < teamData.characters.Count; i++)
            {
                if (teamData.characters[i].characterID == selectedCharacterID)
                {
                    targetMode = (DisplayMode)(i + 1); // Character1/2/3
                    if (showDebugInfo)
                    {
                        Debug.Log($"SkillStatusPanel: 智能跳转到选中角色页面：{targetMode}");
                    }
                    break;
                }
            }
        }
        
        SwitchToTab(targetMode);
    }
    
    #endregion
    
    #region 标签页切换
    
    /// <summary>
    /// ✅ 切换到指定标签页（用 SetActive 控制容器显示）
    /// </summary>
    void SwitchToTab(DisplayMode mode)
    {
        currentMode = mode;
        
        // 更新标签高亮
        UpdateTabHighlight();
        
        // ✅ 用 SetActive 控制容器显示/隐藏
        switch (mode)
        {
            case DisplayMode.Overview:
                // 总览：显示所有容器
                if (characterContainer1 != null) characterContainer1.gameObject.SetActive(true);
                if (characterContainer2 != null) characterContainer2.gameObject.SetActive(true);
                if (characterContainer3 != null) characterContainer3.gameObject.SetActive(true);
                break;
                
            case DisplayMode.Character1:
                // 只显示角色1
                if (characterContainer1 != null) characterContainer1.gameObject.SetActive(true);
                if (characterContainer2 != null) characterContainer2.gameObject.SetActive(false);
                if (characterContainer3 != null) characterContainer3.gameObject.SetActive(false);
                break;
                
            case DisplayMode.Character2:
                // 只显示角色2
                if (characterContainer1 != null) characterContainer1.gameObject.SetActive(false);
                if (characterContainer2 != null) characterContainer2.gameObject.SetActive(true);
                if (characterContainer3 != null) characterContainer3.gameObject.SetActive(false);
                break;
                
            case DisplayMode.Character3:
                // 只显示角色3
                if (characterContainer1 != null) characterContainer1.gameObject.SetActive(false);
                if (characterContainer2 != null) characterContainer2.gameObject.SetActive(false);
                if (characterContainer3 != null) characterContainer3.gameObject.SetActive(true);
                break;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillStatusPanel: 切换到标签页 - {mode} (容器显示已更新)");
        }
    }
    
    /// <summary>
    /// 更新标签按钮高亮状态
    /// </summary>
    void UpdateTabHighlight()
    {
        SetTabHighlight(overviewTabButton, currentMode == DisplayMode.Overview);
        SetTabHighlight(character1TabButton, currentMode == DisplayMode.Character1);
        SetTabHighlight(character2TabButton, currentMode == DisplayMode.Character2);
        SetTabHighlight(character3TabButton, currentMode == DisplayMode.Character3);
    }
    
    /// <summary>
    /// 设置单个标签按钮的高亮状态
    /// </summary>
    void SetTabHighlight(Button button, bool highlighted)
    {
        if (button == null) return;
        
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = highlighted ? selectedTabColor : normalTabColor;
        }
    }
    
    #endregion
    
    #region 技能列表管理
    
    /// <summary>
    /// ✅ 刷新技能列表（刷新3个容器的技能）
    /// </summary>
    void RefreshSkillList()
    {
        if (teamData == null || teamData.characters.Count < 3) return;
        
        // 刷新3个容器的技能列表
        RefreshContainerSkills(characterContainer1, teamData.characters[0]);
        RefreshContainerSkills(characterContainer2, teamData.characters[1]);
        RefreshContainerSkills(characterContainer3, teamData.characters[2]);
        
        // 更新技能总数显示
        UpdateTotalSkillCount();
    }
    
    /// <summary>
    /// ✅ 刷新单个容器的技能列表
    /// </summary>
    void RefreshContainerSkills(CharacterSkillContainer container, CharacterInstance character)
    {
        if (container == null || character == null) return;
        
        // 清空容器的技能列表
        container.ClearSkillList();
        
        // 获取该角色的技能
        List<SkillInstance> characterSkills = GetCharacterSkills(character.characterID);
        
        if (characterSkills.Count == 0)
        {
            // ✅ 显示"暂无技能"提示
            container.ShowNoSkillHint(true);
            return;
        }
        
        // ✅ 隐藏"暂无技能"提示
        container.ShowNoSkillHint(false);
        
        // 技能去重并排序
        var uniqueSkills = GetUniqueSkills(characterSkills)
            .OrderByDescending(s => s.currentLevel)
            .ToList();
        
        // 创建技能项到容器的 skillListContainer
        bool isHighlight = (character.characterID == selectedCharacterID && character.isAlive && currentMode == DisplayMode.Overview);
        bool isDead = !character.isAlive;
        
        foreach (var skill in uniqueSkills)
        {
            CreateSkillItemToContainer(skill, container.skillListContainer, isHighlight, isDead);
        }
    }
    
    /// <summary>
    /// ✅ 更新技能总数显示
    /// </summary>
    void UpdateTotalSkillCount()
    {
        if (teamData == null) return;
        
        int totalCount = 0;
        foreach (var character in teamData.characters)
        {
            totalCount += GetCharacterSkills(character.characterID).Count;
        }
        
        UpdateSkillCount(totalCount);
    }
    
    
    /// <summary>
    /// 获取指定角色的技能列表
    /// </summary>
    List<SkillInstance> GetCharacterSkills(string characterID)
    {
        SkillManager skillManager = SkillManager.Instance;
        
        if (skillManager == null)
        {
            Debug.LogWarning("SkillStatusPanel: SkillManager.Instance 为空！");
            return new List<SkillInstance>();
        }
        
        return skillManager.GetCharacterSkillInstances(characterID);
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
    /// ✅ 创建单个技能项到指定容器（支持高亮和置灰）
    /// </summary>
    void CreateSkillItemToContainer(SkillInstance skillInstance, Transform parent, bool isHighlight = false, bool isDead = false)
    {
        if (skillItemPrefab == null)
        {
            Debug.LogError("SkillStatusPanel: SkillItem预制体未配置！");
            return;
        }
        
        if (parent == null)
        {
            Debug.LogError("SkillStatusPanel: 父容器为空！");
            return;
        }
        
        // 实例化技能项到指定容器
        GameObject itemObj = Instantiate(skillItemPrefab, parent);
        SkillItem skillItem = itemObj.GetComponent<SkillItem>();
        
        if (skillItem == null)
        {
            Debug.LogError("SkillStatusPanel: SkillItem预制体缺少SkillItem组件！");
            Destroy(itemObj);
            return;
        }
        
        // 设置技能数据
        skillItem.SetSkillData(skillInstance);
        
        // 应用视觉效果
        if (isDead)
        {
            // 死亡角色技能置灰
            ApplyDeadSkillEffect(skillItem);
        }
        else if (isHighlight)
        {
            // 选中角色技能高亮
            ApplyHighlightSkillEffect(skillItem);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillStatusPanel: 创建技能项 - {skillInstance.config.skillName} Lv.{skillInstance.currentLevel} (高亮:{isHighlight}, 置灰:{isDead})");
        }
    }
    
    
    /// <summary>
    /// 应用选中角色技能高亮效果
    /// </summary>
    void ApplyHighlightSkillEffect(SkillItem skillItem)
    {
        if (skillItem == null) return;
        
        // 修改背景颜色（如果有 Image 组件）
        var bgImage = skillItem.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = highlightSkillColor;
        }
    }
    
    /// <summary>
    /// 应用死亡角色技能置灰效果
    /// </summary>
    void ApplyDeadSkillEffect(SkillItem skillItem)
    {
        if (skillItem == null) return;
        
        // 修改背景颜色
        var bgImage = skillItem.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = deadSkillColor;
        }
        
        // 修改所有文本颜色
        var allTexts = skillItem.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var text in allTexts)
        {
            text.color = Color.gray;
        }
        
        // 修改图标透明度
        var allImages = skillItem.GetComponentsInChildren<Image>();
        foreach (var image in allImages)
        {
            var color = image.color;
            color.a = 0.5f; // 半透明
            image.color = color;
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
                 $"Character Container Prefab: {(characterContainerPrefab != null ? "已配置" : "未配置")}\n" +
                 $"Close Button: {(closeButton != null ? "已配置" : "未配置")}\n" +
                 $"当前模式: {currentMode}\n" +
                 $"容器1: {(characterContainer1 != null ? "已创建" : "未创建")}\n" +
                 $"容器2: {(characterContainer2 != null ? "已创建" : "未创建")}\n" +
                 $"容器3: {(characterContainer3 != null ? "已创建" : "未创建")}");
    }
    
    #endregion
}

