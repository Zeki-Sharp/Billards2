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
        // ✅ 3D适配：不再订阅 OnBallStopped，现在只通过 SkillManager 的 StoppedEvent 触发
    }
    
    /// <summary>
    /// 检查事件 - 检查是否球刚停止
    /// ✅ 3D适配：使用StoppedEvent（带轨迹数据）
    /// </summary>
    /// <param name="args">技能参数</param>
    /// <returns>是否触发</returns>
    public bool CheckEvent(SkillArgs args)
    {
        // ✅ 3D适配：检查 StoppedEvent（带3D轨迹数据）
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
    /// 清理事件订阅（由外部调用）
    /// ✅ 3D适配：不再需要清理 OnBallStopped 订阅
    /// </summary>
    public void Cleanup()
    {
        // ✅ 3D适配：不再订阅 OnBallStopped，无需清理
    }
}
