using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色实例数据 - 单个角色的运行时数据
/// 
/// 【核心职责】：
/// - 存储单个角色的运行时状态
/// - 关联角色配置和场景实例
/// - 管理角色专属技能列表
/// - 追踪角色存活状态
/// 
/// 【数据内容】：
/// - 角色唯一标识
/// - 角色配置引用
/// - 当前血量和存活状态
/// - 技能列表
/// - 场景球体实例引用
/// 
/// 【使用场景】：
/// - 跨场景保存角色状态
/// - 技能系统查询角色技能
/// - 战斗系统查询角色血量
/// - UI系统显示角色信息
/// </summary>
[System.Serializable]
public class CharacterInstance
{
    #region 基础信息
    
    /// <summary>
    /// 角色唯一ID（用于识别角色）
    /// 格式：character_1, character_2, character_3
    /// </summary>
    public string characterID;
    
    /// <summary>
    /// 角色位置编号（1号位、2号位、3号位）
    /// 用于UI显示和技能分配
    /// </summary>
    public int positionIndex;
    
    /// <summary>
    /// 角色配置数据引用
    /// 包含角色的所有静态配置（攻击力、技能池等）
    /// </summary>
    public PlayerData characterData;
    
    #endregion
    
    #region 运行时状态
    
    /// <summary>
    /// 当前血量
    /// 由战斗系统更新，UI系统读取显示
    /// </summary>
    public float currentHealth;
    
    /// <summary>
    /// 最大血量（可能被技能修改）
    /// </summary>
    public float maxHealth;
    
    /// <summary>
    /// 是否存活
    /// 用于快速判断角色是否可用
    /// </summary>
    public bool isAlive;
    
    #endregion
    
    #region 技能数据
    
    /// <summary>
    /// 该角色拥有的技能列表
    /// 技能归属到角色，独立管理
    /// </summary>
    public List<string> skillIDs = new List<string>();
    
    #endregion
    
    #region 场景引用（运行时）
    
    /// <summary>
    /// 场景中的球体实例引用（运行时）
    /// 注意：此字段不会序列化保存，每次进入关卡重新生成
    /// </summary>
    [System.NonSerialized]
    public GameObject ballInstance;
    
    #endregion
    
    #region 构造方法
    
    /// <summary>
    /// 构造函数 - 创建角色实例
    /// </summary>
    /// <param name="characterData">角色配置数据</param>
    /// <param name="positionIndex">位置编号（1/2/3）</param>
    public CharacterInstance(PlayerData characterData, int positionIndex)
    {
        this.characterData = characterData;
        this.positionIndex = positionIndex;
        
        // ✅ 修复：从 PlayerData.info.characterID 读取角色ID
        if (characterData != null && characterData.info != null && !string.IsNullOrEmpty(characterData.info.characterID))
        {
            this.characterID = characterData.info.characterID;
        }
        else
        {
            // 如果 PlayerData 未配置 characterID，使用位置编号作为后备
            this.characterID = $"character_{positionIndex}";
            Debug.LogWarning($"[CharacterInstance] PlayerData '{characterData?.info.name}' 未配置 characterID，使用后备方案: {this.characterID}");
        }
        
        // 初始化血量
        this.maxHealth = characterData.baseMaxHealth;
        this.currentHealth = maxHealth;
        this.isAlive = true;
        
        // 初始化技能列表
        this.skillIDs = new List<string>();
    }
    
    /// <summary>
    /// 无参构造函数（用于序列化）
    /// </summary>
    public CharacterInstance()
    {
        this.skillIDs = new List<string>();
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 添加技能到角色
    /// </summary>
    /// <param name="skillID">技能ID</param>
    public void AddSkill(string skillID)
    {
        if (!skillIDs.Contains(skillID))
        {
            skillIDs.Add(skillID);
        }
    }
    
    /// <summary>
    /// 移除技能
    /// </summary>
    /// <param name="skillID">技能ID</param>
    public void RemoveSkill(string skillID)
    {
        skillIDs.Remove(skillID);
    }
    
    /// <summary>
    /// 检查是否拥有指定技能
    /// </summary>
    /// <param name="skillID">技能ID</param>
    /// <returns>是否拥有该技能</returns>
    public bool HasSkill(string skillID)
    {
        return skillIDs.Contains(skillID);
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isAlive = false;
        }
    }
    
    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="healAmount">治疗量</param>
    public void Heal(float healAmount)
    {
        if (!isAlive) return;
        
        currentHealth += healAmount;
        
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    
    /// <summary>
    /// 重置角色状态（新关卡开始时）
    /// </summary>
    public void ResetForNewLevel()
    {
        // 恢复血量（可选：是否要在新关卡恢复血量？）
        // currentHealth = maxHealth;
        
        // 清除场景引用
        ballInstance = null;
    }
    
    /// <summary>
    /// 获取血量百分比
    /// </summary>
    /// <returns>血量百分比（0-1）</returns>
    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0f;
        return currentHealth / maxHealth;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试字符串</returns>
    public string GetDebugInfo()
    {
        return $"[{characterID}] {characterData?.info.name ?? "未知"} | " +
               $"血量: {currentHealth:F0}/{maxHealth:F0} | " +
               $"存活: {isAlive} | " +
               $"技能数: {skillIDs.Count} | " +
               $"位置: {positionIndex}号位";
    }
    
    #endregion
}

