using UnityEngine;

/// <summary>
/// 击杀触发器 - 检测击杀事件
/// 监听 GameEventBus.OnDeath 事件，检测击杀事件是否发生
/// 支持目标标签过滤和击杀类型过滤
/// </summary>
public class KillTrigger : ITrigger
{
    public string TriggerName => "KillTrigger";
    
    private string targetTag = "Enemy"; // 默认目标标签
    
    /// <summary>
    /// 设置目标标签
    /// </summary>
    /// <param name="tag">目标标签</param>
    public void SetTargetTag(string tag)
    {
        targetTag = tag;
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查是否检测到击杀事件
    /// </summary>
    /// <param name="eventData">事件数据，期望是 DeathData</param>
    /// <returns>是否检测到击杀事件</returns>
    public bool CheckEvent(object eventData)
    {
        // 检查事件数据类型
        if (eventData is DeathData deathData)
        {
            // 检查目标标签是否匹配
            bool tagMatches = string.IsNullOrEmpty(targetTag) || deathData.DeadObjectTag == targetTag;
            
            if (tagMatches)
            {
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"[{TriggerName}] 收到非 DeathData 类型的事件: {eventData?.GetType()}");
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 击杀触发器不需要特殊重置逻辑
    }
}
