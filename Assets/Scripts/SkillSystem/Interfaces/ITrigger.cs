using UnityEngine;

/// <summary>
/// 触发器接口 - 技能系统的第一阶段最小验证
/// 职责：检测游戏中的具体事件发生
/// 只负责"检测到事件"这个动作，不判断事件是否满足技能触发条件
/// </summary>
public interface ITrigger
{
    /// <summary>
    /// 触发器名称
    /// </summary>
    string TriggerName { get; }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 检查是否检测到事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否检测到事件</returns>
    bool CheckEvent(object eventData);
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    void Reset();
}
