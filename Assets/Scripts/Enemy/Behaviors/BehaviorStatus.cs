/// <summary>
/// 行为执行状态枚举
/// 所有行为接口返回此状态，用于支持行为组合和决策
/// </summary>
public enum BehaviorStatus
{
    /// <summary>
    /// 准备就绪，等待执行
    /// </summary>
    Ready,
    
    /// <summary>
    /// 正在执行中（需要多帧完成）
    /// </summary>
    Running,
    
    /// <summary>
    /// 执行成功完成
    /// </summary>
    Success,
    
    /// <summary>
    /// 执行失败
    /// </summary>
    Failure
}

