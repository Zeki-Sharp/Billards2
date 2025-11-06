using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 游戏阶段触发器配置 - 在特定游戏阶段触发技能
/// 
/// 【使用场景】：
/// - 玩家回合开始时触发（PlayerPhaseStart）- 如：掉落物补充
/// - 玩家回合进行中触发（PlayerPhasePlaying）
/// - 玩家回合结束时触发（PlayerPhaseEnd）- 如：收集打击
/// - 敌人回合开始时触发（EnemyPhaseStart）
/// - 敌人回合进行中触发（EnemyPhasePlaying）
/// - 敌人回合结束时触发（EnemyPhaseEnd）
/// 
/// 【配置示例】：
/// - targetStates = [PlayerPhaseStart] → 玩家回合开始时触发
/// - targetStates = [PlayerPhaseEnd] → 玩家回合结束时触发
/// - targetStates = [PlayerPhaseStart, PlayerPhaseEnd] → 支持多选
/// </summary>
[System.Serializable]
public class PhaseStateTriggerConfig : TriggerBase
{
    /// <summary>
    /// 获取可用的游戏状态列表
    /// </summary>
    private static IEnumerable<ValueDropdownItem<GameFlowState>> GetAvailableStates()
    {
        return new ValueDropdownList<GameFlowState>
        {
            { "玩家回合开始 (PlayerPhaseStart)", GameFlowState.PlayerPhaseStart },
            { "玩家回合中 (PlayerPhasePlaying)", GameFlowState.PlayerPhasePlaying },
            { "玩家回合结束 (PlayerPhaseEnd)", GameFlowState.PlayerPhaseEnd },
            { "敌人回合开始 (EnemyPhaseStart)", GameFlowState.EnemyPhaseStart },
            { "敌人回合中 (EnemyPhasePlaying)", GameFlowState.EnemyPhasePlaying },
            { "敌人回合结束 (EnemyPhaseEnd)", GameFlowState.EnemyPhaseEnd }
        };
    }
    
    [LabelText("触发的游戏阶段")]
    [Tooltip("选择哪些游戏阶段会触发技能（可多选）")]
    [ValueDropdown("GetAvailableStates")]
    [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false)]
    public GameFlowState[] targetStates = new GameFlowState[] { GameFlowState.PlayerPhaseStart };
    
    [LabelText("显示调试日志")]
    [Tooltip("是否在Console中显示触发日志")]
    public bool showDebugLog = false;
    
    /// <summary>
    /// 创建触发器实例
    /// </summary>
    public override ITrigger CreateTrigger()
    {
        var trigger = new PhaseStateTrigger
        {
            targetStates = this.targetStates,
            showDebugLog = this.showDebugLog
        };
        return trigger;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        string states = string.Join(", ", targetStates);
        return $"PhaseStateTrigger [阶段: {states}]";
    }
}

