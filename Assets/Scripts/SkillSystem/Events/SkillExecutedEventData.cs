using UnityEngine;

/// <summary>
/// 技能执行完毕事件数据
/// 用于在技能执行完毕后发布事件，供重置条件响应
/// </summary>
public class SkillExecutedEventData
{
    /// <summary>
    /// 技能名称
    /// </summary>
    public string SkillName { get; set; }
    
    /// <summary>
    /// 技能实例ID（唯一标识）
    /// </summary>
    public string SkillInstanceId { get; set; }
    
    /// <summary>
    /// 原始事件数据（触发技能的事件）
    /// </summary>
    public object OriginalEventData { get; set; }
    
    /// <summary>
    /// 执行是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public float Timestamp { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public SkillExecutedEventData()
    {
        Timestamp = Time.time;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="skillName">技能名称</param>
    /// <param name="skillInstanceId">技能实例ID</param>
    /// <param name="originalEventData">原始事件数据</param>
    /// <param name="success">执行是否成功</param>
    public SkillExecutedEventData(string skillName, string skillInstanceId, object originalEventData, bool success)
    {
        SkillName = skillName;
        SkillInstanceId = skillInstanceId;
        OriginalEventData = originalEventData;
        Success = success;
        Timestamp = Time.time;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"SkillExecutedEventData - 技能: {SkillName}, 实例ID: {SkillInstanceId}, 成功: {Success}, 时间: {Timestamp:F2}";
    }
}
