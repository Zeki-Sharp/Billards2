using UnityEngine;

/// <summary>
/// 球停止触发器配置
/// </summary>
[System.Serializable]
public class MovingEndTriggerConfig : TriggerBase
{
    // 无需参数
    
    public override ITrigger CreateTrigger()
    {
        return new MovingEndTrigger();
    }
}

