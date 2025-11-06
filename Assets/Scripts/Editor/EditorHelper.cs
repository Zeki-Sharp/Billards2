#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 编辑器辅助工具类 - 统一管理 Odin Inspector 的下拉框选项
/// 
/// 【核心功能】：
/// - 提供所有角色的 characterID 下拉选项
/// - 提供所有角色的显示名称下拉选项
/// - 提供所有技能标签下拉选项
/// - 缓存结果，避免重复查询
/// 
/// 【使用方式】：
/// [ValueDropdown("EditorHelper.GetAllCharacterIDs")]
/// public string characterID;
/// 
/// [ValueDropdown("EditorHelper.GetAllCharacterNames")]
/// public string characterName;
/// </summary>
public static class EditorHelper
{
    #region 角色相关下拉框
    
    /// <summary>
    /// 获取所有角色的 characterID 列表（用于精确匹配）
    /// 格式：角色名称 (characterID) → characterID
    /// </summary>
    public static IEnumerable<ValueDropdownItem<string>> GetAllCharacterIDs()
    {
        var list = new ValueDropdownList<string>();
        
        var characters = GetAllPlayerData();
        
        if (characters.Count == 0)
        {
            list.Add("（未找到任何 PlayerData）", "");
            return list;
        }
        
        foreach (var playerData in characters)
        {
            if (!string.IsNullOrEmpty(playerData.info.characterID))
            {
                // 显示格式：角色名称 (characterID)
                // 实际值：characterID
                string displayName = $"{playerData.info.name} ({playerData.info.characterID})";
                list.Add(displayName, playerData.info.characterID);
            }
        }
        
        if (list.Count == 0)
        {
            list.Add("（所有 PlayerData 都未配置 characterID）", "");
        }
        
        return list;
    }
    
    /// <summary>
    /// 获取所有角色的显示名称列表（只返回角色名称，用于动态添加到其他列表）
    /// </summary>
    public static IEnumerable<string> GetAllCharacterNamesOnly()
    {
        var list = new List<string>();
        
        var characters = GetAllPlayerData();
        
        foreach (var playerData in characters)
        {
            if (!string.IsNullOrEmpty(playerData.info.name))
            {
                string characterName = playerData.info.name;
                if (!list.Contains(characterName))
                {
                    list.Add(characterName);
                }
            }
        }
        
        return list;
    }
    
    /// <summary>
    /// 获取所有角色的 characterID 列表（简单格式，只返回 ID）
    /// </summary>
    public static IEnumerable<string> GetAllCharacterIDsSimple()
    {
        var list = new List<string>();
        
        var characters = GetAllPlayerData();
        
        foreach (var playerData in characters)
        {
            if (!string.IsNullOrEmpty(playerData.info.characterID))
            {
                if (!list.Contains(playerData.info.characterID))
                {
                    list.Add(playerData.info.characterID);
                }
            }
        }
        
        return list;
    }
    
    /// <summary>
    /// 获取所有 PlayerData（内部辅助方法，避免重复代码）
    /// </summary>
    private static List<PlayerData> GetAllPlayerData()
    {
        var list = new List<PlayerData>();
        
        string[] guids = AssetDatabase.FindAssets("t:PlayerData");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PlayerData playerData = AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            
            if (playerData != null && playerData.info != null)
            {
                list.Add(playerData);
            }
        }
        
        return list;
    }
    
    #endregion
    
    #region 技能相关下拉框
    
    /// <summary>
    /// 获取所有可用的技能标签（使用 characterID）
    /// 
    /// 【组成】：
    /// 1. 固定标签（default, common）
    /// 2. 所有角色的 characterID（从 PlayerData 动态读取）
    /// 
    /// 【用途】：
    /// - 技能筛选
    /// - 技能分类
    /// - 角色专属技能标记
    /// 
    /// 【显示格式】：
    /// "角色名称 (characterID)" → 存储 "characterID"
    /// </summary>
    public static IEnumerable<ValueDropdownItem<string>> GetAllSkillTags()
    {
        var tags = new ValueDropdownList<string>();
        
        // 1. 添加固定标签
        tags.Add("通用 (default)", "default");
        tags.Add("公共 (common)", "common");
        
        // 2. 添加所有角色的 characterID
        var characters = GetAllPlayerData();
        foreach (var playerData in characters)
        {
            if (!string.IsNullOrEmpty(playerData.info.characterID))
            {
                // 显示格式：角色名称 (characterID)
                // 存储值：characterID
                string displayName = $"{playerData.info.name} ({playerData.info.characterID})";
                tags.Add(displayName, playerData.info.characterID);
            }
        }
        
        return tags;
    }
    
    /// <summary>
    /// 获取自定义技能标签（不包含角色标签，用于扩展）
    /// </summary>
    public static IEnumerable<string> GetCustomSkillTags()
    {
        return new List<string>
        {
            "default",
            "common",
            "rare",      // 可选：稀有标签
            "epic",      // 可选：史诗标签
            "legendary"  // 可选：传说标签
        };
    }
    
    #endregion
    
    #region 其他常用下拉框
    
    /// <summary>
    /// 获取所有可用的游戏阶段
    /// </summary>
    public static IEnumerable<ValueDropdownItem<GameFlowState>> GetAllGameFlowStates()
    {
        return new ValueDropdownList<GameFlowState>
        {
            { "玩家回合开始 (PlayerPhaseStart)", GameFlowState.PlayerPhaseStart },
            { "玩家回合中 (PlayerPhasePlaying)", GameFlowState.PlayerPhasePlaying },
            { "玩家回合结束 (PlayerPhaseEnd)", GameFlowState.PlayerPhaseEnd },
            { "敌人回合开始 (EnemyPhaseStart)", GameFlowState.EnemyPhaseStart },
            { "敌人回合中 (EnemyPhasePlaying)", GameFlowState.EnemyPhasePlaying },
            { "敌人回合结束 (EnemyPhaseEnd)", GameFlowState.EnemyPhaseEnd }
        };
    }
    
    /// <summary>
    /// 获取所有可用的伤害触发类型
    /// </summary>
    public static IEnumerable<ValueDropdownItem<DamageTriggerType>> GetAllDamageTriggerTypes()
    {
        return new ValueDropdownList<DamageTriggerType>
        {
            { "碰撞伤害 (Collision)", DamageTriggerType.Collision },
            { "停止攻击 (Stopped)", DamageTriggerType.Stopped },
            { "间隔伤害 (Interval)", DamageTriggerType.Interval },
            { "技能伤害 (Skill)", DamageTriggerType.Skill }
        };
    }
    
    /// <summary>
    /// 获取所有可用的目标标签
    /// </summary>
    public static IEnumerable<ValueDropdownItem<string>> GetAllTargetTags()
    {
        return new ValueDropdownList<string>
        {
            { "玩家 (Player)", "Player" },
            { "敌人 (Enemy)", "Enemy" },
            { "墙壁 (Wall)", "Wall" },
            { "洞 (Hole)", "Hole" },
            { "范围 (Range)", "Range" },
            { "陷阱 (Trap)", "Trap" },
            { "物品 (Item)", "Item" }
        };
    }
    
    #endregion
}
#endif

