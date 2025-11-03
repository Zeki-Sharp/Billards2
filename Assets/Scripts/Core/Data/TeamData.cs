using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 队伍数据容器 - 管理3个角色的队伍数据
/// 
/// 【核心职责】：
/// - 管理队伍的3个角色实例
/// - 追踪队伍整体状态
/// - 提供队伍相关的查询接口
/// - 支持跨场景数据保存
/// 
/// 【数据内容】：
/// - 3个角色实例列表
/// - 队伍状态统计
/// - 队伍级别的配置
/// 
/// 【使用场景】：
/// - GameSession 跨场景保存队伍数据
/// - LevelManager 生成队伍时初始化
/// - SkillSelectionManager 技能选择时查询角色
/// - UI 显示队伍状态
/// </summary>
[System.Serializable]
public class TeamData
{
    #region 常量定义
    
    /// <summary>
    /// 队伍固定角色数量
    /// </summary>
    public const int TEAM_SIZE = 3;
    
    #endregion
    
    #region 核心数据
    
    /// <summary>
    /// 角色列表（固定3个）
    /// 索引对应位置：0=1号位, 1=2号位, 2=3号位
    /// </summary>
    public List<CharacterInstance> characters = new List<CharacterInstance>(TEAM_SIZE);
    
    #endregion
    
    #region 队伍状态属性
    
    /// <summary>
    /// 存活角色数量
    /// </summary>
    public int AliveCount
    {
        get
        {
            return characters.Count(c => c != null && c.isAlive);
        }
    }
    
    /// <summary>
    /// 队伍是否全灭
    /// </summary>
    public bool IsTeamWiped
    {
        get
        {
            return AliveCount == 0;
        }
    }
    
    /// <summary>
    /// 队伍是否已满（3个角色）
    /// </summary>
    public bool IsTeamFull
    {
        get
        {
            return characters.Count >= TEAM_SIZE;
        }
    }
    
    /// <summary>
    /// 队伍平均血量百分比
    /// </summary>
    public float AverageHealthPercentage
    {
        get
        {
            if (characters.Count == 0) return 0f;
            
            float totalPercentage = 0f;
            foreach (var character in characters)
            {
                if (character != null)
                {
                    totalPercentage += character.GetHealthPercentage();
                }
            }
            
            return totalPercentage / characters.Count;
        }
    }
    
    #endregion
    
    #region 构造方法
    
    /// <summary>
    /// 无参构造函数
    /// </summary>
    public TeamData()
    {
        characters = new List<CharacterInstance>(TEAM_SIZE);
    }
    
    /// <summary>
    /// 从角色配置列表创建队伍
    /// </summary>
    /// <param name="selectedCharacters">选中的角色配置列表</param>
    public TeamData(List<PlayerData> selectedCharacters)
    {
        characters = new List<CharacterInstance>(TEAM_SIZE);
        
        for (int i = 0; i < selectedCharacters.Count && i < TEAM_SIZE; i++)
        {
            if (selectedCharacters[i] != null)
            {
                CharacterInstance character = new CharacterInstance(selectedCharacters[i], i + 1);
                characters.Add(character);
            }
        }
    }
    
    #endregion
    
    #region 角色管理方法
    
    /// <summary>
    /// 添加角色到队伍
    /// </summary>
    /// <param name="characterData">角色配置</param>
    /// <returns>是否成功添加</returns>
    public bool AddCharacter(PlayerData characterData)
    {
        if (IsTeamFull)
        {
            Debug.LogWarning("[TeamData] 队伍已满，无法添加角色");
            return false;
        }
        
        int positionIndex = characters.Count + 1;
        CharacterInstance character = new CharacterInstance(characterData, positionIndex);
        characters.Add(character);
        
        return true;
    }
    
    /// <summary>
    /// 根据角色ID获取角色实例
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>角色实例，未找到返回null</returns>
    public CharacterInstance GetCharacter(string characterID)
    {
        return characters.FirstOrDefault(c => c != null && c.characterID == characterID);
    }
    
    /// <summary>
    /// 根据位置索引获取角色实例（0-based）
    /// </summary>
    /// <param name="index">位置索引（0、1、2）</param>
    /// <returns>角色实例，未找到返回null</returns>
    public CharacterInstance GetCharacterByIndex(int index)
    {
        if (index < 0 || index >= characters.Count)
        {
            return null;
        }
        
        return characters[index];
    }
    
    /// <summary>
    /// 根据位置编号获取角色实例（1-based）
    /// </summary>
    /// <param name="positionIndex">位置编号（1、2、3）</param>
    /// <returns>角色实例，未找到返回null</returns>
    public CharacterInstance GetCharacterByPosition(int positionIndex)
    {
        return characters.FirstOrDefault(c => c != null && c.positionIndex == positionIndex);
    }
    
    /// <summary>
    /// 根据场景球体GameObject获取角色实例
    /// </summary>
    /// <param name="ballGameObject">球体GameObject</param>
    /// <returns>角色实例，未找到返回null</returns>
    public CharacterInstance GetCharacterByBall(GameObject ballGameObject)
    {
        if (ballGameObject == null) return null;
        
        return characters.FirstOrDefault(c => c != null && c.ballInstance == ballGameObject);
    }
    
    /// <summary>
    /// 获取所有存活的角色
    /// </summary>
    /// <returns>存活角色列表</returns>
    public List<CharacterInstance> GetAliveCharacters()
    {
        return characters.Where(c => c != null && c.isAlive).ToList();
    }
    
    /// <summary>
    /// 获取所有死亡的角色
    /// </summary>
    /// <returns>死亡角色列表</returns>
    public List<CharacterInstance> GetDeadCharacters()
    {
        return characters.Where(c => c != null && !c.isAlive).ToList();
    }
    
    #endregion
    
    #region 技能相关方法
    
    /// <summary>
    /// 为指定角色添加技能
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <param name="skillID">技能ID</param>
    /// <returns>是否成功添加</returns>
    public bool AddSkillToCharacter(string characterID, string skillID)
    {
        CharacterInstance character = GetCharacter(characterID);
        if (character == null)
        {
            Debug.LogWarning($"[TeamData] 未找到角色: {characterID}");
            return false;
        }
        
        character.AddSkill(skillID);
        return true;
    }
    
    /// <summary>
    /// 获取角色的所有技能
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>技能ID列表</returns>
    public List<string> GetCharacterSkills(string characterID)
    {
        CharacterInstance character = GetCharacter(characterID);
        if (character == null)
        {
            return new List<string>();
        }
        
        return new List<string>(character.skillIDs);
    }
    
    #endregion
    
    #region 状态管理方法
    
    /// <summary>
    /// 重置队伍状态（新游戏）
    /// </summary>
    public void Reset()
    {
        foreach (var character in characters)
        {
            if (character != null)
            {
                character.currentHealth = character.maxHealth;
                character.isAlive = true;
                character.skillIDs.Clear();
                character.ballInstance = null;
            }
        }
    }
    
    /// <summary>
    /// 重置场景引用（进入新关卡时）
    /// </summary>
    public void ResetSceneReferences()
    {
        foreach (var character in characters)
        {
            if (character != null)
            {
                character.ballInstance = null;
            }
        }
    }
    
    /// <summary>
    /// 清除所有数据
    /// </summary>
    public void Clear()
    {
        characters.Clear();
    }
    
    #endregion
    
    #region 验证方法
    
    /// <summary>
    /// 验证队伍数据是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        if (characters == null || characters.Count == 0)
        {
            return false;
        }
        
        // 检查是否有有效的角色数据
        foreach (var character in characters)
        {
            if (character != null && character.characterData != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region 调试方法
    
    /// <summary>
    /// 获取队伍调试信息
    /// </summary>
    /// <returns>调试字符串</returns>
    public string GetDebugInfo()
    {
        string info = $"[TeamData] 队伍状态:\n";
        info += $"  角色数量: {characters.Count}/{TEAM_SIZE}\n";
        info += $"  存活数量: {AliveCount}\n";
        info += $"  队伍全灭: {IsTeamWiped}\n";
        info += $"  平均血量: {AverageHealthPercentage:P0}\n";
        info += "  角色列表:\n";
        
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null)
            {
                info += $"    [{i + 1}] {characters[i].GetDebugInfo()}\n";
            }
            else
            {
                info += $"    [{i + 1}] <空>\n";
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 打印队伍调试信息到控制台
    /// </summary>
    public void PrintDebugInfo()
    {
        Debug.Log(GetDebugInfo());
    }
    
    #endregion
}

