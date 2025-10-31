using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 始终为真触发器配置
/// </summary>
[System.Serializable]
public class AlwaysTrueTriggerConfig : TriggerBase
{
    // 无需参数
    
    public override ITrigger CreateTrigger()
    {
        return new AlwaysTrueTrigger();
    }
}

