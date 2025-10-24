using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
#endif

/// <summary>
/// 技能配置 ScriptableObject - 用于在 Inspector 中配置技能
/// 支持可视化配置，替代硬编码的技能定义
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Game/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [BoxGroup("技能基本信息")]
    [LabelText("技能名称")]
    [Tooltip("技能名称")]
    public string skillName = "碰撞连击";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能描述")]
    [Tooltip("技能描述")]
    [TextArea(3, 5)]
    public string description = "碰撞敌人2次后，攻击力提升100%";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能标签")]
    [Tooltip("技能所属的标签，用于区分通用技能和角色专属技能")]
    [ValueDropdown("GetAvailableTags")]
    public string skillTag = "default";
    
    // 技能图标暂时移除，简化配置界面
    // [BoxGroup("技能基本信息")]
    // [LabelText("技能图标")]
    // [Tooltip("技能图标")]
    // public Sprite skillIcon;
    
    [BoxGroup("技能等级配置")]
    [LabelText("技能等级列表")]
    [Tooltip("技能的所有等级配置")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 5)]
    public List<SkillLevelConfig> skillLevels = new List<SkillLevelConfig>();
    
    /// <summary>
    /// 自动分配等级编号
    /// </summary>
    [Button("自动分配等级编号")]
    [BoxGroup("技能等级配置")]
    public void AutoAssignLevelNumbers()
    {
        for (int i = 0; i < skillLevels.Count; i++)
        {
            skillLevels[i].level = i + 1; // 从1开始编号
        }
        Debug.Log($"技能 {skillName} 已自动分配等级编号: [{string.Join(", ", skillLevels.Select(l => l.level))}]");
    }
    
    [BoxGroup("解锁条件")]
    [LabelText("前置技能列表")]
    [Tooltip("需要拥有哪些技能才能解锁此技能（手动输入技能名称）")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<string> requiredSkills = new List<string>();
    
    [BoxGroup("技能属性")]
    [LabelText("是否激活")]
    [Tooltip("是否激活（控制整个技能是否可用）")]
    public bool isActive = true;
    
    /// <summary>
    /// 创建技能实例（多等级版本）
    /// </summary>
    /// <param name="currentLevel">当前激活的等级，默认为最低可用等级</param>
    public virtual SkillInstance CreateSkillInstance(int currentLevel = -1)
    {
        if (!isActive)
        {
            Debug.LogWarning($"技能 {skillName} 未激活");
            return null;
        }
        
        // 自动分配等级编号（确保等级编号正确）
        AutoAssignLevelNumbers();
        
        // 如果未指定等级，使用最低可用等级
        if (currentLevel == -1)
        {
            var availableLevels = GetAvailableLevels();
            if (availableLevels.Count == 0)
            {
                Debug.LogError($"技能 {skillName} 没有可用的等级配置");
                return null;
            }
            currentLevel = availableLevels[0]; // 使用最低等级
        }
        
        // 查找指定等级
        var levelConfig = GetLevelConfig(currentLevel);
        if (levelConfig == null)
        {
            Debug.LogError($"技能 {skillName} 没有找到等级 {currentLevel} 的配置");
            return null;
        }
        
        // 创建等级实例
        var levelInstance = levelConfig.CreateLevelInstance(skillName);
        if (levelInstance == null)
        {
            Debug.LogError($"技能 {skillName} 等级 {currentLevel} 实例创建失败");
            return null;
        }
        
        // 创建技能实例（包装等级实例）
        var skillInstance = new SkillInstance(this, levelInstance, currentLevel);
        return skillInstance;
    }
    
    /// <summary>
    /// 获取指定等级的配置
    /// </summary>
    /// <param name="level">等级</param>
    /// <returns>等级配置，如果不存在返回null</returns>
    public SkillLevelConfig GetLevelConfig(int level)
    {
        if (skillLevels == null || skillLevels.Count == 0)
        {
            return null;
        }
        
        return skillLevels.FirstOrDefault(l => l.level == level && l.isActive);
    }
    
    /// <summary>
    /// 获取最高可用等级
    /// </summary>
    /// <returns>最高等级，如果没有可用等级返回0</returns>
    public int GetMaxLevel()
    {
        if (skillLevels == null || skillLevels.Count == 0)
        {
            return 0;
        }
        
        return skillLevels.Where(l => l.isActive).Max(l => l.level);
    }
    
    /// <summary>
    /// 获取所有可用等级
    /// </summary>
    /// <returns>可用等级列表</returns>
    public List<int> GetAvailableLevels()
    {
        if (skillLevels == null || skillLevels.Count == 0)
        {
            return new List<int>();
        }
        
        return skillLevels.Where(l => l.isActive).Select(l => l.level).OrderBy(l => l).ToList();
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public virtual bool IsValid()
    {
        // 检查基本信息
        if (string.IsNullOrEmpty(skillName))
        {
            return false;
        }
        
        // 检查是否有等级配置
        if (skillLevels == null || skillLevels.Count == 0)
        {
            return false;
        }
        
        // 检查至少有一个有效的等级
        bool hasValidLevel = skillLevels.Any(level => level != null && level.IsValid() && level.isActive);
        if (!hasValidLevel)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public virtual string GetDebugInfo()
    {
        string info = $"技能: {skillName}\n" +
                     $"- 激活: {isActive}\n" +
                     $"- 等级数量: {skillLevels?.Count ?? 0}\n" +
                     $"- 最高等级: {GetMaxLevel()}\n" +
                     $"- 可用等级: [{string.Join(", ", GetAvailableLevels())}]";
        
        if (skillLevels != null)
        {
            foreach (var level in skillLevels.Where(l => l != null && l.isActive))
            {
                info += $"\n\n等级 {level.level}:\n{level.GetDebugInfo()}";
            }
        }
        
        return info;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取所有可用的技能标签
    /// </summary>
    private IEnumerable<string> GetAvailableTags()
    {
        var tags = new List<string>();
        
        // 添加固定标签
        tags.Add("default");
        tags.Add("common");
        
        // 尝试从 Resources 加载角色选择数据
        var characterSelectionData = UnityEngine.Resources.Load<CharacterSelectionData>("Data/CharacterSelectionData");
        if (characterSelectionData != null && characterSelectionData.availableCharacters != null)
        {
            // 添加所有角色名称
            foreach (var character in characterSelectionData.availableCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.playerName))
                {
                    tags.Add(character.playerName);
                }
            }
        }
        else
        {
            // 如果无法加载角色选择数据，尝试从 Resources/Data/Player 目录加载所有 PlayerData
            var allPlayerData = UnityEngine.Resources.LoadAll<PlayerData>("Data/Player");
            if (allPlayerData != null && allPlayerData.Length > 0)
            {
                foreach (var playerData in allPlayerData)
                {
                    if (playerData != null && !string.IsNullOrEmpty(playerData.playerName))
                    {
                        tags.Add(playerData.playerName);
                    }
                }
            }
        }
        
        return tags.Distinct().OrderBy(t => t); // 去重并排序
    }
#endif
    
    /// <summary>
    /// 获取动态生成的技能描述（等级1）
    /// </summary>
    /// <returns>动态生成的描述文字</returns>
    public string GetDynamicDescription()
    {
        return GetDynamicDescription(1); // 默认使用等级1
    }
    
    /// <summary>
    /// 获取动态生成的技能描述（指定等级）
    /// </summary>
    /// <param name="targetLevel">目标等级</param>
    /// <returns>动态生成的描述文字</returns>
    public string GetDynamicDescription(int targetLevel)
    {
        // 使用新的技能描述生成器
        return SkillDescriptionGenerator.GenerateDescription(this, targetLevel);
    }
}


/// <summary>
/// 技能实例 - 包含配置和运行时组件（多等级版本）
/// </summary>
public class SkillInstance
{
    public SkillConfig config;
    public SkillLevelInstance currentLevelInstance;
    public int currentLevel;
    
    /// <summary>
    /// 技能实例唯一ID
    /// </summary>
    public string InstanceId { get; private set; }
    
    public SkillInstance(SkillConfig config, SkillLevelInstance levelInstance, int level)
    {
        this.config = config;
        this.currentLevelInstance = levelInstance;
        this.currentLevel = level;
        
        // 生成唯一实例ID
        this.InstanceId = $"{config.skillName}_Lv{level}_{System.Guid.NewGuid()}";
    }
    
    /// <summary>
    /// 升级到指定等级
    /// </summary>
    /// <param name="newLevel">新等级</param>
    /// <returns>是否升级成功</returns>
    public bool UpgradeToLevel(int newLevel)
    {
        if (newLevel <= currentLevel)
        {
            Debug.LogWarning($"技能 {config.skillName} 无法降级到等级 {newLevel}");
            return false;
        }
        
        var newLevelConfig = config.GetLevelConfig(newLevel);
        if (newLevelConfig == null)
        {
            Debug.LogError($"技能 {config.skillName} 没有找到等级 {newLevel} 的配置");
            return false;
        }
        
        // 重置当前等级实例
        currentLevelInstance?.Reset();
        
        // 创建新等级实例
        var newLevelInstance = newLevelConfig.CreateLevelInstance(config.skillName);
        if (newLevelInstance == null)
        {
            Debug.LogError($"技能 {config.skillName} 等级 {newLevel} 实例创建失败");
            return false;
        }
        
        // 更新等级信息
        currentLevelInstance = newLevelInstance;
        currentLevel = newLevel;
        
        // 更新实例ID
        InstanceId = $"{config.skillName}_Lv{newLevel}_{System.Guid.NewGuid()}";
        
        Debug.Log($"技能 {config.skillName} 升级到等级 {newLevel}");
        return true;
    }
    
    /// <summary>
    /// 获取当前等级的下一个等级
    /// </summary>
    /// <returns>下一个等级，如果没有返回-1</returns>
    public int GetNextLevel()
    {
        var availableLevels = config.GetAvailableLevels();
        int nextLevel = availableLevels.FirstOrDefault(l => l > currentLevel);
        
        return nextLevel > 0 ? nextLevel : -1;
    }
    
    /// <summary>
    /// 检查是否可以升级
    /// </summary>
    /// <returns>是否可以升级</returns>
    public bool CanUpgrade()
    {
        return GetNextLevel() > 0;
    }
    
    /// <summary>
    /// 重置技能状态
    /// </summary>
    public void Reset()
    {
        currentLevelInstance?.Reset();
    }
    
    /// <summary>
    /// 处理事件
    /// </summary>
    public bool ProcessEvent(object eventData)
    {
        if (currentLevelInstance == null)
        {
            Debug.LogError($"[SkillInstance] 技能 {config.skillName} 当前等级实例为空");
            return false;
        }
        
        return currentLevelInstance.ProcessEvent(eventData);
    }
    
    /// <summary>
    /// 处理技能执行完毕事件
    /// </summary>
    /// <param name="eventData">技能执行完毕事件数据</param>
    public void HandleSkillExecutedEvent(object eventData)
    {
        currentLevelInstance?.HandleSkillExecutedEvent(eventData);
    }
    
    /// <summary>
    /// 处理回合结束事件
    /// </summary>
    /// <param name="eventData">回合结束事件数据</param>
    public void HandlePhaseEndEvent(object eventData)
    {
        currentLevelInstance?.HandlePhaseEndEvent(eventData);
    }
}
