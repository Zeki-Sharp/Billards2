using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能选择选项 - 包含技能配置和选择类型信息
/// </summary>
[System.Serializable]
public class SkillSelectionOption
{
    public SkillConfig skillConfig;
    public bool isUpgrade;  // true表示升级，false表示新技能
    public int targetLevel; // 目标等级
}

/// <summary>
/// 技能选择管理器 - 负责管理技能选择逻辑
/// 
/// 【核心职责】：
/// - 监听关卡完成事件，启动技能选择
/// - 从技能库中随机选择3个技能（去重）
/// - 处理技能选择事件
/// - 将选中技能添加到玩家技能列表
/// - 通知关卡管理器进入下一关卡
/// </summary>
public class SkillSelectionManager : MonoBehaviour
{
    public static SkillSelectionManager Instance { get; private set; }
    
    [Header("技能库配置")]
    [SerializeField] private bool autoDiscoverSkills = true; // 是否自动发现技能
    [SerializeField] private int skillSelectionCount = 3; // 每次选择的技能数量
    
    // 自动发现的技能列表（运行时填充）
    private List<SkillConfig> allAvailableSkills = new List<SkillConfig>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private SkillManager skillManager;
    private LevelManager levelManager;
    
    // 状态管理
    private bool isSkillSelectionActive = false;
    private List<SkillConfig> currentSelection = new List<SkillConfig>();
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持跨场景存在
        }
        else
        {
            Debug.LogWarning("发现多个SkillSelectionManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeSkillSelectionManager();
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        GameEventBus.OnLevelCompleted -= OnLevelCompleted;
    }
    
    /// <summary>
    /// 初始化技能选择管理器
    /// </summary>
    void InitializeSkillSelectionManager()
    {
        // 获取组件引用
        skillManager = SkillManager.GetOrCreateInstance(); // 使用单例，如果不存在则创建
        levelManager = FindFirstObjectByType<LevelManager>();
        
        // 自动发现技能配置
        if (autoDiscoverSkills)
        {
            DiscoverAllSkills();
        }
        
        // 订阅事件
        GameEventBus.OnLevelCompleted += OnLevelCompleted;
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionManager: 初始化完成");
            Debug.Log($"SkillSelectionManager: 技能库包含 {allAvailableSkills.Count} 个技能");
        }
    }
    
    /// <summary>
    /// 自动发现所有技能配置
    /// </summary>
    void DiscoverAllSkills()
    {
        allAvailableSkills.Clear();
        
        // 获取当前角色名称
        string currentCharacterName = GetCurrentCharacterName();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 当前角色名称 = '{currentCharacterName}'");
        }
        
        // 使用 Resources.LoadAll 从 Resources 文件夹加载所有 SkillConfig 资产
        SkillConfig[] allSkills = Resources.LoadAll<SkillConfig>("");
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: Resources.LoadAll 找到 {allSkills.Length} 个 SkillConfig 资产");
        }
        
        foreach (var skill in allSkills)
        {
            if (showDebugInfo)
            {
                Debug.Log($"检查技能: 资产名={skill?.name}, 对象={skill != null}, 有效={skill?.IsValid()}, 名称='{skill?.skillName}', Tag='{skill?.skillTag}'");
            }
            
            // 检查技能是否为 null
            if (skill == null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  → 被过滤：技能对象为 null");
                }
                continue;
            }
            
            // 检查技能是否有效
            if (!skill.IsValid())
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  → 被过滤：技能无效 (IsValid() = false)");
                }
                continue;
            }
            
            // 检查技能名称是否为空
            if (string.IsNullOrEmpty(skill.skillName))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  → 被过滤：技能名称为空");
                }
                continue;
            }
            
            // 检查技能标签是否匹配当前角色或通用标签
            if (!IsSkillAvailableForCurrentCharacter(skill, currentCharacterName))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  → 被过滤：技能标签 '{skill.skillTag}' 不匹配当前角色 '{currentCharacterName}'");
                }
                continue;
            }
            
            // 检查是否重复
            if (allAvailableSkills.Contains(skill))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"  → 被过滤：技能已存在 (重复)");
                }
                continue;
            }
            
            // 添加到列表
            allAvailableSkills.Add(skill);
            if (showDebugInfo)
            {
                Debug.Log($"  → 添加成功：{skill.skillName} (Tag: {skill.skillTag})");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 最终发现 {allAvailableSkills.Count} 个有效技能配置");
            foreach (var skill in allAvailableSkills)
            {
                Debug.Log($"  - {skill.skillName} (Tag: {skill.skillTag})");
            }
        }
    }
    
    /// <summary>
    /// 获取当前角色名称
    /// </summary>
    /// <returns>当前角色名称，如果无法获取则返回空字符串</returns>
    string GetCurrentCharacterName()
    {
        PlayerData currentCharacter = SceneTransitionManager.GetSelectedCharacter();
        
        if (currentCharacter != null && !string.IsNullOrEmpty(currentCharacter.playerName))
        {
            return currentCharacter.playerName;
        }
        
        if (showDebugInfo)
        {
            Debug.LogWarning("SkillSelectionManager: 无法获取当前角色名称，将只加载 common 和 default 标签的技能");
        }
        
        return "";
    }
    
    /// <summary>
    /// 检查技能是否对当前角色可用
    /// </summary>
    /// <param name="skill">要检查的技能</param>
    /// <param name="characterName">当前角色名称</param>
    /// <returns>如果技能可用返回 true，否则返回 false</returns>
    bool IsSkillAvailableForCurrentCharacter(SkillConfig skill, string characterName)
    {
        if (skill == null)
        {
            return false;
        }
        
        string skillTag = skill.skillTag;
        
        // 如果技能没有设置标签，默认为 "default"
        if (string.IsNullOrEmpty(skillTag))
        {
            skillTag = "default";
        }
        
        // 通用技能对所有角色可用
        if (skillTag.Equals("common", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 默认技能对所有角色可用（兼容旧技能）
        if (skillTag.Equals("default", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        
        // 角色专属技能
        if (!string.IsNullOrEmpty(characterName) && 
            skillTag.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 关卡完成事件处理
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    /// <param name="levelConfig">关卡配置</param>
    void OnLevelCompleted(int levelIndex, LevelConfig levelConfig)
    {
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 关卡 {levelIndex + 1} 完成，准备启动技能选择");
        }
        
        StartSkillSelection();
    }
    
    /// <summary>
    /// 启动技能选择
    /// </summary>
    public void StartSkillSelection()
    {
        if (isSkillSelectionActive)
        {
            Debug.LogWarning("SkillSelectionManager: 技能选择已在进行中，忽略重复启动");
            return;
        }
        
        if (allAvailableSkills == null || allAvailableSkills.Count == 0)
        {
            Debug.LogError("SkillSelectionManager: 技能库为空，无法进行技能选择！");
            // 直接进入下一关卡
            ProceedToNextLevel();
            return;
        }
        
        // 生成随机技能选择
        GenerateRandomSkillSelection();
        
        if (currentSelection.Count == 0)
        {
            Debug.LogWarning("SkillSelectionManager: 没有可选择的技能，直接进入下一关卡");
            ProceedToNextLevel();
            return;
        }
        
        isSkillSelectionActive = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 技能选择启动，提供 {currentSelection.Count} 个技能选择");
            for (int i = 0; i < currentSelection.Count; i++)
            {
                Debug.Log($"  - 技能 {i + 1}: {currentSelection[i].skillName}");
            }
        }
        
        // 发布技能选择开始事件
        GameEventBus.PublishSkillSelectionStarted(currentSelection);
    }
    
    /// <summary>
    /// 生成随机技能选择（支持技能升级）
    /// </summary>
    void GenerateRandomSkillSelection()
    {
        currentSelection.Clear();
        
        // 获取玩家已有的技能
        List<SkillConfig> playerSkills = GetPlayerExistingSkills();
        
        // 创建技能选择选项列表
        List<SkillSelectionOption> availableOptions = new List<SkillSelectionOption>();
        
        // 1. 添加新技能选项（未学习的技能）
        foreach (var skill in allAvailableSkills)
        {
            if (!playerSkills.Contains(skill) && IsSkillUnlocked(skill) && skill.isActive)
            {
                availableOptions.Add(new SkillSelectionOption
                {
                    skillConfig = skill,
                    isUpgrade = false,
                    targetLevel = 1
                });
            }
        }
        
        // 2. 添加技能升级选项（已学习但可升级的技能）
        foreach (var skill in playerSkills)
        {
            if (skillManager != null)
            {
                var skillInstance = skillManager.GetSkillInstance(skill.skillName);
                if (skillInstance != null && skillInstance.CanUpgrade())
                {
                    int nextLevel = skillInstance.GetNextLevel();
                    availableOptions.Add(new SkillSelectionOption
                    {
                        skillConfig = skill,
                        isUpgrade = true,
                        targetLevel = nextLevel
                    });
                }
            }
        }
        
        if (availableOptions.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("SkillSelectionManager: 没有可选择的技能或升级选项");
            }
            return;
        }
        
        // 随机选择技能选项
        int selectionCount = Mathf.Min(skillSelectionCount, availableOptions.Count);
        
        // 使用 Fisher-Yates 洗牌算法进行随机选择
        for (int i = 0; i < selectionCount; i++)
        {
            int randomIndex = Random.Range(i, availableOptions.Count);
            
            // 交换元素
            SkillSelectionOption temp = availableOptions[i];
            availableOptions[i] = availableOptions[randomIndex];
            availableOptions[randomIndex] = temp;
            
            // 添加到选择列表
            currentSelection.Add(availableOptions[i].skillConfig);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 从 {availableOptions.Count} 个可用选项中选择了 {currentSelection.Count} 个技能");
            foreach (var option in availableOptions.Take(selectionCount))
            {
                string optionType = option.isUpgrade ? $"升级到Lv{option.targetLevel}" : "新技能";
                Debug.Log($"  - {option.skillConfig.skillName} ({optionType})");
            }
        }
    }
    
    /// <summary>
    /// 获取玩家已有的技能
    /// </summary>
    /// <returns>玩家已有的技能列表</returns>
    List<SkillConfig> GetPlayerExistingSkills()
    {
        List<SkillConfig> existingSkills = new List<SkillConfig>();
        
        if (skillManager != null)
        {
            // 从 SkillManager 获取玩家已有的技能
            existingSkills.AddRange(skillManager.activeSkills);
            
            if (showDebugInfo)
            {
                Debug.Log($"SkillSelectionManager: 玩家已有 {existingSkills.Count} 个技能");
                foreach (var skill in existingSkills)
                {
                    Debug.Log($"  - {skill.skillName}");
                }
            }
        }
        
        return existingSkills;
    }
    
    /// <summary>
    /// 检查技能是否已解锁（满足前置技能要求）
    /// </summary>
    /// <param name="skill">要检查的技能</param>
    /// <returns>如果技能已解锁返回 true，否则返回 false</returns>
    bool IsSkillUnlocked(SkillConfig skill)
    {
        if (skill == null)
        {
            return false;
        }
        
        // 如果没有前置技能要求，直接解锁
        if (skill.requiredSkills == null || skill.requiredSkills.Count == 0)
        {
            return true;
        }
        
        // 获取玩家已有的技能
        List<SkillConfig> playerSkills = GetPlayerExistingSkills();
        
        // 检查是否拥有所有前置技能
        foreach (string requiredSkillName in skill.requiredSkills)
        {
            // 跳过空的前置技能名称
            if (string.IsNullOrEmpty(requiredSkillName))
            {
                continue;
            }
            
            // 检查玩家是否拥有此前置技能
            bool hasRequiredSkill = playerSkills.Any(playerSkill => 
                playerSkill.skillName == requiredSkillName);
            
            if (!hasRequiredSkill)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"SkillSelectionManager: 技能 '{skill.skillName}' 未解锁 - 缺少前置技能 '{requiredSkillName}'");
                }
                return false; // 缺少前置技能
            }
        }
        
        // 拥有所有前置技能，已解锁
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 技能 '{skill.skillName}' 已解锁");
        }
        return true;
    }
    
    /// <summary>
    /// 处理技能选择
    /// </summary>
    /// <param name="selectedSkill">选中的技能</param>
    public void OnSkillSelected(SkillConfig selectedSkill)
    {
        if (!isSkillSelectionActive)
        {
            Debug.LogWarning("SkillSelectionManager: 技能选择未激活，忽略技能选择");
            return;
        }
        
        if (selectedSkill == null)
        {
            Debug.LogError("SkillSelectionManager: 选中的技能为空！");
            return;
        }
        
        if (!currentSelection.Contains(selectedSkill))
        {
            Debug.LogError($"SkillSelectionManager: 选中的技能 {selectedSkill.skillName} 不在当前选择列表中！");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 玩家选择了技能 - {selectedSkill.skillName}");
        }
        
        // 将技能添加到玩家技能列表或升级技能
        AddOrUpgradeSkill(selectedSkill);
        
        // 发布技能选择事件
        GameEventBus.PublishSkillSelected(selectedSkill, currentSelection);
        
        // 技能选择完成
        CompleteSkillSelection();
    }
    
    /// <summary>
    /// 将技能添加到玩家技能列表或升级技能
    /// </summary>
    /// <param name="skill">要添加或升级的技能</param>
    void AddOrUpgradeSkill(SkillConfig skill)
    {
        // 尝试获取 SkillManager 单例
        if (skillManager == null)
        {
            skillManager = SkillManager.GetOrCreateInstance();
        }
        
        if (skillManager == null)
        {
            Debug.LogError("SkillSelectionManager: SkillManager 单例未找到，无法添加或升级技能！");
            return;
        }
        
        // 检查玩家是否已经拥有此技能
        var existingSkillInstance = skillManager.GetSkillInstance(skill.skillName);
        
        if (existingSkillInstance != null)
        {
            // 玩家已拥有此技能，执行升级
            if (existingSkillInstance.CanUpgrade())
            {
                int nextLevel = existingSkillInstance.GetNextLevel();
                bool upgradeSuccess = skillManager.UpgradeSkill(skill.skillName, nextLevel);
                
                if (upgradeSuccess)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"SkillSelectionManager: 成功升级技能 - {skill.skillName} 到等级 {nextLevel}");
                    }
                }
                else
                {
                    Debug.LogError($"SkillSelectionManager: 技能升级失败 - {skill.skillName}");
                }
            }
            else
            {
                Debug.LogWarning($"SkillSelectionManager: 技能 {skill.skillName} 已达到最高等级，无法升级");
            }
        }
        else
        {
            // 玩家未拥有此技能，添加新技能
            skillManager.AddSkill(skill);
            
            if (showDebugInfo)
            {
                Debug.Log($"SkillSelectionManager: 成功添加新技能到玩家 - {skill.skillName}");
            }
            
            // 发布技能添加事件
            GameEventBus.PublishSkillAddedToPlayer(skill);
        }
    }
    
    /// <summary>
    /// 完成技能选择
    /// </summary>
    void CompleteSkillSelection()
    {
        isSkillSelectionActive = false;
        currentSelection.Clear();
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionManager: 技能选择完成");
        }
        
        // 发布技能选择完成事件
        GameEventBus.PublishSkillSelectionCompleted();
        
        // 通知关卡管理器进入下一关卡
        ProceedToNextLevel();
    }
    
    /// <summary>
    /// 进入下一关卡
    /// </summary>
    void ProceedToNextLevel()
    {
        if (levelManager != null)
        {
            if (showDebugInfo)
            {
                Debug.Log("SkillSelectionManager: 技能选择完成，通知 LevelManager 加载下一关卡场景");
            }
            
            levelManager.LoadNextLevel();
        }
        else
        {
            Debug.LogError("SkillSelectionManager: LevelManager 未找到，无法进入下一关卡！");
        }
    }
    
    #region 公共方法
    
    /// <summary>
    /// 获取当前可选择的技能列表
    /// </summary>
    /// <returns>当前可选择的技能列表</returns>
    public List<SkillConfig> GetCurrentSelection()
    {
        return new List<SkillConfig>(currentSelection);
    }
    
    /// <summary>
    /// 检查技能选择是否激活
    /// </summary>
    /// <returns>技能选择是否激活</returns>
    public bool IsSkillSelectionActive()
    {
        return isSkillSelectionActive;
    }
    
    /// <summary>
    /// 获取技能库中的技能数量
    /// </summary>
    /// <returns>技能库中的技能数量</returns>
    public int GetAvailableSkillCount()
    {
        return allAvailableSkills != null ? allAvailableSkills.Count : 0;
    }
    
    /// <summary>
    /// 手动刷新技能库（重新发现所有技能）
    /// </summary>
    public void RefreshSkillLibrary()
    {
        if (autoDiscoverSkills)
        {
            DiscoverAllSkills();
            
            if (showDebugInfo)
            {
                Debug.Log($"SkillSelectionManager: 手动刷新技能库完成，发现 {allAvailableSkills.Count} 个技能");
            }
        }
        else
        {
            Debug.LogWarning("SkillSelectionManager: 自动发现技能已禁用，无法刷新技能库");
        }
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("强制启动技能选择")]
    void ForceStartSkillSelection()
    {
        StartSkillSelection();
    }
    
    [ContextMenu("刷新技能库")]
    void RefreshSkillLibraryContextMenu()
    {
        RefreshSkillLibrary();
    }
    
    [ContextMenu("显示当前选择")]
    void ShowCurrentSelection()
    {
        if (currentSelection.Count > 0)
        {
            Debug.Log($"SkillSelectionManager 当前选择 ({currentSelection.Count} 个技能):");
            for (int i = 0; i < currentSelection.Count; i++)
            {
                Debug.Log($"  - {i + 1}. {currentSelection[i].skillName}");
            }
        }
        else
        {
            Debug.Log("SkillSelectionManager: 当前没有技能选择");
        }
    }
    
    [ContextMenu("显示技能库信息")]
    void ShowSkillLibraryInfo()
    {
        Debug.Log($"SkillSelectionManager 技能库信息:\n" +
                  $"自动发现技能: {autoDiscoverSkills}\n" +
                  $"技能库总数: {GetAvailableSkillCount()}\n" +
                  $"技能选择数量: {skillSelectionCount}\n" +
                  $"技能选择激活: {isSkillSelectionActive}");
        
        if (allAvailableSkills != null && allAvailableSkills.Count > 0)
        {
            Debug.Log("技能库内容:");
            for (int i = 0; i < allAvailableSkills.Count; i++)
            {
                if (allAvailableSkills[i] != null)
                {
                    Debug.Log($"  - {i + 1}. {allAvailableSkills[i].skillName}");
                }
                else
                {
                    Debug.Log($"  - {i + 1}. [空技能]");
                }
            }
        }
        else
        {
            Debug.Log("技能库为空");
        }
    }
    
    #endregion
}
