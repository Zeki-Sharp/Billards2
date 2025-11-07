using UnityEngine;

/// <summary>
/// 冰冻状态：阻止敌人在冻结期间执行回合行动
/// </summary>
public class FreezingStatus : TurnBasedStatusComponent
{
    private IEnemyTurnSkipper turnSkipper;

    protected override void OnStatusApplied()
    {
        RequestSkip("StatusApplied");
    }

    protected override void OnTurnTrigger()
    {
        RequestSkip("TurnTrigger");

        if (showDebugLog)
        {
            Debug.Log($"[冰冻] ❄️ {gameObject.name} 本回合被冻结，剩余 {RemainingTurns} 回合");
        }
    }

    protected override void OnStatusRemoved()
    {
        if (turnSkipper != null)
        {
            turnSkipper.ClearSkipRequest(this);
        }
    }

    private void RequestSkip(string reason)
    {
        if (turnSkipper == null)
        {
            turnSkipper = GetComponentInParent<IEnemyTurnSkipper>();
        }

        if (turnSkipper != null)
        {
            turnSkipper.RequestSkipOnce(this, reason);
        }
        else if (showDebugLog)
        {
            Debug.LogWarning($"[冰冻] {gameObject.name} 未找到 IEnemyTurnSkipper，跳过请求未执行");
        }
    }
}



