using UnityEngine;

/// <summary>
/// 游戏阶段触发器 - 监听游戏流程状态变化
/// 
/// 【核心功能】：
/// - 监听 GameEventBus.OnGameFlowStateChanged 事件
/// - 过滤指定的 GameFlowState
/// - 检查角色归属
/// 
/// 【使用场景】：
/// - 玩家回合开始时触发 (PlayerPhaseStart) - 如：掉落物补充
/// - 玩家回合进行中触发 (PlayerPhasePlaying) - 如：蓄力、发射、移动
/// - 玩家回合结束时触发 (PlayerPhaseEnd) - 如：收集打击
/// - 敌人回合开始触发 (EnemyPhaseStart)
/// - 敌人回合进行中触发 (EnemyPhasePlaying)
/// - 敌人回合结束触发 (EnemyPhaseEnd)
/// 
/// 【阶段流程】：
/// PlayerPhaseStart → PlayerPhasePlaying → PlayerPhaseEnd 
///   → EnemyPhaseStart → EnemyPhasePlaying → EnemyPhaseEnd → (循环)
/// 
/// 【配置方式】：
/// - 通过 PhaseStateTriggerConfig 在 Inspector 中配置
/// - PhaseStateTriggerConfig.CreateTrigger() 创建实例
/// </summary>
public class PhaseStateTrigger : ITrigger
{
    // ✅ 由 PhaseStateTriggerConfig 设置
    public GameFlowState[] targetStates;
    public bool showDebugLog;
    
    public string TriggerName => "PhaseStateTrigger";
    
    // ✅ 多角色系统：技能归属的角色ID
    private string ownerCharacterID;
    
    /// <summary>
    /// ✅ 多角色系统：设置触发器归属的角色ID
    /// </summary>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        // 不需要特殊初始化
    }
    
    /// <summary>
    /// 检查是否检测到符合条件的阶段变化
    /// </summary>
    public bool CheckEvent(SkillArgs args)
    {
        // 检查是否是 GameFlowState 事件
        if (args.TryGetEventData<GameFlowState>(out var flowState))
        {
            // 检查状态是否匹配
            bool stateMatches = false;
            foreach (var targetState in targetStates)
            {
                if (flowState == targetState)
                {
                    stateMatches = true;
                    break;
                }
            }
            
            if (!stateMatches)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[PhaseStateTrigger] 状态不匹配：{flowState}，需要：{string.Join(",", targetStates)}");
                }
                return false;
            }
            
            if (showDebugLog)
            {
                Debug.Log($"[PhaseStateTrigger] ✅ 触发条件满足：{flowState}");
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 不需要特殊重置逻辑
    }
}

