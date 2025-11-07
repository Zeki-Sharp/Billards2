/// <summary>
/// 提供敌人“跳过本回合行动”能力的接口。
/// Phase 2 实现：状态脚本仅依赖该接口，不直接操控具体敌人逻辑。
/// </summary>
public interface IEnemyTurnSkipper
{
    /// <summary>
    /// 请求在本回合跳过一次行动。
    /// </summary>
    /// <param name="source">提出请求的对象（通常为状态组件）</param>
    /// <param name="reason">调试用途的原因说明</param>
    /// <returns>是否成功接受请求</returns>
    bool RequestSkipOnce(object source, string reason);

    /// <summary>
    /// 取消指定来源提出的跳过请求。
    /// </summary>
    /// <param name="source">提出请求的对象</param>
    void ClearSkipRequest(object source);
}


