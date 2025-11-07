using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 状态行为配置基类：定义具体状态的运行时组件类型及初始化、堆叠逻辑
/// </summary>
[Serializable]
public abstract class TurnBasedStatusBehaviourConfig
{
    /// <summary>
    /// 对应的运行时组件类型
    /// </summary>
    public abstract Type ComponentType { get; }

    /// <summary>
    /// 初始化运行时组件
    /// </summary>
    public virtual void ApplyInitialValues(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        component.SetRemainingTurns(GetInitialDuration(data));
        component.SetDamagePerTurn(GetDamagePerTurn(data));
        component.SetCurrentStacks(0);
        ClampMaxStacks(GetMaxStacks(data), component);
    }

    /// <summary>
    /// 对同一状态再次施加时调用
    /// </summary>
    public virtual void OnStackApplied(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        component.AddRemainingTurns(GetInitialDuration(data));
        ClampMaxStacks(GetMaxStacks(data), component);
    }

    /// <summary>
    /// 每回合结算后调用，可在此修改栈数或持续时间
    /// </summary>
    public virtual void OnTurnResolved(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        // 默认无需处理
    }

    protected void ClampMaxStacks(int maxStacks, TurnBasedStatusComponent component)
    {
        if (maxStacks > 0 && component.RemainingTurns > maxStacks)
        {
            component.SetRemainingTurns(maxStacks);
        }
    }

    /// <summary>
    /// 获取初始持续回合数（默认读取旧字段）
    /// </summary>
    protected virtual int GetInitialDuration(TurnBasedStatusData data) => data.LegacyBaseDurationInTurns;

    /// <summary>
    /// 获取每回合伤害（默认读取旧字段）
    /// </summary>
    protected virtual float GetDamagePerTurn(TurnBasedStatusData data) => data.LegacyBaseDamagePerTurn;

    /// <summary>
    /// 获取最大堆叠层数（默认读取旧字段）
    /// </summary>
    protected virtual int GetMaxStacks(TurnBasedStatusData data) => data.LegacyMaxStacks;

    /// <summary>
    /// 供首次迁移时从旧字段复制数据
    /// </summary>
    public virtual void SyncLegacyValues(TurnBasedStatusData data)
    {
    }

    /// <summary>
    /// 调试描述
    /// </summary>
    public virtual string GetDebugDescription(TurnBasedStatusData data)
    {
        return $"{data.displayName} ({GetInitialDuration(data)}回合，{GetDamagePerTurn(data)}伤害/回合)";
    }
}

/// <summary>
/// 点燃状态的默认行为配置（保持现有逻辑）
/// </summary>
[Serializable]
public class BurningStatusBehaviourConfig : TurnBasedStatusBehaviourConfig
{
    public override Type ComponentType => typeof(BurningStatus);

    [BoxGroup("点燃参数")] [LabelText("基础持续回合数")] [MinValue(1)]
    public int baseDurationInTurns = 2;

    [BoxGroup("点燃参数")] [LabelText("每回合伤害")] [MinValue(0f)]
    public float damagePerTurn = 5f;

    [BoxGroup("点燃参数")] [LabelText("最大总回合数")] [Tooltip("0表示不限制")] [MinValue(0)]
    public int maxTotalTurns = 0;

    protected override int GetInitialDuration(TurnBasedStatusData data) => baseDurationInTurns;
    protected override float GetDamagePerTurn(TurnBasedStatusData data) => damagePerTurn;
    protected override int GetMaxStacks(TurnBasedStatusData data) => maxTotalTurns;

    public override void SyncLegacyValues(TurnBasedStatusData data)
    {
        baseDurationInTurns = data.LegacyBaseDurationInTurns;
        damagePerTurn = data.LegacyBaseDamagePerTurn;
        maxTotalTurns = data.LegacyMaxStacks;
    }

    public override string GetDebugDescription(TurnBasedStatusData data)
    {
        return $"{data.displayName} ({baseDurationInTurns}回合，{damagePerTurn}伤害/回合)";
    }
}

/// <summary>
/// 中毒状态行为配置：按叠层输出伤害，每回合衰减层数
/// </summary>
[Serializable]
public class PoisonStatusBehaviourConfig : TurnBasedStatusBehaviourConfig
{
    public override Type ComponentType => typeof(PoisonStatus);

    [BoxGroup("中毒参数")] [LabelText("初始层数")] [MinValue(1)]
    public int initialStacks = 1;

    [BoxGroup("中毒参数")] [LabelText("每次施加增加层数")] [MinValue(0)]
    public int stacksPerApply = 1;

    [BoxGroup("中毒参数")] [LabelText("每回合衰减层数")] [MinValue(0)]
    public int decayPerTurn = 1;

    [BoxGroup("中毒参数")] [LabelText("最大层数")] [Tooltip("0表示无限制")] [MinValue(0)]
    public int maxStacks = 0;

    public override void ApplyInitialValues(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        if (!(component is PoisonStatus poisonComponent))
        {
            Debug.LogError($"[PoisonStatusBehaviourConfig] 目标上缺少 PoisonStatus 组件，实际类型：{component?.GetType().Name}");
            return;
        }

        poisonComponent.Configure(decayPerTurn);

        component.SetCurrentStacks(Mathf.Max(0, initialStacks));
        ClampStacks(component);
        SyncRuntimeState(component, compensateForAutoDecrement: false);
    }

    public override void OnStackApplied(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        if (!(component is PoisonStatus poisonComponent))
        {
            return;
        }

        poisonComponent.Configure(decayPerTurn);

        if (stacksPerApply > 0)
        {
            component.AddStacks(stacksPerApply);
        }

        ClampStacks(component);
        SyncRuntimeState(component, compensateForAutoDecrement: false);
    }

    public override void OnTurnResolved(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        if (!(component is PoisonStatus poisonComponent))
        {
            return;
        }

        poisonComponent.Configure(decayPerTurn);

        if (decayPerTurn > 0)
        {
            component.AddStacks(-decayPerTurn);
        }

        ClampStacks(component);
        SyncRuntimeState(component, compensateForAutoDecrement: true);
    }

    public override void SyncLegacyValues(TurnBasedStatusData data)
    {
        initialStacks = Mathf.Max(1, data.LegacyBaseDurationInTurns);
        maxStacks = Mathf.Max(0, data.LegacyMaxStacks);
        stacksPerApply = Mathf.Max(1, initialStacks);
        decayPerTurn = Mathf.Clamp(initialStacks, 0, initialStacks);
    }

    public override string GetDebugDescription(TurnBasedStatusData data)
    {
        return $"{data.displayName} (初始{initialStacks}层，衰减{decayPerTurn}/回合)";
    }

    void ClampStacks(TurnBasedStatusComponent component)
    {
        if (maxStacks > 0 && component.CurrentStacks > maxStacks)
        {
            component.SetCurrentStacks(maxStacks);
        }
    }

    void SyncRuntimeState(TurnBasedStatusComponent component, bool compensateForAutoDecrement)
    {
        int stacks = component.CurrentStacks;

        component.SetDamagePerTurn(Mathf.Max(0, stacks));

        if (compensateForAutoDecrement)
        {
            if (stacks > 0)
            {
                component.SetRemainingTurns(stacks + 1);
            }
            else
            {
                component.SetRemainingTurns(1);
            }
        }
        else
        {
            component.SetRemainingTurns(Mathf.Max(stacks, 1));
        }
    }
}


/// <summary>
/// 冰冻状态行为配置：控制敌人跳过行动
/// </summary>
[Serializable]
public class FreezingStatusBehaviourConfig : TurnBasedStatusBehaviourConfig
{
    public override Type ComponentType => typeof(FreezingStatus);

    public enum FreezingStackMode
    {
        Refresh,
        Extend,
        Ignore
    }

    [BoxGroup("冰冻参数"), LabelText("持续回合数"), MinValue(1)]
    public int durationInTurns = 1;

    [BoxGroup("冰冻参数"), LabelText("叠加策略"), Tooltip("Refresh：刷新剩余回合；Extend：在当前剩余回合基础上增加；Ignore：忽略新的施加请求")]
    public FreezingStackMode stackMode = FreezingStackMode.Refresh;

    public override void ApplyInitialValues(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        component.SetRemainingTurns(durationInTurns);
        component.SetDamagePerTurn(0f);
        component.SetCurrentStacks(0);
    }

    public override void OnStackApplied(TurnBasedStatusData data, TurnBasedStatusComponent component)
    {
        switch (stackMode)
        {
            case FreezingStackMode.Refresh:
                component.SetRemainingTurns(durationInTurns);
                break;
            case FreezingStackMode.Extend:
                component.AddRemainingTurns(durationInTurns);
                break;
            case FreezingStackMode.Ignore:
                // 不做处理
                break;
        }
    }

    public override string GetDebugDescription(TurnBasedStatusData data)
    {
        return $"{data.displayName} ({durationInTurns}回合，策略:{stackMode})";
    }
}


