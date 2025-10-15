using UnityEngine;

/// <summary>
/// 球停止触发器 - 监听玩家球停止移动事件
/// 用于触发"停球后技能"，如 Transition、范围攻击等
/// </summary>
public class MovingEndTrigger : ITrigger
{
    public string TriggerName => "MovingEndTrigger";
    
    private bool hasTriggered = false;
    private PlayerStateMachine playerStateMachine;
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        // 查找 PlayerStateMachine
        playerStateMachine = Object.FindFirstObjectByType<PlayerStateMachine>();
        if (playerStateMachine == null)
        {
            Debug.LogError("[MovingEndTrigger] 未找到 PlayerStateMachine！");
            return;
        }
        
        // 订阅球停止事件
        GameEventBus.OnBallStopped += OnBallStopped;
        
        Debug.Log($"[{TriggerName}] 初始化完成 - 监听球停止事件");
    }
    
    /// <summary>
    /// 检查事件 - 检查是否球刚停止
    /// </summary>
    /// <param name="eventData">事件数据（BallPhysics）</param>
    /// <returns>是否触发</returns>
    public bool CheckEvent(object eventData)
    {
        // 检查是否是球停止事件
        if (eventData is BallPhysics ballPhysics)
        {
            // 检查是否是玩家球
            if (ballPhysics.gameObject.CompareTag("Player"))
            {
                // 检查是否在 MovingEnd 状态
                if (playerStateMachine != null && 
                    playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.MovingEnd)
                {
                    if (!hasTriggered)
                    {
                        hasTriggered = true;
                        Debug.Log($"[{TriggerName}] 检测到玩家球停止，触发技能");
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        hasTriggered = false;
        Debug.Log($"[{TriggerName}] 重置触发器状态");
    }
    
    /// <summary>
    /// 球停止事件处理
    /// </summary>
    /// <param name="ballPhysics">停止的球</param>
    void OnBallStopped(BallPhysics ballPhysics)
    {
        // 这个方法主要用于初始化时的订阅，实际检查在 CheckEvent 中进行
        Debug.Log($"[{TriggerName}] 收到球停止事件 - 球: {ballPhysics.gameObject.name}");
    }
    
    /// <summary>
    /// 清理事件订阅（由外部调用）
    /// </summary>
    public void Cleanup()
    {
        GameEventBus.OnBallStopped -= OnBallStopped;
        Debug.Log($"[{TriggerName}] 清理事件订阅");
    }
}
