using UnityEngine;

/// <summary>
/// 始终为真的触发器 - 用于标记型技能
/// 这种触发器总是返回true，主要用于不需要特定触发条件的技能
/// 例如：被动技能、状态标记技能等
/// </summary>
public class AlwaysTrueTrigger : ITrigger
{
    public string TriggerName => "AlwaysTrueTrigger";
    
    // ✅ 多角色系统：技能归属的角色ID（此触发器不使用，但需要实现接口）
    private string ownerCharacterID;
    
    /// <summary>
    /// ✅ 多角色系统：设置触发器归属的角色ID
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
        // AlwaysTrueTrigger 不使用角色过滤，始终返回true
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查事件 - 始终返回true
    /// </summary>
    /// <param name="args">技能参数（忽略）</param>
    /// <returns>始终返回true</returns>
    public bool CheckEvent(SkillArgs args)
    {
        // 始终返回true，用于标记型技能
        // 这种技能不需要特定的触发条件，只需要被激活即可
        return true;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 始终为真的触发器不需要重置逻辑
    }
}
