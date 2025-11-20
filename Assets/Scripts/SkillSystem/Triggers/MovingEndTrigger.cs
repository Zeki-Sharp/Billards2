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
    
    // ✅ 多角色系统：技能归属的角色ID
    private string ownerCharacterID;
    
    /// <summary>
    /// ✅ 多角色系统：设置触发器归属的角色ID
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        // ⚠️ 多角色系统：不再使用全局查找，改为事件驱动
        // playerStateMachine 不再需要（改用角色ID过滤）
        
        // 订阅球停止事件
        GameEventBus.OnBallStopped += OnBallStopped;
    }
    
    /// <summary>
    /// 检查事件 - 检查是否球刚停止
    /// ✅ 3D适配：优先使用StoppedEvent（带轨迹数据），后备使用BallPhysics
    /// </summary>
    /// <param name="args">技能参数</param>
    /// <returns>是否触发</returns>
    public bool CheckEvent(SkillArgs args)
    {
        // ✅ 3D适配：优先检查 StoppedEvent（带3D轨迹数据）
        if (args.TryGetEventData<StoppedEvent>(out var stoppedEvent))
        {
            // 检查是否是玩家球
            if (stoppedEvent.Source == null || !stoppedEvent.Source.CompareTag("Player"))
            {
                return false;
            }
            
            // ✅ 多角色系统：检查是否是归属角色的球停止事件
            if (!TriggerHelper.CheckEventSource(stoppedEvent, ownerCharacterID))
            {
                return false;  // 不是归属角色，不触发
            }
            
            // 检查是否已触发过（每次停止只触发一次）
            if (!hasTriggered)
            {
                hasTriggered = true;
                return true;
            }
        }
        // 后备：检查 BallPhysics（向后兼容）
        else if (args.TryGetEventData<BallPhysics>(out var ballPhysics))
        {
            // 检查是否是玩家球
            if (!ballPhysics.gameObject.CompareTag("Player"))
            {
                return false;
            }
            
            // ✅ 多角色系统：检查是否是归属角色的球停止事件
            if (!TriggerHelper.CheckEventSource(ballPhysics, ownerCharacterID))
            {
                return false;  // 不是归属角色，不触发
            }
            
            // 检查是否已触发过（每次停止只触发一次）
            if (!hasTriggered)
            {
                hasTriggered = true;
                return true;
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
    }
    
    /// <summary>
    /// 球停止事件处理
    /// </summary>
    /// <param name="ballPhysics">停止的球</param>
    void OnBallStopped(BallPhysics ballPhysics)
    {
        // 这个方法主要用于初始化时的订阅，实际检查在 CheckEvent 中进行
    }
    
    /// <summary>
    /// 清理事件订阅（由外部调用）
    /// </summary>
    public void Cleanup()
    {
        GameEventBus.OnBallStopped -= OnBallStopped;
    }
}
