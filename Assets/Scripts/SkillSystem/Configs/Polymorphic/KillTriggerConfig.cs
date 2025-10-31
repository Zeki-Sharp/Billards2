using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

/// <summary>
/// 击杀触发器配置
/// </summary>
[System.Serializable]
public class KillTriggerConfig : TriggerBase
{
    /// <summary>
    /// 获取可用的 Tag 列表
    /// </summary>
    private static IEnumerable<ValueDropdownItem<string>> GetAvailableTags()
    {
        return new ValueDropdownList<string>
        {
            { "玩家 (Player)", "Player" },
            { "敌人 (Enemy)", "Enemy" },
            { "墙壁 (Wall)", "Wall" },
            { "洞 (Hole)", "Hole" },
            { "范围 (Range)", "Range" },
            { "陷阱 (Trap)", "Trap" },
            { "物品 (Item)", "Item" }
        };
    }

    [LabelText("击杀目标标签")]
    [Tooltip("检测击杀哪个标签的物体")]
    [ValueDropdown("GetAvailableTags")]
    public string killTargetTag = "Enemy";
    
    public override ITrigger CreateTrigger()
    {
        var trigger = new KillTrigger();
        trigger.SetTargetTag(killTargetTag);
        return trigger;
    }
}

