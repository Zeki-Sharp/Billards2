using UnityEngine;

/// <summary>
/// 始终为真的触发器 - 用于标记型技能
/// 这种触发器总是返回true，主要用于不需要特定触发条件的技能
/// 例如：被动技能、状态标记技能等
/// </summary>
public class AlwaysTrueTrigger : ITrigger
{
    public string TriggerName => "AlwaysTrueTrigger";
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{TriggerName}] 初始化完成 - 始终返回true");
    }
    
    /// <summary>
    /// 检查事件 - 始终返回true
    /// </summary>
    /// <param name="eventData">事件数据（忽略）</param>
    /// <returns>始终返回true</returns>
    public bool CheckEvent(object eventData)
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
        Debug.Log($"[{TriggerName}] 重置触发器状态");
    }
}
