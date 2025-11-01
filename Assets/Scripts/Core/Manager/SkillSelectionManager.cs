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
/// 
/// 【执行顺序】：LEVEL 层 (-30)
/// 【依赖】：SYSTEM 层 (SkillManager)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.LEVEL)]
public class SkillSelectionManager : SingletonManager<SkillSelectionManager>
{
    
    [Header("技能库配置")]
    [Tooltip("技能数据库 - 包含所有可用技能的配置")]
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private int skillSelectionCount = 3; // 每次选择的技能数量
    
    // 可用技能列表（运行时填充）
    private List<SkillConfig> allAvailableSkills = new List<SkillConfig>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private SkillManager skillManager;
    private LevelManager levelManager;
    
    // 状态管理
    private bool isSkillSelectionActive = false;
    private List<SkillConfig> currentSelection = new List<SkillConfig>();
    private List<SkillSelectionOption> currentSelectionOptions = new List<SkillSelectionOption>(); // 保存选项（包含等级信息）
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        GameEventBus.OnGameRestart += ResetState;
        GameEventBus.OnLevelCompleted += OnLevelCompleted;
        LoadSkillsFromDatabase();
    }
    
    protected override void OnManagerDestroyed()
    {
        GameEventBus.OnGameRestart -= ResetState;
        GameEventBus.OnLevelCompleted -= OnLevelCompleted;
    }
    
    #endregion
    
    void Start()
    {
        skillManager = SkillManager.Instance;
        levelManager = LevelManager.Instance;
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionManager: 初始化完成");
            Debug.Log($"SkillSelectionManager: 技能库包含 {allAvailableSkills.Count} 个技能");
        }
    }
    
    /// <summary>
    /// 从技能数据库加载技能配置
    /// </summary>
    void LoadSkillsFromDatabase()
    {
        allAvailableSkills.Clear();
        
        // 检查数据库引用
        if (skillDatabase == null)
        {
            Debug.LogError("SkillSelectionManager: 技能数据库未配置！请在Inspector中分配SkillDatabase资源。");
            return;
        }
        
        // 获取当前角色名称
        string currentCharacterName = GetCurrentCharacterName();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 当前角色名称 = '{currentCharacterName}'");
        }
        
        // 从数据库获取适合当前角色的技能
        List<SkillConfig> availableSkills = skillDatabase.GetSkillsForCharacter(currentCharacterName);
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 从数据库找到 {availableSkills.Count} 个适合角色 '{currentCharacterName}' 的技能");
        }
        
        // 添加到可用技能列表
        allAvailableSkills.AddRange(availableSkills);
        
        if (allAvailableSkills.Count == 0)
        {
            Debug.LogWarning($"SkillSelectionManager: 没有找到适合角色 '{currentCharacterName}' 的技能！");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 最终加载 {allAvailableSkills.Count} 个有效技能配置");
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
        
        if (currentCharacter != null && !string.IsNullOrEmpty(currentCharacter.info.name))
        {
            return currentCharacter.info.name;
        }
        
        if (showDebugInfo)
        {
            Debug.LogWarning("SkillSelectionManager: 无法获取当前角色名称，将只加载 common 和 default 标签的技能");
        }
        
        return "";
    }
    
    
    /// <summary>
    /// 关卡完成事件处理
    /// </summary>
    /// <param name="levelIndex">关卡索引</param>
    /// <param name="levelConfig">关卡配置</param>
    void OnLevelCompleted(int levelIndex, LevelConfig levelConfig)
    {
        // 检查是否是最后一关
        if (levelManager != null && !levelManager.HasNextLevel())
        {
            if (showDebugInfo)
            {
                Debug.Log($"SkillSelectionManager: 关卡 {levelIndex + 1} 是最后一关，跳过技能选择，直接触发游戏完成");
            }
            // 最后一关不进行技能选择，直接触发游戏完成流程
            // 通过调用 LevelManager.LoadNextLevel，它会检测到没有下一关并触发 GameCompleted
            levelManager.LoadNextLevel();
            return;
        }
        
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
        
        // 发布技能选择开始事件（GameManager 会监听此事件并暂停游戏）
        GameEventBus.PublishSkillSelectionStarted(currentSelection);
    }
    
    /// <summary>
    /// 生成随机技能选择（支持技能升级）
    /// </summary>
    void GenerateRandomSkillSelection()
    {
        currentSelection.Clear();
        currentSelectionOptions.Clear(); // 清空选项列表
        
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
            currentSelectionOptions.Add(availableOptions[i]); // 保存选项（包含等级信息）
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
        
        // 发布技能选择完成事件（GameManager 会监听此事件并恢复游戏）
        GameEventBus.PublishSkillSelectionCompleted();
        
        // 通知关卡管理器进入下一关卡
        ProceedToNextLevel();
    }
    
    /// <summary>
    /// 进入下一关卡
    /// </summary>
    void ProceedToNextLevel()
    {
        // 技能选择完成，返回地图场景
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionManager: 技能选择完成，返回地图场景");
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }
    
    /// <summary>
    /// 重置技能选择管理器状态（游戏重启时调用）
    /// </summary>
    public void ResetState()
    {
        // 复用现有的清空逻辑
        currentSelection.Clear();
        currentSelectionOptions.Clear();
        
        // 重置状态标志
        isSkillSelectionActive = false;
        
        if (showDebugInfo)
        {
            Debug.Log("SkillSelectionManager: 重置完成 - 选择状态已清空");
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
    /// 根据索引获取技能选项（包含目标等级信息）
    /// </summary>
    /// <param name="index">技能索引（0-2）</param>
    /// <returns>技能选项，如果索引无效返回 null</returns>
    public SkillSelectionOption GetSkillOption(int index)
    {
        if (index >= 0 && index < currentSelectionOptions.Count)
        {
            return currentSelectionOptions[index];
        }
        return null;
    }
    
    /// <summary>
    /// 根据技能名获取技能选项（包含目标等级信息）
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <returns>技能选项，如果未找到返回 null</returns>
    public SkillSelectionOption GetSkillOptionByName(string skillName)
    {
        return currentSelectionOptions.FirstOrDefault(option => 
            option.skillConfig != null && option.skillConfig.skillName == skillName);
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
        LoadSkillsFromDatabase();
        
        if (showDebugInfo)
        {
            Debug.Log($"SkillSelectionManager: 手动刷新技能库完成，加载 {allAvailableSkills.Count} 个技能");
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
                  $"技能数据库: {(skillDatabase != null ? skillDatabase.name : "未配置")}\n" +
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
